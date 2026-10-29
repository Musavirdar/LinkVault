# Research & Decisions: Purge Committed Credentials & Render Secret Remediation

**Feature**: `003-purge-committed-credentials`  
**Date**: 2026-10-29  

## Technical Decisions & Analysis

### 1. Secret Invalidation vs. Git History Rewriting

- **Decision**: Invalidate credentials at the source (Render Cloud) via password rotation/recreation, delete `backend/.env.render` from the current working tree and git index, and expand `.gitignore`. Do not attempt destructive history rewriting (`git filter-branch` / force-push) on public branches.
- **Rationale**: 
  - Once credentials are committed to a publicly accessible repository, they must be considered compromised immediately. Web crawlers, indexers, and public GitHub mirrors archive commits within seconds.
  - Rewriting git history via `git filter-repo` or BFG creates disconnected commit hashes that break existing collaborator clones and CI checkouts without protecting the compromised database. 
  - Real-world security protection is solely achieved by rotating the credentials on the provider (Render) so that the leaked connection string becomes completely invalid.
- **Alternatives Considered**:
  - *Git History Rewriting with Force Push*: Rejected because force-pushing rewritten history gives false confidence of security while anyone who cloned the repository during its public window still retains the secret. Credential rotation is strictly required regardless.

### 2. Comprehensive Environment File Exclusion in `.gitignore`

- **Decision**: Update `.gitignore` to match all `.env*` variations while preserving explicit safe template files:
  ```gitignore
  .env
  .env.*
  !.env.example
  *.env
  *.env.*
  ```
- **Rationale**:
  - The previous `.gitignore` specifically listed `.env`, `.env.production`, `.env.local`, `.env.*.local`, which unintentionally left `.env.render` unignored.
  - Using a wildcard `.env.*` with an explicit negative exemption `!.env.example` prevents any future custom provider files (such as `.env.render`, `.env.staging`, `.env.vercel`) from being tracked.
- **Alternatives Considered**:
  - *Explicitly adding only `.env.render`*: Rejected because it leaves open the possibility of `.env.supabase`, `.env.railway`, etc. being accidentally committed later.

### 3. Fail-Fast Blacklist Enforcement in `StartupValidator.cs`

- **Decision**: Add the specific exposed Render Postgres password and the exposed placeholder JWT secret to `StartupValidator.cs`:
  - `InsecurePasswords`: add `"QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg"`
  - `InsecurePlaceholders`: add `"replace_this_with_a_very_long_secure_random_string_for_production"`
- **Rationale**:
  - Defense-in-depth: If a developer pulls the repo or an engineer mistakenly pastes the old `.env.render` contents into their local environment or deployment settings, the application refuses to boot, directly informing them that the credentials are blacklisted/compromised.
- **Alternatives Considered**:
  - *Relying solely on Render rotation*: Rejected because blacklisting provides automated regression testing in CI and prevents developers from resurrecting dead credentials.

### 4. Operational Runbook for Render Secret Rotation

- **Decision**: Document a concise, step-by-step verification checklist in `quickstart.md` and `plan.md` for the operator to execute in the Render Cloud Console.
- **Rationale**: 
  - Automated tools cannot access the user's private Render account without API tokens. A clear, explicit runbook ensures zero ambiguity during rotation.
