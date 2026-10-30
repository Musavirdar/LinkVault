# API & Hub Contracts: Verifiable Workspace Agent (LinkVault Agent)

**Feature**: `004-verifiable-workspace-agent`  
**Date**: 2026-10-29  
**Status**: Draft  

---

## 1. REST Endpoints

### 1.1 `POST /api/agent/plan`
Generates an actionable multi-step plan based on natural language or voice directive.

- **Security**: Requires JWT (`Bearer <token>`). Operates under caller's `orgId` and RBAC claims.
- **Request Body**:
```json
{
  "userGoal": "Organize my uploads and share Q3 financial report with design team for 7 days",
  "preferredModel": "llama-3.3-70b-versatile"
}
```

- **Response Body** (`200 OK`):
```json
{
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userGoal": "Organize my uploads and share Q3 financial report with design team for 7 days",
  "status": "AwaitingApproval",
  "summary": "Found 3 Q3 files in root. Propose creating 'Finance/Q3' directory, moving files into it, and generating a 7-day share link.",
  "steps": [
    {
      "stepNumber": 1,
      "toolName": "create_directory",
      "parameters": { "name": "Q3", "parentDirectoryName": "Finance" },
      "requiresApproval": true,
      "description": "Create folder 'Finance/Q3'"
    },
    {
      "stepNumber": 2,
      "toolName": "move_resource",
      "parameters": { "resourceName": "Q3_Report.pdf", "targetDirectory": "Finance/Q3" },
      "requiresApproval": true,
      "description": "Move 'Q3_Report.pdf' to 'Finance/Q3'"
    },
    {
      "stepNumber": 3,
      "toolName": "create_share_link",
      "parameters": { "resourceName": "Q3_Report.pdf", "expiryDays": 7 },
      "requiresApproval": true,
      "description": "Create 7-day share link for 'Q3_Report.pdf'"
    }
  ],
  "requiresUserApproval": true
}
```

---

### 1.2 `POST /api/agent/plan/{planId}/approve`
Executes an approved plan step-by-step, streaming live updates over SignalR.

- **Security**: Requires JWT. Caller must own the plan.
- **Response Body** (`200 OK`):
```json
{
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Executing",
  "message": "Execution started. Streaming live step events to client."
}
```

---

### 1.3 `POST /api/agent/plan/{planId}/reject`
Cancels a proposed plan without executing any state changes.

- **Security**: Requires JWT. Caller must own the plan.
- **Response Body** (`200 OK`):
```json
{
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Rejected",
  "message": "Plan cancelled. No workspace modifications were made."
}
```

---

### 1.4 `POST /api/agent/safe-share/scan`
Pre-share credential and PII inspection endpoint.

- **Request Body**:
```json
{
  "resourceId": "8f3e2b1a-4c5d-6e7f-8a9b-0c1d2e3f4a5b"
}
```

- **Response Body** (`200 OK`):
```json
{
  "resourceId": "8f3e2b1a-4c5d-6e7f-8a9b-0c1d2e3f4a5b",
  "fileName": "db_config.json",
  "status": "Blocked",
  "riskScore": 95,
  "detectedThreats": [
    "PostgreSQL Connection String Password",
    "High-Entropy Secret Key"
  ],
  "canShare": false,
  "message": "Safe Share Guard blocked sharing: File contains exposed database credentials."
}
```

---

### 1.5 `POST /api/agent/transcribe`
Converts speech audio payload into text for voice commands.

- **Request**: Multipart form data with audio stream (`audio/webm` or `audio/wav`).
- **Response Body** (`200 OK`):
```json
{
  "transcribedText": "Organize my uploads and share the financial report",
  "confidence": 0.98,
  "durationSeconds": 3.4
}
```

---

## 2. SignalR Hub Events (`/hubs/agent`)

Clients join group `agentPlan_{planId}` upon plan creation to receive live progress events:

| Event Name | Payload | Description |
|---|---|---|
| `OnPlanStatusChanged` | `{ planId, status, message }` | Broadcast when plan transitions state |
| `OnStepStarted` | `{ planId, stepNumber, toolName, description }` | Fired when a step begins execution |
| `OnStepCompleted` | `{ planId, stepNumber, toolName, result }` | Fired when a step successfully completes |
| `OnStepFailed` | `{ planId, stepNumber, toolName, errorMessage }` | Fired if a step encounters an error |
| `OnPlanFinished` | `{ planId, status, completedSteps, totalSteps }` | Fired when all steps terminate |

---

## 3. Tool Function Calling Schema (Muse Spark / Llama 3.3)

```json
[
  {
    "type": "function",
    "function": {
      "name": "list_directory",
      "description": "List files and subdirectories within a directory",
      "parameters": {
        "type": "object",
        "properties": {
          "directoryId": { "type": "string", "description": "Optional directory ID. If omitted, lists root." }
        }
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "search",
      "description": "Search workspace resources by name, description, or keyword",
      "parameters": {
        "type": "object",
        "properties": {
          "query": { "type": "string", "description": "Search query" }
        },
        "required": ["query"]
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "create_directory",
      "description": "Create a new folder in the directory hierarchy",
      "parameters": {
        "type": "object",
        "properties": {
          "name": { "type": "string", "description": "Folder name" },
          "parentDirectoryId": { "type": "string", "description": "Optional parent folder ID" }
        },
        "required": ["name"]
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "move_resource",
      "description": "Move a file or link into a target directory",
      "parameters": {
        "type": "object",
        "properties": {
          "resourceId": { "type": "string", "description": "ID of resource to move" },
          "targetDirectoryId": { "type": "string", "description": "Target folder ID (null for root)" }
        },
        "required": ["resourceId"]
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "create_share_link",
      "description": "Create a secure share link for a file or link resource",
      "parameters": {
        "type": "object",
        "properties": {
          "resourceId": { "type": "string", "description": "ID of resource to share" },
          "expiryDays": { "type": "integer", "description": "Number of days until link expires" }
        },
        "required": ["resourceId"]
      }
    }
  }
]
```
