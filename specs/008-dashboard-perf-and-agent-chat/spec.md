# Feature Specification: Dashboard Performance & Fully Functional AI Agent Chat

**Feature Branch**: `008-dashboard-perf-and-agent-chat`

**Created**: 2026-11-04

**Status**: Draft

**Input**: User description: "the app feels very slow and laggy. when i open dashboard it takes time to load item like recent files etc second i try ai agent but it did nothing i give it a prompt like hello and other prompts but it did nothing and didn't gave any replies. I want resolve these issues make app fast and agent fully working 100 percent and did real work in the app"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Instant & Responsive Dashboard (Priority: P1)

Users opening or navigating to the Dashboard currently experience noticeable lag, an artificial 200ms page entry delay, and delayed pop-in of recent files caused by duplicate, uncoordinated network requests (`resourcesApi.getAll()` and `linksApi.getAll()` executed multiple times without caching or skeleton loaders).

Under this story, the Dashboard entry transition is instantaneous, network fetching for recent files and aggregate stats is consolidated and cached in memory, and sleek loading skeletons provide immediate feedback during initial load with zero layout shift or table animation stutter.

**Why this priority**: The Dashboard is the primary home screen of the application. Perceived sluggishness and delayed file pop-in diminish user confidence in the entire product.

**Independent Test**: Navigate from the sidebar or login directly to the Dashboard. Verify the page and canvas render immediately without artificial delays (< 250ms), skeleton placeholders display smoothly while data loads, and recent files appear promptly with smooth, non-blocking hover states.

**Acceptance Scenarios**:

1. **Given** an authenticated user navigating to `/dashboard`, **When** the page mounts, **Then** the main canvas renders immediately with zero artificial delay (`delay: 0`) and graceful fade-in.
2. **Given** the dashboard is loading recent files and stats for the first time, **When** data is in-flight, **Then** elegant skeleton placeholders display for the stats cards and recent files table instead of blank or jarring empty states.
3. **Given** dashboard data has been loaded once in the active session, **When** the user navigates away and returns to `/dashboard`, **Then** the cached recent files and stats render instantly while revalidating in the background.
4. **Given** the recent files table renders items, **When** hovering or clicking table rows, **Then** interactions are fluid with zero framer-motion layout thrashing or stutter.

---

### User Story 2 - Conversational AI Agent Chat & Immediate Responses (Priority: P1)

When a user submits a conversational prompt such as "hello", "what can you do?", or asks a question about their workspace files, the backend model processes the request and returns a detailed markdown response in `summary` with zero destructive steps (`steps: []`). Currently, the frontend checks `steps.length > 0` and completely discards or hides the response, making the screen appear as though nothing happened.

Under this story, the AI Agent interface features a true conversational chat stream: both user prompts and assistant responses are rendered as distinct message bubbles in a persistent thread, with typing indicators during model inference, rich markdown rendering (headings, bullet points, code blocks), and quick-action prompt suggestions.

**Why this priority**: The user explicitly reported that giving the agent "hello" and other prompts did nothing and gave no replies. Users must see clear, immediate conversational feedback for any prompt.

**Independent Test**: Navigate to `/agent`, type "hello" in the prompt box, and press Enter (or click Synthesize). Verify that the user's prompt appears immediately in the chat thread, a thinking indicator is shown while the model generates, and the assistant's formatted reply (greeting the user and summarizing available tools/workspace directories) is displayed clearly in the conversation feed.

**Acceptance Scenarios**:

1. **Given** a user on the `/agent` page, **When** they submit a conversational prompt (e.g., "hello", "what directories do I have?"), **Then** the user message is added to the chat stream immediately with a timestamp and avatar.
2. **Given** a prompt is being processed by the backend Meta model, **When** awaiting response, **Then** an animated thinking/generating indicator is visible in the chat feed with model status ("Thinking with Meta Llama...").
3. **Given** the Meta model returns a response with zero destructive steps (`steps: []`), **When** the response arrives, **Then** the assistant's markdown summary is rendered in full within the conversation stream, preserving lists, links, and bold text.
4. **Given** a user submits consecutive prompts in the same session, **When** multiple turns take place, **Then** all previous turns remain visible in the scrollable conversation feed in chronological order.

---

### User Story 3 - Verifiable Real-World Workspace Execution (Priority: P2)

When a user gives an action-oriented instruction (e.g., "Create a directory called Projects and move my vector kit file into it", "Group invoices into an Invoices folder", or "Scan config files with Safe Share Guard"), the agent must not only synthesize a verifiable plan but also present an intuitive inline action card with approval controls, execute real backend tools upon approval, stream live progress with SignalR events, and immediately reflect updated files/directories in the application state.

**Why this priority**: The user requested that the agent "did real work in the app". Autonomous workspace organization is the core value proposition of the LinkVault Workspace Agent.

**Independent Test**: In the agent chat, submit "Create a directory called Projects and move brand_assets_vector_kit.zip into it". Verify a verifiable plan card appears in the chat stream showing the step breakdown. Click "Approve & Execute", verify real-time step checkmarks update to Completed, and verify that navigating to `/directories` reveals the newly created "Projects" folder containing the file.

**Acceptance Scenarios**:

1. **Given** an action instruction requiring state changes, **When** the agent synthesizes a plan, **Then** an interactive Action Plan card appears directly in the conversation flow detailing each proposed step (tool name, parameters, target paths).
2. **Given** an action plan awaiting approval, **When** the user clicks "Approve Plan", **Then** the status transitions to "Executing" and SignalR events stream step progress (Pending → Running → Completed) live on screen.
3. **Given** an approved plan completes execution, **When** all steps succeed, **Then** the conversation shows a success summary and triggers background cache revalidation so the files, directories, and dashboard reflect the changes immediately without manual page refresh.
4. **Given** the user clicks "Reject Plan", **When** rejected, **Then** the plan status updates to "Rejected" in the feed, no destructive changes are made to the workspace, and the user can enter another command.

---

### User Story 4 - Robust Error Recovery & Guardrail Alerts (Priority: P3)

When an operation encounters a guardrail violation (e.g. Safe Share Guard blocking files containing API secrets or database credentials) or a network/timeout error, the user must receive clear, actionable in-stream diagnostics rather than silent failures or cryptic console errors.

**Why this priority**: Trustworthy autonomy requires transparent guardrails and resilience when upstream model inference or backend operations fail.

**Independent Test**: Request the agent to share a file flagged with sensitive credentials. Verify the Safe Share Guard modal/card displays the risk score and detected threats, explaining why the action was prevented.

**Acceptance Scenarios**:

1. **Given** a step that triggers Safe Share Guard, **When** blocked by the security scanner, **Then** a prominent security alert card appears in the chat detailing the threat (e.g. exposed credentials) and advice on how to sanitize the file.
2. **Given** a network interruption or temporary model timeout, **When** a request fails, **Then** an inline retry button allows re-submitting the prompt without retyping.

---

### Edge Cases

- **Empty / Whitespace-only prompts**: Disable submit button and ignore Enter key if prompt is blank or whitespace-only.
- **Rapid consecutive submissions**: Prevent overlapping plan creation requests by disabling submit button while a request is actively processing.
- **Large conversation feeds**: Conversation container must automatically scroll to the latest message on new inputs while maintaining manual scrollability for reading past messages.
- **Page refresh during execution**: Persist current active plan status in component/store state so refreshing or navigating does not orphan in-progress actions.
- **Zero recent files in new workspace**: Display a friendly empty state ("No files uploaded yet - upload your first file to get started") with a one-click upload shortcut instead of a broken empty table.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST eliminate artificial delays (`delay: 0.2s`) in `MainCanvas` and layout wrappers to enable instant route transitions.
- **FR-002**: System MUST consolidate dashboard data fetching and cache recent files and statistics in memory to prevent duplicate HTTP calls on mount.
- **FR-003**: System MUST provide skeleton loaders for dashboard stats, action cards, and the recent files table during initial data retrieval.
- **FR-004**: System MUST render an interactive conversation thread in the AI Agent interface displaying both user messages and assistant responses chronologically.
- **FR-005**: System MUST render conversational agent responses (where `steps` is empty) directly in the chat feed with full Markdown formatting and code block highlighting.
- **FR-006**: System MUST render action plans with pending steps (`steps.length > 0`) as interactive, verifiable action cards directly in the conversation flow with one-click "Approve" and "Reject" buttons.
- **FR-007**: System MUST connect to the Agent SignalR Hub and stream real-time step execution progress (Started, Completed, Failed) directly on the inline plan card.
- **FR-008**: System MUST execute real workspace modifications (`create_directory`, `move_resource`, `create_share_link`, `delete_resource`, `classify_image`) upon user approval.
- **FR-009**: System MUST trigger automatic cache revalidation across files, directories, and dashboard data when an agent action plan finishes executing, ensuring changes appear across the app without manual refresh.
- **FR-010**: System MUST display an animated typing indicator while awaiting model inference, indicating the active model name (`meta-models/muse-spark-open`).
- **FR-011**: System MUST provide suggested prompt chips (e.g., "List all files", "Organize invoices into Invoices folder", "Scan config files") that populate the prompt box and can be submitted with a single click.
- **FR-012**: System MUST handle Safe Share Guard alerts gracefully, displaying risk score and threat details when sensitive resources are intercepted.

### Key Entities

- **AgentMessage**: Represents a single message in the conversation thread. Attributes: `id` (string), `role` ('user' | 'assistant'), `content` (string), `timestamp` (Date), `plan` (optional `AgentActionPlanDto`), `isThinking` (boolean).
- **DashboardCache**: In-memory cache holding aggregated stats (`totalLinks`, `activeLinks`, `totalFiles`, `totalClicks`, `totalViews`) and `recentFiles` with a 30-second TTL and immediate background revalidation.
- **AgentActionPlanDto**: Verifiable plan entity holding `planId`, `userGoal`, `status`, `summary`, `steps` array, and approval metadata.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Dashboard initial navigation renders within **300ms** on desktop, eliminating the previous 600ms+ delay from artificial transitions and duplicate network calls.
- **SC-002**: Conversational prompts (such as "hello" or questions) produce a visible, formatted assistant response in the chat feed **100% of the time** (0 discarded responses).
- **SC-003**: Actionable workspace commands (e.g., folder creation, file moving) generate verifiable steps and execute live upon approval with real changes reflected in the workspace **100% of the time**.
- **SC-004**: Redundant network calls on `/dashboard` mount reduced from **4 separate calls** down to consolidated, cached retrieval with 0 duplicate endpoints invoked.
- **SC-005**: All unit and end-to-end tests for dashboard and agent pass with **0 regressions**.

## Assumptions

- The Meta Model inference endpoint (`meta-models/muse-spark-open` on Baseten) is operational and `META_MODEL_API_KEY` is configured in development settings.
- SignalR connection to `/hubs/agent` is available when authenticated and falls back to polling/refresh if websockets are temporarily unavailable.
- In-memory caching for dashboard stats uses standard client-side state (Zustand or lightweight module cache) with cache invalidation triggered on upload/delete/agent execution.

