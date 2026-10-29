# Implementation Plan: Purge Committed Credentials & Render Secret Remediation

**Branch**: `003-purge-committed-credentials` | **Date**: 2026-10-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-purge-committed-credentials/spec.md`

## Summary

Remediate the exposure of Render Postgres database credentials and placeholder JWT keys committed in `backend/.env.render`. Untrack and delete `backend/.env.render`, expand root `.gitignore` to comprehensively block `.env*` files, augment `StartupValidator.cs` to actively reject the leaked password (`QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg`) and leaked placeholder JWT string (`replace_this_with_a_very_long_secure_random_string_for_production`), and provide an operational rotation runbook for Render Cloud.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0`)  
**Primary Dependencies**: ASP.NET Core 8, Microsoft.Extensions.Configuration, Entity Framework Core 8  
**Storage**: PostgreSQL 16 (production), EF Core InMemory (testing)  
**Testing**: xUnit, FluentAssertions, `Microsoft.NET.Test.Sdk` under `dotnet test`  
**Target Platform**: Linux (Render container environment) & Windows 11 (developer environment)  
**Project Type**: Web API backend service  
**Performance Goals**: Startup validation overhead < 5ms  
**Constraints**: Zero secrets in source repository; immediate fail-fast abort on blacklisted credentials  
**Scale/Scope**: 1 deleted configuration file, 1 `.gitignore` update, 1 service validator update, unit test suite updates  

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [X] **Simplicity First**: Minimal, targeted changes without extra dependencies or framework additions.
- [X] **Single Responsibility**: `StartupValidator` handles configuration integrity; `.gitignore` handles VCS exclusion.
- [X] **API-First / Secure**: Connection strings and signing keys strictly sourced from runtime environment variables.
- [X] **Test Coverage**: Acceptance tests in `StartupValidationTests.cs` cover all blacklisted credentials.
- [X] **Docker Compose Compatibility**: Preserves `${POSTGRES_PASSWORD}` and `${JWT_SECRET}` variable interpolations.

## Project Structure

### Documentation (this feature)

```text
specs/003-purge-committed-credentials/
├── plan.md              # Implementation plan
├── research.md          # Technical decisions & secret rotation rationale
├── data-model.md        # Configuration schema & blacklist definitions
├── contracts/           # API and runtime validation contracts
│   └── api-contracts.md
├── quickstart.md        # Verification scenarios & Render runbook
└── tasks.md             # Implementation tasks (/speckit-tasks output)
```

### Source Code (repository root)

```text
.gitignore                           # Expand to ignore all .env.* files (preserve .env.example)
backend/
├── .env.render                      # DELETED & UNTRACKED from repository
├── Services/
│   └── StartupValidator.cs          # Add leaked password and placeholder JWT to blacklists
└── LinkVaultApi.Tests/
    └── Unit/
        └── StartupValidationTests.cs # Add test cases asserting rejection of leaked credentials
```

**Structure Decision**: Direct modification of root `.gitignore`, deletion of `backend/.env.render`, and addition of rejection rules to `backend/Services/StartupValidator.cs` and `StartupValidationTests.cs`.

## Complexity Tracking

*No constitutional violations; no additional complexity introduced.*
