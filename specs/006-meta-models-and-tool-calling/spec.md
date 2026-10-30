# Feature Specification: Meta Model API Integration, Tool Calling Specification & Submission Integrity

**Feature Branch**: `006-meta-models-and-tool-calling`  
**Created**: 2026-10-30  
**Status**: Draft  
**Input**: User description: "Still blocking: Still not Meta’s hackathon models. The config still points at Groq with llama-3.3-70b, llama-guard-3-8b, and llama-3.2-vision. The rules require the Meta Model API or Muse Spark Open or Glimmer. Stage 1 is pass/fail on this. Change the base URL and model names once you have an endpoint, which is the Baseten step from earlier. The model still can’t call tools. I found no tools array in MetaLlamaClient, so a real model returns no tool_calls, and every plan comes from the local keyword fallback. Nothing in this commit changes that. Voice is still fake on the backend. TranscribeAudioAsync still returns the same fixed sentence at 98% confidence. If VoiceCommandButton uses the browser’s Web Speech API, then voice works without it and you should delete the /transcribe endpoint. If it uploads audio to /transcribe, your demo shows a fake result. Check which one it uses. HACKATHON.md has claims that aren’t true. This matters, because the rules say misidentified work means disqualification. It says the baseline tag d69b3ed is 'before hackathon development.' That commit is from Oct 28, inside the event. Describe the baseline honestly: what existed on Oct 26, and that the security hardening was done during the event. It says 'all commits are untampered,' but the history was force-pushed earlier. Remove that line. The headline says Llama 3.3 does 'autonomous tool calling,' which isn’t happening yet (item 2). Describe what actually runs. It names the wrong download route (/api/files/ticket/{id}/stream; the real one is /api/files/{id}/ticket). The model table and headline will need rewriting after item 1."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real Autonomous Tool Calling via Meta Model API (Priority: P1) 🎯 MVP

As an authenticated workspace user, I want the AI workspace agent to send formal tool schemas in its completions request to the Meta model so that the model directly returns structured, atomic `tool_calls` for complex workspace goals, rather than relying exclusively on keyword fallback heuristics.

**Why this priority**: Without a formal `tools` definition array in the completions API payload, remote LLMs cannot execute function calling, causing all agent planning requests to silently downgrade to local fallback strings. Stage 1 evaluation requires genuine Meta model tool calling.

**Independent Test**: Send a multi-step user goal (e.g., "Create a folder named Reports, move file X into it, and create a share link for 7 days") to the plan generation service with a remote endpoint configured. The system delivers a completions payload containing the standard `tools` schema array and `tool_choice`, parses the assistant's structured `tool_calls` response, and saves the verified step plan without falling back to local heuristic strings.

**Acceptance Scenarios**:
1. **Given** a user inputs a natural language directive and the Meta model service is enabled, **When** the agent initiates plan generation, **Then** the request payload includes a formal `tools` array defining `create_directory`, `move_resource`, `create_share_link`, `search`, `list_directory`, and `delete_resource` with JSON schema properties.
2. **Given** the remote Meta model returns an assistant message with one or more `tool_calls`, **When** the response is parsed, **Then** the agent extracts each function name and arguments, passes them through schema validation, and creates the pending action plan.
3. **Given** the remote model endpoint is unreachable or returns an error, **When** plan generation is attempted, **Then** the agent gracefully activates the offline heuristic planner as an explicit resilience fallback while logging an informative diagnostic event.

---

### User Story 2 - Native Browser Voice Command Integrity (Priority: P1)

As a workspace user speaking voice directives, I want the microphone button to use native browser speech recognition directly in real-time, and I want any fake mock transcription endpoints deleted so that the application never produces deceptive or hardcoded demo transcripts.

**Why this priority**: Hackathon rules disqualify misleading demonstrations. Returning a fixed mock sentence from the backend when microphone audio is uploaded invalidates verifiability. Modern browsers provide native Web Speech API recognition; removing the fake backend endpoint guarantees authentic voice interactions.

**Independent Test**: Click the microphone button in a supported browser, speak an arbitrary prompt (e.g., "Find all spreadsheet files in finances"), and verify the spoken words appear directly in the agent text prompt without sending audio to a hardcoded backend mock endpoint.

**Acceptance Scenarios**:
1. **Given** a user clicks the microphone button in a browser supporting speech recognition, **When** they speak a command, **Then** the native browser speech recognition captures and populates the actual words spoken into the prompt field.
2. **Given** a browser without speech recognition support or when microphone permissions are denied, **When** the voice button is clicked, **Then** the interface clearly informs the user that browser speech recognition is unavailable or permitted, without faking results.
3. **Given** the application backend, **When** inspecting API routes, **Then** no mock `/transcribe` endpoint or hardcoded transcription response exists.

---

### User Story 3 - Hackathon Submission Honesty & Baseline Transparency (Priority: P1)

As a hackathon judge or repository reviewer, I want `HACKATHON.md` to accurately and transparently document the exact baseline starting point (Oct 26), explicitly credit security hardening as event work (Oct 27–29), accurately identify active Meta models and endpoints, remove inaccurate claims regarding commit history, and state real API endpoints.

**Why this priority**: Hackathon rules explicitly disqualify misidentified work. The baseline commit `d69b3ed` was authored on Oct 28 (inside the event), not before the hackathon. Honesty about what existed prior to Oct 26 vs. what was built during the event is critical to maintain submission eligibility.

**Independent Test**: Read `HACKATHON.md` and compare every statement against `git log` and route definitions. Verify that commit dates, force-push history disclosure, active Meta models/endpoints, and file ticket routes (`/api/files/{id}/ticket`) match the codebase exactly.

**Acceptance Scenarios**:
1. **Given** `HACKATHON.md`, **When** reviewing the baseline section, **Then** it clearly states that the pre-event baseline was the foundational LinkVault web app (pre-Oct 26), while the security hardening (download tickets, path traversal guards, proxy headers, credential scrub) and verifiable workspace agent were completed during the hackathon period.
2. **Given** `HACKATHON.md`, **When** reviewing the git history section, **Then** misleading phrases like "all commits are untampered" are removed, and the document transparently notes the commit progression.
3. **Given** `HACKATHON.md`, **When** reviewing API references, **Then** the download ticket issuance route is correctly documented as `POST /api/files/{id}/ticket`.
4. **Given** `HACKATHON.md`, **When** reviewing the agent architecture, **Then** the headline and model table accurately describe what runs, including Meta model tool calling and fallback mechanics.

---

### User Story 4 - Meta Hackathon Model Provider & Endpoint Configuration (Priority: P2)

As a DevOps engineer or deployer, I want the agent configuration to support Meta's hackathon models (Meta Model API, Muse Spark Open, Glimmer, or Baseten-hosted Meta models) with configurable base URLs and model names via environment variables and configuration files.

**Why this priority**: Hackathon Stage 1 evaluates whether Meta models are used. Default configurations pointing at third-party services like Groq fail Stage 1 compliance. Deployers must be able to point to Meta's authorized hackathon endpoints.

**Independent Test**: Configure `META_MODEL_BASE_URL` and `META_MODEL_API_KEY` to a Meta Model API or Baseten endpoint with model `meta-llama/Llama-3.3-70B-Instruct`; verify that outgoing HTTP requests connect to the specified Meta endpoint with appropriate authentication headers.

**Acceptance Scenarios**:
1. **Given** the backend configuration, **When** checking default settings, **Then** the configuration fields and environment variables explicitly support Meta Model API / Muse Spark / Baseten endpoints rather than vendor-locked third-party URLs.
2. **Given** `META_MODEL_BASE_URL`, `META_MODEL_API_KEY`, or `META_MODEL_NAME` environment variables are provided, **When** the service starts, **Then** the agent client respects these overrides.

---

### Edge Cases

- **Model returns text instead of tool calls**: If the Meta model returns a plain text response with no `tool_calls` despite tools being offered, the system informs the user or safely decomposes the goal via verified fallback.
- **Model calls a tool with invalid JSON**: If a tool call contains malformed JSON or invalid parameter types, the validator rejects the plan upfront with HTTP 400 Bad Request.
- **Browser speech recognition error or timeout**: If the user stays silent or browser speech recognition encounters an error (`no-speech`, `audio-capture`, `not-allowed`), the UI resets the microphone button state and displays a user-friendly message.
- **Offline / Disconnected environment during test runs**: When running automated CI/CD unit tests without network connectivity, the test suite uses mocked endpoints or verified fallbacks without failing or invoking remote networks.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The agent client MUST send a formal `tools` definition array in OpenAI/Meta chat completion requests, defining the functions: `create_directory`, `move_resource`, `create_share_link`, `search`, `list_directory`, and `delete_resource`.
- **FR-002**: Each tool definition in the `tools` array MUST include standard JSON schema definitions specifying parameter names, data types, descriptions, and required properties.
- **FR-003**: The chat completion payload MUST specify `"tool_choice": "auto"` to instruct the Meta model to produce structured tool calls when resolving workspace goals.
- **FR-004**: The agent client MUST inspect `choice.message.tool_calls` and deserialize the function name and JSON arguments into strongly-typed plan steps.
- **FR-005**: If the completions endpoint is not configured, unreachable, or returns an error status code, the agent MUST explicitly log the situation and fall back to local rule-based plan decomposition as a secondary resilience mechanism.
- **FR-006**: The `/api/agent/transcribe` endpoint and `TranscribeAudioAsync` method MUST be completely removed from the backend API, service interface, and implementation.
- **FR-007**: The frontend `VoiceCommandButton` MUST rely exclusively on browser-native speech recognition (`SpeechRecognition` / `webkitSpeechRecognition`) and MUST NOT submit audio blobs to any mock backend endpoint.
- **FR-008**: The frontend voice button MUST handle recognition errors and lack of browser support by presenting clear visual feedback to the user rather than faking a transcript.
- **FR-009**: The backend configuration (`appsettings.json` and `MetaLlamaConfig.cs`) MUST support Meta hackathon endpoints (Meta Model API, Muse Spark Open, Glimmer, or Baseten) with standard environment variables (`META_MODEL_BASE_URL`, `META_MODEL_API_KEY`, `META_MODEL_NAME`).
- **FR-010**: `HACKATHON.md` MUST honestly document the baseline status prior to the event (Oct 26), and identify the security hardening work (Oct 27–29) and workspace agent development (Oct 29–30) as hackathon achievements.
- **FR-011**: `HACKATHON.md` MUST remove any false statements stating commits are "untampered" and provide a factual description of repository history.
- **FR-012**: `HACKATHON.md` MUST correctly document the download ticket route as `POST /api/files/{id}/ticket` (and download endpoint `GET /api/files/{id}/download?ticket={token}`).
- **FR-013**: `HACKATHON.md` MUST accurately reflect the Meta models utilized and their operational roles.
- **FR-014**: All automated unit and integration tests MUST pass cleanly without regression.

---

### Key Entities

- **ToolDefinition**: Represents a tool exposed to the Meta model containing `type: "function"` and `function: { name, description, parameters }`.
- **MetaModelConfig**: Configuration parameters defining the target Meta model endpoint, API key, base URL, and model identifiers.
- **AgentActionPlan**: The persisted database entity tracking user goal, status (`AwaitingApproval`, `Executing`, `Completed`, `Failed`, `Rejected`), and associated `PlanStep` records.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of outgoing plan generation requests sent to the completions API include the formal `tools` definition array covering all 6 workspace tools.
- **SC-002**: 0 mock or hardcoded sentences remain in backend speech transcription code; the `/api/agent/transcribe` endpoint is eliminated.
- **SC-003**: 100% of factual statements in `HACKATHON.md` regarding baseline dates, event work, git history, and download ticket API routes are verifiable against the repository.
- **SC-004**: System successfully starts and operates when configured with Meta Model API / Baseten endpoint settings.
- **SC-005**: All existing and new automated unit tests (minimum 50+ tests) pass with zero failures.

---

## Assumptions

- Users accessing voice commands use modern Chromium, Safari, or Edge browsers that support the Web Speech API; users on unsupported browsers type their workspace directives into the prompt input box.
- The Meta model endpoint exposes a standard OpenAI/Meta-compatible `/chat/completions` API schema supporting the `tools` and `tool_choice` parameters.
- The pre-hackathon baseline code corresponds to the repository state before hackathon development commenced on Oct 26, 2026.

