# Data Model: Security, Access Control & Quality Hardening

**Feature**: `001-security-access-hardening`
**Phase**: 1 — Design
**Date**: 2026-10-27

---

## Entities

### 1. `DownloadTicket` (NEW)

Short-lived, single-use token enabling browser-native file downloads without exposing the long-lived JWT.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK | Auto-generated |
| `Token` | `string(64)` | Required, unique, indexed | Cryptographically random (32 bytes, base64url) |
| `ResourceId` | `Guid` | FK ? Resources, Required | The resource this ticket grants access to |
| `UserId` | `Guid` | FK ? Users, Required | The requesting user |
| `OrganizationId` | `Guid` | Required | Denormalized for quick tenant check |
| `ExpiresAt` | `DateTime` | Required | UTC; 60 seconds from creation |
| `IsUsed` | `bool` | Required, default `false` | Set to `true` atomically when consumed |
| `CreatedAt` | `DateTime` | Required, default UTC now | |

**Validation Rules**:
- `ExpiresAt` must be in the future at validation time.
- `IsUsed` must be `false` at validation time. Once set true, any re-use returns 401.
- Stale records (`IsUsed = true` or `ExpiresAt < UTC now - 24h`) should be purged periodically (background job or on-demand).

**State Transitions**:
```
Created (IsUsed=false, ExpiresAt future)
    ? Consumed (IsUsed=true)  — on first successful download
    ? Expired (ExpiresAt past, IsUsed=false) — never consumed; treated as invalid
```

---

### 2. `Resource` (MODIFIED)

Existing entity. Changes in this feature:

| Field | Change | Before | After |
|-------|--------|--------|-------|
| `DirectoryId` | Type changed | `Guid` (required, `[Required]`) | `Guid?` (nullable) |

**Validation Rules** (updated):
- `DirectoryId` is nullable; null means "root level" (no parent directory).
- The FK constraint in EF Core must be updated: `IsRequired(false)` / `DeleteBehavior.SetNull`.

---

### 3. `ShareLink` (MODIFIED)

Existing entity. Changes in this feature:

| Field | Change | Notes |
|-------|--------|-------|
| `OrganizationId` | ADDED — `Guid`, required | Denormalized tenant marker for faster cross-org checks at link resolution |

**Validation Rules** (updated):
- `OrganizationId` must equal the creating user's organization at creation time.
- Any cross-organization resolution attempt returns 404.

---

### 4. `AuditLog` (REFERENCED, no structural change)

Existing entity used by this feature to record:
- Unauthorized download attempts (action: `UnauthorizedDownload`)
- Cross-org share link attempts (action: `PolicyViolation`)
- Ticket creation / consumption (action: `DownloadTicketIssued`, `DownloadTicketConsumed`)

---

## EF Core Migration Notes

Three migrations are required:

| Migration Name | Schema Change |
|----------------|--------------|
| `AddDownloadTickets` | Create `DownloadTickets` table |
| `MakeDirectoryIdNullable` | `ALTER COLUMN DirectoryId DROP NOT NULL` on `Resources` |
| `AddOrganizationIdToShareLinks` | `ADD COLUMN OrganizationId UUID NOT NULL DEFAULT gen_random_uuid()` + backfill from joined Resources |

> **Important**: The `ShareLinks.OrganizationId` backfill must be part of the migration data seed (`migrationBuilder.Sql(...)`) using:
> ```sql
> UPDATE "ShareLinks" sl
> SET "OrganizationId" = r."OrganizationId"
> FROM "Resources" r
> WHERE sl."ResourceId" = r."Id";
> ```

---

## Index Recommendations

| Table | Index | Purpose |
|-------|-------|---------|
| `DownloadTickets` | `(Token)` UNIQUE | O(1) ticket lookup |
| `DownloadTickets` | `(ExpiresAt, IsUsed)` | Efficient cleanup queries |
| `ShareLinks` | `(OrganizationId)` | Tenant-scoped listing |
