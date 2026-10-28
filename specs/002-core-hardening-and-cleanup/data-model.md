# Data Model & Query Specifications: Core Hardening & Cleanup

**Feature**: `002-core-hardening-and-cleanup`  
**Date**: 2026-10-28  

---

## 1. Entity Definitions & Integrity Rules

### DownloadTicket
Represents a short-lived (60 seconds) single-use authorization token for downloading a specific resource.

| Field | Type | Nullable | Description |
|---|---|:---:|---|
| `Id` | `Guid` | No | Primary key |
| `Token` | `string` | No | Cryptographically random unique token identifier |
| `ResourceId` | `Guid` | No | Foreign key to `Resource` entity |
| `UserId` | `Guid` | No | Foreign key to `User` who requested the download ticket |
| `OrganizationId` | `Guid` | No | Tenant identifier enforcing tenant isolation |
| `CreatedAtUtc` | `DateTime` | No | UTC timestamp of ticket creation |
| `ExpiresAtUtc` | `DateTime` | No | Expiration timestamp (`CreatedAtUtc + 60s`) |
| `IsUsed` | `bool` | No | Flags whether ticket has been consumed |

#### Database Indexes
- `IX_DownloadTickets_Id_IsUsed_ExpiresAtUtc`: Composite index for fast atomic update queries.
- `IX_DownloadTickets_ResourceId`: Index for resource relation navigation.

---

### Resource (Integrity Checks)
Represents uploaded files and folders within LinkVault.

| Field | Type | Validation on Ticket Redemption |
|---|---|---|
| `Id` | `Guid` | Must match `ticket.ResourceId` |
| `OrganizationId` | `Guid` | Must match `ticket.OrganizationId` |
| `IsActive` | `bool` | MUST be `true` |
| `IsDeleted` | `bool` | MUST be `false` |
| `StoragePath` | `string` | Physical or object storage pointer |
| `ContentType` | `string` | MIME type stream header |

---

### User (Integrity Checks)
Represents the user redeeming or generating a ticket.

| Field | Type | Validation on Ticket Redemption |
|---|---|---|
| `Id` | `Guid` | Must match `ticket.UserId` |
| `IsActive` | `bool` | MUST be `true` (deactivated user blocked) |
| `IsDeleted` | `bool` | MUST be `false` |

---

## 2. Query Semantics

### Atomic Single-Statement Ticket Consumption
```sql
UPDATE "DownloadTickets"
SET "IsUsed" = TRUE
WHERE "Id" = @ticketId
  AND "IsUsed" = FALSE
  AND "ExpiresAtUtc" > CURRENT_TIMESTAMP;
```

EF Core implementation:
```csharp
var affectedRows = await dbContext.DownloadTickets
    .Where(t => t.Id == ticketGuid && !t.IsUsed && t.ExpiresAtUtc > DateTime.UtcNow)
    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsUsed, true));

if (affectedRows == 0)
{
    return NotFound("Invalid, expired, or already-used download ticket.");
}
```

---

### Integrity Verification Query
```csharp
var resource = await dbContext.Resources
    .AsNoTracking()
    .FirstOrDefaultAsync(r => r.Id == ticket.ResourceId 
                           && r.OrganizationId == ticket.OrganizationId 
                           && r.IsActive 
                           && !r.IsDeleted);

if (resource == null)
{
    return NotFound("Resource not found, inactive, or removed.");
}

var user = await dbContext.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == ticket.UserId 
                           && u.OrganizationId == ticket.OrganizationId 
                           && u.IsActive 
                           && !u.IsDeleted);

if (user == null)
{
    return Forbid("User account is inactive or revoked.");
}
```
