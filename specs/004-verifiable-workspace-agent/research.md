# Research & Architecture Decisions: Verifiable Workspace Agent (LinkVault Agent)

**Feature**: `004-verifiable-workspace-agent`  
**Date**: 2026-10-29  
**Status**: Completed  

---

## 1. Agent Architecture & Tool-Calling Planning Loop

### Context & Challenge
Traditional "basic RAG" systems merely retrieve text chunks and inject them into a chat prompt. For LinkVault to be an actionable, verifiable workspace agent that complies with Meta's "Agents 102: Verifiable Agents" standards, it must:
1. Autonomously plan a sequence of operations based on high-level natural language intent.
2. Express that plan through structured tool calling.
3. Require explicit human-in-the-loop confirmation before applying destructive or outward-facing changes (moves, shares, deletes).
4. Execute operations strictly under the authenticated user's authorization context.

### Decision
- **Inference & Reasoning**: Use **Meta Llama 3.3 (70B Instruct)** via Muse Spark / OpenAI-compatible tool-calling protocol (`tools` array with JSON Schema function definitions).
- **Core Tool Catalog**:
  1. `list_directory(directoryId?: string)`: Enumerate folders and files within the user's organization scope.
  2. `read_file_text(resourceId: string, maxBytes?: number)`: Stream file contents safely via the existing zero-trust storage provider, respecting file size and type boundaries.
  3. `search(query: string, fileType?: string)`: Semantic and keyword search across resources and directories.
  4. `create_directory(name: string, parentDirectoryId?: string)`: Create a hierarchical directory node.
  5. `move_resource(resourceId: string, targetDirectoryId?: string)`: Relocate a file or link into a folder.
  6. `create_share_link(resourceId: string, expiryDays?: number, password?: string)`: Generate a single-use or time-bound download link after passing Safe Share checks.
- **Planning vs. Execution Phase Separation**:
  - **Phase A (Plan Synthesis)**: The agent executes read-only query tools (`search`, `list_directory`, `read_file_text`) to explore the workspace and returns an `AgentActionPlan` containing proposed state modifications.
  - **Phase B (Human Review)**: The user inspects the preview in the UI and clicks "Approve" or "Reject".
  - **Phase C (Execution & Live Streaming)**: If approved, the agent runner dispatches state-altering tools (`create_directory`, `move_resource`, `create_share_link`) sequentially, streaming live progress over SignalR.

### Alternatives Evaluated
- *Immediate Execution without Approval*: Rejected. Violates Meta's Verifiable Agent safety principles and creates accidental file movement/deletion risks.
- *Client-Side Direct API Calls*: Rejected. Exposing tool execution orchestration solely in the frontend risks bypassing server-side audit tagging and Safe Share verification.

---

## 2. Authorization Boundary, RBAC & Audit Tagging

### Context & Challenge
Autonomous agents must not act as a backdoor or gain permissions beyond the active user. If a user has `CanUpload` but not `CanDelete` or `CanShare`, the agent must be bound by those exact claims. Furthermore, every operation must be auditable and distinguishable from direct manual user actions.

### Decision
- **User Delegation**: The agent controller and services resolve the user's JWT identity (`UserId`, `OrganizationId`, and RBAC permission claims `canUpload`, `canUpdate`, `canDelete`, `canShare`).
- **Enforced Security Checks**:
  - All resource queries filter strictly by `OrganizationId`.
  - Permission checks invoke the same validation logic as existing controllers (`FilesController`, `ShareController`, `DirectoriesController`).
- **Audit Attribution**:
  - The `AuditLog` entity is extended with an `ActorType` / `IsViaAgent` metadata flag.
  - When the agent performs an action on behalf of the user, the audit entry records:
    - `UserId`: The authenticated user's ID
    - `ActorName`: The user's name
    - `Description`: Action details with prefix `[via agent]` (e.g., `[via agent] Moved 'Q3_Report.pdf' to directory 'Finance'`)
    - `UserAgent`: Marked with `LinkVault-Agent/1.0`
- **Audit View Enhancement**: Add an "Agent Activity" filter tab in the Audit Log UI to isolate and inspect all actions performed autonomously.

### Alternatives Evaluated
- *Dedicated Service Account for Agent*: Rejected. Service accounts break per-user quota, RBAC inheritance, and individual user accountability.

---

## 3. Safe Share Guardrail (Credential & PII Pre-Share Inspection)

### Context & Challenge
Automating share link creation poses data leakage risks if users inadvertently share files containing secrets, API keys, database passwords, or PII.

### Decision
- **Two-Tier Inspection Pipeline**:
  1. **Deterministic Regex Engine**: Fast, local pattern scanning for high-entropy secrets, private keys (`-----BEGIN PRIVATE KEY-----`), connection strings (`Host=...;Password=...`), JWT tokens (`eyJ...`), AWS keys (`AKIA...`), and generic passwords.
  2. **Model Safety Verification**: If regex finds potential matches or if the document is unstructured text, invoke **Meta Llama Guard 3 (8B)** or **Llama 3.3** with a strict zero-trust moderation prompt to classify risk severity (`Safe`, `Warning`, `Critical`).
- **Enforcement Policy**:
  - If `Critical` (e.g. database credentials or private keys): Creation of public share link is strictly **blocked** with an explicit error identifying the detected secret pattern.
  - If `Warning` (e.g. email addresses, phone numbers, internal hostnames): User is prompted with an explicit confirmation modal before the link can be issued.

### Alternatives Evaluated
- *Post-Share Scanning*: Rejected. Once a share link is published, data leakage has already occurred.
- *Pure LLM Inspection*: Rejected. Regex is orders of magnitude faster and 100% deterministic on standard key formats. Combining regex with LLM provides both speed and contextual reasoning.

---

## 4. Real-Time Progress Streaming (SignalR Hub)

### Context & Challenge
Multi-step plans (e.g. searching 10 files, creating 2 directories, moving 8 items) take several seconds. Users need live, step-by-step progress feedback ("Step 2/5: Creating 'Q3 Finance' folder... Done").

### Decision
- Utilize LinkVault's existing SignalR infrastructure (`ActivityHub.cs` / `ChatHub.cs`).
- Introduce an `AgentHub` (or extend `ActivityHub`) with real-time events:
  - `PlanCreated(planId, stepsCount)`
  - `StepStarted(planId, stepIndex, stepName)`
  - `StepCompleted(planId, stepIndex, stepName, result)`
  - `StepFailed(planId, stepIndex, stepName, errorMessage)`
  - `PlanCompleted(planId, summary)`
- The client subscribes to `agentPlan_{planId}` group, receiving updates instantaneously with <100ms latency.

---

## 5. Voice Input Transcription

### Context & Challenge
Users want to trigger agent actions hands-free or via voice on video presentations.

### Decision
- **Frontend Capture**: Use standard browser `MediaRecorder` API or `webkitSpeechRecognition` / Web Speech API for zero-latency local speech-to-text.
- **Backend Model Fallback**: Add `/api/agent/transcribe` endpoint supporting Muse Voice Transcribe / Whisper-compatible audio file upload for environments where browser speech recognition is unavailable.
- **User Review Step**: Spoken text is populated into the agent input box with visual feedback, allowing the user to review or edit before dispatching the planning loop.

---

## 6. Multimodal Visual Document & Image Filing (Stretch)

### Context & Challenge
Users frequently upload raw screenshots, photo receipts, mockups, and diagrams with generic filenames (`image_1.png`, `Screenshot_20261029.jpg`).

### Decision
- Use **Meta Llama 3.2 Vision (11B)** to inspect image content.
- The agent classifies image into semantic workspace tags: `Diagrams/Architecture`, `Mockups/UI`, `Receipts/Finance`, `Identity/Docs`.
- It autonomously plans directory creation and file movement to sort image assets by semantic visual category.
