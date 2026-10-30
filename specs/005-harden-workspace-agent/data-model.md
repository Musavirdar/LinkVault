# Data Model: Workspace Agent Security & Concurrency Hardening

**Feature**: `005-harden-workspace-agent`  
**Date**: 2026-10-30  
**Status**: Draft  

---

## 1. Domain Entities & State Transitions

### 1.1 AgentActionPlan State Invariants

| Status | Valid Prior Statuses | Transition Trigger | Concurrency Guard |
|---|---|---|---|
| `Draft` | None | Initial LLM synthesis | None |
| `AwaitingApproval` | `Draft` | Plan schema validated & steps <= 10 | Created in request scope |
| `Executing` | `AwaitingApproval` only | User / Admin clicks Approve | `ExecuteUpdateAsync` where `Status == AwaitingApproval` |
| `Rejected` | `AwaitingApproval` only | User / Admin clicks Reject | Atomic ownership + status check |
| `Completed` | `Executing` | All steps terminate successfully | Isolated background scope |
| `Failed` | `Executing` | Any step throws or permission denied | Isolated background scope |

### 1.2 Step Execution Invariants

| Tool Name | Required Claim | Target Entity Validation | Unsafe Default Allowed? |
|---|---|---|---|
| `create_directory` | `canUpload` | `parentDirectoryId` belongs to caller's org | **No** (reject if invalid) |
| `move_resource` | `canUpdate` | `resourceId` AND `targetDirectoryId` belong to caller's org | **No** (never pick first file) |
| `create_share_link` | `canShare` | `resourceId` belongs to caller's org; SafeShare check passes | **No** (never pick first file) |
| `delete_resource` | `canDelete` | `resourceId` belongs to caller's org | **No** (never pick first file) |
| `search` / `list_directory` | None (Read) | Scoped strictly by `OrganizationId` | Scoped to root if omitted |

### 1.3 Tool Argument Schemas

```json
{
  "create_directory": {
    "type": "object",
    "required": ["name"],
    "properties": {
      "name": { "type": "string", "minLength": 1, "maxLength": 100 },
      "parentDirectoryId": { "type": "string", "format": "guid", "nullable": true }
    }
  },
  "move_resource": {
    "type": "object",
    "required": ["resourceId"],
    "properties": {
      "resourceId": { "type": "string", "format": "guid" },
      "targetDirectoryId": { "type": "string", "format": "guid", "nullable": true }
    }
  },
  "create_share_link": {
    "type": "object",
    "required": ["resourceId"],
    "properties": {
      "resourceId": { "type": "string", "format": "guid" },
      "expiryDays": { "type": "integer", "minimum": 1, "maximum": 365, "default": 7 },
      "password": { "type": "string", "nullable": true },
      "maxDownloads": { "type": "integer", "minimum": 1, "nullable": true }
    }
  }
}
```

