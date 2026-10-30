# Quickstart Validation Guide: Verifiable Workspace Agent (LinkVault Agent)

**Feature**: `004-verifiable-workspace-agent`  
**Date**: 2026-10-29  
**Status**: Draft  

---

## Prerequisites

1. .NET 8 SDK installed.
2. PostgreSQL 16 database running on port 5433 (or configured connection string).
3. Node.js 18+ and npm installed for frontend client.
4. Backend and Frontend running:
   ```powershell
   # Terminal 1 (Backend)
   cd backend
   dotnet run
   
   # Terminal 2 (Frontend)
   cd app
   npm run dev
   ```

---

## Scenario 1: Autonomous Planning & Plan Preview

Prove that submitting a natural language goal produces an approval-gated action plan.

1. Navigate to `http://localhost:5173/agent` and sign in.
2. In the LinkVault Agent prompt, enter:
   ```text
   Organize my loose invoices into an Invoices folder and share the latest invoice for 7 days
   ```
3. **Expected Outcome**:
   - The agent synthesizes a plan with 3 ordered steps:
     1. `create_directory` ("Invoices")
     2. `move_resource` (Invoice files → "Invoices")
     3. `create_share_link` (Invoice file → 7 days)
   - Status transitions to `AwaitingApproval`.
   - The UI displays an interactive Plan Preview card showing each proposed step with "Approve" and "Reject" buttons.
   - **No workspace files are moved yet.**

---

## Scenario 2: One-Click Approval & Live Real-Time Progress

Prove that clicking "Approve" executes the plan sequentially and broadcasts live status.

1. Click the green **Approve Plan** button on the Plan Preview card.
2. **Expected Outcome**:
   - The status updates to `Executing`.
   - Progress bar and step status icons transition in real time via SignalR:
     - Step 1: 🟡 Running → 🟢 Completed
     - Step 2: 🟡 Running → 🟢 Completed
     - Step 3: 🟡 Running → 🟢 Completed
   - Final status displays `Completed` with a summary message.
   - Navigating to **Doc Share Tree** (`/directories`) confirms the "Invoices" folder now contains the files.

---

## Scenario 3: Plan Rejection (Zero Mutation Verification)

Prove that rejecting a plan prevents all state changes.

1. Submit a command: `Move all files to an Archive folder`.
2. When the plan preview appears, click **Reject Plan**.
3. **Expected Outcome**:
   - Status updates to `Rejected`.
   - No new directory is created.
   - All files remain in their original directories.

---

## Scenario 4: Safe Share Guardrail (Credential Interception)

Prove that sharing a file containing exposed passwords or secrets is blocked.

1. Upload a text file named `config_secret.env` containing:
   ```text
   ConnectionStrings__DefaultConnection="Host=render.com;Password=SuperSecretPass123!;"
   ```
2. In the agent prompt, enter: `Share config_secret.env with the public`.
3. **Expected Outcome**:
   - Safe Share Guard executes pre-share scanning.
   - Secret pattern `Password=...` is detected.
   - The agent returns a `Blocked` alert:
     > 🛡️ **Safe Share Guard Alert**: File contains exposed credentials. Share link creation is blocked.
   - No share link is created in the database.

---

## Scenario 5: Verifiable Audit Telemetry (`via agent` Attribution)

Prove that all agent-executed actions are immutably attributed in the Audit Log.

1. Open the Audit page at `http://localhost:5173/audit`.
2. Inspect the most recent audit records created during Scenario 2.
3. **Expected Outcome**:
   - The description field shows `[via agent] Created directory 'Invoices'`.
   - The actor is listed as the authenticated user.
   - The `via agent` badge is visible on the audit event.
   - Filtering the audit view by "Agent Activity" displays all and only agent-orchestrated events.
