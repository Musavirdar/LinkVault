# Phase 0 Research: Dashboard Performance & Conversational Agent Architecture

**Feature**: `008-dashboard-perf-and-agent-chat`
**Date**: 2026-11-04

## 1. Dashboard Latency & Sluggishness Analysis

### 1.1 Findings
1. **Artificial Page Entry Delay**:
   - In `app/src/components/layout/MainCanvas.tsx`, line 19 defines:
     ```tsx
     transition={{ duration: 0.4, delay: 0.2 }}
     ```
   - This enforces a mandatory 200ms delay followed by a 400ms fade-in on every single page navigation. Removing this artificial delay immediately cuts 600ms of perceived latency.
2. **Duplicate Network Round Trips**:
   - On `/dashboard` mount, `useDashboardStats` calls `resourcesApi.getAll()` and `linksApi.getAll()`.
   - Simultaneously, `DashboardPage` independently executes `loadRecentFiles()`, which calls `resourcesApi.getAll()` a second time.
   - Both calls fetch the full resource list without caching or sharing the in-flight Promise.
3. **Missing Loading Skeletons**:
   - `FileList.tsx` and `StatsGrid.tsx` render empty states or "Loading..." text without skeleton frames, causing layout shift (CLS) and visual jank once data resolves.
4. **Framer Motion Layout Recalculation**:
   - `motion.tr` on table rows in `FileList.tsx` with `variants={staggerItem}` and `whileHover={{ x: 2 }}` forces layout recalculations on table DOM nodes in Chromium, leading to dropped frames during scrolling and hover.

### 1.2 Decision: Client-Side Dashboard Consolidation & Skeletons
- Set `transition={{ duration: 0.15 }}` with `delay: 0` in `MainCanvas.tsx`.
- Consolidate dashboard fetching in a dedicated store or hook (`useDashboardData`) that fetches links and resources in a single cached promise.
- Cache the result in memory with a 30s TTL or SWR (stale-while-revalidate) pattern so switching tabs renders instantly from cache.
- Implement Apple HIG skeleton loaders for `StatsGrid` and `FileList` using subtle shimmering pulses (`animate-pulse bg-black/[0.04] dark:bg-white/[0.06] rounded-xl`).
- Replace `motion.tr` in `FileList` with hardware-accelerated CSS hover classes (`hover:bg-bg-tertiary transition-colors duration-150`) to eliminate table layout thrashing.

---

## 2. AI Agent Chat Stream & Execution Analysis

### 2.1 Findings
1. **Frontend Discarded Non-Action Replies**:
   - In `app/src/agent/pages/AgentPage.tsx`, `LiveProgressStream` is guarded by:
     ```tsx
     {steps.length > 0 && (
       <LiveProgressStream steps={steps} status={planStatus} summary={currentPlan?.summary} />
     )}
     ```
   - When the user asks "hello" or asks a question ("what files are in my workspace?"), the backend Meta model correctly generates a rich markdown response in `summary` with `steps: []`.
   - Because `steps.length === 0`, `LiveProgressStream` is NOT rendered.
   - `PlanPreviewModal` is NOT opened (`requiresUserApproval` is false).
   - `currentPlan.summary` is never displayed anywhere on the screen!
   - Result: the input spinner finishes, nothing appears, and the user believes the agent "did nothing".
2. **Lack of Conversational Chat Interface**:
   - The UI currently looks like a static form rather than an interactive assistant. There is no conversation log, no message history, and no visual thread where previous turns are preserved.
3. **Execution Gating & Status Feedback**:
   - When action plans are generated (e.g. creating folders, moving files), opening a modal dialog breaks the conversational flow.
   - Rendering verifiable action plan cards directly inline within the chat stream with "Approve" and "Reject" buttons provides a modern, seamless copilot experience.
4. **Live SignalR Event Hookup**:
   - `agentHubClient.ts` already has SignalR handlers (`onPlanStatusChanged`, `onStepStarted`, `onStepCompleted`, `onStepFailed`), but they were only tied to the hidden progress stream. Wiring them into the inline plan card in the chat stream makes step execution vivid and transparent.

### 2.2 Decision: Conversational Copilot Feed with Inline Verifiable Action Cards
- Create a dedicated conversational message model in `AgentPage`:
  ```ts
  interface ChatMessage {
    id: string;
    role: 'user' | 'assistant';
    content?: string;
    plan?: AgentActionPlanDto;
    isThinking?: boolean;
    timestamp: Date;
  }
  ```
- When the user sends any prompt:
  1. Append a user message bubble immediately.
  2. Append an assistant message bubble with `isThinking: true`.
  3. Call `agentApi.createPlan({ userGoal })`.
  4. On response:
     - Replace the thinking state with the model's markdown `summary`.
     - If `plan.steps.length > 0`, attach the interactive plan card inline inside the assistant message bubble.
     - Automatically join the SignalR plan group to stream step status updates in place.
- Provide inline action buttons ("Approve Plan", "Reject Plan") directly on the card.
- When approved, transition steps from `Pending` → `Running` → `Completed`, showing real-time green checkmarks.
- Upon plan completion, trigger background cache invalidation so that new folders or moved files appear immediately across the app without manual refresh.

---

## 3. Technology & Framework Choices
- **UI & Animations**: React 18, Tailwind CSS, Lucide icons, Framer Motion (only for message entry transitions, not for heavy table rows).
- **Markdown Rendering**: Lightweight, secure markdown formatting for assistant responses (headings, bold, bullet points, monospace code blocks).
- **Real-Time Streaming**: SignalR `@microsoft/signalr` connected to `/hubs/agent`.
- **State & Caching**: Lightweight client-side store / in-memory cache for dashboard data with event-driven invalidation.

