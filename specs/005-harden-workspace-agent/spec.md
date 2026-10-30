# Feature Specification: Workspace Agent Security & Concurrency Hardening

**Feature Branch**: `005-harden-workspace-agent`

**Created**: 2026-10-30

**Status**: Draft

**Input**: User description: "Fix these issues in backend/Agent (VaultAgentService, MetaLlamaClient, AgentController). Do not change unrelated code. 1. Background execution: ApprovePlanAsync runs Task.Run using the request-scoped DbContext, and ExecutePlanStepsAsync does `using var scope = _dbContext`. Inject IServiceScopeFactory, create a new scope inside the task, resolve a fresh DbContext and services there, and remove the `using var scope = _dbContext` line. 2. Atomic approval: only allow approval when status is AwaitingApproval. Use ExecuteUpdateAsync to set Executing where Id == planId AND OrganizationId == orgId AND Status == AwaitingApproval; if 0 rows changed, return 409. Reject must also check that the caller owns the plan or is Admin, and only work on AwaitingApproval plans. 3. Remove unsafe defaults: in move_resource and create_share_link, if resourceId is missing or invalid, fail the step. Never pick 'the first resource'. Verify resourceId, targetDirectoryId and parentDirectoryId all belong to the caller's organization. 4. Permissions: before each state-changing tool, check the user's claims (canUpload for create_directory, canUpdate for move_resource, canShare for create_share_link, canDelete for delete_resource). Fail the step with a clear message if missing. Use the same claim names as in Program.cs policies. 5. Sharing: create_share_link must reuse the existing ShareController logic (set OrganizationId, same token generation, expiry, optional password and download limit) instead of building ShareLink by hand. 6. Validate model output: parse tool arguments with TryGetProperty, validate against a per-tool schema, and reject the whole plan if any step is invalid. Cap plans at 10 steps. 7. Add [EnableRateLimiting] on POST /api/agent/plan with a per-user limit of 10 per minute. 8. Add tests for: double approval, approving another user's plan, Viewer trying move/share, missing resourceId, and a resource from another org."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Concurrency-Safe Isolated Background Execution & Atomic Plan Gating (Priority: P1)

As a workspace administrator or user, I want agent plan approvals and rejections to be completely atomic and executed within isolated background service scopes, so that concurrent requests cannot trigger duplicate executions and background processing never fails due to disposed HTTP request contexts.

**Why this priority**: Concurrency hazards and request-scoped lifecycle mismatches cause unhandled database exceptions, resource corruption, and double-execution anomalies under concurrent user interactions.

**Independent Test**: Trigger two concurrent approval requests on the same plan. The first request transitions the plan to Executing; the second immediately returns a 409 Conflict. Background execution completes in an independent service scope without touching disposed request services.

**Acceptance Scenarios**:

1. **Given** an action plan with status `AwaitingApproval`, **When** the owning user approves the plan, **Then** the plan status is atomically updated to `Executing` via conditional update, and step execution is dispatched within a fresh, isolated dependency injection scope.
2. **Given** an action plan already approved, executing, or rejected, **When** another approval request is received for the same plan ID, **Then** zero rows are updated, and the system immediately returns an HTTP 409 Conflict error.
3. **Given** an action plan, **When** a user attempts to reject it who neither owns the plan nor holds an Admin role, **Then** the system rejects the operation with an authorization denial.
4. **Given** a plan that is not in `AwaitingApproval` status, **When** a rejection is attempted, **Then** the system rejects the request without modifying workspace state.

---

### User Story 2 - Zero-Trust Tenant Isolation & Removal of Unsafe Defaults (Priority: P1)

As a security auditor and workspace member, I want agent tools to strictly validate that every resource and directory ID explicitly exists and belongs to my organization, so that cross-tenant access is prevented and missing IDs fail fast instead of guessing or picking arbitrary default files.

**Why this priority**: Implicit default fallbacks (such as selecting "the first resource" when an ID is absent) create critical data corruption risks where unintended files are moved, renamed, or publicly shared.

**Independent Test**: Execute `move_resource` or `create_share_link` with a missing resource ID or a resource ID belonging to a different organization. The tool must immediately fail the step with an informative error message, making zero mutations.

**Acceptance Scenarios**:

1. **Given** an agent step to move or share a resource, **When** the `resourceId` parameter is missing, invalid, or omitted, **Then** the step immediately fails with a descriptive validation error without falling back to any existing file.
2. **Given** an agent step targeting a resource or directory, **When** the specified entity ID belongs to a different tenant organization, **Then** the system flags a tenant boundary violation and halts the step.
3. **Given** a folder creation or move action specifying `parentDirectoryId` or `targetDirectoryId`, **When** the directory does not belong to the caller's organization, **Then** the action is rejected.

---

### User Story 3 - Role-Based Claim Verification for Agent Tools (Priority: P1)

As an organization administrator, I want the agent to enforce the caller's fine-grained permission claims prior to executing each state-altering tool, so that restricted users (such as Viewers) cannot bypass workspace security boundaries through agent automation.

**Why this priority**: Users must never achieve higher privilege through autonomous agents than they possess through manual interactive controls.

**Independent Test**: Authenticate as a user with a Viewer role (`CanUpload = false`, `CanUpdate = false`, `CanShare = false`). Request the agent to move a file or generate a share link. The agent verifies claims prior to each tool and fails the step due to missing permissions.

**Acceptance Scenarios**:

1. **Given** a user lacking the `canUpload` claim, **When** the agent attempts to execute `create_directory`, **Then** the step fails with a clear permission denied message.
2. **Given** a user lacking the `canUpdate` claim, **When** the agent attempts to execute `move_resource`, **Then** the step fails with a clear permission denied message.
3. **Given** a user lacking the `canShare` claim, **When** the agent attempts to execute `create_share_link`, **Then** the step fails with a clear permission denied message.
4. **Given** a user lacking the `canDelete` claim, **When** the agent attempts to execute `delete_resource`, **Then** the step fails with a clear permission denied message.

---

### User Story 4 - Robust Model Schema Validation & Plan Length Boundaries (Priority: P2)

As a platform engineer, I want all tool calls generated by external or local models to be strictly validated against rigid per-tool parameter schemas and capped at 10 steps, so that malformed LLM outputs are rejected upfront before any plan is saved or presented.

**Why this priority**: Prevents runaway planning loops, hallucinated parameter schemas, and partial inconsistent plans from reaching users.

**Independent Test**: Provide an instruction that induces a plan with invalid argument types or exceeding 10 steps. The planning engine rejects the plan before storing it.

**Acceptance Scenarios**:

1. **Given** model-generated tool calls, **When** any tool invocation fails required parameter schema checks, **Then** the entire plan is rejected, and an informative error is returned.
2. **Given** a directive producing more than 10 tool steps, **When** the plan is synthesized, **Then** the system caps the plan at 10 steps or rejects the excess steps.

---

### User Story 5 - Standardized Share Link Creation & Rate Limiting (Priority: P2)

As a security-conscious administrator, I want agent-created share links to fully inherit production link security (organization scoping, secure tokens, expirations, passwords, download limits) and want plan creation endpoints to be rate-limited per user.

**Why this priority**: Ensures security parity across sharing mechanisms and defends AI generation endpoints against denial-of-service or quota exhaustion.

**Independent Test**: Measure request frequency on `POST /api/agent/plan`; verify that exceeding 10 requests per minute results in an HTTP 429 rate limit response. Verify that generated share links populate `OrganizationId` and adhere to production policies.

**Acceptance Scenarios**:

1. **Given** an approved `create_share_link` step, **When** executed, **Then** the resulting share record includes `OrganizationId`, validated expiration, and secure token entropy matching standard links.
2. **Given** an authenticated user sending more than 10 plan requests in a 60-second window, **When** the 11th request arrives, **Then** the system throttles the request with HTTP 429 Too Many Requests.

---

## Edge Cases

- **Concurrent Plan Approval**: Two browser tabs click "Approve" at the exact same millisecond. Only one succeeds; the second receives HTTP 409 Conflict.
- **Tenant Entity Probing**: A malicious prompt attempts to move a file from another organization into the caller's folder. The query fails to find the entity within the caller's tenant and rejects the action.
- **Partial Claim Ownership**: User has `canUpload` but not `canUpdate`. A plan with directory creation and resource move creates the directory, but halts at the move step with a permission error.
- **Malformed JSON Tool Arguments**: The LLM outputs unparseable JSON or missing property keys. `TryGetProperty` detects the missing properties and invalidates the plan before database insertion.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST execute background agent plan execution inside an isolated `IServiceScope` created from `IServiceScopeFactory`, resolving fresh database contexts and scoped services independently of the incoming HTTP request.
- **FR-002**: System MUST atomically transition plan status from `AwaitingApproval` to `Executing` using conditional update logic (`Id == planId AND OrganizationId == orgId AND Status == AwaitingApproval`), returning HTTP 409 Conflict if zero rows are updated.
- **FR-003**: System MUST verify that only the plan creator or a user with the `Admin` role can approve or reject a plan, and rejections MUST only succeed for plans currently in `AwaitingApproval` status.
- **FR-004**: System MUST strictly validate that `resourceId`, `targetDirectoryId`, and `parentDirectoryId` belong to the caller's `OrganizationId`, immediately failing the step if an entity does not exist or belongs to another tenant.
- **FR-005**: System MUST eliminate all arbitrary entity selection fallbacks (e.g., selecting the first resource when ID is omitted) and fail the step when required entity identifiers are absent.
- **FR-006**: System MUST enforce caller RBAC permission claims prior to executing state-altering tools (`canUpload` for `create_directory`, `canUpdate` for `move_resource`, `canShare` for `create_share_link`, `canDelete` for `delete_resource`), failing the step if the claim is false or missing.
- **FR-007**: System MUST validate all model-synthesized tool parameters against per-tool schemas using safe property extraction (`TryGetProperty`), rejecting the entire plan if any tool call contains invalid arguments.
- **FR-008**: System MUST enforce a maximum limit of 10 atomic tool steps per action plan.
- **FR-009**: System MUST apply per-user rate limiting of 10 plan synthesis requests per minute on `POST /api/agent/plan`.
- **FR-010**: System MUST configure `create_share_link` tool execution to associate `OrganizationId` and respect standard link configuration parameters.

### Key Entities

- **AgentActionPlan**: Bounded action plan entity with strict state transitions (`AwaitingApproval` -> `Executing` / `Rejected`).
- **PlanStep**: Individual atomic operation containing validated tool parameters and step status.
- **ShareLink**: Secure sharing record containing tenant `OrganizationId`, token, expiration, and download limits.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of concurrent double-approval attempts return HTTP 409 Conflict on duplicate requests with zero duplicate background executions.
- **SC-002**: 100% of tool invocations referencing cross-tenant or non-existent entity IDs fail without mutating any workspace resources.
- **SC-003**: 100% of state-altering tool invocations by users lacking required RBAC claims are blocked prior to resource modification.
- **SC-004**: Rate limiting reliably throttles plan generation exceeding 10 requests per minute per user.
- **SC-005**: All background execution tasks run to completion without encountering `ObjectDisposedException` or concurrent DbContext access errors.

## Assumptions

- User identity, organization boundary (`orgId`), and permission claims (`canUpload`, `canUpdate`, `canShare`, `canDelete`) are present in the caller's validated JWT token.
- Existing database tables (`AgentActionPlans`, `PlanSteps`, `Resources`, `Directories`, `ShareLinks`) support standard Entity Framework Core operations.
- The system uses ASP.NET Core built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`).

