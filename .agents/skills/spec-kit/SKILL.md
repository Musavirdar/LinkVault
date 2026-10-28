---
name: spec-kit
description: >-
  Implements Spec-Driven Development (SDD) using github/spec-kit for this project.
  Activate when the user asks to spec a feature, write a specification, plan an implementation,
  break down tasks, implement tasks, run /spec, /plan, /tasks, /implement, /converge, or mentions "spec-kit", "specify", or "SDD".
---

# Spec Kit — Antigravity Integration

## What is Spec Kit?

[github/spec-kit](https://github.com/github/spec-kit) is an open source toolkit
implementing **Spec-Driven Development (SDD)**: specifications drive code, not the
other way around. The spec is the source of truth; code is its expression.

### SDD Power Inversion
Traditional: Code is truth. Specs serve code.  
SDD: Specs are truth. Code serves specs.

---

## Project Setup

For this project (LinkVault), spec-kit files live at:

```
.specify/
├── constitution.md              # Project rules all specs must follow
├── templates/
│   ├── spec-template.md         # Feature specification template
│   ├── plan-template.md         # Implementation plan template
│   └── tasks-template.md        # Task breakdown template
memory/                          # AI agent memory (auto-managed)

specs/                           # Created per feature (at project root)
└── [###-feature-name]/
    ├── spec.md
    ├── plan.md
    └── tasks.md
```

---

## Commands & Workflows

When the user asks to spec, plan, break down tasks, or implement, or types `/spec`, `/plan`, `/tasks`, `/implement`, `/converge`, follow this workflow:

### `/spec [description]` or "Spec feature [description]" — Write a Feature Specification

**Purpose**: Transform an idea or requirement into a complete, testable specification.

**Workflow**:
1. Read `.specify/constitution.md` to understand project constraints.
2. Ask clarifying questions to understand the feature requirements (use `/grill-me` interactive style).
3. Read `.specify/templates/spec-template.md`.
4. Create `specs/[###-feature-name]/spec.md` with:
   - Prioritized user stories (P1, P2, P3...)
   - Given/When/Then acceptance scenarios for each story
   - Out-of-scope items
   - Resolved questions
5. Present the spec to the user for review.

---

### `/plan [feature]` or "Plan feature [feature]" — Write an Implementation Plan

**Purpose**: Turn an approved spec into a concrete technical plan.

**Workflow**:
1. Read the spec at `specs/[###-feature-name]/spec.md`.
2. Verify against `.specify/constitution.md` quality gates.
3. Review the codebase (`backend/`, `app/`, `docker-compose.yml`).
4. Read `.specify/templates/plan-template.md`.
5. Write `specs/[###-feature-name]/plan.md` with technical architecture, phases, and file changes.
6. Present the plan to the user for approval.

---

### `/tasks [feature]` or "Create tasks for [feature]" — Break Plan into Tasks

**Purpose**: Create an ordered, trackable task list from the plan.

**Workflow**:
1. Read `specs/[###-feature-name]/plan.md`.
2. Break phases into atomic, executable tasks.
3. Read `.specify/templates/tasks-template.md`.
4. Write `specs/[###-feature-name]/tasks.md` with status checkboxes.

---

### `/implement [feature]` or "Implement [feature]" — Execute Tasks

**Purpose**: Implement tasks from `tasks.md` sequentially.

**Workflow**:
1. Read `specs/[###-feature-name]/tasks.md`.
2. Find the first incomplete task `[ ]`.
3. Implement it cleanly and test/verify.
4. Mark it `[x]` in `tasks.md`.
5. Confirm with the user before proceeding to the next task.

---

### `/converge [feature]` or "Verify [feature]" — Verify & Close

**Purpose**: Verify all acceptance scenarios pass and complete the feature.

**Workflow**:
1. Read `specs/[###-feature-name]/spec.md` acceptance scenarios.
2. Verify each scenario passes.
3. Update `spec.md` status from `Draft` to `Complete`.
4. Check that all tasks are `[x]` in `tasks.md`.

---

## SDD Principles to Follow

1. **Spec before code**: Never write implementation before a spec exists.
2. **Constitution first**: Always check `.specify/constitution.md` before planning.
3. **Acceptance-driven**: Every feature must have Given/When/Then scenarios.
4. **Prioritize ruthlessly**: P1 must be a viable MVP on its own.
5. **One task at a time**: Implement sequentially, not all at once.
6. **Update specs**: If implementation reveals spec gaps, update the spec first.
