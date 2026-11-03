# LinkVault Agent — Meta Hackathon Submission

> **Headline**: A verifiable AI workspace agent built with **Meta Muse Spark Open** (`meta-models/muse-spark-open`), **Meta Muse Glimmer** (`meta-models/Muse-Glimmer-30B`), and **Llama Guard 3 (8B)** via Meta Model API / Baseten endpoints that acts on your files, directories, and share links under your exact permissions with live workspace context injection, human-in-the-loop approval, real-time SignalR progress, zero-trust audit attribution, and an Apple HIG frontend.

---

## 🔗 Quick Links & Verification

- **Git Compare View**: [github.com/Musavirdar/LinkVault/compare/pre-hackathon...main](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
- **Baseline Git Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/releases/tag/pre-hackathon) (Commit `d69b3ed`, detailed below)
- **Live Demo Route**: `/` (Apple HIG Landing & Agent Window) and `/agent` (Verifiable Agent Workspace)

---

## 🧠 Meta Models Used & Architecture

| Model | Role in LinkVault Agent | Integration Point |
|---|---|---|
| **Meta Muse Spark Open** (`meta-models/muse-spark-open`) | **Core Planning & Autonomous Tool Calling**: Primary hackathon model on Baseten OpenAI-compatible endpoint (`https://inference.baseten.co/v1`). Receives formal JSON tool schemas (`create_directory`, `move_resource`, `create_share_link`, `search`, `list_directory`, `delete_resource`) with `"tool_choice": "auto"` and live workspace context (real entity GUIDs). Emits structured `tool_calls`. When tool calls return, `message.content` is null and `message.reasoning_content` is ignored. An offline heuristic planner acts as a resilience fallback if the remote endpoint is unavailable. | `backend/Agent/Services/VaultAgentService.cs`<br>`backend/Agent/Services/MetaLlamaClient.cs` |
| **Meta Muse Spark Open** (`meta-models/muse-spark-open`) | **Multimodal Visual Asset Categorization**: Uses standard OpenAI `image_url` content parts with the unified Muse Spark Open model. *(Note: Verified via automated unit tests using a mock client; not claimed as production-verified without live hardware run).* | `backend/Agent/Services/MetaLlamaClient.cs`<br>`backend/Agent/Services/VaultAgentService.cs` |
| **Meta Muse Glimmer** (`meta-models/Muse-Glimmer-30B`) | **Fast Summaries & Diagnostic Parsing**: High-speed conversational responses, intent detection, and local verification. Distilled from Muse Spark for low-latency agent workflows. | `backend/Agent/Models/MetaModelConfig.cs`<br>`backend/Agent/Services/MetaLlamaClient.cs` |
| **Meta Llama Guard 3 (8B)** (`meta-llama/Llama-Guard-3-8B`) | **Safe Share Security Guardrail**: Scans file content prior to share link creation to detect exposed database connection strings, passwords, private keys, API tokens, and PII, blocking unconfirmed leaks. | `backend/Agent/Services/SafeShareGuard.cs`<br>`backend/Agent/Services/MetaLlamaClient.cs` |

---

## 🌟 What Is LinkVault Agent? ("Agents 102: Verifiable Agents")

Rather than basic Retrieval-Augmented Generation (RAG) that merely summarizes text, **LinkVault Agent** is an **actionable, verifiable workspace assistant**:

1. **Context-Injected Tool Calling**: You say: *"Organize my uploads, then share the Q3 files with the design team for 7 days."* The agent queries live workspace state (real directory and file GUIDs) and feeds both the goal and real entity context to Meta Muse Spark Open with formal per-tool schemas. The model emits precise function calls referencing real database IDs.
2. **Human-in-the-Loop Gating**: State-altering operations (moving files, creating folders, generating public share links, deleting resources) pause and require **one-click user approval**. Users inspect the exact step plan, diff, and parameters before clicking **Approve** or **Reject**.
3. **Isolated Background Execution**: Once approved, steps execute in a dedicated `IServiceScopeFactory` background scope protected by atomic concurrency guards (`409 Conflict` on double-approval attempts).
4. **Live SignalR Streaming**: Progress streams in real-time over WebSocket (`/hubs/agent`), updating step states (Pending 🟡 ➔ Running 🔵 ➔ Completed 🟢) with granular execution receipts.
5. **Verifiable Audit Attribution**: Every agent execution operates strictly under the logged-in user's RBAC claims (`canUpload`, `canUpdate`, `canShare`, `canDelete`) and tenant boundary. Modifications are immutably logged with `[via agent]` attribution, `IsViaAgent = true`, `AgentPlanId`, and `AgentToolName`.
6. **Safe Share Guardrail**: Scans files before link issuance with deterministic regex + Llama Guard 3. Files containing database passwords, private keys, or PII are immediately **blocked** from public sharing.
7. **Native Browser Voice Recognition**: Speech dictation via the browser's native Web Speech API (`SpeechRecognition` / `webkitSpeechRecognition`) transcribes voice directives into agent prompt commands in real time with zero latency and zero deceptive backend mocks.
8. **Apple HIG & Fluid Physical Motion Redesign**: Fully redesigned frontend adhering to Apple Human Interface Guidelines: SF Pro typography, translucent frosted glass materials (`backdrop-blur-2xl`), 20px squircle card radii, 12px control radii, hairline borders, and tactile spring physics (`appleSpring`).

---

## 📂 Key Folders & Architecture

### Backend (`backend/Agent/`)
- `backend/Agent/Controllers/AgentController.cs`: REST endpoints for `/api/agent/plan`, `/approve`, `/reject`, `/safe-share/scan`.
- `backend/Agent/Hubs/AgentHub.cs`: SignalR hub broadcasting `OnPlanStatusChanged`, `OnStepStarted`, `OnStepCompleted`, `OnStepFailed`, `OnPlanFinished`.
- `backend/Agent/Services/IMetaLlamaClient.cs` & `MetaLlamaClient.cs`: Meta Muse Spark Open tool calling, Muse Glimmer, Llama Guard 3, and Llama 3.2 Vision client supporting Meta Model API and Baseten endpoints with real-ID context injection.
- `backend/Agent/Services/IVaultAgentService.cs` & `VaultAgentService.cs`: Core tool-calling orchestrator, live workspace context assembler, atomic approval gating, and permission boundary validator.
- `backend/Agent/Services/ISafeShareGuard.cs` & `SafeShareGuard.cs`: Pre-share secret detection engine combining regex and Llama Guard 3.
- `backend/Agent/Models/AgentActionPlan.cs`, `PlanStep.cs`, `SafeShareScan.cs`: Database models for verifiable plan tracking.
- `backend/Agent/Models/MetaModelConfig.cs`, `MetaLlamaConfig.cs` & `AgentDtos.cs`: Meta provider configuration and workspace context API contracts.

### Frontend (`app/src/agent/` & `app/src/`)
- `app/src/pages/LandingPage.tsx`: Interactive Apple HIG landing page mounted at `/` featuring live autonomous agent run preview (`#842`), traffic light controls, and approval diffs.
- `app/src/agent/pages/AgentPage.tsx`: Full AI Workspace & Security Agent page with Meta Muse model badge and real-time execution stream.
- `app/src/agent/components/VaultAgentChat.tsx`: Conversational prompt card with suggested prompt chips and native voice dictation.
- `app/src/agent/components/PlanPreviewModal.tsx`: Step-by-step preview modal with one-click Approve / Reject buttons.
- `app/src/agent/components/LiveProgressStream.tsx`: SignalR real-time execution visualizer.
- `app/src/agent/components/SafeShareAlertModal.tsx`: Secret detection warning dialog with remediation recommendations.
- `app/src/agent/components/VoiceCommandButton.tsx`: Native browser SpeechRecognition voice input button.
- `app/src/agent/components/AuditInsightsCard.tsx`: Verifiable agent audit trail telemetry card.

### Automated Tests (`backend/LinkVaultApi.Tests/`)
- `backend/LinkVaultApi.Tests/Unit/MetaModelToolCallingTests.cs`: Verifies formal `tools` request payload generation, real-ID context injection into prompts, fallback ID resolution, and remote `tool_calls` response parsing, OpenAI image_url payload structure, and null content handling (64 tests passing).
- `backend/LinkVaultApi.Tests/Unit/AgentHardeningTests.cs`: Concurrency & security hardening tests (atomic double approval 409, unauthorized approval denial, Viewer claim enforcement, missing resourceId rejection, and cross-tenant entity isolation).
- `backend/LinkVaultApi.Tests/Unit/VaultAgentServiceTests.cs`: Live workspace context extraction, tool decomposition, and unauthenticated boundary enforcement.
- `backend/LinkVaultApi.Tests/Unit/AgentApprovalAndAuditTests.cs`: Approval gating, plan rejection, and `via agent` audit attribution.
- `backend/LinkVaultApi.Tests/Unit/SafeShareGuardTests.cs`: Credential detection (Postgres passwords, private keys, AWS keys, JWTs) and sharing blocks.

---

## 🛡️ Hackathon Work & Baseline Distinction

In accordance with hackathon submission integrity rules, the distinction between pre-existing work and hackathon achievements is transparently detailed below:

### Pre-Hackathon Baseline (as of Oct 26, 2026)
Prior to the hackathon event, the repository contained the foundational LinkVault web application:
- Standard user authentication (register, login, JWT issuance, basic 2FA).
- Workspace file management (directory hierarchy, basic local file uploads, resource listings).
- Multi-tenancy and organization-scoped role definitions.
- Basic link sharing without granular ticket redemption.

### Hackathon Platform & Security Hardening (Oct 27–29, 2026)
During the hackathon window, the foundational platform was hardened to enterprise security standards:
1. **Single-Use Download Tickets**: Replaced raw file URLs with cryptographic, single-use, time-bound tickets (`POST /api/files/{id}/ticket` to issue ticket, and `GET /api/files/{id}/download?ticket={token}` to stream file).
2. **Safe Share Guardrail**: Pre-share scanning blocks accidental sharing of connection strings, API tokens, and private keys.
3. **Client IP Spoofing Elimination**: Removed insecure proxy clearing, applied `ForwardLimit = 1` and enabled `ASPNETCORE_FORWARDEDHEADERS_ENABLED` for platform proxy headers.
4. **Credential Purge & Sanitization**: Purged committed `.env.render` credentials, added StartupValidator blacklist, and updated `.gitignore`.
5. **Zero-Trust File Upload Validation**: Enforced magic byte signature inspection and file extension sanitization.

### Hackathon Headline: Verifiable Workspace Agent & Apple HIG Redesign (Oct 29–Nov 3, 2026)
The primary hackathon project is the autonomous, verifiable workspace agent built for the Meta Hackathon:
1. **Meta Muse Spark Open Autonomous Tool Calling**: Real function-calling engine with formal tool schemas, live workspace entity GUID context injection, per-tool argument validation, and bounded plan execution.
2. **Atomic Approval & Isolated Background Execution**: Race-condition-free `ExecuteUpdateAsync` plan approval gating (409 Conflict) and dedicated `IServiceScopeFactory` execution scopes.
3. **Full RBAC Claim Enforcement**: Strict check of user permissions (`canUpload`, `canUpdate`, `canShare`, `canDelete`) before any state-altering tool executes.
4. **Real-Time SignalR Execution Streaming**: Instant feedback for step initiation, completion, and failure over WebSocket.
5. **Native Browser Voice Dictation**: Genuine speech recognition without deceptive backend mocks.
6. **Apple Human Interface Guidelines Redesign**: Complete frontend polish with SF Pro typography, translucent frosted glass materials (`backdrop-blur-2xl`), 20px squircle cards, 12px controls, hairline borders, and tactile spring physics.

---

## 📊 Git History & Baseline Tag Transparency

- **Baseline Tag**: [`pre-hackathon`](https://github.com/Musavirdar/LinkVault/tree/pre-hackathon) points to commit `d69b3ed`.
- **Full Compare View**: [Compare Changes](https://github.com/Musavirdar/LinkVault/compare/pre-hackathon...main)
- **Repository History Transparency**: During early repository cleanup, git history was sanitized and force-pushed to remove sensitive committed environment files (`.env.render`). Consequently, commit `d69b3ed` (titled `feat(security): implement security, access control & quality hardening`) serves as the root commit of the current repository branch. All commits from `d69b3ed` onward were created during the hackathon window and reflect authentic development progress.

---

## 🚀 How to Run & Demo LinkVault Agent

### 1. Prerequisites
- **.NET 8 SDK**
- **Node.js 18+** & **npm**
- **PostgreSQL 14+**

### 2. Configure Environment Variables
To connect LinkVault Agent to Meta's Baseten or Meta Model API endpoints:
```bash
# Meta Muse Model Configuration (Baseten OpenAI-Compatible Endpoint)
export META_MODEL_BASE_URL="https://inference.baseten.co/v1"
export META_MODEL_API_KEY="your-baseten-or-meta-api-key"
export META_MODEL_NAME="meta-models/muse-spark-open"
export MUSE_SPARK_MODEL="meta-models/muse-spark-open"
export MUSE_GLIMMER_MODEL="meta-models/Muse-Glimmer-30B"

# Optional: When running offline without an API key, the local fallback planner
# uses live workspace context to generate verifiable plans with real database GUIDs.
```

### 3. Run Backend API
```bash
cd backend
dotnet run
# API listens on http://localhost:5246
# Health check: http://localhost:5246/health
```

### 4. Run Frontend App
```bash
cd app
npm run dev
# Frontend dev server: http://localhost:5173
```

### 5. Demo Flow
1. Navigate to `http://localhost:5173/` to view the Apple HIG Landing Page with the interactive agent preview.
2. Sign in or click **Try the demo** to navigate to `/agent`.
3. Submit a directive such as:
   - *"Organize my uploads into an Invoices folder and share the latest invoice for 7 days."*
4. Inspect the generated plan: notice the real file and directory names and GUIDs extracted from workspace context.
5. Click **Approve**: watch the live SignalR progress bar stream step initiation and completion in real time.
6. Test Safe Share: attempt to share a file containing sensitive credentials or PII (e.g. `compensation_bands_FY25_internal.xlsx`) to see Llama Guard 3 halt the link creation.
