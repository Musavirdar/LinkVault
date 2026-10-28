# Feature Specification: Core Hardening, Secret Protection & Repository Restructure

**Feature Branch**: `002-core-hardening-and-cleanup`  
**Created**: 2026-10-28  
**Status**: Draft  
**Input**: User description: "Still broken (fix these first): Nested LinkVault-main/ folder, secrets still committed in appsettings/docker-compose, chunked uploads skip validation, ticket check isn't atomic and skips resource/user checks, rate limiting behind proxy lacks UseForwardedHeaders, SVG scan only reads 4KB, remove scaffolding/tracked files from git, restructure README to lead with product."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Repository Restructure & Root Directory Alignment (Priority: P1)

Developers, CI runners, and reviewers expect a clean, canonical project layout at the root of the repository without nested zip extractions or duplicate configuration files.

**Why this priority**: A nested `LinkVault-main/` folder creates path ambiguity across tooling, duplicates configuration, breaks CI workflows without fallback scripts, and degrades code quality appearance.

**Independent Test**: Root directory directly contains `app/`, `backend/`, `sharedb1.sln`, and `.github/workflows/ci.yml`. Running `dotnet test sharedb1.sln` and building the frontend from the repo root executes cleanly without directory navigation into subfolders.

**Acceptance Scenarios**:
1. **Given** a cloned repository, **When** examining the root structure, **Then** `app/`, `backend/`, and `sharedb1.sln` exist directly at the top level and the nested `LinkVault-main/` directory is completely removed.
2. **Given** the CI workflow in `.github/workflows/ci.yml`, **When** the workflow triggers, **Then** all build, test, and package steps reference root paths without nested fallback logic.
3. **Given** git status, **When** reviewing tracked files, **Then** duplicate `.github/` copies, non-code artifacts (`LinkVault_Walkthrough.pptx`, `ponytail.md`, `info.md`), and local tool scaffolding (`.specify/`, `.agents/`, `AGENTS.md`) are ignored and removed from git tracking.

---

### User Story 2 - Zero-Secret Configuration & Strict Startup Validation (Priority: P1)

The application must not store production secrets or default passwords in source-controlled configuration files (`appsettings.json`, `docker-compose.yml`), and must fail fast if committed secrets are present at runtime.

**Why this priority**: Hardcoded secrets in committed repositories pose an immediate credential leakage and compliance risk. Startup validation must act as an ironclad guardrail against using committed defaults.

**Independent Test**: Starting the backend API with committed secret strings in `appsettings.json` or without required environment variables throws an `InvalidOperationException` on boot before accepting traffic. Supplying fresh, non-default secrets via environment variables starts the service successfully.

**Acceptance Scenarios**:
1. **Given** `appsettings.json` and `docker-compose.yml`, **When** inspected in version control, **Then** they contain no real database passwords or JWT signing keys, referencing environment variables or empty templates instead.
2. **Given** an environment where `JwtSettings:Secret` matches any previously committed placeholder or known default (e.g. `linkvault-default-key-override...`), **When** the application starts, **Then** `StartupValidator` throws an exception terminating startup.
3. **Given** valid non-default secrets provided via environment variables, **When** the application boots, **Then** startup succeeds and secret parameters are bound correctly.

---

### User Story 3 - Full Content Validation for Chunked Uploads & SVG Sanitization (Priority: P1)

All file uploads—regardless of whether uploaded as a single payload or assembled via multiple chunks—must be strictly inspected against magic byte profiles, disguised executables, and embedded malicious scripts.

**Why this priority**: Allowing chunked uploads to bypass validation creates a severe vulnerability where an attacker can easily upload disguised binaries or malicious SVGs simply by using chunked upload endpoints.

**Independent Test**: Uploading a disguised PE/ELF executable or an SVG containing a `<script>` tag after the 4 KB boundary via `POST /api/chunked-upload/complete` rejects the assembly with HTTP 400 and removes the temporary chunks.

**Acceptance Scenarios**:
1. **Given** a chunked file upload, **When** `CompleteChunkedUpload` is invoked, **Then** the assembled file is validated via `IFileValidationService` before committing to storage and database records.
2. **Given** an invalid or malicious chunked payload (e.g. disguised executable), **When** validation fails, **Then** the API returns HTTP 400 Bad Request, cleans up chunk files, and records no resource entity.
3. **Given** an SVG upload exceeding 4 KB with script content at any byte offset, **When** inspected, **Then** the validator scans the entire file content and flags the malicious markup.

---

### User Story 4 - Atomic Ticket Consumption with Resource & User Integrity (Priority: P1)

Single-use download tickets must be consumed atomically at the database level to eliminate race-condition replay attacks, and redemption must re-verify that the target resource and downloading user remain active and undeleted.

**Why this priority**: Non-atomic check-then-set permits concurrent download races. Failing to re-check resource deletion or user deactivation permits unauthorized data exfiltration within the 60-second window.

**Independent Test**: Two simultaneous concurrent requests using the same download ticket result in exactly one HTTP 200 stream and one HTTP 410/404 rejection. Attempting to download a resource deleted after ticket issuance returns HTTP 404.

**Acceptance Scenarios**:
1. **Given** a valid single-use download ticket, **When** concurrent requests arrive, **Then** consumption executes via an atomic single-statement update (`ExecuteUpdateAsync`), granting download rights to exactly one request.
2. **Given** a resource that was soft-deleted (`IsDeleted == true` or `IsActive == false`) after ticket creation, **When** the ticket is presented for download, **Then** the request is rejected with HTTP 404.
3. **Given** a user who was deactivated or deleted after creating a ticket, **When** redemption occurs, **Then** the request is rejected.

---

### User Story 5 - Reverse-Proxy-Aware Client Rate Limiting (Priority: P2)

Rate limiting on authentication and sensitive endpoints must accurately distinguish client IP addresses when operating behind reverse proxies (Render, Cloudflare, nginx) to avoid collective lockouts.

**Why this priority**: In production environments behind reverse proxies, without forwarded header handling, all users share the proxy's IP address. A single brute-force attempt would trigger 429 lockouts for all legitimate users.

**Independent Test**: Sending requests with distinct `X-Forwarded-For` client headers under rate limiting tests isolates quota buckets per simulated client IP.

**Acceptance Scenarios**:
1. **Given** incoming HTTP requests behind a reverse proxy, **When** `X-Forwarded-For` and `X-Forwarded-Proto` are present, **Then** ASP.NET Core processes forwarded headers so `HttpContext.Connection.RemoteIpAddress` reflects the real client IP.
2. **Given** client A exceeds 10 requests/minute on authentication endpoints, **When** client B sends an authentication request, **Then** client B is unaffected and succeeds.

---

### User Story 6 - Product-First Presentation & README Hierarchy (Priority: P3)

The project documentation must prioritize the LinkVault application's core product capabilities, enterprise collaboration features, and visual architecture for evaluators, with development specifications positioned as supporting documentation.

**Why this priority**: First impressions matter for judges, users, and reviewers. The primary README should highlight user value, key features, and visual demos before technical scaffolding details.

**Independent Test**: Reading `README.md` immediately showcases the product overview, screenshots, feature breakdown, and tech stack, while Spec Kit methodology notes are organized in an engineering section below.

**Acceptance Scenarios**:
1. **Given** the root `README.md`, **When** viewed on GitHub, **Then** the top section presents the LinkVault vision, screenshots, feature capabilities, and quickstart commands.
2. **Given** the specification and governance section, **When** referenced in the documentation, **Then** it is structured as an engineering/architecture chapter.

---

### Edge Cases

- **Concurrent Ticket Redemption**: High-frequency concurrent requests using the same token must be serialized at the database row level without deadlocks.
- **Large Chunked Uploads**: Assembled files must be validated in streams or buffers without triggering out-of-memory exceptions.
- **Corrupted SVGs**: Malformed XML or partial tags across buffer boundaries must still be safely detected or rejected.
- **Spoofed Forwarded Headers**: Untrusted internal proxy chains must be configured with standard proxy network limits.
- **Missing Environment Variables**: Missing required runtime secrets must produce human-readable startup errors explaining which variable is absent.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System repository structure MUST maintain all source code (`app/`, `backend/`, `sharedb1.sln`, `docker-compose.yml`) directly at the repository root.
- **FR-002**: System CI workflow MUST compile and test the solution from root directory paths without nested directory workarounds.
- **FR-003**: Tool scaffolding (`.specify/`, `.agents/`, `AGENTS.md`) and non-repository files (`LinkVault_Walkthrough.pptx`, `ponytail.md`, `info.md`) MUST NOT be tracked in git.
- **FR-004**: System MUST NOT store real secrets or passwords in `appsettings.json`, `appsettings.Development.json`, or `docker-compose.yml`.
- **FR-005**: `StartupValidator` MUST reject known committed placeholder strings, default passwords (`linkvault123`, `linkvault-default-key...`), and keys shorter than 32 bytes.
- **FR-006**: System MUST support reading database connection strings and JWT signing keys from standard environment variables (`ConnectionStrings__DefaultConnection`, `JwtSettings__Secret`).
- **FR-007**: `ChunkedUploadController` MUST invoke `IFileValidationService` on fully assembled chunked files before saving to persistent storage and database.
- **FR-008**: System MUST reject and purge assembled chunked files that fail magic byte or script scanning.
- **FR-009**: `FileValidationService` MUST scan the entire content of SVG uploads for `<script>`, `javascript:`, and active event handlers, regardless of file size.
- **FR-010**: `FilesController` MUST atomically mark download tickets as used using a single atomic update query (`ExecuteUpdateAsync` or equivalent) conditioned on `!IsUsed && ExpiresAtUtc > now`.
- **FR-011**: Ticket download flow MUST verify that the referenced `Resource` is active (`IsActive == true`) and not deleted (`IsDeleted == false`).
- **FR-012**: Ticket download flow MUST verify that the user who generated the ticket is active and undeleted in the organization.
- **FR-013**: System MUST enable `UseForwardedHeaders` configured with `XForwardedFor` and `XForwardedProto` before rate limiting and authentication middleware.
- **FR-014**: Rate limiting policies MUST extract client IP from forwarded connection metadata.
- **FR-015**: Root `README.md` MUST lead with LinkVault product capabilities, architecture, screenshots, and getting started guides before Spec Kit appendices.
- **FR-016**: Test suite MUST verify chunked upload validation, atomic ticket consumption, and forwarded header rate limiting.

---

### Key Entities

- **DownloadTicket**: Represents single-use temporary authorization. Key attributes: `Id`, `ResourceId`, `UserId`, `OrganizationId`, `ExpiresAtUtc`, `IsUsed`.
- **Resource**: Digital asset record. Key attributes: `Id`, `OrganizationId`, `DirectoryId`, `StoragePath`, `FileName`, `ContentType`, `IsActive`, `IsDeleted`.
- **ChunkedUpload**: Multi-part upload session. Key attributes: `UploadId`, `FileName`, `TotalChunks`, `TotalSize`, `IsAssembled`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of single-file and chunked uploads undergo file signature and malicious script validation prior to persistence.
- **SC-002**: 0 instances of race-condition double downloads possible on single-use download tickets under concurrent test load.
- **SC-003**: 0 hardcoded credentials or passwords present in committed configuration files (`appsettings.json`, `docker-compose.yml`).
- **SC-004**: System startup terminates in < 1 second with an explicit error when default or committed placeholder secrets are supplied.
- **SC-005**: 100% of integration and unit tests pass against the root-level solution.

---

## Assumptions

- Operating environment supports setting environment variables for container and bare-metal executions.
- Reverse proxy configurations in hosting platforms (Render, nginx) correctly set the standard `X-Forwarded-For` header.
- Local development continues to support seamless offline development with environment variable fallbacks or `.env` templates.
- Git repository history will cleanly record the folder promotion from `LinkVault-main/` to the repository root.
