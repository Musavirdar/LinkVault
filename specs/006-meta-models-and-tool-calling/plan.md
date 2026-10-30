# Implementation Plan: Meta Model API Integration, Tool Calling Specification & Submission Integrity

**Branch**: `006-meta-models-and-tool-calling` | **Date**: 2026-10-30 | **Spec**: [specs/006-meta-models-and-tool-calling/spec.md](file:///c:/Users/darmu/OneDrive/Attachments/Desktop/LinkVault-main/specs/006-meta-models-and-tool-calling/spec.md)

**Input**: Feature specification from `specs/006-meta-models-and-tool-calling/spec.md`

---

## Summary

This plan addresses all remaining hackathon blocking items and submission integrity issues:
1. **Meta Hackathon Models**: Updates configuration to support Meta Model API, Muse Spark Open, Glimmer, and Baseten-hosted Meta endpoints (`meta-llama/Llama-3.3-70B-Instruct`, `meta-llama/Llama-Guard-3-8B`, `meta-llama/Llama-3.2-11B-Vision-Instruct`), eliminating vendor lock-in to Groq.
2. **Autonomous Tool Calling**: Injects a formal OpenAI/Meta-compatible `tools` array into chat completions requests with schemas for all 6 workspace tools, instructing the model via `"tool_choice": "auto"` and parsing real `tool_calls` into verified plan steps.
3. **Voice Recognition Authenticity**: Eliminates the fake backend speech mock (`/api/agent/transcribe` and `TranscribeAudioAsync`) and relies exclusively on browser-native `SpeechRecognition` in `VoiceCommandButton.tsx` with clear user feedback if unsupported.
4. **HACKATHON.md Submission Integrity**: Replaces inaccurate claims with verifiable facts (baseline as pre-Oct 26, security hardening as Oct 27–29 event work, transparent history notes, correct download ticket route `/api/files/{id}/ticket`, and accurate Meta model table).

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (Backend), TypeScript 5.6 / React 19 (Frontend)  
**Primary Dependencies**: ASP.NET Core, Microsoft.EntityFrameworkCore 8.0, Microsoft.AspNetCore.SignalR, HttpClient, Tailwind CSS, Lucide React  
**Storage**: PostgreSQL (Npgsql) for persistence; EF Core InMemory for unit/integration testing  
**Testing**: xUnit 2.6.2, FluentAssertions 6.12.0, Moq 4.20.70 (`dotnet test`)  
**Target Platform**: Linux/Render cloud container, Windows local dev, modern web browsers (Chrome, Edge, Safari)  
**Project Type**: Full-stack web application with verifiable autonomous AI agent  
**Performance Goals**: Model tool-calling overhead < 100ms deserialization and schema validation time  
**Constraints**: Meta Hackathon compliance (Meta models only), zero fake mocks, strict RBAC and tenant boundaries  
**Scale/Scope**: 4 backend files, 2 frontend files, 1 test file, 1 markdown showcase file  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [X] **Principle 1: Specs before Code**: Specification completed at `specs/006-meta-models-and-tool-calling/spec.md` and requirements checklist validated.
- [X] **Principle 2: Source of Truth**: Design artifacts created in `specs/006-meta-models-and-tool-calling/` (`research.md`, `data-model.md`, `contracts/agent-api.md`, `quickstart.md`).
- [X] **Principle 3: Truthfulness & Verifiability**: Deceptive mocks removed; all claims in `HACKATHON.md` grounded in verifiable code and git history.
- [X] **Principle 4: Non-Breaking Changes**: Existing API contracts (plan generation, approval, Safe Share Guard) maintained without breaking callers.

---

## Project Structure

### Documentation (this feature)

```text
specs/006-meta-models-and-tool-calling/
├── spec.md              # Feature specification
├── checklists/
│   └── requirements.md  # Requirements completeness checklist
├── plan.md              # Implementation plan (this file)
├── research.md          # Phase 0 research and technical decisions
├── data-model.md        # Phase 1 data models and schemas
├── contracts/
│   └── agent-api.md     # Phase 1 API and payload contracts
├── quickstart.md        # Phase 1 verification procedures
└── tasks.md             # Phase 2 task breakdown (via /speckit-tasks)
```

### Source Code Impact

```text
backend/
├── Agent/
│   ├── Controllers/
│   │   └── AgentController.cs           # Remove /transcribe endpoint
│   ├── Models/
│   │   ├── AgentDtos.cs                 # Remove TranscribeResponse DTO
│   │   └── MetaLlamaConfig.cs           # Configure Meta Model API & Baseten defaults
│   └── Services/
│       ├── IMetaLlamaClient.cs          # Method signatures & tool calling support
│       ├── MetaLlamaClient.cs           # Build formal tools array, tool_choice, parse tool_calls
│       ├── IVaultAgentService.cs        # Remove TranscribeAudioAsync interface method
│       └── VaultAgentService.cs         # Remove TranscribeAudioAsync implementation
├── LinkVaultApi.Tests/
│   └── Unit/
│       ├── MetaModelToolCallingTests.cs # New tests verifying tools array payload & tool_calls extraction
│       └── VaultAgentServiceTests.cs    # Update test fixtures
└── appsettings.json                     # Set default BaseUrl and Meta model names

app/
└── src/
    └── agent/
        ├── components/
        │   └── VoiceCommandButton.tsx   # Native SpeechRecognition only; clear error toast on unsupported
        └── services/
            └── agentApi.ts              # Remove transcribeAudio HTTP method

HACKATHON.md                             # Comprehensive factual correction and submission audit
```

---

## Planned Phases

### Phase 0: Research (Completed)
- Resolved Meta Model API / Baseten endpoint configuration and model strings.
- Defined formal `tools` schema array for all 6 workspace tools.
- Confirmed browser Web Speech API capabilities and identified fake backend removal.
- Mapped all `HACKATHON.md` statements against verifiable git history and code routes.

### Phase 1: Design Artifacts (Completed)
- Generated `specs/006-meta-models-and-tool-calling/research.md`.
- Generated `specs/006-meta-models-and-tool-calling/data-model.md`.
- Generated `specs/006-meta-models-and-tool-calling/contracts/agent-api.md`.
- Generated `specs/006-meta-models-and-tool-calling/quickstart.md`.

### Phase 2: Implementation (Pending `/speckit-tasks`)
- Break down implementation into atomic tasks across configuration, model tool calling, voice cleanup, test verification, and `HACKATHON.md` truth corrections.
