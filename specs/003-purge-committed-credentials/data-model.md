# Data Model & Configuration Schemas: Credential Remediation

**Feature**: `003-purge-committed-credentials`  
**Date**: 2026-10-29  

## Configuration Entities & Validation Constraints

### 1. Database Connection Configuration (`ConnectionStrings:DefaultConnection`)

Connection string structure passed via environment variable `ConnectionStrings__DefaultConnection`:

| Field | Type | Required | Security Constraints |
|---|---|---|---|
| `Host` | string | Yes | Remote database hostname (e.g., Render/Postgres cluster) |
| `Port` | integer | Yes | Standard PostgreSQL port (`5432`) |
| `Database` | string | Yes | Isolated database name (e.g., `linkvault`) |
| `Username` | string | Yes | Non-default database user |
| `Password` | string | Yes | Must NOT match any entry in `InsecurePasswords` (`linkvault123`, `QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg`) |
| `SslMode` | string | Yes | Must be `Require` or `VerifyFull` for cloud databases |

### 2. JWT Authentication Configuration (`JwtSettings`)

Signing credentials passed via environment variables (`JwtSettings__*`):

| Setting | Type | Required | Security Constraints |
|---|---|---|---|
| `Secret` | string | Yes | Minimum 256 bits (32 UTF-8 bytes). Must NOT match any entry in `InsecurePlaceholders` (`replace_this_with_a_very_long_secure_random_string_for_production`, `linkvault-default-key...`) |
| `Issuer` | string | Yes | `LinkVaultApi` |
| `Audience` | string | Yes | `LinkVaultClient` |
| `ExpiryMinutes` | integer | No | Default `60` |
| `RefreshTokenExpiryDays` | integer | No | Default `7` |

### 3. Startup Validator Blacklists (`StartupValidator.cs`)

#### Insecure Passwords Blacklist
- `linkvault123` (development default)
- `QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg` (compromised Render Postgres password)

#### Insecure Placeholders Blacklist
- `your-super-secret-key-change-this-in-production`
- `secret`
- `changeme`
- `placeholder`
- `12345678901234567890123456789012`
- `linkvault-default-key-override-with-jwt-secret-in-production-env`
- `lv-dev-secret-key-a8f3b9c2e1d4a7f6b5c8e2d1f4a9b3c7`
- `your-super-secret-key-that-is-at-least-32-characters-long-for-jwt-signing`
- `replace_this_with_a_very_long_secure_random_string_for_production` (exposed Render placeholder)
- Partial matching patterns: `change-this-in-production`, `override-with-jwt-secret`, `replace_this_with_a_very_long_secure`
