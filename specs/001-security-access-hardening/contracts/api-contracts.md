# API Contracts: Security, Access Control & Quality Hardening

**Feature**: `001-security-access-hardening`
**Phase**: 1 — Design
**Date**: 2026-10-27

All endpoints use JSON request/response bodies unless noted. All authenticated endpoints require `Authorization: Bearer <access_token>` header unless a `?ticket=` query parameter is accepted.

---

## New: Authenticated File Download

### `POST /api/files/{resourceId:guid}/ticket`
Issue a short-lived single-use download ticket for a browser `<a href>` download.

**Auth**: Required (Bearer JWT)

**Path Parameters**:
- `resourceId` — GUID of the resource to download

**Response `200 OK`**:
```json
{
  "ticket": "<64-char base64url token>",
  "expiresAt": "2026-10-27T10:01:00Z",
  "downloadUrl": "/api/files/{resourceId}/download?ticket=<token>"
}
```

**Errors**:
- `401 Unauthorized` — missing/invalid JWT
- `403 Forbidden` — resource belongs to a different organization, or directory read access denied
- `404 Not Found` — resource does not exist or is soft-deleted

---

### `GET /api/files/{resourceId:guid}/download`
Stream the authenticated file. Accepts Bearer JWT **or** a single-use `?ticket=` parameter.

**Auth**: Bearer JWT header **OR** `?ticket=<token>` query string

**Path Parameters**:
- `resourceId` — GUID of the resource

**Query Parameters** (ticket path only):
- `ticket` — single-use ticket token (max age 60 s)

**Response `200 OK`** (file stream):
- `Content-Type`: resource's stored MIME type
- `Content-Disposition`: `inline; filename="<name>"` for non-SVG; `attachment; filename="<name>"` for SVG
- `X-Content-Type-Options`: `nosniff` (always)
- `Content-Length`: file size in bytes

**Errors**:
- `401 Unauthorized` — no credential, expired/invalid ticket, reused ticket
- `403 Forbidden` — cross-org access
- `404 Not Found` — resource not found / soft-deleted
- `410 Gone` — ticket expired (distinct from invalid)

---

## Modified: Share Link Creation

### `POST /api/share`
Create a share link for an organization-scoped resource.

**Auth**: Required — `[Authorize(Policy = "CanShare")]`

**Request Body**:
```json
{
  "resourceId": "<guid>",
  "password": "<optional string>",
  "expiresAt": "<optional ISO-8601 datetime>",
  "maxDownloads": "<optional int>",
  "allowPreview": true,
  "requireEmail": false
}
```

**Changes from current**:
- Now enforces `[Authorize(Policy = "CanShare")]` (previously plain `[Authorize]`).
- Resource lookup now filters: `ResourceId == request.ResourceId AND OrganizationId == callerOrgId AND IsDeleted == false`.
- `ShareLink.OrganizationId` is recorded in the created row.

**Response `200 OK`** (unchanged shape):
```json
{
  "shareId": "<guid>",
  "token": "<token>",
  "url": "/s/<token>",
  "expiresAt": "<datetime|null>",
  "maxDownloads": "<int|null>",
  "hasPassword": false,
  "allowPreview": true,
  "requireEmail": false
}
```

**Errors**:
- `401 Unauthorized` — not authenticated
- `403 Forbidden` — `CanShare` policy check failed
- `404 Not Found` — resource not found in caller's organization

---

## Modified: Auth Endpoints — Rate Limiting

### `POST /api/auth/login`
### `POST /api/auth/2fa/verify`

**Rate Limit**: `[EnableRateLimiting("auth")]` — 10 requests per minute per IP.

**Additional Response Header** on limit breach:
- `Retry-After: <seconds>` (set by ASP.NET Core `RejectionStatusCode = 429` handler)

**No change to request/response shapes.**

---

## Modified: Audit Log Endpoints

### `GET /api/audit`
### `GET /api/audit/summary`
### `GET /api/audit/recent`

**Auth change**: `[Authorize(Roles = "Admin")]` (was: `[Authorize(Policy = "CanViewAuditLogs")]` = any authenticated user)

**No change to request/response shapes.**

---

## Upload Validation Changes (Internal — No Contract Shape Change)

### `POST /api/fileupload` (existing)
### `POST /api/chunkedupload` (existing)

**Behavioral changes (not shape changes)**:
- Server now reads first 16 bytes of the file stream to validate magic bytes before persisting.
- SVG files: content-scan first 4 KB for `<script`, `javascript:`, `on*=` patterns. Reject if found.
- `DirectoryId` stored as `null` (not `Guid.Empty`) when not provided.

**Additional error**:
- `400 Bad Request` — `{ "error": "File content does not match declared MIME type." }` when magic byte mismatch detected.
