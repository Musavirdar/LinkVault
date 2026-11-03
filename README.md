# LinkVault — Enterprise Digital Workspace & Verifiable AI Agent

[![CI](https://github.com/Musavirdar/LinkVault/actions/workflows/ci.yml/badge.svg)](https://github.com/Musavirdar/LinkVault/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/React-18.0-61dafb.svg)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178c6.svg)](https://www.typescriptlang.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**LinkVault** is an enterprise-grade digital workspace and verifiable AI assistant combining secure file management, link hubs, hierarchical directories, real-time collaboration, and an autonomous AI agent powered by **Meta Muse Spark Open** (`meta-models/muse-spark-open-1026`).

Built for the **Meta Hackathon**, LinkVault Agent implements *"Agents 102: Verifiable Agents"* — autonomous multi-step planning with human-in-the-loop approval gating, live SignalR progress streaming, Safe Share secret detection, zero-trust audit attribution, and an Apple HIG frontend.

---

## 🌟 Verifiable AI Workspace Agent Highlights

| Capability | Description |
|---|---|
| **Meta Model Integration** | Powered by **Meta Muse Spark Open** (`meta-models/muse-spark-open-1026`) via Baseten's OpenAI-compatible Chat Completions API (`https://inference.baseten.co/v1`). |
| **Multi-Round Planning Loop** | Context-aware planner that executes read-only query tools (`list_directory`, `search`) immediately during planning (up to 5 rounds) to gather real workspace entity IDs, while compiling state-changing tools into an actionable plan. |
| **Human-in-the-Loop Gating** | State-altering operations (moving files, creating folders, generating public share links, deleting resources) require explicit user approval. Protected by atomic concurrency guards (`409 Conflict`). |
| **Isolated Execution & SignalR Streaming** | Approved plans execute inside dedicated `IServiceScopeFactory` scopes with live WebSocket updates (`/hubs/agent`) for step initiation, execution receipts, and completion. |
| **Safe Share Guardrail** | Two-tier secret leak detection combining deterministic regex and model verification over the first 8 KB of text files, blocking accidental sharing of database credentials, private keys, or API tokens. |
| **Native Voice Dictation** | Zero-latency speech recognition powered directly by the browser's Web Speech API (`SpeechRecognition`). |
| **One-Click Evaluator Demo** | Built-in demo mode (`DEMO_MODE=true`) with automatic workspace seeding (15 files, 5 folders, 5 links) and one-click login for Admin and Member roles without 2FA. |

---

## 🛠️ Architecture & Tech Stack

```text
├── app/                        # Frontend: React 18 + TypeScript + Vite + Tailwind CSS (Apple HIG)
│   ├── src/agent/              # Agent UI: chat, preview modal, SignalR progress stream, Safe Share alert
│   └── src/pages/              # Dashboard, Files, Links, Directories, Audit, Login, Landing
├── backend/                    # Backend API: ASP.NET Core 8 Web API + EF Core 8
│   ├── Agent/                  # Agent subsystem
│   │   ├── Controllers/        # /api/agent/plan, /approve, /reject, /safe-share/scan
│   │   ├── Hubs/               # AgentHub SignalR WebSocket endpoint
│   │   ├── Models/             # AgentActionPlan, PlanStep, SafeShareScan, MetaModelConfig
│   │   └── Services/           # MetaModelClient, VaultAgentService, SafeShareGuard
│   ├── Controllers/            # Auth, Files, Links, Directories, Audit REST controllers
│   ├── Data/                   # LinkVaultDbContext & PostgreSQL configuration
│   ├── Services/               # DemoSeedService, FileValidation, Storage, JWT, Auth
│   └── LinkVaultApi.Tests/     # xUnit test suite (71 automated unit & integration tests)
├── docker-compose.yml          # Containerized local environment (PostgreSQL & API)
└── sharedb1.sln                # Root .NET Solution file
```

---

## ⚙️ Environment Variables & Configuration

All secrets and model parameters are read exclusively from environment variables:

| Variable | Description | Default / Example |
|---|---|---|
| `META_MODEL_BASE_URL` | Baseten / Meta OpenAI-compatible endpoint | `https://inference.baseten.co/v1` |
| `META_MODEL_API_KEY` | Your Baseten or Meta Model API key | `""` (Set in your environment) |
| `META_MODEL_NAME` | Primary hackathon model | `meta-models/muse-spark-open-1026` |
| `META_GUARD_MODEL_NAME` | Optional dedicated security guard model | `""` |
| `META_VISION_MODEL_NAME` | Optional multimodal vision model | `""` |
| `DEMO_MODE` | Enables demo workspace seeding & one-click login | `"true"` (for demo/evaluator) |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Database=linkvault;...` |
| `JwtSettings__Secret` | Cryptographically random 256-bit JWT secret | (At least 32 characters) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Reverse proxy forwarded header handling | `"true"` |

---

## 🚀 Quickstart & Running the App

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) & npm
- [PostgreSQL 14+](https://www.postgresql.org/) or [Docker Desktop](https://www.docker.com/)

### 2. Configure Environment
```bash
export META_MODEL_BASE_URL="https://inference.baseten.co/v1"
export META_MODEL_API_KEY="your-baseten-api-key"
export META_MODEL_NAME="meta-models/muse-spark-open-1026"
export DEMO_MODE="true"
export JwtSettings__Secret="your-at-least-32-byte-long-secure-production-key-here"
```

*(On Windows PowerShell):*
```powershell
$env:META_MODEL_BASE_URL = "https://inference.baseten.co/v1"
$env:META_MODEL_API_KEY = "your-baseten-api-key"
$env:META_MODEL_NAME = "meta-models/muse-spark-open-1026"
$env:DEMO_MODE = "true"
$env:JwtSettings__Secret = "your-at-least-32-byte-long-secure-production-key-here"
```

### 3. Run Backend API
```powershell
cd backend
dotnet run
```
API runs on `http://localhost:5000` (or `http://localhost:5246`). Health check: `/health`.

### 4. Run Frontend Client
```powershell
cd app
npm install
npm run dev
```
Client runs on `http://localhost:5173`.

### 5. Running Automated Tests
```powershell
dotnet test backend/LinkVaultApi.Tests
```
All 71 xUnit unit and integration tests execute with zero network dependencies using fake model clients.

---

## 🎯 Evaluator Demo Instructions (One-Click)

1. Open `http://localhost:5173/login`.
2. In the **Hackathon Evaluator Quick Access** card at the top, click **"Try Demo (Admin)"**.
   - Bypasses 2FA, logs into the seeded `Acme Corp Demo Workspace`, and redirects to `/agent`.
3. Give the agent an instruction:
   - *"Organize my uploads into an Invoices folder and share the latest invoice for 7 days."*
4. Review the generated plan in the **Plan Review Modal**:
   - Inspect the referenced real file IDs, tool types, and model name.
5. Click **Approve Plan**:
   - Observe real-time SignalR progress streaming as steps transition from Pending to Completed.
6. Test Safe Share:
   - Try creating a share link for `database_backup_config.env` (seeded with test database credentials).
   - Observe the Safe Share Guard block the action to prevent unconfirmed data leakage.
7. Open `/audit`:
   - Verify zero-trust accountability with the `[via agent]` badge identifying all autonomous changes.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
