import React from 'react';

export function StatCardSkeleton() {
  return (
    <div className="rounded-2xl p-5 bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)] animate-pulse">
      <div className="flex items-center justify-between mb-3">
        <div className="h-4 w-20 bg-black/[0.06] dark:bg-white/10 rounded-md" />
        <div className="w-8 h-8 rounded-xl bg-black/[0.05] dark:bg-white/10" />
      </div>
      <div className="h-7 w-28 bg-black/[0.08] dark:bg-white/15 rounded-lg mb-2" />
      <div className="h-3 w-16 bg-black/[0.04] dark:bg-white/10 rounded-md" />
    </div>
  );
}

export function StatsGridSkeleton() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {Array.from({ length: 4 }).map((_, idx) => (
        <StatCardSkeleton key={idx} />
      ))}
    </div>
  );
}

export function FileListSkeleton() {
  return (
    <div className="rounded-2xl overflow-hidden bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)] animate-pulse">
      <div className="px-4 py-3 border-b border-black/[0.06] dark:border-white/10 flex items-center justify-between">
        <div className="h-5 w-24 bg-black/[0.06] dark:bg-white/10 rounded-md" />
      </div>
      <div className="p-4 space-y-3">
        {Array.from({ length: 5 }).map((_, idx) => (
          <div key={idx} className="flex items-center justify-between py-2 border-b border-black/[0.03] dark:border-white/5 last:border-0">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-black/[0.06] dark:bg-white/10" />
              <div className="space-y-1.5">
                <div className="h-4 w-40 bg-black/[0.07] dark:bg-white/12 rounded" />
                <div className="h-3 w-20 bg-black/[0.04] dark:bg-white/8 rounded" />
              </div>
            </div>
            <div className="h-3 w-16 bg-black/[0.05] dark:bg-white/10 rounded hidden sm:block" />
            <div className="h-3 w-12 bg-black/[0.05] dark:bg-white/10 rounded" />
          </div>
        ))}
      </div>
    </div>
  );
}

