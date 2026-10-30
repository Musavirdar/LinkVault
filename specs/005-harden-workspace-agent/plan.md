# Implementation Plan: Workspace Agent Security & Concurrency Hardening

**Branch**: `005-harden-workspace-agent` | **Date**: 2026-10-30 | **Spec**: [specs/005-harden-workspace-agent/spec.md](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/specs/005-harden-workspace-agent/spec.md)

**Input**: Feature specification from `/specs/005-harden-workspace-agent/spec.md`

## Summary

Harden the LinkVault Autonomous Workspace Agent backend (`VaultAgentService`, `MetaLlamaClient`, `AgentController`) against concurrency races, lifecycle memory hazards, cross-tenant leaks, and unauthorized execution. Implement isolated service scopes (`IServiceScopeFactory`) for background tasks, atomic database-level plan approval gating (`ExecuteUpdateAsync`), rigid model schema validation, removal of unsafe fallbacks, strict tenant verification, caller claim enforcement, standardized share link generation, and per-user endpoint rate limiting.

## Technical Context

**Language/Version**: C# (.NET 8.0)  
**Primary Dependencies**:
- `Microsoft.AspNetCore.SignalR`
- `Microsoft.EntityFrameworkCore` (8.0), `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.RateLimiting`
- `Microsoft.Extensions.DependencyInjection` (`IServiceScopeFactory`)
- `System.Text.Json`

**Storage**: PostgreSQL 16 (using EF Core conditional updates: `ExecuteUpdateAsync`)  
**Testing**: `xUnit`, `FluentAssertions`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`  
**Target Platform**: Linux / Windows containerized deployments (`docker-compose.yml`), Render Web Services  
**Project Type**: Enterprise Web API with background execution and WebSocket streaming  
**Performance Goals**:
- Atomic approval transition < 50ms.
- Model output schema validation < 10ms.
- Zero `ObjectDisposedException` or database concurrency errors.

**Constraints**:
- Isolated Dependency Injection Scope: Never pass request-scoped `DbContext` to detached `Task.Run` background threads.
- Zero-Trust Permission Parity: State-altering tools must verify `canUpload`, `canUpdate`, `canShare`, `canDelete` matching `Program.cs` policies.
- Atomic Transition: Only transition plans from `AwaitingApproval` to `Executing`; return HTTP 409 Conflict if 0 rows updated.
- No Unsafe Defaults: Missing `resourceId` must fail the step; never pick the first resource.
- Scope Boundaries: Only touch files in `backend/Agent/` (`VaultAgentService.cs`, `MetaLlamaClient.cs`, `AgentController.cs`), `backend/Program.cs` (rate limiter policy), and `backend/LinkVaultApi.Tests/`. Do not modify unrelated code.

## Constitution Check

| Principle / Gate | Assessment | Status |
|---|---|---|
| **Simplicity First** | Leverages built-in EF Core `ExecuteUpdateAsync` and ASP.NET Core `IServiceScopeFactory` without heavy external distributed locking mechanisms. | PASS |
| **Single Responsibility** | Keeps architectural separation intact: `AgentController` handles HTTP & rate limiting; `VaultAgentService` enforces business logic & tenant isolation; `MetaLlamaClient` validates model output schemas. | PASS |
| **API-First** | Fully documented REST contracts returning explicit HTTP 409 Conflict and 429 Too Many Requests status codes. | PASS |
| **Test Coverage** | Comprehensive automated unit tests for double approval, unauthorized approval, Viewer role denials, missing resource IDs, and cross-tenant entity targets. | PASS |
| **Zero Regressions** | Scoped strictly to `backend/Agent/` and tests without changing unrelated backend services or frontend pages. | PASS |

## Project Structure

### Documentation (this feature)

```text
specs/005-harden-workspace-agent/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Concurrency and security architecture decisions (/speckit-plan output)
├── data-model.md        # State transitions and tool schemas (/speckit-plan output)
├── quickstart.md        # Runnable verification guide (/speckit-plan output)
├── contracts/           # API contracts (/speckit-plan output)
│   └── agent-api.md
├── checklists/          # Requirements completeness checklist
│   └── requirements.md
└── tasks.md             # Work item breakdown (/speckit-tasks output)
```

### Source Code Layout

```text
backend/
├── Agent/
│   ├── Controllers/
│   │   └── AgentController.cs        # Rate limiting, conflict (409) handling, ownership validation
│   └── Services/
│       ├── MetaLlamaClient.cs        # Schema validation (TryGetProperty) & 10-step plan cap
│       └── VaultAgentService.cs      # IServiceScopeFactory, atomic approvals, claim checks, tenant verification
├── Program.cs                        # Rate limiting policy registration ("agent_plan")
└── LinkVaultApi.Tests/
    └── Unit/
        └── AgentHardeningTests.cs    # Automated tests for double approval, Viewer denials, tenant boundaries
```

## Complexity Tracking

| Issue | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `IServiceScopeFactory` in `Task.Run` | Avoids `ObjectDisposedException` when request terminates while background plan is executing | Passing existing DbContext causes intermittent crashes in production |
| EF Core `ExecuteUpdateAsync` for approval | Atomically prevents duplicate approval races at the database level | In-memory locks fail across multiple server instances or cluster nodes |
| Per-tool schema validation with `TryGetProperty` | Guards against malformed LLM tool arguments | Relying on execution-time exceptions produces inconsistent partial plans |
