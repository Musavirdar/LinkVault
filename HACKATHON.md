# LinkVault Agent — Meta Hackathon Submission

> **Headline**: An autonomous, verifiable AI workspace agent built with **Meta Llama 3.3 (70B Instruct)**, **Llama 3.2 Vision (11B)**, and **Llama Guard 3 (8B)** that acts on your files, directories, and share links under your exact permissions with human-in-the-loop approval, real-time SignalR progress, and zero-trust audit attribution.

---

## 🔗 Quick Links & Verification

- **Git Compare View**: [github.com/Musavirdar/LinkVault/compare/pre-hackathon...main](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
- **Baseline Git Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/releases/tag/pre-hackathon) (Commit `d69b3ed`, before hackathon development)
- **Live Demo Route**: `/agent` (Interactive Workspace & Security Agent)

---

## 🧠 Meta Models Used & Architecture

| Model | Role in LinkVault Agent | Integration Point |
|---|---|---|
| **Meta Llama 3.3 (70B Instruct)** | **Core Planning & Autonomous Tool Calling**: Decomposes natural language and voice directives into atomic, dependency-ordered tool steps (`create_directory`, `move_resource`, `create_share_link`, `search`, `list_directory`). | `backend/Agent/Services/VaultAgentService.cs`<br>`backend/Agent/Services/MetaLlamaClient.cs` |
| **Meta Llama Guard 3 (8B)** | **Safe Share Security Guardrail**: Scans file content prior to share link creation to detect exposed database connection strings, passwords, private keys, API tokens, and PII, blocking unconfirmed leaks. | `backend/Agent/Services/SafeShareGuard.cs` |
| **Meta Llama 3.2 Vision (11B)** | **Multimodal Visual Asset Categorization**: Inspects uploaded screenshots, diagrams, and receipts, extracting visual semantics and filing assets into thematic workspace folders. | `backend/Agent/Services/MetaLlamaClient.cs`<br>`backend/Agent/Services/VaultAgentService.cs` |
| **Meta Llama 3.1 (8B Instant)** | **Fast Heuristics & Low-Latency Summaries**: High-speed conversational responses, intent detection, and audit action summarization. | `backend/Agent/Models/MetaLlamaConfig.cs` |

---

## 🌟 What Is LinkVault Agent? ("Agents 102: Verifiable Agents")

Rather than basic Retrieval-Augmented Generation (RAG) that merely summarizes text, **LinkVault Agent** is an **actionable, verifiable workspace assistant**:

1. **Autonomous Planning**: You say: *"Organize my uploads, then share the Q3 files with the design team for 7 days."* The agent synthesizes an ordered execution plan via Llama 3.3 tool calls.
2. **Human-in-the-Loop Gating**: State-altering operations (moving files, creating folders, generating public share links) pause and require **one-click user approval**. You can inspect the step plan and click **Approve** or **Reject**.
3. **Live SignalR Streaming**: Progress streams in real-time over WebSocket (`/hubs/agent`), updating progress bars and step states (Pending 🟡 ➔ Running 🔵 ➔ Completed 🟢).
4. **Verifiable Audit Attribution**: Every agent execution operates strictly under the logged-in user's RBAC and tenant boundary. Modifications are immutably logged with `[via agent]` attribution and `IsViaAgent = true`.
5. **Safe Share Guardrail**: Scans files before link issuance with deterministic regex + Llama Guard 3. Files containing database passwords or private keys are immediately **blocked** from public sharing.
6. **Voice Commands**: Microphone dictation via browser Web Speech API / MediaRecorder transcribes voice directives into agent prompt commands hands-free.
7. **Multimodal Visual Filing**: Automatically categorizes visual media (diagrams, mockups, invoices) using Llama 3.2 Vision.

---

## 📂 New Folders & Key Files

### Backend (`backend/Agent/`)
- `backend/Agent/Controllers/AgentController.cs`: REST endpoints for `/api/agent/plan`, `/approve`, `/reject`, `/safe-share/scan`, `/transcribe`.
- `backend/Agent/Hubs/AgentHub.cs`: SignalR hub broadcasting `OnPlanStatusChanged`, `OnStepStarted`, `OnStepCompleted`, `OnStepFailed`, `OnPlanFinished`.
- `backend/Agent/Services/IMetaLlamaClient.cs` & `MetaLlamaClient.cs`: Meta Llama 3.3, Llama Guard 3, and Llama 3.2 Vision client with intelligent offline fallback.
- `backend/Agent/Services/IVaultAgentService.cs` & `VaultAgentService.cs`: Core tool-calling orchestrator, planning loop, and permission boundary validator.
- `backend/Agent/Services/ISafeShareGuard.cs` & `SafeShareGuard.cs`: Pre-share secret detection engine.
- `backend/Agent/Models/AgentActionPlan.cs`, `PlanStep.cs`, `SafeShareScan.cs`: Database models for verifiable plan tracking.
- `backend/Agent/Models/MetaLlamaConfig.cs` & `AgentDtos.cs`: Meta provider configuration and API contracts.

### Frontend (`app/src/agent/`)
- `app/src/agent/pages/AgentPage.tsx`: Full AI Workspace & Security Agent page with Meta badge.
- `app/src/agent/components/VaultAgentChat.tsx`: Conversational prompt card with suggested prompts and dictation button.
- `app/src/agent/components/PlanPreviewModal.tsx`: Step-by-step preview modal with one-click Approve / Reject buttons.
- `app/src/agent/components/LiveProgressStream.tsx`: SignalR real-time execution visualizer.
- `app/src/agent/components/SafeShareAlertModal.tsx`: Secret detection warning dialog.
- `app/src/agent/components/VoiceCommandButton.tsx`: Speech-to-text voice input button.
- `app/src/agent/components/AuditInsightsCard.tsx`: Verifiable agent audit trail telemetry card.
- `app/src/agent/services/agentApi.ts` & `agentHubClient.ts`: REST and SignalR clients.

### Automated Tests (`backend/LinkVaultApi.Tests/`)
- `backend/LinkVaultApi.Tests/Unit/VaultAgentServiceTests.cs`: Planning loop, tool decomposition, and unauthenticated boundary enforcement.
- `backend/LinkVaultApi.Tests/Unit/AgentApprovalAndAuditTests.cs`: Approval gating, plan rejection, and `via agent` audit attribution.
- `backend/LinkVaultApi.Tests/Unit/SafeShareGuardTests.cs`: Credential detection (Postgres passwords, private keys, AWS keys, JWTs) and sharing blocks.

---

## 🛡️ Hackathon Security Hardening Highlights

During the hackathon development cycle, the foundational repository was also hardened to enterprise standards:
1. **Single-Use Download Tickets**: Replaced raw file URLs with cryptographic, single-use, time-bound tickets (`/api/files/ticket/{id}/stream`).
2. **Safe Share Guardrail**: Pre-share scanning blocks accidental sharing of connection strings and private tokens.
3. **Client IP Spoofing Elimination**: Removed insecure proxy clearing, applied `ForwardLimit = 1` for Render platform headers.
4. **Credential Purge**: Sanitized committed environment files (`.env.render`) and enforced `.gitignore` exclusions.
5. **Zero-Trust File Upload Validation**: Magic byte signature inspection and extension sanitization.

---

## 📊 Git History & Baseline Integrity

In accordance with hackathon guidelines, all commits are small, sequential, and untampered:
- **Baseline Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/tree/pre-hackathon) (Commit `d69b3ed`)
- **Final Compare**: [Compare Changes](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
