# Feature Specification: Verifiable Workspace Agent (LinkVault Agent)

**Feature Branch**: `004-verifiable-workspace-agent`

**Created**: 2026-10-29

**Status**: Draft

**Input**: User description: "Build LinkVault Agent, a workspace agent that acts on your files and links with the user’s own permissions and shows every step. It’s agentic, so it avoids the “basic RAG” anti-project, and it fits “Agents 102: Verifiable Agents”, a session Meta is running this week. What it does: You type or say: 'Organize my uploads, then share the Q3 files with the design team for 7 days.' The agent then: Plans (returns a step list through tool calls), Shows the plan in the UI (moves, shares, and deletes need your one-click approval), Executes each step through your existing API as the logged-in user (RBAC, org checks, and audit log apply; each audit entry is tagged 'via agent'), Streams progress live over real-time updates. Features in priority order: 1. Agent core (tool-calling loop with tools: list_directory, read_file_text, move_resource, create_directory, create_share_link, search; runs through secure download and share code), 2. Approval and audit (plan preview, approve and reject buttons, agent activity view in audit page), 3. Safe Share guard (scans file for API keys, passwords, and PII before share link creation; blocks or warns), 4. Voice commands (speech transcription turns speech into requests), 5. Stretch: vision to file images into folders by content."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Autonomous Workspace Planning & Tool-Calling Agent Core (Priority: P1)

As a workspace user, I want to provide high-level natural language instructions (such as "organize my uploads and group all financial sheets into a Finance directory") so that the system generates a structured, multi-step action plan using autonomous workspace tools and executes actions strictly within my authenticated permissions.

**Why this priority**: Core value of an agentic platform over simple search or retrieval-augmented generation (RAG). Transforming complex user intent into concrete, verifiable actions (listing, inspecting, creating folders, moving items, and generating secure share links) is the foundational engine of the feature.

**Independent Test**: Provide the agent with a prompt to find all documents matching a keyword and move them to a new directory. The agent decomposes the request, searches resources, creates the directory if missing, moves the files, and reports the results.

**Acceptance Scenarios**:

1. **Given** an authenticated user with permission to organize resources, **When** the user submits "Organize my loose invoices into an Invoices folder", **Then** the agent plans and executes search, folder creation, and resource move steps using authenticated workspace tools.
2. **Given** a user lacks permission to modify resources or access an organization, **When** the agent attempts to execute an action on behalf of that user, **Then** the workspace permission boundary rejects the action, and the agent halts with an informative permission error.
3. **Given** an instruction requiring file reading, **When** the agent calls the file reading tool, **Then** content is accessed via the zero-trust secure file streaming architecture without bypassing format validation.

---

### User Story 2 - Plan Preview, One-Click Approval & Verifiable Agent Audit (Priority: P1)

As a security-conscious administrator or team member, I want to inspect a clear step-by-step preview of what the agent intends to do before any moves, shares, or deletions occur, approve or reject the plan with a single click, see live execution progress, and review immutable audit records tagged as executed "via agent".

**Why this priority**: Essential for the "Verifiable Agents" paradigm. Without human-in-the-loop verification and end-to-end auditability, autonomous agents create severe compliance and data loss risks.

**Independent Test**: Trigger a command that involves moving or sharing a file. Verify that execution pauses until the user clicks "Approve", progress streams in real time as each step completes, and the Audit Log displays the completed actions with the "via agent" indicator.

**Acceptance Scenarios**:

1. **Given** an agent action plan containing resource movements, deletions, or share creations, **When** the plan is synthesized, **Then** the system displays the proposed steps in the user interface and pauses execution awaiting user confirmation.
2. **Given** a presented action plan, **When** the user clicks "Reject", **Then** the entire plan is cancelled, no workspace modifications are executed, and the session status is recorded as cancelled.
3. **Given** a presented action plan, **When** the user clicks "Approve", **Then** each step executes sequentially, live progress updates stream to the user interface, and each resulting operation in the workspace audit log is explicitly tagged with `via agent`.

---

### User Story 3 - Safe Share Guardrail (Credential & PII Pre-Share Inspection) (Priority: P2)

As a data compliance manager, I want the agent to automatically inspect any file before a public or team share link is created to detect exposed API keys, passwords, connection strings, or personally identifiable information (PII), so that accidental data leaks are caught and blocked or warned before external access is granted.

**Why this priority**: Protects organizations from accidental credential leaks (such as connection strings or JWT secrets) during automated sharing workflows, reinforcing zero-trust security.

**Independent Test**: Ask the agent to share a file that contains a test API key or database password. Verify that the Safe Share Guard detects the secret, displays a high-severity warning or blocks link creation, and refuses to emit the share link until the sensitive data is confirmed or cleared.

**Acceptance Scenarios**:

1. **Given** an instruction to create a share link for a file, **When** the file contains an exposed secret (e.g., API key, connection password, private token), **Then** the Safe Share Guard intercepts the share step, flags the specific risk pattern, and prevents link issuance without explicit administrative override.
2. **Given** a file with clean content free of detected secrets or sensitive PII, **When** evaluated by the Safe Share Guard, **Then** the check passes, and the agent proceeds to generate the atomic share link.

---

### User Story 4 - Voice-Driven Workspace Commands (Priority: P3)

As a busy professional or mobile worker, I want to dictate workspace instructions using my voice and have them transcribed into actionable agent requests in real time, so that I can organize and share content hands-free.

**Why this priority**: Enhances accessibility, accelerates workflow input, and provides a compelling, interactive demonstration interface.

**Independent Test**: Speak a phrase such as "Find the presentation uploaded yesterday and share it with the team" into the microphone. Verify that speech is accurately converted into text and immediately fed into the agent planning loop.

**Acceptance Scenarios**:

1. **Given** microphone access in the browser, **When** the user holds the voice command button and speaks an instruction, **Then** speech is captured, transcribed into text, and placed into the agent request input.
2. **Given** ambient noise or unclear speech, **When** transcription completes, **Then** the user can review and edit the transcribed text before dispatching it to the agent.

---

### User Story 5 - Multimodal Visual Document & Image Categorization (Priority: P4)

As a creative director or digital asset manager, I want the agent to understand the visual contents of uploaded images, screenshots, and diagrams, so that it can autonomously categorize visual assets into thematic directories (e.g., "Mockups", "Infographics", "Receipts") without manual tagging.

**Why this priority**: Extends the agent beyond text files to visual media, fulfilling high-value multi-format workspace automation.

**Independent Test**: Upload three distinct image types (a receipt, a software architecture diagram, and a marketing illustration). Ask the agent to organize images by visual category. Verify that the agent inspects each image and sorts them into appropriate folders.

**Acceptance Scenarios**:

1. **Given** unorganized image files in the uploads folder, **When** the agent processes an image organization command, **Then** it analyzes visual content, identifies semantic categories, and creates/moves images into designated folders.

---

### Edge Cases

- **Permission Revocation Mid-Execution**: If a user's permissions or organization membership is revoked while a multi-step plan is partially executed, remaining steps must immediately fail-fast without continuing.
- **Concurrent Resource Deletion**: If another user renames, moves, or deletes a resource referenced in an active agent plan before approval, the agent must detect the missing entity and report an error rather than creating an inconsistent state.
- **Malformed or Recursive Directives**: When a user provides circular instructions (e.g., "move folder A into folder B and folder B into folder A"), the planning loop must detect the circular hierarchy and return an error before proposing the plan.
- **Large File Inspection Timeout**: Safe Share inspection of very large documents must stream content up to a safety threshold or operate asynchronously to prevent request timeouts.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an autonomous tool-calling planning loop capable of decomposing user goals into atomic, dependency-ordered tool steps.
- **FR-002**: Agent execution MUST operate strictly within the authenticated user's session, applying existing organization isolation and Role-Based Access Control (RBAC) rules.
- **FR-003**: Agent tool catalog MUST include tools for: listing directories, searching resources, reading file text, creating directories, moving resources, and creating share links.
- **FR-004**: System MUST present action plans containing resource modifications, deletions, or sharing actions in a visual preview interface requiring one-click user approval before execution.
- **FR-005**: Users MUST be able to reject a proposed plan, cancelling all queued operations with zero changes to workspace resources.
- **FR-006**: System MUST stream real-time execution progress of each step (pending, running, completed, failed) to the client interface.
- **FR-007**: Every workspace modification or share creation executed by the agent MUST record an audit log entry explicitly flagged with an `AgentExecution` attribution tag (`via agent`).
- **FR-008**: System MUST provide an "Agent Activity" filter or view in the workspace Audit Log page to isolate and inspect actions executed by autonomous agents.
- **FR-009**: System MUST execute a Safe Share Guard check scanning file content for sensitive patterns (API keys, database passwords, private tokens, PII) prior to creating any public or team share link.
- **FR-010**: System MUST block or warn link creation if the Safe Share Guard detects high-risk credentials or sensitive leakage patterns.
- **FR-011**: System MUST support voice input transcription, allowing users to speak natural language instructions and review/dispatch them to the agent.
- **FR-012**: System MUST support multimodal visual classification of images, enabling the agent to sort diagrams, screenshots, and visual media into appropriate directories based on visual features.

### Key Entities

- **AgentActionPlan**: Represents the decomposed step list synthesized by the agent, containing: `Id`, `OrganizationId`, `UserId`, `Goal`, `Status` (PendingApproval, Approved, Executing, Completed, Rejected, Failed), `CreatedAt`, and ordered `PlanSteps`.
- **PlanStep**: Represents an individual atomic action within a plan: `StepNumber`, `ToolName`, `Parameters`, `Status` (Pending, InProgress, Succeeded, Failed), `ErrorMessage`, `ExecutionResult`.
- **SafeShareScanResult**: Output of the pre-share security guard: `ResourceId`, `HasSensitiveContent`, `DetectedThreats` (list of detected patterns), `Severity` (Safe, Warning, Critical), `ScannedAt`.
- **AgentAuditTelemetry**: Audit record enhancement tracking actor mechanism (`UserDirect` vs `ViaAgent`), `AgentPlanId`, `ToolName`, and `StepNumber`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of state-altering agent actions (moves, shares, deletions) pause for explicit user approval before executing.
- **SC-002**: 100% of audit log entries generated through agent tool execution include the `via agent` attribution tag.
- **SC-003**: Safe Share Guard detects 100% of known high-risk secret formats (connection strings, API keys, private tokens) in test files and blocks unconfirmed link creation.
- **SC-004**: Multi-step plans stream live execution updates to the user interface with latency under 500 milliseconds per step completion.
- **SC-005**: A user can speak or type an organization and share directive and achieve end-to-end plan execution in under 15 seconds.

## Assumptions

- The user is authenticated with a valid JWT token representing an active tenant organization.
- Existing database tables (`Resources`, `Directories`, `ShareLinks`, `AuditLogs`) provide the foundational entity storage.
- Real-time communication channel (SignalR / WebSocket) is accessible between client browser and backend server.
- Browser supports standard audio recording Web APIs (`MediaRecorder` / AudioContext) for voice commands.
- External language and vision model inference endpoints are available via configured API keys, with graceful local fallback when keys are absent.
