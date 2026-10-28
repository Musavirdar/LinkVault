# API Contracts: Core Hardening & Security

**Feature**: `002-core-hardening-and-cleanup`  
**Date**: 2026-10-28  

---

## 1. Chunked Upload Completion

### `POST /api/chunked-upload/complete`

Completes a multi-part upload, concatenates chunks on disk, validates magic bytes and embedded scripts via `IFileValidationService`, and persists the resource.

#### Request Headers
- `Authorization: Bearer <jwt-token>`
- `Content-Type: application/json`

#### Request Body
```json
{
  "uploadId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "document.pdf",
  "directoryId": null
}
```

#### Responses

**200 OK** (Validation Succeeded & File Persisted)
```json
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "document.pdf",
  "fileSize": 1048576,
  "contentType": "application/pdf",
  "directoryId": null,
  "createdAt": "2026-10-28T12:00:00Z"
}
```

**400 Bad Request** (Validation Failed — e.g. disguised binary or malicious SVG script)
```json
{
  "error": "File content validation failed: The file payload does not match the declared type or contains malicious content."
}
```

---

## 2. Authenticated File Download with Ticket

### `GET /api/files/{id}/download?ticket={ticketId}`

Downloads a file using a 60-second single-use ticket. The ticket must be consumed atomically, and both resource and user states are re-verified.

#### Query Parameters
- `ticket` (`Guid`): Single-use download ticket issued by `POST /api/files/{id}/ticket`.

#### Responses

**200 OK**
- `Content-Disposition: attachment; filename="document.pdf"`
- `Content-Type: application/pdf`
- `X-Content-Type-Options: nosniff` (Mandatory for SVG / active types)
- Binary file stream.

**404 Not Found**
- Ticket is expired, invalid, already consumed, or referenced resource was deleted/inactive.
```json
{
  "error": "Download ticket not found or already used."
}
```

**403 Forbidden**
- Ticket user was deactivated or revoked from the organization.
```json
{
  "error": "User account is inactive."
}
```

---

## 3. Rate Limiting with Forwarded Headers

### Protected Endpoints
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/2fa/verify`
- `POST /api/auth/change-password`

#### Policy: `"auth"`
- **Limit**: 10 requests per 1 minute window.
- **Partition Key**: Client IP (derived from `X-Forwarded-For` when behind proxy).
- **On Limit Exceeded**:
  - Status: `429 Too Many Requests`
  - Header: `Retry-After: 60`
  - Body:
    ```json
    {
      "error": "Too many requests. Please slow down.",
      "status": 429
    }
    ```
