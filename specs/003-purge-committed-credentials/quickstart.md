# Quickstart Validation Guide: Credential Remediation & Rotation

**Feature**: `003-purge-committed-credentials`  
**Date**: 2026-10-29  

This guide provides step-by-step verification procedures for validating that committed credentials have been purged, startup validation enforces blacklists, and cloud credentials are safely rotated.

---

## Scenario 1: Verify Git Untracking & `.gitignore`

Run from the repository root:

```powershell
# 1. Verify backend/.env.render does not exist in working tree
Test-Path backend/.env.render
# Expected: False

# 2. Verify backend/.env.render is ignored by Git
git check-ignore backend/.env.render
# Expected: backend/.env.render (matches .gitignore rule)

# 3. Create a temporary .env.render and verify git status ignores it
New-Item -ItemType File -Path backend/.env.render -Force | Out-Null
git status --porcelain backend/.env.render
# Expected: Empty output (file is completely ignored)
Remove-Item backend/.env.render -Force
```

---

## Scenario 2: Validate Rejection of Leaked Database Password

Run unit tests or run API with leaked password:

```powershell
# Run the StartupValidator unit test verifying rejection of leaked Render password
& "$env:LocalAppData\Microsoft\dotnet\dotnet.exe" test backend/LinkVaultApi.Tests/LinkVaultApi.Tests.csproj --filter "FullyQualifiedName~StartupValidationTests"
# Expected: All startup validation tests pass (including leaked password test)
```

Manual verification:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=example.com;Database=db;Username=u;Password=QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg;"
$env:JwtSettings__Secret = "a-valid-secure-key-that-is-at-least-32-characters-long"
& "$env:LocalAppData\Microsoft\dotnet\dotnet.exe" run --project backend/LinkVaultApi.csproj
# Expected: Throws InvalidOperationException: "FATAL: Insecure default database password detected: 'QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg'..."
```

---

## Scenario 3: Validate Rejection of Leaked Placeholder JWT Secret

Manual verification:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=linkvault;Username=postgres;Password=secure_dev_pass"
$env:JwtSettings__Secret = "replace_this_with_a_very_long_secure_random_string_for_production"
& "$env:LocalAppData\Microsoft\dotnet\dotnet.exe" run --project backend/LinkVaultApi.csproj
# Expected: Throws InvalidOperationException: "FATAL: Insecure default JWT secret placeholder detected: 'replace_this_with_a_very_long_secure_random_string_for_production'..."
```

---

## Scenario 4: Render Production Console Rotation Runbook

Execute in the [Render Dashboard](https://dashboard.render.com):

1. **Database Password Reset**:
   - Navigate to the PostgreSQL database instance.
   - Go to **Settings** → **Access Control**.
   - Click **Reset Password** (or provision a new database instance and delete the old one).
   - Copy the newly generated connection string (`postgres://...` or Key-Value format).

2. **Web Service Environment Update**:
   - Navigate to the LinkVault backend Web Service.
   - Open the **Environment** tab.
   - Set `ConnectionStrings__DefaultConnection` to the new connection string (include `SslMode=Require;` for ADO.NET / Npgsql).
   - Generate a fresh 256-bit JWT secret (e.g., using `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))`).
   - Set `JwtSettings__Secret` to this new secret, ensuring it does **not** match the placeholder.
   - Set `ASPNETCORE_FORWARDEDHEADERS_ENABLED` to `true` (enables platform reverse proxy forwarded headers so rate limiting works correctly without spoofing).
   - Click **Save Changes** and trigger a manual redeploy.

3. **Verify Deployment Health**:
   - Access `https://<your-render-app>.onrender.com/health`.
   - Confirm status returns HTTP `200 OK` (`Healthy`).
