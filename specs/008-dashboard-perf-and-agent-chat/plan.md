# Implementation Plan: Dashboard Performance & Conversational Agent Chat

**Branch**: `008-dashboard-perf-and-agent-chat` | **Date**: 2026-11-04 | **Spec**: [specs/008-dashboard-perf-and-agent-chat/spec.md](spec.md)

**Input**: Feature specification from `specs/008-dashboard-perf-and-agent-chat/spec.md`

## Summary

Eliminate perceived dashboard lag and file loading delays by removing artificial animation pauses (`delay: 0.2s`), consolidating parallel API queries into a unified in-memory cached hook (`useDashboardData`), adding Apple-grade loading skeletons, and replacing Framer Motion layout thrashing on table rows with GPU-accelerated CSS.

Overhaul the AI Agent UI from a static form with discarded responses into a conversational chat stream that renders user bubbles, live model thinking states, and assistant markdown responses for all queries (including "hello" and exploratory prompts). For action-oriented requests, render verifiable action plan cards directly within the stream with inline "Approve" / "Reject" controls, live SignalR step execution indicators, and automatic post-execution workspace cache revalidation.

## Technical Context

**Language/Version**: TypeScript 5.3 (React 18 frontend) / C# .NET 8 (ASP.NET Core Web API backend)

**Primary Dependencies**:
- Frontend: React 18, React Router v6, Tailwind CSS, Lucide React, Framer Motion, `@microsoft/signalr`
- Backend: ASP.NET Core 8, Entity Framework Core, SignalR, Meta Model Client (`meta-models/muse-spark-open` via Baseten inference)

**Storage**: In-memory EF Core database (development) / PostgreSQL (production), local file storage

**Testing**:
- Frontend: Vitest / React Testing Library, TypeScript typecheck (`tsc --noEmit`), Vite build check
- Backend: xUnit (`dotnet test`), integration/unit agent execution tests

**Target Platform**: Web browsers (Chrome, Safari, Firefox, Edge) / Windows, macOS, Linux

**Performance Goals**:
- Dashboard page navigation & initial render < **300ms** (down from > 600ms)
- Cached tab switching < **10ms** with zero layout shift
- Conversational assistant replies visible **100% of the time** on receipt

**Constraints**:
- Zero breaking changes to existing REST endpoints (`/api/resources`, `/api/links`, `/api/agent/plan`)
- Zero external package additions required
- Preserve Safe Share Guard security checks and approval gating

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Rule | Compliance Status | Analysis |
|---|---|---|
| **Specs before Code** | **PASSED** | Specification created and approved in `specs/008-dashboard-perf-and-agent-chat/spec.md`. |
| **Simplicity & Performance** | **PASSED** | Consolidates client state, removes artificial layout delays, avoids heavy DOM thrashing. |
| **Non-Destructive Autonomy** | **PASSED** | State-changing agent actions remain gated behind explicit user approval. |
| **Security & Guardrails** | **PASSED** | Safe Share Guard retains authority to intercept sensitive credential leaks. |

## Project Structure

### Documentation (this feature)

```text
specs/008-dashboard-perf-and-agent-chat/
├── spec.md              # Feature specification
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 research & architectural decisions
├── data-model.md        # Phase 1 data models & state diagrams
├── quickstart.md        # Developer validation guide
├── contracts/
│   ├── agent-chat-api.yaml  # OpenAPI spec for agent chat & approval
│   └── dashboard-perf.md    # Performance SLA & hook contract
└── checklists/
    └── requirements.md      # Specification validation checklist
```

### Source Code

```text
app/
├── src/
│   ├── agent/
│   │   ├── components/
│   │   │   ├── VaultAgentChat.tsx       # Overhauled conversational chat input & feed
│   │   │   ├── AgentMessageBubble.tsx   # Markdown response & user bubble component
│   │   │   ├── InlinePlanCard.tsx       # Inline verifiable action card with approval buttons
│   │   │   └── LiveProgressStream.tsx   # Step-by-step progress tracking
│   │   ├── pages/
│   │   │   └── AgentPage.tsx            # Main conversational agent view with active thread
│   │   └── services/
│   │       ├── agentApi.ts              # Agent API calls
│   │       └── agentHubClient.ts        # SignalR connection & event subscribers
│   ├── components/
│   │   ├── dashboard/
│   │   │   ├── FileList.tsx             # Optimized table with skeleton states & CSS hover
│   │   │   ├── StatsGrid.tsx            # Shimmer skeleton loader support
│   │   │   └── DashboardSkeleton.tsx    # Unified shimmering skeleton layout
│   │   └── layout/
│   │       └── MainCanvas.tsx           # Zero artificial delay on route transitions
│   ├── hooks/
│   │   ├── useDashboardData.ts          # Consolidated & cached dashboard hook
│   │   └── useDashboardStats.ts         # Backward-compatible stats hook
│   └── pages/
│       └── DashboardPage.tsx            # Integrated with useDashboardData
```

## Phase 0: Research Decisions Summary

1. **Remove MainCanvas Delay**: Immediate 600ms perceived speedup by dropping `delay: 0.2` and shortening fade duration to 150ms.
2. **Consolidate Dashboard Network Requests**: Single hook `useDashboardData` combines resources & links into a shared cached structure with 30s TTL.
3. **Table Row Layout Optimization**: Replaced `motion.tr` layout animation with standard CSS hardware-accelerated transitions to avoid frame drops on Chromium.
4. **Conversational Agent Feed**: Store `messages: ChatMessage[]` in `AgentPage` so that when `POST /api/agent/plan` returns a summary with 0 steps (e.g. "hello" or exploratory queries), it renders immediately as an assistant message bubble with full markdown support.
5. **Inline Action Plan Cards**: When steps are present, display the action plan card with approval buttons directly in the chat feed. SignalR updates step progress in real time.
6. **Workspace Auto-Revalidation**: When an action plan completes execution, trigger store invalidation so `/directories` and `/files` update instantly.

## Phase 1: Design Artifacts

- **Data Models**: Documented in [`data-model.md`](data-model.md).
- **API Contracts**: Documented in [`contracts/agent-chat-api.yaml`](contracts/agent-chat-api.yaml) and [`contracts/dashboard-perf.md`](contracts/dashboard-perf.md).
- **Quickstart Guide**: Documented in [`quickstart.md`](quickstart.md).
