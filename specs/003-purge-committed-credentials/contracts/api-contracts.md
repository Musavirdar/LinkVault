# Interface & Runtime Contracts: Credential Remediation

**Feature**: `003-purge-committed-credentials`  
**Date**: 2026-10-29  

## Runtime Validation Contract

### `StartupValidator.Validate`

Executes during application bootstrap in `Program.cs` before service registration.

```csharp
public static void Validate(IConfiguration configuration)
```

#### Invariants & Exceptions

| Precondition | Result | Error Message Format |
|---|---|---|
| Database connection string contains `QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg` | Throws `InvalidOperationException` | `FATAL: Insecure default database password detected: 'QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg'. Configure a unique database password via environment variables.` |
| Database connection string contains `linkvault123` | Throws `InvalidOperationException` | `FATAL: Insecure default database password detected: 'linkvault123'. Configure a unique database password via environment variables.` |
| JWT Secret equals `replace_this_with_a_very_long_secure_random_string_for_production` | Throws `InvalidOperationException` | `FATAL: Insecure default JWT secret placeholder detected: 'replace_this_with_a_very_long_secure_random_string_for_production'. Replace with a cryptographically secure key.` |
| JWT Secret length < 32 bytes | Throws `InvalidOperationException` | `FATAL: JWT secret key must be at least 256 bits (32 bytes) long for HMAC-SHA256.` |
| Non-blacklisted secrets >= 32 bytes | Returns cleanly | Startup continues to HTTP pipeline initialization |

---

## Git Index Contract

| File | Status | Expected Git Response |
|---|---|---|
| `backend/.env.render` | Untracked & Deleted | `git status` does not list file; `git check-ignore backend/.env.render` returns exit code 0 |
| Any `.env.*` (e.g. `.env.staging`) | Ignored | `git status` does not list untracked file |
| `.env.example` | Tracked | Allowed for sanitized template reference |

---

## Health Check Contract

```http
GET /health HTTP/1.1
Host: localhost:5000
```

### Response
```http
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```
- Must not output or expose any portion of connection strings, hostnames, or credentials.
