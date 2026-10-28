# AI Assistant Guidelines — GitHub Spec Kit (Spec-Driven Development)

This repository is integrated with official **[GitHub Spec Kit](https://github.com/github/spec-kit)** for Spec-Driven Development (SDD).

## Core Principles
1. **Specs before Code**: Never write code or tests before a feature specification is approved.
2. **Source of Truth**: Specs in `specs/[###-feature-name]/` are the ultimate source of truth.
3. **Constitution Adherence**: Check `.specify/constitution.md` before approving specs or plans.

## Installed Spec Kit Skills
All official GitHub Spec Kit skills are installed in `.agents/skills/`:
- `speckit-constitution` — Establish and manage project principles
- `speckit-specify` — Create or update baseline feature specification
- `speckit-clarify` — Ask structured questions to de-risk ambiguous areas before planning
- `speckit-checklist` — Generate quality checklists to validate requirements completeness
- `speckit-plan` — Create implementation plan from approved spec
- `speckit-tasks` — Generate actionable, atomic tasks
- `speckit-analyze` — Cross-artifact consistency & alignment report
- `speckit-implement` — Execute implementation task by task
- `speckit-converge` — Assess codebase against spec and verify completion

## Supported Commands
The assistant responds to both official `speckit-*` commands and standard shorthand:

| Command / Trigger | Skill Invoked | Workflow |
|---|---|---|
| `/speckit-specify [desc]` or `/spec [desc]` | `speckit-specify` | Create `specs/[###-feature-name]/spec.md` with prioritized user stories & acceptance scenarios |
| `/speckit-clarify` | `speckit-clarify` | Clarify ambiguous spec requirements before planning |
| `/speckit-plan [feature]` or `/plan [feature]` | `speckit-plan` | Run `.specify/scripts/powershell/setup-plan.ps1`, verify gates, create `plan.md` |
| `/speckit-tasks [feature]` or `/tasks [feature]` | `speckit-tasks` | Run `.specify/scripts/powershell/setup-tasks.ps1`, break plan into `tasks.md` |
| `/speckit-implement [feature]` or `/implement [feature]` | `speckit-implement` | Execute tasks sequentially, tracking checkboxes |
| `/speckit-converge [feature]` or `/converge [feature]` | `speckit-converge` | Verify all Given/When/Then scenarios and close spec |
