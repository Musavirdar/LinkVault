# Research: Security, Access Control & Quality Hardening

**Feature**: `001-security-access-hardening`
**Phase**: 0 — Research
**Date**: 2026-10-27

---

## 1. Authenticated File Download — Hybrid Ticket/Bearer Pattern

### Decision
Implement a two-path authenticated download endpoint at `GET /api/files/{resourceId}/download`.

- **Path A — Bearer JWT** (SPA / API consumers): Standard `Authorization: Bearer <token>` header. The endpoint validates the JWT, resolves the user's `OrganizationId` claim, and checks the resource's organization scope and the directory-level read permission before streaming via `IStorageProvider.GetAsync`.
- **Path B — Single-use Ticket** (browser `<a href>` / `<iframe>` that cannot attach headers): A separate endpoint `POST /api/files/{resourceId}/ticket` generates a short-lived (60 s) opaque token stored in a new `DownloadTickets` database table. The download endpoint accepts `?ticket=<token>`, validates expiry and single-use, then streams the file. The ticket row is marked `IsUsed = true` atomically before streaming.

### Rationale
ASP.NET Core's `JwtBearerEvents.OnMessageReceived` could support query-string tokens, but repurposing the access token as a download URL defeats single-use semantics and leaks the full JWT in server logs. A dedicated short-lived ticket avoids both problems while remaining transparent to native browser `<a download>` links.

### Alternatives Considered
- **Signed pre-signed URL (HMAC)**: Stateless but requires shared secret distribution and cannot enforce single-use without a Redis/DB state store — same complexity, less flexibility.
- **Cookie-based auth**: Not viable for cross-origin API downloads without `SameSite=None; Secure` and CORS credential headers — fragile.
- **Access token in query string**: Simple but exposes long-lived credential in logs, referrer headers, and browser history.

---

## 2. Rate Limiting on Auth Endpoints — ASP.NET Core Built-in Middleware

### Decision
Apply a dedicated named policy `"auth"` with `PermitLimit = 10` and `Window = 1 minute` to `POST /api/auth/login` and `POST /api/auth/2fa/verify` using `[EnableRateLimiting("auth")]`. This tightens the per-endpoint limit from the global 100/min down to 10/min for credential endpoints.

### Rationale
The general `fixed` limiter (100 req/min) is too permissive for credential endpoints. ASP.NET Core 8.0 natively supports multiple named policies from a single `AddRateLimiter` registration. Tagging endpoints with `[EnableRateLimiting("auth")]` is idiomatic and requires zero extra packages.

### Alternatives Considered
- **Middleware-level blanket limiter**: Too broad; would throttle API consumers doing rapid legitimate reads.
- **IP-based sliding window via Redis**: More accurate for distributed deployments but adds operational overhead not yet needed.

---

## 3. SVG / Magic Byte Upload Validation

### Decision
Add server-side magic byte inspection in `FileService` before accepting the upload stream. Read the first 16 bytes and compare against a known signature table. For SVGs (text-based, no magic bytes), perform a lightweight scan of the first 4 KB for `<script`, `javascript:`, and `on` event handler patterns.

Serve SVGs with `Content-Disposition: attachment; filename="..."` and `X-Content-Type-Options: nosniff` at the download endpoint — not at the static file layer (which is being removed).

### Rationale
The MIME type supplied by the browser (`file.ContentType`) is trivially spoofed. Reading magic bytes (PNG: `89 50 4E 47`, PDF: `25 50 44 46`) gives deterministic type verification. SVG content scanning is the OWASP-recommended approach.

### Alternatives Considered
- **NuGet: `MimeDetective` / `FileSignatures`**: Mature library approach; acceptable fallback if the magic byte table grows large but adds a dependency.
- **Rejecting SVG entirely**: Spec resolution chose "Force Download/Attachment"; content scan + attachment disposition is safe.

---

## 4. Secrets Validation at Startup

### Decision
Add a `ValidateConfiguration` static method called from `Program.cs` before `app.Run()`. It reads `JwtSettings:Secret` and throws `InvalidOperationException` when:
1. The key is null or empty.
2. UTF-8 byte length < 32 bytes (per HMAC-SHA256 minimum).
3. The key matches any known-placeholder: `"your-super-secret"`, `"changeme"`, `"dev"`, `"default"`.

`ConnectionStrings:DefaultConnection` containing the dev default `Password=linkvault123` logs a `Critical` warning (not hard-stop, allowing container override via environment `DATABASE_URL`).

### Rationale
Startup validation is .NET's idiomatic fail-fast pattern. Crashing before accepting traffic is unambiguous signal to the operator.

### Alternatives Considered
- **`IOptions<T>.ValidateOnStart`**: Cleaner architecture; valid future improvement but requires Options refactor out of scope for this sprint.
- **CI secret scanning only**: Catches committed secrets but does not prevent runtime misconfiguration.

---

## 5. Cross-Tenant Resource Isolation in ShareController

### Decision
Replace `_db.Resources.FindAsync(request.ResourceId)` with a scoped query:
```csharp
var resource = await _db.Resources
    .FirstOrDefaultAsync(r => r.Id == request.ResourceId
                           && r.OrganizationId == orgId
                           && !r.IsDeleted);
```
Add `[Authorize(Policy = "CanShare")]` to `CreateShareLink`. Add `OrganizationId` to `ShareLink` model to record tenant at creation time.

### Alternatives Considered
- **Row-level security at DB**: More robust long-term but outside .NET-only scope.
- **Resource ownership table**: Overkill given the existing `OrganizationId` FK on Resources.

---

## 6. Audit Log Role Restriction

### Decision
Change `AuditController` class-level attribute from `[Authorize(Policy = "CanViewAuditLogs")]` (which maps to `RequireAuthenticatedUser` — any logged-in user) to `[Authorize(Roles = "Admin")]`. Remove the `CanViewAuditLogs` policy from `Program.cs`.

---

## 7. Directory FK — Nullable Guid

### Decision
Change `Resource.DirectoryId` from `Guid` (required) to `Guid?` (nullable) in `Resource.cs`. Update `FileUploadController` to assign `DirectoryId = directoryId` (nullable) instead of `directoryId ?? Guid.Empty`. Add an EF Core migration.

### Rationale
`Guid.Empty` as a sentinel FK is a design smell. NULL is the correct relational expression for "no parent directory."

---

## 8. Legacy Package Cleanup

### Decision
Remove from `LinkVaultApi.csproj`:
- `Microsoft.AspNetCore.SignalR 1.1.0` — built into ASP.NET Core 8 SDK.
- `Microsoft.AspNetCore.Cors 2.2.0` — built into ASP.NET Core 8 SDK.

No code changes needed — all usages in `Program.cs` are provided by the framework.

---

## 9. Automated Test Project

### Decision
Create `backend/LinkVaultApi.Tests/` as an xUnit project using `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory<Program>` for integration tests and `Moq` for unit test mocks.

| Test | Type | Spec Requirement |
|------|------|-----------------|
| `DownloadEndpoint_AnonymousUser_Returns401` | Integration | SC-001 |
| `DownloadEndpoint_CrossTenant_Returns403` | Integration | SC-001 |
| `CreateShareLink_NoPolicyUser_Returns403` | Integration | SC-002 |
| `CreateShareLink_CrossTenant_Returns404` | Integration | SC-002 |
| `Login_BruteForce_Returns429` | Integration | SC-003 |
| `Upload_MimeSpoofed_Returns400` | Unit | SC-004 |
| `Startup_MissingJwtSecret_ThrowsException` | Unit | SC-005 |
| `AuditLog_MemberRole_Returns403` | Integration | US-6 |
| `ShareLink_Expired_Returns410` | Integration | Edge case |
| `DownloadTicket_Reuse_Returns401` | Integration | Edge case |

---

## 10. CI Pipeline

### Decision
Add `.github/workflows/ci.yml` that runs:
1. `dotnet test` on `backend/LinkVaultApi.sln` (or `backend/`)
2. `npm ci && npm run build` on the `app/` frontend

Triggered on `push` and `pull_request` to `main`.

### Rationale
No CI pipeline configuration was found in the repository root. FR-010 mandates CI verification for both frontend and backend.
