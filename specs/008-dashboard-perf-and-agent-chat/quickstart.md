# Quickstart: Verifying Dashboard Performance & AI Agent Chat

**Feature**: `008-dashboard-perf-and-agent-chat`
**Date**: 2026-11-04

## 1. Quick Verification: Instant Dashboard Navigation

1. Launch frontend dev server (`cd app; npm run dev`) and backend (`cd backend; dotnet run`).
2. Log in using demo credentials (`demo.admin@linkvault.local` / `DemoPassword123!`).
3. Click between **Files**, **Directories**, and **Dashboard** in the sidebar.
   - **Verification**: The transition to the Dashboard is instant (< 300ms) with zero artificial delay or pause.
   - Skeletons render during initial cold fetch.
   - Recent files table is responsive and smooth on hover without jank or layout shift.

## 2. Quick Verification: Conversational AI Agent ("Hello" & Questions)

1. Navigate to `/agent` via the sidebar.
2. In the prompt area, type `hello` and press Enter.
   - **Verification**: The user bubble `hello` appears immediately in the chat thread.
   - The thinking status displays "Thinking with Meta Llama...".
   - The assistant's markdown response renders clearly in the chat stream with full greetings and directory breakdown.
   - The conversation thread preserves the exchange and keeps the input ready for the next prompt.

## 3. Quick Verification: Real-World Action Execution ("Create Folder & Move File")

1. In the agent prompt box, enter:
   `Create a directory called Projects and move brand_assets_vector_kit.zip into it`
2. Click **Synthesize Plan** (or press Enter).
   - **Verification**: An interactive Action Plan card appears inside the chat stream.
   - Two steps are clearly listed: `create_directory` ("Projects") and `move_resource`.
   - Click **Approve Plan**.
   - Watch the SignalR live progress stream update each step from Pending → Running → Completed with green checkmarks.
3. Navigate to **Directories** in the sidebar.
   - **Verification**: The "Projects" directory is present and contains `brand_assets_vector_kit.zip`.

