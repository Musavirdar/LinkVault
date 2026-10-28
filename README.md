# LinkVault — Secure Digital Workspace

LinkVault is a unified workspace for teams and organizations to manage links, files, and directories with enterprise-grade access control and real-time collaboration.

This repository is integrated with official **[GitHub Spec Kit](https://github.com/github/spec-kit)** for Spec-Driven Development (SDD).

---

## 🔒 Security & Access Hardening (`001-security-access-hardening`)

The codebase includes full security hardening implemented under `specs/001-security-access-hardening/`:

- **Authenticated File Downloads:** Eliminated static file exposure at `/uploads`. Streaming file downloads require Bearer JWT authorization or 60-second single-use download tickets (`/api/files/{id}/download`, `/api/files/{id}/ticket`).
- **Organization-Scoped Sharing:** External share link creation enforces the `CanShare` authorization policy and strict tenant isolation (`OrganizationId`).
- **Authentication Rate Limiting:** Fixed-window rate limiting (10 requests/minute per client IP) on all authentication and sensitive endpoints with automatic `Retry-After: 60` headers.
- **Upload Inspection & SVG XSS Prevention:** Magic byte header verification for PDF, PNG, JPEG, SVG, and Office formats; rejection of disguised PE/ELF executables; sanitization against directory traversal; and forced `Content-Disposition: attachment` + `X-Content-Type-Options: nosniff` for SVGs.
- **Fail-Fast Startup Validation:** Refuses boot if `JwtSettings:Secret` is under 32 bytes or matches default placeholders, or if database connection strings are unconfigured.
- **Admin-Only Audit Logs:** Audit logs restricted strictly to the `Admin` role with mandatory organization isolation.
- **Data Integrity:** Directory hierarchy normalized so root folders use `null` rather than `Guid.Empty`.
- **Automated Testing:** 26 comprehensive unit and integration tests (`LinkVaultApi.Tests`) running in CI via GitHub Actions.

---

## 📂 Project Structure

```text
├── .agents/                    # GitHub Spec Kit agent skills & workflows
├── .github/workflows/          # CI/CD pipeline (backend test & frontend build)
├── .specify/                   # Spec Kit configuration, scripts & constitution
├── specs/                      # Feature specifications, plans & tasks
│   └── 001-security-access-hardening/
└── LinkVault-main/             # Application source code
    ├── app/                    # React 18 + Vite + Tailwind CSS frontend
    ├── backend/                # ASP.NET Core 8 Web API
    │   └── LinkVaultApi.Tests/ # xUnit + WebApplicationFactory test suite
    ├── docker-compose.yml      # Local PostgreSQL & services
    └── sharedb1.sln            # .NET Solution file
```

---

## 🚀 Quick Start

### 1. Run Tests
```powershell
dotnet test LinkVault-main/backend/LinkVaultApi.Tests/LinkVaultApi.Tests.csproj
```

### 2. Backend API
```powershell
cd LinkVault-main/backend
dotnet run
```

### 3. Frontend Client
```powershell
cd LinkVault-main/app
npm install
npm run dev
```

---

## 📜 Spec-Driven Development (SDD) Commands

| Command | Skill | Action |
|---|---|---|
| `/speckit-specify [desc]` | `speckit-specify` | Create new feature specification |
| `/speckit-clarify` | `speckit-clarify` | Clarify ambiguous spec requirements |
| `/speckit-plan [feature]` | `speckit-plan` | Generate implementation plan |
| `/speckit-tasks [feature]` | `speckit-tasks` | Break plan into atomic tasks |
| `/speckit-implement [feature]` | `speckit-implement` | Execute tasks sequentially |
| `/speckit-converge [feature]` | `speckit-converge` | Verify code against spec |
