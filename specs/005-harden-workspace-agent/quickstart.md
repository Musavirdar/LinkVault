# Quickstart Validation Guide: Workspace Agent Security & Concurrency Hardening

**Feature**: `005-harden-workspace-agent`  
**Date**: 2026-10-30  
**Status**: Draft  

---

## Scenario 1: Double Approval Atomic Concurrency Guard

Prove that concurrent duplicate approval requests return HTTP 409 Conflict without duplicate execution.

1. Send `POST /api/agent/plan` to synthesize an awaiting approval plan.
2. Send two concurrent `POST /api/agent/plan/{planId}/approve` requests simultaneously.
3. **Expected Outcome**:
   - Exactly one request returns `200 OK` with status `Executing`.
   - The other request returns `409 Conflict`.
   - Exactly one background execution task runs.

---

## Scenario 2: Cross-Tenant Resource Access Prevention

Prove that tools cannot touch resources outside the caller's organization.

1. Create Resource A under Organization 1 (`org1`).
2. Authenticate as User 2 in Organization 2 (`org2`).
3. Execute `move_resource` or `create_share_link` targeting Resource A's ID.
4. **Expected Outcome**:
   - The step fails immediately with `"Resource not found or belongs to another organization"`.
   - Zero modifications or share links are created.

---

## Scenario 3: RBAC Claim Enforcement (Viewer Role Block)

Prove that users lacking required claims cannot execute state-altering tools.

1. Authenticate as a user with Viewer role (all permission claims false).
2. Attempt to approve or execute a plan with `create_directory` or `move_resource`.
3. **Expected Outcome**:
   - Tool execution fails with: `"Permission denied: Missing 'canUpload' permission required to create directories."`

---

## Scenario 4: Missing Resource ID Rejection (No Arbitrary Defaults)

Prove that omitting resource IDs fails fast instead of picking the first available file.

1. Submit a tool call to `move_resource` with `{ "targetDirectoryId": "..." }` and no `resourceId`.
2. **Expected Outcome**:
   - Step validation halts with error: `"resourceId is required"`.
   - The first file in the tenant remains untouched.

---

## Scenario 5: Rate Limiting Enforcement

Prove that exceeding 10 plan requests per minute results in HTTP 429.

1. Loop 11 calls to `POST /api/agent/plan` within 60 seconds.
2. **Expected Outcome**:
   - The first 10 calls succeed.
   - The 11th call returns `429 Too Many Requests` with a `Retry-After: 60` header.

