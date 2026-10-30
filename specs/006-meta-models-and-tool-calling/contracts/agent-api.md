# API Contracts: Meta Model Tool Calling & Verified Workspace Agent

**Feature**: `006-meta-models-and-tool-calling`  
**Date**: 2026-10-30  
**Status**: Completed  

---

## 1. REST Endpoints Summary

| Endpoint | Method | Auth | Description | Status |
|---|---|---|---|---|
| `/api/agent/plan` | `POST` | Bearer JWT + Rate Limited (10/min) | Generates multi-step plan using Meta Llama 3.3 tool calling | Active |
| `/api/agent/plan/{planId}` | `GET` | Bearer JWT | Retrieves existing plan state and execution steps | Active |
| `/api/agent/plan/{planId}/approve` | `POST` | Bearer JWT (Owner/Admin) | Atomically approves plan and executes background steps | Active |
| `/api/agent/plan/{planId}/reject` | `POST` | Bearer JWT (Owner/Admin) | Rejects plan and skips pending steps | Active |
| `/api/agent/safe-share/scan` | `POST` | Bearer JWT | Scans resource for exposed secrets prior to sharing | Active |
| `/api/files/{id}/ticket` | `POST` | Bearer JWT | Issues cryptographic single-use download ticket | Active |
| `/api/files/{id}/download?ticket={t}` | `GET` | Anonymous (Validated Ticket) | Streams file content using single-use ticket | Active |
| `/api/agent/transcribe` | `POST` | N/A | **REMOVED** (Voice handled via native browser Web Speech API) | **Deleted** |

---

## 2. Meta Chat Completions Payload Contract

When invoking the Meta model endpoint (`POST {BaseUrl}/chat/completions`):

### Request Body
```json
{
  "model": "meta-llama/Llama-3.3-70B-Instruct",
  "messages": [
    {
      "role": "system",
      "content": "You are the LinkVault Verifiable Workspace Agent..."
    },
    {
      "role": "user",
      "content": "Organize my uploads and share the financial report for 7 days"
    }
  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "create_directory",
        "description": "Create a new directory in the workspace",
        "parameters": {
          "type": "object",
          "properties": {
            "name": { "type": "string", "description": "Folder name" },
            "parentDirectoryId": { "type": "string", "description": "Optional parent directory GUID" }
          },
          "required": ["name"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "move_resource",
        "description": "Move a file or resource into a target directory",
        "parameters": {
          "type": "object",
          "properties": {
            "resourceId": { "type": "string", "description": "Resource GUID" },
            "targetDirectoryId": { "type": "string", "description": "Target directory GUID" }
          },
          "required": ["resourceId"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "create_share_link",
        "description": "Generate a secure share link for a resource",
        "parameters": {
          "type": "object",
          "properties": {
            "resourceId": { "type": "string", "description": "Resource GUID" },
            "expiryDays": { "type": "integer", "description": "Expiry in days" }
          },
          "required": ["resourceId"]
        }
      }
    }
  ],
  "tool_choice": "auto",
  "temperature": 0.1
}
```

### Expected Response Format
```json
{
  "id": "chatcmpl-12345",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "I will organize your files and generate a share link.",
        "tool_calls": [
          {
            "id": "call_1",
            "type": "function",
            "function": {
              "name": "create_directory",
              "arguments": "{\"name\":\"Finances\"}"
            }
          },
          {
            "id": "call_2",
            "type": "function",
            "function": {
              "name": "create_share_link",
              "arguments": "{\"resourceId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"expiryDays\":7}"
            }
          }
        ]
      },
      "finish_reason": "tool_calls"
    }
  ]
}
```

