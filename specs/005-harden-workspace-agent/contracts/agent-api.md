# API Contracts: Workspace Agent Security & Concurrency Hardening

**Feature**: `005-harden-workspace-agent`  
**Date**: 2026-10-30  
**Status**: Draft  

---

## 1. REST Endpoints

### 1.1 `POST /api/agent/plan`
Generates an actionable multi-step plan. Rate-limited to 10 requests per minute per user.

- **Rate Limiting**: `[EnableRateLimiting("agent_plan")]` (10 requests/min/user). Exceeding limit returns `429 Too Many Requests`.
- **Validation**: Rejects plan if any tool arguments fail schema or if steps count > 10.
- **Error Responses**:
  - `400 Bad Request`: `{"error": "Validation failed: Tool 'move_resource' is missing required parameter 'resourceId'."}`
  - `429 Too Many Requests`: `{"error": "Too many requests. Please slow down.", "retryAfterSeconds": 60}`

---

### 1.2 `POST /api/agent/plan/{planId}/approve`
Atomically transitions plan from `AwaitingApproval` to `Executing` and dispatches execution within an isolated service scope.

- **Security**: Caller must own the plan or be Admin.
- **Success Response** (`200 OK`):
  ```json
  {
    "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Executing",
    "message": "Plan approved. Live step updates streaming."
  }
  ```
- **Conflict Response** (`409 Conflict`):
  ```json
  {
    "error": "Plan is not in AwaitingApproval status or has already been processed."
  }
  ```
- **Forbidden Response** (`403 Forbidden`): Caller is not the plan owner and not an Admin.

---

### 1.3 `POST /api/agent/plan/{planId}/reject`
Cancels an awaiting plan without state changes.

- **Security**: Caller must own the plan or be Admin.
- **Success Response** (`200 OK`):
  ```json
  {
    "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Rejected",
    "message": "Plan rejected. Workspace state unmodified."
  }
  ```
- **Conflict Response** (`409 Conflict`): Plan is not in `AwaitingApproval` status.
- **Forbidden Response** (`403 Forbidden`): Caller does not own the plan and is not Admin.

