# Feature Specification: Purge Committed Credentials & Render Secret Remediation

**Feature Branch**: `003-purge-committed-credentials`  
**Created**: 2026-10-29  
**Status**: Draft  
**Input**: User description: "Do this now: a real database password is committed. backend/.env.render contains a full Render Postgres connection string with the real host, username, and password. It was already in the repo when I first read it (my earlier output redacted it), and it’s in git history, so the repo being public means anyone can read it. In the Render dashboard, rotate that database’s password or delete and recreate the database. Delete .env.render from the repo and add it to .gitignore. Removing it won’t clear history, so rotation is what protects you. Check the placeholder JWT secret in the same file isn’t what your deployed backend uses."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Repository Sanitization & Git Tracking Elimination (Priority: P1)

As a security-conscious maintainer, I want all committed environment files containing cloud connection strings and secrets purged from git tracking and ignored by version control, so that secrets cannot be accidentally re-committed or distributed.

**Why this priority**: Leaving live database hostnames, usernames, and passwords in a public repository allows unauthorized external access, compromise of production data, and violation of security compliance standards.

**Independent Test**: Running `git status` and inspecting the working tree shows `backend/.env.render` is completely removed from tracking and filesystem; re-creating any `.env.render` file is ignored by `.gitignore`.

**Acceptance Scenarios**:

1. **Given** `backend/.env.render` is currently tracked in the repository, **When** the cleanup process executes, **Then** `backend/.env.render` is deleted and untracked from Git.
2. **Given** root `.gitignore`, **When** inspected or tested against environment file patterns, **Then** `.env.render`, `backend/.env.render`, and wildcard `.env.*` (excluding sanitized `.env.example`) are explicitly ignored.
3. **Given** a developer attempts to add a new `.env.render` file, **When** `git add` is executed, **Then** Git rejects or ignores the file unless forced.

---

### User Story 2 - Startup Fail-Safe for Compromised Credentials (Priority: P1)

As a system administrator, I want the backend application startup validator to actively reject the specific compromised database credentials and placeholder JWT keys that were exposed, so that the application cannot boot with exposed credentials even if misconfigured.

**Why this priority**: While Git history may preserve past commits, the backend must guarantee at runtime that neither the compromised database password nor the leaked JWT placeholder can ever be used by a running service.

**Independent Test**: Configuring the backend with the leaked database password or leaked JWT placeholder string causes `StartupValidator` to throw `InvalidOperationException` and abort boot before listening on any port.

**Acceptance Scenarios**:

1. **Given** a connection string containing the compromised database password (`QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg`), **When** `StartupValidator.Validate` is invoked during application boot, **Then** the service terminates immediately with an `InvalidOperationException` stating an insecure/compromised database password was detected.
2. **Given** a JWT configuration set to the exposed placeholder string (`replace_this_with_a_very_long_secure_random_string_for_production`), **When** `StartupValidator.Validate` executes, **Then** the service terminates immediately with an `InvalidOperationException` identifying an insecure/blacklisted placeholder key.
3. **Given** freshly generated, non-blacklisted production secrets, **When** `StartupValidator.Validate` executes, **Then** validation succeeds and startup proceeds.

---

### User Story 3 - Render Production Secret Rotation & Environment Verification Protocol (Priority: P2)

As a DevOps engineer, I want an actionable, documented operational runbook to rotate the live Render Postgres credentials and verify deployed backend environment variables, so that production assets are safely isolated and secured against past Git exposure.

**Why this priority**: Deleting the file from Git does not invalidate credentials that were already exposed in Git history. Rotation in the cloud provider dashboard is the mandatory step to eliminate external risk.

**Independent Test**: The rotation guide provides verifiable, step-by-step instructions to rotate the database password in Render, update Render Web Service environment variables, and verify that the deployed backend uses a distinct 256-bit JWT secret.

**Acceptance Scenarios**:

1. **Given** a compromised Render PostgreSQL database instance, **When** an operator follows the runbook, **Then** the database password is reset or a new database instance is provisioned in the Render dashboard, invalidating all leaked credentials.
2. **Given** a deployed Render Web Service, **When** configuring service environment variables, **Then** the operator verifies that `JwtSettings__Secret` is a freshly generated 32+ byte cryptographically secure key distinct from any repository placeholder.
3. **Given** updated production variables in Render, **When** the deployed backend restarts, **Then** health checks at `/health` return HTTP 200 without exposing sensitive connection parameters.

---

### Edge Cases

- **Developer Local Setup**: Developers running locally must still be able to use `.env.example` templates with generic dummy values without accidentally triggering startup blacklists.
- **Git History vs. Working Directory**: Removing `.env.render` from `HEAD` does not rewrite past commits; the spec and documentation must explicitly state that credential invalidation (rotation) is the definitive remediation.
- **Render Build Pipeline**: If the Render deployment was previously configured to copy `.env.render`, the build/deploy configuration in Render must rely exclusively on environment variables set in the Render Dashboard "Environment" tab.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System repository MUST delete `backend/.env.render` and remove it from git tracking.
- **FR-002**: Root `.gitignore` MUST be updated to include `.env.render`, `*.env.render`, and ensure wildcard `.env.*` ignores any environment file variant while preserving `.env.example`.
- **FR-003**: `StartupValidator.cs` MUST include the exposed Render database password (`QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg`) in its `InsecurePasswords` rejection list.
- **FR-004**: `StartupValidator.cs` MUST include the exposed placeholder JWT secret (`replace_this_with_a_very_long_secure_random_string_for_production`) in its `InsecurePlaceholders` rejection list.
- **FR-005**: Unit tests in `StartupValidationTests.cs` MUST verify rejection of both the exposed Render database password and the exposed placeholder JWT secret.
- **FR-006**: System documentation MUST include a clear operational rotation checklist for Render database credentials and JWT secret verification.

---

### Key Entities *(include if feature involves data)*

- **Database Connection String**: Secure connection metadata (`Host`, `Port`, `Database`, `Username`, `Password`, `SslMode`) passed exclusively via environment variable (`ConnectionStrings__DefaultConnection`) in cloud platforms.
- **JWT Signing Secret**: Minimum 256-bit symmetric encryption key passed via `JwtSettings__Secret` in cloud platform environment variables.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0 committed files containing live database connection strings or passwords remain in the repository working tree.
- **SC-002**: 100% of attempts to boot the backend using the leaked password or leaked placeholder secret result in immediate startup abortion.
- **SC-003**: All automated startup validation tests pass with zero regressions across the existing test suite.
- **SC-004**: Operator rotation checklist is complete and actionable for immediate Render dashboard execution.

---

## Assumptions

- The Render database is hosted on Render Postgres and can be rotated or recreated directly from the Render web dashboard without downtime impacting external customers.
- Production deployments inject configuration via container environment variables or dashboard settings, not file-based `.env` loading.
- Repository git history will be secured via credential rotation rather than destructive force-push history rewrites (which could break existing collaborator clones unless requested).
