# Research & Architecture Decisions: Workspace Agent Security & Concurrency Hardening

**Feature**: `005-harden-workspace-agent`  
**Date**: 2026-10-30  
**Status**: Completed  

---

## 1. Concurrency Safety & Background Execution Lifecycles

### Context & Challenge
Currently, `ApprovePlanAsync` executes `Task.Run` while referencing the HTTP request's scoped `LinkVaultDbContext`, and `ExecutePlanStepsAsync` has `using var scope = _dbContext`. In ASP.NET Core, once the HTTP request completes and returns the response, the request-level DI scope is disposed, disposing `_dbContext`. If the background task is still running or begins running, it throws `ObjectDisposedException` or concurrent DbContext access exceptions.

### Decision
- **Isolated Service Scope**: Inject `IServiceScopeFactory` into `VaultAgentService`. Inside `Task.Run`, call `using var scope = _scopeFactory.CreateScope()` to generate an isolated, dedicated dependency scope.
- **Service Resolution**: Resolve fresh, independent instances of `LinkVaultDbContext`, `IHubContext<AgentHub>`, `ISafeShareGuard`, `IStorageProvider`, and `ILogger` from `scope.ServiceProvider`.
- **Remove Invalid `using var scope = _dbContext`**: Dispose only the created `IServiceScope`, letting DI clean up the resolved scoped DbContext and services when background steps finish.

---

## 2. Atomic Approval & Ownership Gating

### Context & Challenge
If a user rapidly double-clicks "Approve", or if two clients concurrently approve the same plan, multiple background execution loops can be spawned simultaneously, causing duplicate directory creation and conflicting resource moves.

### Decision
- **Conditional Database Update**: Use EF Core's `ExecuteUpdateAsync` with an atomic filter:
  ```csharp
  var affected = await dbContext.AgentActionPlans
      .Where(p => p.Id == planId && p.OrganizationId == orgId && p.Status == PlanStatus.AwaitingApproval)
      .ExecuteUpdateAsync(s => s
          .SetProperty(p => p.Status, PlanStatus.Executing)
          .SetProperty(p => p.ApprovedAt, DateTime.UtcNow));
  ```
- **Conflict Response (HTTP 409)**: If `affected == 0`, the plan was either already approved, rejected, completed, or belongs to another organization. Return HTTP 409 Conflict.
- **Ownership & Status Verification on Rejection**:
  - Rejection checks that caller is the creator (`plan.UserId == userId`) OR in the `Admin` role.
  - Rejection also atomically verifies `Status == PlanStatus.AwaitingApproval`.

---

## 3. Removal of Unsafe Defaults & Tenant Isolation

### Context & Challenge
Previously, if a model omitted `resourceId` in `move_resource` or `create_share_link`, the code attempted fallback to `await _dbContext.Resources.FirstOrDefaultAsync(r => r.OrganizationId == orgId && !r.IsDeleted)`, picking the first arbitrary file in the tenant. This creates severe accidental modification/sharing risks. Furthermore, entity IDs were not strictly checked against the caller's `OrganizationId`.

### Decision
- **Fail Fast on Missing IDs**: If `resourceId` is missing, unparseable, or empty, the step immediately fails with a descriptive validation error.
- **Strict Tenant Scoping**:
  - `Resources`: Verified with `r.Id == resourceId && r.OrganizationId == orgId && !r.IsDeleted`.
  - `Directories`: Verified with `d.Id == directoryId && d.OrganizationId == orgId`.
  - `Parent Directories`: Verified with `d.Id == parentDirectoryId && d.OrganizationId == orgId`.
  - Any entity not found within the caller's organization immediately fails the step without modifying workspace state.

---

## 4. Fine-Grained RBAC Claim Verification

### Context & Challenge
The agent must not allow restricted roles (e.g. `Viewer` who has `CanUpload = false`, `CanUpdate = false`, `CanShare = false`) to bypass permission checks.

### Decision
- **Claim Names Matching `Program.cs` Policies**:
  - `create_directory`: Requires `canUpload` == "true" / "True" OR role == "Admin".
  - `move_resource`: Requires `canUpdate` == "true" / "True" OR role == "Admin".
  - `create_share_link`: Requires `canShare` == "true" / "True" OR role == "Admin".
  - `delete_resource`: Requires `canDelete` == "true" / "True" OR role == "Admin".
- **Step-Level Enforcement**: If the claim is missing, the tool execution halts immediately with a clear error: `"Permission denied: Missing '{claimName}' claim required to execute '{toolName}'."`

---

## 5. Standardized Share Link Provisioning

### Context & Challenge
`create_share_link` must follow the same security controls as `ShareController` rather than ad-hoc entity instantiation.

### Decision
- Populate tenant `OrganizationId = orgId`.
- Set secure crypto token from `ShareLink` default (`Guid.NewGuid().ToString("N")[..12]`).
- Honor optional `password` (hashed) and `maxDownloads`.
- Validate expiration date within policy boundaries.

---

## 6. Model Output Schema Validation & Plan Boundaries

### Context & Challenge
Raw LLM output can be malformed, missing properties, or hallucinate deep step trees.

### Decision
- Use `JsonDocument.Parse` and `TryGetProperty` for all tool parameter parsing.
- Validate per-tool schemas:
  - `create_directory`: requires non-empty `name` (max 100); optional `parentDirectoryId` must be valid Guid.
  - `move_resource`: requires valid `resourceId` (Guid); optional `targetDirectoryId` must be valid Guid.
  - `create_share_link`: requires valid `resourceId` (Guid); optional `expiryDays` (1-365).
- If any tool invocation in the plan fails validation, reject the whole plan and return HTTP 400 Bad Request.
- Hard limit: Cap all action plans at 10 steps.

