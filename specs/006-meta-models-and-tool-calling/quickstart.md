# Quickstart & Verification: Meta Model Tool Calling & Submission Integrity

**Feature**: `006-meta-models-and-tool-calling`  
**Date**: 2026-10-30  
**Status**: Ready  

---

## Scenario 1: Verify Meta Model Tool Calling Request Payload
Verify that the agent client sends a formal `tools` array in the request body to `/chat/completions`.

```csharp
// In MetaLlamaClient.cs:
// The request body contains:
// - tools: Array of 6 tools (create_directory, move_resource, create_share_link, search, list_directory, delete_resource)
// - tool_choice: "auto"
```

1. Configure endpoint or execute unit test: `dotnet test backend/LinkVaultApi.Tests/LinkVaultApi.Tests.csproj --filter FullyQualifiedName~ToolCalling`.
2. Inspect the serialized HTTP request payload.
3. **Expected**: Payload contains `tools` array with schemas and `"tool_choice": "auto"`.

---

## Scenario 2: Verify Tool Call Extraction into Plan Steps
Verify that when the remote model responds with `tool_calls`, the agent extracts them into actionable plan steps.

1. Send POST request to `/api/agent/plan`:
   ```bash
   curl -X POST http://localhost:5000/api/agent/plan \
     -H "Authorization: Bearer <TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"userGoal": "Create folder Reports and move document into it"}'
   ```
2. **Expected**: The returned plan contains steps parsed directly from `tool_calls` with status `AwaitingApproval`.

---

## Scenario 3: Verify Native Browser Voice Recognition
Verify that speech dictation runs through the browser's Web Speech API without sending audio to a fake mock backend.

1. Open the `/agent` page in Chrome, Safari, or Edge.
2. Click the microphone icon.
3. Speak: *"List files in workspace root"*.
4. **Expected**:
   - The browser's native speech recognizer transcribes the actual spoken words in real time into the prompt box.
   - Network tab shows NO POST requests to `/api/agent/transcribe`.

---

## Scenario 4: Verify Fake Backend Transcribe Endpoint Removal
Verify that the mock `/api/agent/transcribe` route has been deleted.

1. Send a POST request to `/api/agent/transcribe`:
   ```bash
   curl -X POST http://localhost:5000/api/agent/transcribe
   ```
2. **Expected**: Returns **HTTP 404 Not Found**.

---

## Scenario 5: Verify HACKATHON.md Submission Integrity
Verify that `HACKATHON.md` contains accurate, truthful claims.

1. Verify pre-hackathon baseline is documented as pre-Oct 26.
2. Verify security hardening (download tickets, path traversal, proxy headers, credential purge) is explicitly identified as Oct 27–29 event work.
3. Verify "all commits are untampered" statement is removed.
4. Verify file ticket routes are correctly listed as `POST /api/files/{id}/ticket` and `GET /api/files/{id}/download?ticket={token}`.
5. Verify Meta model table lists Meta Model API / Baseten endpoints and model names.

