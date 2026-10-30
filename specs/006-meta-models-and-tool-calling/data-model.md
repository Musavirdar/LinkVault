# Phase 1 Data Model: Meta Model Integration & Configuration

**Feature**: `006-meta-models-and-tool-calling`  
**Date**: 2026-10-30  
**Status**: Completed  

---

## 1. Meta Model Configuration Entity

### `MetaLlamaConfig`
Located in `backend/Agent/Models/MetaLlamaConfig.cs` and bound from configuration section `"MetaLlama"` or environment variables.

| Field | Type | Default Value | Description |
|---|---|---|---|
| `BaseUrl` | `string` | `https://bridge.baseten.co/v1` | Base URL for OpenAI/Meta-compatible `/chat/completions` endpoint |
| `ApiKey` | `string` | `""` | Resolved from `META_MODEL_API_KEY`, `BASETEN_API_KEY`, or `LLAMA_API_KEY` |
| `Llama33Model` | `string` | `meta-llama/Llama-3.3-70B-Instruct` | Model identifier for autonomous planning and tool calling |
| `Llama31Model` | `string` | `meta-llama/Llama-3.1-8B-Instruct` | Model identifier for quick summaries |
| `LlamaVisionModel` | `string` | `meta-llama/Llama-3.2-11B-Vision-Instruct` | Model identifier for visual asset classification |
| `LlamaGuardModel` | `string` | `meta-llama/Llama-Guard-3-8B` | Model identifier for pre-share safety scanning |
| `FallbackToLocal` | `bool` | `true` | When true, permits offline heuristic planning if the remote endpoint fails |

---

## 2. Tool Schema Models

### Formal Tool Calling Request Structure
The payload sent by `MetaLlamaClient` to `{BaseUrl}/chat/completions`:

```json
{
  "model": "meta-llama/Llama-3.3-70B-Instruct",
  "messages": [
    { "role": "system", "content": "..." },
    { "role": "user", "content": "..." }
  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "create_directory",
        "description": "...",
        "parameters": { ... }
      }
    }
  ],
  "tool_choice": "auto",
  "temperature": 0.1
}
```

### Model Tool Response Structure
The structured output received from the Meta model:

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "I have created a plan to organize your workspace...",
        "tool_calls": [
          {
            "id": "call_123",
            "type": "function",
            "function": {
              "name": "create_directory",
              "arguments": "{\"name\":\"Reports\"}"
            }
          }
        ]
      }
    }
  ]
}
```

---

## 3. Removals

- **Removed DTO**: `TranscribeResponse` removed from `backend/Agent/Models/AgentDtos.cs`.
- **Removed Method**: `TranscribeAudioAsync` removed from `IVaultAgentService` and `VaultAgentService`.
- **Removed Endpoint**: `POST /api/agent/transcribe` removed from `AgentController`.

