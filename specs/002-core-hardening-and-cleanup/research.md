# Technical Research: Core Hardening, Secret Protection & Repository Restructure

**Feature**: `002-core-hardening-and-cleanup`  
**Date**: 2026-10-28  
**Status**: Completed  

---

## 1. Repository Root Alignment & Scaffolding Hygiene

### Decision
Promote the entire contents of `LinkVault-main/` (`app/`, `backend/`, `sharedb1.sln`, `docker-compose.yml`, `screenshots/`, `README.md`) to the repository root. Delete the nested `LinkVault-main/` folder and the duplicate `LinkVault-main/.github/` directory. Ignore tool scaffolding (`.specify/`, `.agents/`, `AGENTS.md`) and delete non-repository tracked presentation/scratch files (`LinkVault_Walkthrough.pptx`, `ponytail.md`, `info.md`).

### Rationale
- GitHub only executes workflows located in the root `.github/workflows/`. The nested `.github/` was duplicate and redundant.
- Nested folders cause path drift in IDEs, CI runners, build scripts, and Docker mounts.
- Non-code presentation files (`.pptx`, scratch `.md`) bloat git history and create clutter.
- Scaffolding tools like `.specify/` and `.agents/` are development assistants; ignoring them in `.gitignore` keeps the repository clean for judges and production while preserving local assistant usability.

### Alternatives Considered
- Keep nested structure and maintain fallback paths: Rejected because it permanently complicates paths and causes confusing repository navigation.

---

## 2. Zero-Secret Configuration & Enhanced Startup Validation

### Decision
- Remove hardcoded secrets from `appsettings.json`, `appsettings.Development.json`, and `docker-compose.yml`. Replace with empty strings or non-secret template indicators.
- Require runtime secrets (`JwtSettings:Secret`, `ConnectionStrings:DefaultConnection`, Postgres passwords) to be set via environment variables in production.
- Update `StartupValidator.cs` to explicitly block:
  - Strings matching known committed defaults: `"linkvault-default-key-override-with-jwt-secret-in-production-env"`, `"lv-dev-secret-key-a8f3b9c2e1d4a7f6b5c8e2d1f4a9b3c7"`, `"linkvault123"`, `"your-super-secret-key..."`.
  - Keys with length < 32 bytes (256 bits).
  - Empty or localhost-only production database connection strings.

### Rationale
- Hardcoded secrets in source control constitute CWE-798 (Use of Hard-coded Credentials).
- Blacklisting known previous secrets guarantees that old committed credentials cannot be reused accidentally.

### Alternatives Considered
- Relying solely on key length: Rejected because a 64-character default placeholder passes a length check if not explicitly rejected by value.

---

## 3. Chunked Upload Security Validation

### Decision
In [ChunkedUploadController](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/LinkVault-main/backend/Controllers/ChunkedUploadController.cs), invoke `IFileValidationService.ValidateFileContent(assembledStream, fileName, contentType)` immediately after chunk concatenation and before committing the file to storage or inserting records into `LinkVaultDbContext`. If validation fails, immediately delete the assembled file and temporary chunk files, and return HTTP 400 Bad Request.

### Rationale
- Chunked uploads assemble arbitrary byte chunks on disk. Without validation, an attacker could bypass the magic byte checks on standard upload endpoints by splitting a PE/ELF binary or malicious SVG into chunks.
- Validating the assembled file stream ensures consistent security enforcement across all ingress paths.

### Alternatives Considered
- Validating individual chunks: Rejected because magic bytes and full script tags span across chunk boundaries.

---

## 4. Atomic Download Ticket Consumption & State Checks

### Decision
- In [FilesController](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/LinkVault-main/backend/Controllers/FilesController.cs), replace the read-then-write ticket consumption with an atomic database query:
  ```csharp
  var updatedRows = await _dbContext.DownloadTickets
      .Where(t => t.Id == ticketGuid && !t.IsUsed && t.ExpiresAtUtc > DateTime.UtcNow)
      .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsUsed, true));
  
  if (updatedRows == 0) return NotFound("Invalid or expired download ticket");
  ```
- Before streaming, join or query the target `Resource` and `User` to verify:
  - `resource.IsActive && !resource.IsDeleted`
  - `user.IsActive && !user.IsDeleted`
  - If either is false, return HTTP 404/410.

### Rationale
- Eliminates race conditions where two simultaneous HTTP requests consume the same single-use ticket concurrently.
- Prevents downloading a file that was deleted or revoked during the 60-second ticket lifetime.
- Prevents deactivated employees from using tickets generated prior to deactivation.

### Alternatives Considered
- In-memory lock (`lock` or `SemaphoreSlim`): Rejected because it fails in multi-instance or load-balanced environments; database-level atomic updates work across all replicas.

---

## 5. Reverse-Proxy-Aware Rate Limiting (`UseForwardedHeaders`)

### Decision
Configure `ForwardedHeadersOptions` in [Program.cs](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/LinkVault-main/backend/Program.cs):
```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { /* clear to allow reverse proxy forwarding */ },
    KnownProxies = { /* clear to allow container environments */ }
});
```
Position `app.UseForwardedHeaders()` at the very top of the middleware pipeline before CORS, rate limiting, and authentication.

### Rationale
- Platforms like Render, AWS ALB, Cloudflare, and Nginx terminate TLS and proxy requests. Without forwarded headers, `HttpContext.Connection.RemoteIpAddress` resolves to the reverse proxy's internal IP (e.g. `10.x.x.x` or `127.0.0.1`).
- All users would share one rate limit bucket, causing denial-of-service for legitimate users when one client is throttled.

### Alternatives Considered
- Parsing `X-Forwarded-For` manually in the rate limiter: Rejected because `UseForwardedHeaders` is ASP.NET Core's standard, battle-tested solution that also correctly sets scheme, host, and client IP.

---

## 6. Comprehensive SVG Content Scanning

### Decision
Update `FileValidationService.ValidateSvgContent`:
- Read the entire SVG stream/bytes into memory (since SVG uploads are subject to the 10 MB maximum file size limit).
- Convert to lowercase text and inspect for `<script`, `javascript:`, `onload=`, `onerror=`, `onclick=`, `<animate`, `<foreignobject`, and embedded CDATA script blocks.
- If any match is detected, return false.

### Rationale
- An attacker can easily pad an SVG file with 4 KB of comments (`<!-- padding -->`) and place their payload at byte 4097. Scanning only the first 4 KB gave a false sense of security.

### Alternatives Considered
- Drop SVG support entirely: Full scanning provides safe SVG support without sacrificing user flexibility for avatars and icons.

---

## 7. Product-First Documentation Hierarchy

### Decision
Restructure [README.md](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/README.md):
- **Hero & Value Proposition**: Mission, branding, live badges.
- **Visuals & Screenshots**: Product screenshots, core user journeys.
- **Key Capabilities**: Smart Directories, Team Workspaces, Secure Vault, SSO, SignalR Chat.
- **Tech Stack & Architecture**: Overview of React, .NET 8, PostgreSQL.
- **Quickstart**: Running locally in minutes.
- **Security & Quality Appendix**: Documenting Spec-Driven Development, constitution, and hardened security controls.

### Rationale
- Evaluators and hackathon judges prioritize understanding what the product does and how it looks within the first 10 seconds of landing on the repository.
