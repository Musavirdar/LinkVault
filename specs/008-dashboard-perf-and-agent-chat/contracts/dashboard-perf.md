# Dashboard Performance & Consolidating Contract

**Feature**: `008-dashboard-perf-and-agent-chat`
**Date**: 2026-11-04

## 1. Consolidated Data Contract

### Hook Interface: `useDashboardData()`

```typescript
export interface DashboardData {
  stats: {
    totalLinks: number;
    activeLinks: number;
    totalFiles: number;
    totalViews: number;
    totalClicks: number;
  };
  recentFiles: FileItem[];
  isLoading: boolean;
  isInitialLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}
```

### SLA & Latency Guarantees
- **Cold Navigation**: MainCanvas renders skeleton state within **< 50ms**.
- **Data Load**: Single parallel query resolves and hydrates stats + recent files within **< 300ms**.
- **Warm Navigation (Tab Switching)**: Rendered from in-memory cache in **< 10ms** with zero flash of empty content or layout shift.
- **Cache TTL**: 30 seconds fresh duration; revalidates on window focus or explicit mutations (`onUploaded`, `onDeleted`, `onAgentExecuted`).

### CSS & Hardware Acceleration Rules
- No `framer-motion` layout animations on table rows (`motion.tr`).
- Table rows use GPU-composited CSS transforms and opacity for hover highlights:
  ```css
  tr.recent-file-row {
    transition: background-color 150ms ease, transform 150ms ease;
    will-change: transform;
  }
  ```
- Skeleton elements use hardware-accelerated CSS shimmer without JavaScript animation loops.

