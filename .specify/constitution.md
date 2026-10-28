# LinkVault Project Constitution

> This constitution defines the guiding principles, constraints, and standards
> that ALL feature specs and implementation plans must follow.
> Source: github/spec-kit Spec-Driven Development (SDD)

## Project Overview

**Project**: LinkVault — A bookmark/link management application
**Stack**: .NET backend, frontend app (see `app/` and `backend/` directories)
**Source**: [github/spec-kit](https://github.com/github/spec-kit)

---

## Architectural Principles

- **Simplicity First**: Prefer the simplest solution that meets requirements. Avoid over-engineering.
- **Single Responsibility**: Each module/service should have one clear purpose.
- **API-First**: Backend changes should be designed around clean, versioned APIs.
- **Test Coverage**: Every new feature must include acceptance scenario tests.

## Technology Constraints

- **Backend**: .NET (C#) — see `backend/` directory
- **Frontend**: See `app/` directory
- **Containerization**: Docker Compose is the standard deployment mechanism (`docker-compose.yml`)
- **No New Frameworks** without spec approval and documented rationale.

## Quality Gates (must pass before implementation begins)

- [ ] Feature spec reviewed and accepted
- [ ] Implementation plan reviewed
- [ ] Acceptance scenarios defined (Given/When/Then)
- [ ] No duplicate functionality introduced
- [ ] Docker Compose compatibility maintained

## Naming Conventions

- Specs live in: `specs/[###-feature-name]/`
- Feature branches: `###-feature-name`
- Tasks file is always: `specs/[###-feature-name]/tasks.md`
- Spec file is always: `specs/[###-feature-name]/spec.md`
- Plan file is always: `specs/[###-feature-name]/plan.md`

## Process

This project follows **Spec-Driven Development (SDD)** via [github/spec-kit](https://github.com/github/spec-kit).

1. **Spec** → Write a feature spec using `.specify/templates/spec-template.md`
2. **Plan** → Write an implementation plan using `.specify/templates/plan-template.md`
3. **Tasks** → Break plan into tasks using `.specify/templates/tasks-template.md`
4. **Implement** → Execute tasks in order, updating task status
5. **Converge** → Verify acceptance scenarios pass, update spec if needed
