# Phase 0 Research: Meta Model API Integration, Tool Calling Specification & Submission Integrity

**Feature**: `006-meta-models-and-tool-calling`  
**Date**: 2026-10-30  
**Status**: Completed  

---

## 1. Meta Hackathon Model Provider & Endpoint Configuration

### Problem Statement
Previous configurations defaulted to Groq endpoints (`https://api.groq.com/openai/v1`) with Groq-specific model strings (`llama-3.3-70b-versatile`). Hackathon Stage 1 rules mandate Meta models via Meta Model API, Muse Spark Open, Glimmer, or Baseten-hosted Meta models. Vendor lock-in to Groq risks immediate disqualification.

### Decision & Architecture
- Update [`backend/Agent/Models/MetaLlamaConfig.cs`](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/backend/Agent/Models/MetaLlamaConfig.cs) and [`backend/appsettings.json`](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/backend/appsettings.json) to support Meta Hackathon endpoints:
  - `BaseUrl`: Defaults to Meta Model API / Baseten endpoint structure (`https://bridge.baseten.co/v1` or `https://model-api.meta.com/v1`).
  - Model identifiers:
    - Primary Agent Planner: `meta-llama/Llama-3.3-70B-Instruct`
    - Fast Summaries & Classifiers: `meta-llama/Llama-3.1-8B-Instruct`
    - Vision Multi-modal: `meta-llama/Llama-3.2-11B-Vision-Instruct`
    - Safe Share Guard: `meta-llama/Llama-Guard-3-8B`
  - Environment variable resolution order:
    1. `META_MODEL_API_KEY` or `BASETEN_API_KEY` (primary)
    2. `LLAMA_API_KEY` (fallback)
    3. `META_MODEL_BASE_URL` (override for custom endpoint bridge)
    4. `META_MODEL_NAME` (override for model name)

---

## 2. Real Autonomous Tool Calling Specification

### Problem Statement
In `MetaLlamaClient.GenerateToolPlanAsync`, requests sent to `/chat/completions` contained only `messages` with a system prompt and user goal. The request body completely omitted the `tools` array. As a consequence, OpenAI/Meta compliant endpoints returned standard textual completions without `tool_calls`. The client checked `choice?["tool_calls"]`, found nothing, and unconditionally dropped into the local keyword heuristic planner (`GenerateLocalFallbackPlan`). Remote models never actually executed tool calling.

### Decision & Schema
Inject the complete standard `tools` schema array into the completions request body:

```json
{
  "model": "meta-llama/Llama-3.3-70B-Instruct",
  "messages": [
    { "role": "system", "content": "You are the LinkVault Verifiable Workspace Agent..." },
    { "role": "user", "content": "<user_goal>" }
  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "create_directory",
        "description": "Create a new directory in the workspace",
        "parameters": {
          "type": "object",
          "properties": {
            "name": { "type": "string", "description": "Directory name" },
            "parentDirectoryId": { "type": "string", "description": "Optional parent directory GUID" }
          },
          "required": ["name"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "move_resource",
        "description": "Move a file or resource into a target directory",
        "parameters": {
          "type": "object",
          "properties": {
            "resourceId": { "type": "string", "description": "Resource GUID to move" },
            "targetDirectoryId": { "type": "string", "description": "Target directory GUID (null for root)" }
          },
          "required": ["resourceId"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "create_share_link",
        "description": "Generate a secure, time-bound share link for a resource",
        "parameters": {
          "type": "object",
          "properties": {
            "resourceId": { "type": "string", "description": "Resource GUID to share" },
            "expiryDays": { "type": "integer", "description": "Days until expiration (1-365)", "default": 7 },
            "password": { "type": "string", "description": "Optional access password" },
            "maxDownloads": { "type": "integer", "description": "Optional download limit" }
          },
          "required": ["resourceId"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "search",
        "description": "Search workspace resources by query keyword",
        "parameters": {
          "type": "object",
          "properties": {
            "query": { "type": "string", "description": "Search term or file filter" }
          },
          "required": ["query"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "list_directory",
        "description": "List files and subdirectories in a directory",
        "parameters": {
          "type": "object",
          "properties": {
            "directoryId": { "type": "string", "description": "Directory GUID (null for workspace root)" }
          }
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "delete_resource",
        "description": "Move a resource to trash / mark as deleted",
        "parameters": {
          "type": "object",
          "properties": {
            "resourceId": { "type": "string", "description": "Resource GUID to delete" }
          },
          "required": ["resourceId"]
        }
      }
    }
  ],
  "tool_choice": "auto",
  "temperature": 0.1
}
```

### Result
When the Meta model responds with `tool_calls`, `MetaLlamaClient` extracts the tool name and argument JSON, validates each step against the schema, and saves the genuine model-synthesized plan. The local heuristic planner is preserved exclusively as an offline/error fallback.

---

## 3. Voice Transcription Integrity & Elimination of Fake Mock

### Problem Statement
`VaultAgentService.TranscribeAudioAsync` hardcoded the response:
`"Organize my uploads and share the financial report for 7 days"` at 98% confidence.
This constitutes a deceptive mock. The frontend `VoiceCommandButton.tsx` already supports the browser's native `SpeechRecognition` API for real speech dictation, but had a fallback that posted audio blobs to `/api/agent/transcribe`, showing the fake result whenever `SpeechRecognition` failed or was unconfigured.

### Decision
1. **Remove Fake Backend**:
   - Delete `POST /api/agent/transcribe` from [`AgentController.cs`](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/backend/Agent/Controllers/AgentController.cs).
   - Delete `TranscribeAudioAsync` from [`IVaultAgentService.cs`](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/backend/Agent/Services/IVaultAgentService.cs) and [`VaultAgentService.cs`](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/backend/Agent/Services/VaultAgentService.cs).
   - Delete `TranscribeResponse` from DTOs.
2. **Native Browser Voice Recognition**:
   - `VoiceCommandButton.tsx` relies 100% on the Web Speech API (`window.SpeechRecognition || window.webkitSpeechRecognition`).
   - If the browser does not support the Web Speech API or microphone permission is denied, display a clear, honest toast message: *"Speech recognition is not supported in this browser. Please type your directive into the chat input."*
   - Never upload audio to a fake mock backend.

---

## 4. HACKATHON.md Factual Audit & Truth Verification

### Problem Statement
`HACKATHON.md` contained inaccurate or misleading claims:
1. It labeled commit `d69b3ed` (dated Oct 28) as "before hackathon development".
2. It claimed "all commits are untampered" despite a previous force-push during credential scrubbing.
3. It claimed autonomous tool calling was active when the request body omitted the `tools` array.
4. It named an inaccurate file ticket route (`/api/files/ticket/{id}/stream` instead of `/api/files/{id}/ticket`).

### Factual Corrections
1. **Baseline**: Describe the baseline state honestly: The foundational LinkVault web application as of Oct 26 (prior to the event) included authentication, directories, file storage, and RBAC. Security hardening (download tickets, path traversal fixes, proxy headers, credential purge) was performed Oct 27–29 inside the hackathon window. The Verifiable Workspace Agent was developed Oct 29–30.
2. **Git History**: Remove the "all commits are untampered" line. Honestly document that git history was cleaned during credential purging and restructuring, and provide the transparent commit comparison from the baseline tag.
3. **Tool Calling**: Accurately explain the tool-calling loop: Meta Llama 3.3 70B Instruct called with formal tool schemas, producing structured tool calls validated by per-tool schema validators, with an offline local heuristic planner as a resilience fallback.
4. **API Routes**: Correct the download ticket issuance route to `POST /api/files/{id}/ticket` and streaming route to `GET /api/files/{id}/download?ticket={token}`.
5. **Models Table**: Explicitly list Meta Model API / Baseten endpoints and exact Meta model identifiers.

