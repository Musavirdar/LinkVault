# Implementation Plan: Core Hardening, Secret Protection & Repository Restructure

**Branch**: `002-core-hardening-and-cleanup` | **Date**: 2026-10-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-core-hardening-and-cleanup/spec.md`

---

## Summary

This plan addresses foundational repository cleanup, secret sanitization, and deep security fixes:
1. Promote `LinkVault-main/*` to the repository root and remove the nested folder and duplicate `.github/`.
2. Strip hardcoded secrets from `appsettings.json` and `docker-compose.yml`, rotate keys, and expand `StartupValidator` to fail fast on any previously committed placeholders or default passwords.
3. Integrate `IFileValidationService` into `ChunkedUploadController` to prevent magic-byte and SVG script inspection bypasses.
4. Expand SVG script scanning to read the full file payload rather than truncating at 4 KB.
5. Implement atomic download ticket consumption via EF Core `ExecuteUpdateAsync` to eliminate replay races, while re-checking resource and user activity states.
6. Enable `UseForwardedHeaders` in ASP.NET Core so rate limiting keys on the real client IP behind reverse proxies.
7. Remove non-code presentation files (`LinkVault_Walkthrough.pptx`, `ponytail.md`, `info.md`) and tool scaffolding from git tracking via `.gitignore`.
8. Restructure `README.md` to lead with LinkVault product capabilities and architecture.

---

## Technical Context

**Language/Version**: C# 12 (.NET 8.0 SDK), TypeScript 5.5, Node.js v24 LTS  
**Primary Dependencies**: ASP.NET Core 8 Web API, Entity Framework Core 8, Npgsql PostgreSQL, Vite 7, React 18, Tailwind CSS 3.4  
**Storage**: PostgreSQL 16, Local File System / Object Storage  
**Testing**: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, FluentAssertions, Moq  
**Target Platform**: Cross-platform (Linux / Docker / Windows)  
**Project Type**: Full-stack web application (REST API + SPA)  
**Performance Goals**: < 100ms ticket validation latency; < 1s startup fail-fast check  
**Constraints**: Zero hardcoded secrets in version control; 100% of uploads validated before disk persistence; zero race conditions on ticket consumption.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (Specs Before Code)**: PASS — Specification established in `spec.md` and approved before planning.
- **Principle II (Source of Truth)**: PASS — All functional requirements trace directly to `specs/002-core-hardening-and-cleanup/spec.md`.
- **Principle III (Automated Verification)**: PASS — Unit and integration tests in `LinkVaultApi.Tests` validate all security behaviors.

---

## Project Structure

### Documentation (this feature)

```text
specs/002-core-hardening-and-cleanup/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Technical research & decisions
├── data-model.md        # Entity definitions & atomic query semantics
├── contracts/           # API request/response contracts
│   └── api-contracts.md
├── checklists/          # Requirements completeness checklist
│   └── requirements.md
├── quickstart.md        # End-to-end verification guide
└── tasks.md             # Implementation tasks (/speckit-tasks output)
```

### Source Code (repository root layout post-restructure)

```text
.
├── .github/
│   └── workflows/
│       └── ci.yml             # GitHub Actions CI workflow (root paths)
├── app/                       # React 18 + Vite frontend
│   ├── src/
│   ├── package.json
│   └── vite.config.ts
├── backend/                   # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── ChunkedUploadController.cs  # Validates assembled files
│   │   ├── FilesController.cs          # Atomic ticket consumption & checks
│   │   └── AuthController.cs           # Rate-limited auth endpoints
│   ├── Data/
│   │   └── LinkVaultDbContext.cs
│   ├── Services/
│   │   ├── FileValidationService.cs    # Full SVG & magic-byte scanner
│   │   └── StartupValidator.cs         # Rejects committed secrets
│   ├── LinkVaultApi.Tests/             # xUnit integration & unit test suite
│   ├── appsettings.json                # Empty secret templates (zero credentials)
│   ├── appsettings.Development.json
│   ├── .env.example                    # Environment variable guide
│   └── Program.cs                      # UseForwardedHeaders & pipeline
├── docker-compose.yml         # Uses environment variables (${JWT_SECRET})
├── sharedb1.sln               # Top-level .NET Solution file
├── README.md                  # Product-first documentation
└── .gitignore                 # Excludes build, uploads, secrets, scaffolding
```

**Structure Decision**: Promote all contents of `LinkVault-main/` to the repository root. Eliminate the nested directory and obsolete `.github/` folder. All paths in CI and IDE configurations become standard root paths.

---

## Complexity Tracking

| Component | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `ExecuteUpdateAsync` | Atomic ticket redemption | Check-then-write allows race conditions during simultaneous requests. |
| `UseForwardedHeaders` | Real client IP isolation | Without it, all users share the reverse proxy's IP on Render/Cloudflare, causing collective rate-limit lockouts. |
| Full SVG Stream Scan | Complete XSS defense | 4 KB scan limit can be bypassed by padding with benign comments before the script tag. |
