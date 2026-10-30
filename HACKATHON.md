# LinkVault Agent — Meta Hackathon Submission

> **Headline**: An autonomous, verifiable AI workspace agent built with **Meta Llama 3.3 (70B Instruct)**, **Llama 3.2 Vision (11B)**, and **Llama Guard 3 (8B)** via Meta Model API / Baseten endpoints that acts on your files, directories, and share links under your exact permissions with human-in-the-loop approval, real-time SignalR progress, and zero-trust audit attribution.

---

## 🔗 Quick Links & Verification

- **Git Compare View**: [github.com/Musavirdar/LinkVault/compare/pre-hackathon...main](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
- **Baseline Git Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/releases/tag/pre-hackathon) (Commit `d69b3ed`)
- **Live Demo Route**: `/agent` (Interactive Workspace & Security Agent)

---

## 🧠 Meta Models Used & Architecture

| Model | Role in LinkVault Agent | Integration Point |
|---|---|---|
| **Meta Llama 3.3 (70B Instruct)** (`meta-llama/Llama-3.3-70B-Instruct`) | **Core Planning & Autonomous Tool Calling**: Receives formal JSON tool schemas (`create_directory`, `move_resource`, `create_share_link`, `search`, `list_directory`, `delete_resource`) with `"tool_choice": "auto"` and emits structured `tool_calls`. An offline heuristic planner acts as a resilience fallback if the remote endpoint is unavailable. | `backend/Agent/Services/VaultAgentService.cs`<br>`backend/Agent/Services/MetaLlamaClient.cs` |
| **Meta Llama Guard 3 (8B)** (`meta-llama/Llama-Guard-3-8B`) | **Safe Share Security Guardrail**: Scans file content prior to share link creation to detect exposed database connection strings, passwords, private keys, API tokens, and PII, blocking unconfirmed leaks. | `backend/Agent/Services/SafeShareGuard.cs` |
| **Meta Llama 3.2 Vision (11B)** (`meta-llama/Llama-3.2-11B-Vision-Instruct`) | **Multimodal Visual Asset Categorization**: Inspects uploaded screenshots, diagrams, and receipts, extracting visual semantics and filing assets into thematic workspace folders. | `backend/Agent/Services/MetaLlamaClient.cs`<br>`backend/Agent/Services/VaultAgentService.cs` |
| **Meta Llama 3.1 (8B Instruct)** (`meta-llama/Llama-3.1-8B-Instruct`) | **Fast Summaries & Diagnostic Parsing**: High-speed conversational responses, intent detection, and audit action summarization. | `backend/Agent/Models/MetaLlamaConfig.cs` |

---

## 🌟 What Is LinkVault Agent? ("Agents 102: Verifiable Agents")

Rather than basic Retrieval-Augmented Generation (RAG) that merely summarizes text, **LinkVault Agent** is an **actionable, verifiable workspace assistant**:

1. **Autonomous Tool Calling**: You say: *"Organize my uploads, then share the Q3 files with the design team for 7 days."* The agent formats the request with formal tool schemas, and Meta Llama 3.3 emits structured function calls validated against per-tool schemas.
2. **Human-in-the-Loop Gating**: State-altering operations (moving files, creating folders, generating public share links, deleting resources) pause and require **one-click user approval**. You can inspect the step plan and click **Approve** or **Reject**.
3. **Live SignalR Streaming**: Progress streams in real-time over WebSocket (`/hubs/agent`), updating progress bars and step states (Pending 🟡 ➔ Running 🔵 ➔ Completed 🟢).
4. **Verifiable Audit Attribution**: Every agent execution operates strictly under the logged-in user's RBAC claims (`canUpload`, `canUpdate`, `canShare`, `canDelete`) and tenant boundary. Modifications are immutably logged with `[via agent]` attribution and `IsViaAgent = true`.
5. **Safe Share Guardrail**: Scans files before link issuance with deterministic regex + Llama Guard 3. Files containing database passwords or private keys are immediately **blocked** from public sharing.
6. **Native Browser Voice Recognition**: Speech dictation via the browser's native Web Speech API (`SpeechRecognition` / `webkitSpeechRecognition`) transcribes voice directives into agent prompt commands in real time with zero latency and zero deceptive backend mocks.
7. **Multimodal Visual Filing**: Automatically categorizes visual media (diagrams, mockups, invoices) using Llama 3.2 Vision.

---

## 📂 New Folders & Key Files

### Backend (`backend/Agent/`)
- `backend/Agent/Controllers/AgentController.cs`: REST endpoints for `/api/agent/plan`, `/approve`, `/reject`, `/safe-share/scan`.
- `backend/Agent/Hubs/AgentHub.cs`: SignalR hub broadcasting `OnPlanStatusChanged`, `OnStepStarted`, `OnStepCompleted`, `OnStepFailed`, `OnPlanFinished`.
- `backend/Agent/Services/IMetaLlamaClient.cs` & `MetaLlamaClient.cs`: Meta Llama 3.3 tool calling, Llama Guard 3, and Llama 3.2 Vision client supporting Meta Model API and Baseten endpoints.
- `backend/Agent/Services/IVaultAgentService.cs` & `VaultAgentService.cs`: Core tool-calling orchestrator, planning loop, atomic gating, and permission boundary validator.
- `backend/Agent/Services/ISafeShareGuard.cs` & `SafeShareGuard.cs`: Pre-share secret detection engine.
- `backend/Agent/Models/AgentActionPlan.cs`, `PlanStep.cs`, `SafeShareScan.cs`: Database models for verifiable plan tracking.
- `backend/Agent/Models/MetaLlamaConfig.cs` & `AgentDtos.cs`: Meta provider configuration and API contracts.

### Frontend (`app/src/agent/`)
- `app/src/agent/pages/AgentPage.tsx`: Full AI Workspace & Security Agent page with Meta badge.
- `app/src/agent/components/VaultAgentChat.tsx`: Conversational prompt card with suggested prompts and dictation button.
- `app/src/agent/components/PlanPreviewModal.tsx`: Step-by-step preview modal with one-click Approve / Reject buttons.
- `app/src/agent/components/LiveProgressStream.tsx`: SignalR real-time execution visualizer.
- `app/src/agent/components/SafeShareAlertModal.tsx`: Secret detection warning dialog.
- `app/src/agent/components/VoiceCommandButton.tsx`: Native browser SpeechRecognition voice input button.
- `app/src/agent/components/AuditInsightsCard.tsx`: Verifiable agent audit trail telemetry card.
- `app/src/agent/services/agentApi.ts` & `agentHubClient.ts`: REST and SignalR clients.

### Automated Tests (`backend/LinkVaultApi.Tests/`)
- `backend/LinkVaultApi.Tests/Unit/MetaModelToolCallingTests.cs`: Verifies formal `tools` request payload generation and remote `tool_calls` response parsing.
- `backend/LinkVaultApi.Tests/Unit/AgentHardeningTests.cs`: Concurrency & security hardening tests (atomic double approval 409, unauthorized approval denial, Viewer claim enforcement, missing resourceId rejection, and cross-tenant entity isolation).
- `backend/LinkVaultApi.Tests/Unit/VaultAgentServiceTests.cs`: Planning loop, tool decomposition, and unauthenticated boundary enforcement.
- `backend/LinkVaultApi.Tests/Unit/AgentApprovalAndAuditTests.cs`: Approval gating, plan rejection, and `via agent` audit attribution.
- `backend/LinkVaultApi.Tests/Unit/SafeShareGuardTests.cs`: Credential detection (Postgres passwords, private keys, AWS keys, JWTs) and sharing blocks.

---

## 🛡️ Hackathon Work & Baseline Distinction

In accordance with hackathon submission integrity rules, the distinction between pre-existing work and hackathon achievements is clearly delineated:

### Pre-Hackathon Baseline (as of Oct 26, 2026)
Prior to the hackathon event, the repository contained the foundational LinkVault web application:
- Standard user authentication (register, login, JWT issuance, basic 2FA).
- Workspace file management (directory hierarchy, basic local file uploads, resource listings).
- Multi-tenancy and organization-scoped role definitions.
- Basic link sharing without granular ticket redemption.

### Hackathon Security & Platform Hardening (Oct 27–29, 2026)
During the hackathon window, the foundational platform was hardened to enterprise security standards:
1. **Single-Use Download Tickets**: Replaced raw file URLs with cryptographic, single-use, time-bound tickets (`POST /api/files/{id}/ticket` to issue, and `GET /api/files/{id}/download?ticket={token}` to stream).
2. **Safe Share Guardrail**: Pre-share scanning blocks accidental sharing of connection strings, API tokens, and private keys.
3. **Client IP Spoofing Elimination**: Removed insecure proxy clearing, applied `ForwardLimit = 1` and enabled `ASPNETCORE_FORWARDEDHEADERS_ENABLED` for Render platform proxy headers.
4. **Credential Purge & Sanitization**: Purged committed `.env.render` credentials, added StartupValidator blacklist, and updated `.gitignore`.
5. **Zero-Trust File Upload Validation**: Enforced magic byte signature inspection and file extension sanitization.

### Hackathon Headline: Verifiable Workspace Agent (Oct 29–30, 2026)
The primary hackathon project is the autonomous, verifiable workspace agent built for the Meta Hackathon:
1. **Meta Llama 3.3 Autonomous Tool Calling**: Real function-calling loop with formal tool schemas, per-tool argument validation, and bounded plan execution.
2. **Atomic Approval & Isolated Background Execution**: Race-condition-free `ExecuteUpdateAsync` plan approval gating (409 Conflict) and dedicated `IServiceScopeFactory` execution scopes.
3. **Full RBAC Claim Enforcement**: Strict check of user permissions (`canUpload`, `canUpdate`, `canShare`, `canDelete`) before any state-altering tool executes.
4. **Real-Time SignalR Execution Streaming**: Instant feedback for step initiation, completion, and failure.
5. **Native Browser Voice Dictation**: Genuine speech recognition without deceptive backend mocks.

---

## 📊 Git History & Repository Notes

- **Baseline Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/tree/pre-hackathon) (Commit `d69b3ed`)
- **Full Compare View**: [Compare Changes](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
- **History Transparency Note**: During early repository setup, git history was cleaned and force-pushed to remove sensitive committed environment files (`.env.render`). From the `pre-hackathon` tag onwards, all commits are chronological and reflect genuine hackathon feature development.
