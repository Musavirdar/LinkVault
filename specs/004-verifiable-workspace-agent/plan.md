# Implementation Plan: Verifiable Workspace Agent (LinkVault Agent)

**Branch**: `004-verifiable-workspace-agent` | **Date**: 2026-10-29 | **Spec**: [specs/004-verifiable-workspace-agent/spec.md](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/specs/004-verifiable-workspace-agent/spec.md)

**Input**: Feature specification from `/specs/004-verifiable-workspace-agent/spec.md`

## Summary

Build the LinkVault Autonomous Workspace Agent — an actionable, verifiable AI assistant powered by **Meta Llama 3.3 (70B Instruct)**, **Llama 3.2 Vision (11B)**, and **Llama Guard 3 (8B)** that acts directly on workspace files, directories, and share links under the authenticated user's permissions. The agent decomposes high-level goals into multi-step tool calls, pauses for one-click human approval before modifying resources, streams execution progress live over SignalR, guards against secret leakage via Safe Share, and tags all resulting audit events with `via agent`.

## Technical Context

**Language/Version**: C# (.NET 8.0) for backend; TypeScript 5.0 & React 18 for frontend.  
**Primary Dependencies**:
- Backend: `Microsoft.AspNetCore.SignalR`, `Microsoft.EntityFrameworkCore` (8.0), `Npgsql.EntityFrameworkCore.PostgreSQL`, `System.Text.Json`, `HttpClient`.
- Frontend: `React 18`, `lucide-react`, `tailwindcss`, `@microsoft/signalr`, `axios`/`fetch`.
- Inference: Meta Llama 3.3 (70B Instruct), Llama 3.1 (8B Instruct), Llama 3.2 Vision (11B), Llama Guard 3 (8B) via OpenAI-compatible tool-calling endpoints (Groq / Together / Ollama) with intelligent local fallback.

**Storage**: PostgreSQL 16 (with EF Core migrations) for `AgentActionPlan`, `PlanStep`, `SafeShareScan`, and `AuditLog`.  
**Testing**: `xUnit`, `FluentAssertions`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`.  
**Target Platform**: Linux / Windows containerized deployments (`docker-compose.yml`), Render Web Services with platform proxy headers.  
**Project Type**: Full-stack enterprise web application with REST API and real-time WebSocket hub.  
**Performance Goals**:
- Plan generation turnaround < 2.5 seconds.
- Real-time step progress streaming latency < 500 milliseconds.
- Safe Share regex & heuristic scan < 100 milliseconds.

**Constraints**:
- Zero-Trust Permission Parity: The agent cannot perform any action that the user lacks RBAC claims or tenant access to perform directly.
- Human-in-the-Loop Gating: All state-altering actions (`move`, `create_directory`, `create_share_link`, `delete`) require explicit one-click user approval.
- Zero Committed Credentials: Model API keys (`LLAMA_API_KEY`, `GROQ_API_KEY`) passed exclusively via environment variables.

**Scale/Scope**: Multi-tenant organizations with thousands of files and links; plans up to 20 atomic steps.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Gate | Assessment | Status |
|---|---|---|
| **Simplicity First** | Builds directly on existing `ResourceService`, `FileService`, and `AuditService` without redundant abstraction layers or complex external vector databases. | PASS |
| **Single Responsibility** | Clean separation of concerns: `MetaLlamaClient` handles inference; `VaultAgentService` handles planning & tool execution; `SafeShareGuard` handles pre-share scanning; `AgentHub` handles live streaming. | PASS |
| **API-First** | Fully documented REST endpoints in `/api/agent/*` and typed SignalR hub contracts in `/hubs/agent`. | PASS |
| **Test Coverage** | Comprehensive unit tests for planning, tool execution, Safe Share blocking, and audit tagging, plus end-to-end integration scenarios. | PASS |
| **Containerization & Docker** | Runs seamlessly in existing `docker-compose.yml` with no additional required services. | PASS |
| **No New Frameworks** | Utilizes existing .NET 8 Web API, SignalR, and React/Tailwind frontend. | PASS |

## Project Structure

### Documentation (this feature)

```text
specs/004-verifiable-workspace-agent/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Architectural decisions & model mapping (/speckit-plan output)
├── data-model.md        # Entity definitions & state transitions (/speckit-plan output)
├── quickstart.md        # Runnable verification guide (/speckit-plan output)
├── contracts/           # API & SignalR schemas (/speckit-plan output)
│   └── agent-api.md
├── checklists/          # Requirements completeness checklist
│   └── requirements.md
└── tasks.md             # Work item breakdown (/speckit-tasks output)
```

### Source Code Layout

```text
backend/
├── Agent/
│   ├── Controllers/
│   │   └── AgentController.cs        # REST endpoints (plan, approve, reject, safe-share, transcribe)
│   ├── Hubs/
│   │   └── AgentHub.cs               # SignalR live step progress streaming
│   ├── Models/
│   │   ├── AgentActionPlan.cs        # Entity: Multi-step plan definition
│   │   ├── PlanStep.cs               # Entity: Atomic tool step
│   │   ├── SafeShareScan.cs          # Entity: Pre-share secret detection result
│   │   ├── MetaLlamaConfig.cs        # Configuration for Meta Llama models & providers
│   │   └── AgentDtos.cs              # DTOs for plan requests, responses, and tools
│   └── Services/
│       ├── IMetaLlamaClient.cs       # Client abstraction for Meta Llama 3.3, 3.2 Vision, Llama Guard 3
│       ├── MetaLlamaClient.cs        # Implementation with tool calling & local fallback
│       ├── IVaultAgentService.cs     # Planning loop & tool orchestrator interface
│       ├── VaultAgentService.cs      # Core agent runner with RBAC & audit attribution
│       ├── ISafeShareGuard.cs        # Credential & PII scanner interface
│       └── SafeShareGuard.cs         # Regex + Llama Guard 3 pre-share scanner
├── LinkVaultApi.Tests/
│   └── Unit/
│       ├── VaultAgentServiceTests.cs # Planning, tool execution, and approval tests
│       └── SafeShareGuardTests.cs    # Secret detection and link blocking tests
└── Program.cs                        # Service registration & SignalR hub mapping

app/src/
├── agent/
│   ├── components/
│   │   ├── VaultAgentChat.tsx        # Interactive chat & plan preview card
│   │   ├── PlanPreviewModal.tsx      # Step preview with Approve/Reject buttons
│   │   ├── LiveProgressStream.tsx    # SignalR real-time execution visualizer
│   │   ├── SafeShareAlertModal.tsx   # Pre-share secret warning dialog
│   │   ├── VoiceCommandButton.tsx    # Speech-to-text input button
│   │   └── AuditInsightsCard.tsx     # Cybersecurity anomaly review card
│   ├── pages/
│   │   └── AgentPage.tsx             # Full AI Workspace & Security Agent page
│   ├── services/
│   │   ├── agentApi.ts               # Client API methods
│   │   └── agentHubClient.ts         # SignalR hub connection manager
│   └── types/
│       └── agent.types.ts            # TypeScript interfaces
├── components/layout/
│   └── ChannelSidebar.tsx            # Navigation link to /agent with Meta Llama badge
└── App.tsx                           # Route registration for /agent
```

## Complexity Tracking

| Issue | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| SignalR Hub for agent progress | Live step-by-step progress feedback during multi-step execution | Polling HTTP status creates excessive server load and latency |
| Safe Share dual-layer scan (regex + model) | Immediate blocking of database passwords and API keys before link creation | Pure LLM scan is slower and can occasionally miss strict token formats; regex alone misses unstructured secrets |
