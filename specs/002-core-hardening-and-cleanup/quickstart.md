# Quickstart Validation Guide: Core Hardening & Security

**Feature**: `002-core-hardening-and-cleanup`  
**Date**: 2026-10-28  

This guide provides end-to-end verification steps for the core hardening, atomic ticket consumption, chunked upload validation, and reverse proxy rate limiting features.

---

## Prerequisites

- .NET 8 SDK
- Local PostgreSQL instance (running on port `5433` with database `linkvault`)
- Repository promoted to root: `sharedb1.sln`, `backend/`, `app/` at top level.

---

## Scenario 1: Root Layout & CI Validation

### Steps
```powershell
# 1. Verify root layout contains solution and projects directly
Test-Path sharedb1.sln, backend\LinkVaultApi.csproj, app\package.json
# Expected: True, True, True

# 2. Build entire solution from root
dotnet build sharedb1.sln --configuration Release
# Expected: Build succeeded (0 errors, 0 warnings)

# 3. Verify no nested LinkVault-main directory
Test-Path LinkVault-main
# Expected: False
```

---

## Scenario 2: Secret Rejection at Startup

### Steps
```powershell
# 1. Run with committed default JWT key
$env:JwtSettings__Secret = "linkvault-default-key-override-with-jwt-secret-in-production-env"
dotnet run --project backend/LinkVaultApi.csproj
# Expected: Startup terminates immediately with InvalidOperationException: Insecure default JWT key detected

# 2. Run with valid 32+ byte secret
$env:JwtSettings__Secret = "super-secret-rotated-jwt-key-for-local-testing-purposes-12345"
dotnet run --project backend/LinkVaultApi.csproj
# Expected: Application started listening on configured port
```

---

## Scenario 3: Chunked Upload Security Validation

### Steps
1. Initiate chunked upload session for `malicious.exe` renamed to `payload.pdf`.
2. Upload chunks via `POST /api/chunked-upload/chunk`.
3. Complete chunked upload via `POST /api/chunked-upload/complete`.
4. **Expected**: HTTP 400 Bad Request (`File content validation failed`). The assembled executable is discarded, and no Resource is added to the database.

---

## Scenario 4: Atomic Single-Use Ticket Concurrency

### Steps
1. Generate download ticket for a valid file: `POST /api/files/{id}/ticket`.
2. Simultaneously dispatch 5 concurrent HTTP requests to `GET /api/files/{id}/download?ticket={ticketId}`.
3. **Expected**: Exactly 1 request receives HTTP 200 with the file stream. The remaining 4 requests receive HTTP 404 (`Invalid, expired, or already-used download ticket`).

---

## Scenario 5: Soft-Deleted Resource & Inactive User Check

### Steps
1. Generate download ticket for a valid resource.
2. Soft-delete the resource (`IsDeleted = true`).
3. Attempt to fetch file with ticket: `GET /api/files/{id}/download?ticket={ticketId}`.
4. **Expected**: HTTP 404 (`Resource not found or inactive`).

---

## Scenario 6: Reverse Proxy Rate Limiting

### Steps
```bash
# Client 1: Send 11 rapid requests with X-Forwarded-For: 203.0.113.1
# 10 requests return 200/401; 11th request returns 429 Too Many Requests (Retry-After: 60)

# Client 2: Send request with X-Forwarded-For: 198.51.100.2
# Expected: Returns 200/401 normally (unaffected by Client 1's lockout)
```
