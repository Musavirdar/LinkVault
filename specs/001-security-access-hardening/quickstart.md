# Quickstart Validation Guide: Security, Access Control & Quality Hardening

**Feature**: `001-security-access-hardening`
**Phase**: 1 — Design
**Date**: 2026-10-27

This guide documents runnable validation scenarios that prove each acceptance scenario end-to-end. See [data-model.md](data-model.md) and [contracts/api-contracts.md](contracts/api-contracts.md) for entity and endpoint details.

---

## Prerequisites

- Docker Compose up: `docker compose up -d` from repo root
- Backend running at `http://localhost:5000`
- Frontend accessible at `http://localhost:5173`
- `curl` or a REST client (Postman / HTTPie) available
- Two test organizations (Org A, Org B) seeded; two users (Admin, Member) in Org A

---

## Scenario 1: Unauthenticated Upload Access Returns 401/404 (SC-001)

### Setup
Note a valid `StoragePath` from any existing resource (e.g., `uploads/user-id/file.pdf`).

### Validation Steps
```bash
# 1a. Direct static URL — must be gone / return 404 (static mount removed)
curl -i http://localhost:5000/uploads/any-path/file.pdf
# Expected: 404 Not Found (no static mount)

# 1b. Authenticated download endpoint — unauthenticated
curl -i http://localhost:5000/api/files/<resource-guid>/download
# Expected: 401 Unauthorized
```

### Pass Criteria
- `1a` returns 404 (no static file server).
- `1b` returns 401.

---

## Scenario 2: Cross-Organization Download Returns 403 (SC-001)

### Setup
Obtain `TOKEN_ORG_B` (a valid JWT for a user in Org B). Note a `RESOURCE_ID` that belongs to Org A.

### Validation Steps
```bash
curl -i -H "Authorization: Bearer $TOKEN_ORG_B" \
  http://localhost:5000/api/files/$RESOURCE_ID/download
# Expected: 403 Forbidden
```

---

## Scenario 3: Single-Use Download Ticket (US-1, Scenario 3)

### Validation Steps
```bash
# Step 1: Issue a ticket
TICKET=$(curl -s -X POST \
  -H "Authorization: Bearer $TOKEN_ORG_A" \
  http://localhost:5000/api/files/$RESOURCE_ID/ticket | jq -r '.ticket')

# Step 2: Use the ticket — must succeed
curl -i "http://localhost:5000/api/files/$RESOURCE_ID/download?ticket=$TICKET"
# Expected: 200 OK with file stream, X-Content-Type-Options: nosniff

# Step 3: Reuse the ticket — must fail
curl -i "http://localhost:5000/api/files/$RESOURCE_ID/download?ticket=$TICKET"
# Expected: 401 Unauthorized
```

---

## Scenario 4: Share Link CanShare Policy + Cross-Tenant Isolation (SC-002)

```bash
# 4a. Member without CanShare — attempt to create share link
curl -i -X POST \
  -H "Authorization: Bearer $TOKEN_MEMBER" \
  -H "Content-Type: application/json" \
  -d "{\"resourceId\": \"$RESOURCE_ID\"}" \
  http://localhost:5000/api/share
# Expected: 403 Forbidden

# 4b. Org B user trying to share Org A resource
curl -i -X POST \
  -H "Authorization: Bearer $TOKEN_ORG_B" \
  -H "Content-Type: application/json" \
  -d "{\"resourceId\": \"$RESOURCE_ID_ORG_A\"}" \
  http://localhost:5000/api/share
# Expected: 404 Not Found
```

---

## Scenario 5: Auth Rate Limiting Returns 429 (SC-003)

```bash
# Send 11 login attempts rapidly
for i in $(seq 1 11); do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{"email":"x@x.com","password":"wrong"}' \
    http://localhost:5000/api/auth/login)
  echo "Request $i: $STATUS"
done
# Expected: first 10 return 200 or 401, 11th returns 429
# Response headers on 429 must include: Retry-After
```

---

## Scenario 6: SVG Upload — Force Attachment Download (US-4, Scenario 2)

```bash
# Upload an SVG file
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -F "file=@test.svg;type=image/svg+xml" \
  http://localhost:5000/api/fileupload

RESOURCE_ID_SVG=<id from response>

# Download — must be attachment
curl -i -H "Authorization: Bearer $TOKEN_ADMIN" \
  http://localhost:5000/api/files/$RESOURCE_ID_SVG/download
# Expected headers:
#   Content-Disposition: attachment; filename="test.svg"
#   X-Content-Type-Options: nosniff
```

---

## Scenario 7: Magic Byte Mismatch Rejection (SC-004)

```bash
# Rename an EXE to photo.png and attempt upload
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -F "file=@malicious.exe;filename=photo.png;type=image/png" \
  http://localhost:5000/api/fileupload
# Expected: 400 Bad Request
# Body: {"error": "File content does not match declared MIME type."}
```

---

## Scenario 8: Startup Fails Without JWT Secret (SC-005)

```bash
# Run backend container without JWT secret
docker run --rm \
  -e "ConnectionStrings__DefaultConnection=..." \
  linkvault-api
# Expected: Process exits with non-zero code immediately
# Logs must contain: "FATAL" or "InvalidOperationException" and "JwtSettings:Secret"
```

---

## Scenario 9: Audit Log Admin-Only (US-6)

```bash
# Member role access — must be denied
curl -i -H "Authorization: Bearer $TOKEN_MEMBER" \
  http://localhost:5000/api/audit
# Expected: 403 Forbidden

# Admin role access — must succeed
curl -i -H "Authorization: Bearer $TOKEN_ADMIN" \
  http://localhost:5000/api/audit
# Expected: 200 OK with audit log entries
```

---

## Scenario 10: Automated Tests Pass (SC-006)

```bash
cd backend
dotnet test --verbosity normal
# Expected: All tests pass, 0 failures

cd ../app
npm ci && npm run build
# Expected: Build succeeds, dist/ directory created
```

---

## References
- API contracts: [contracts/api-contracts.md](contracts/api-contracts.md)
- Data model: [data-model.md](data-model.md)
- Feature spec: [spec.md](spec.md)
