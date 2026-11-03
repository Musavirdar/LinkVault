import React from 'react';
import { CheckCircle2, Clock, Loader2, XCircle, ArrowRight, ShieldCheck } from 'lucide-react';
import type { PlanStepDto, PlanStatus } from '../types/agent.types';

interface LiveProgressStreamProps {
  steps: PlanStepDto[];
  status: PlanStatus;
  summary?: string;
}

export const LiveProgressStream: React.FC<LiveProgressStreamProps> = ({
  steps,
  status,
  summary,
}) => {
  const completedCount = steps.filter((s) => s.status === 'Completed').length;
  const progressPercent = steps.length > 0 ? Math.round((completedCount / steps.length) * 100) : 0;

  return (
    <div data-testid="live-progress-stream" className="bg-card text-card-foreground rounded-2xl border border-black/[0.06] dark:border-white/10 p-6 space-y-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-bold tracking-tight text-foreground text-base flex items-center gap-2">
            <span>Execution Progress</span>
            {status === 'Executing' && (
              <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-[#0071E3]/15 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/25 flex items-center gap-1.5">
                <Loader2 className="w-3.5 h-3.5 animate-spin" /> Live Streaming
              </span>
            )}
            {status === 'Completed' && (
              <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-[#34C759]/15 text-[#34C759] border border-[#34C759]/25 flex items-center gap-1.5">
                <ShieldCheck className="w-3.5 h-3.5" /> Verifiable Execution Done
              </span>
            )}
            {status === 'Failed' && (
              <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-red-500/15 text-[#FF3B30] border border-red-500/25">
                Execution Halted
              </span>
            )}
          </h3>
          <p className="text-xs text-muted-foreground mt-1">
            {completedCount} of {steps.length} steps completed ({progressPercent}%)
          </p>
        </div>
      </div>

      {/* Progress Bar */}
      <div className="w-full bg-black/[0.05] dark:bg-white/10 h-2 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all duration-300 ${
            status === 'Failed'
              ? 'bg-[#FF3B30]'
              : status === 'Completed'
              ? 'bg-[#34C759]'
              : 'bg-[#0071E3]'
          }`}
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {summary && (
        <div className="p-3.5 bg-black/[0.025] dark:bg-white/[0.04] rounded-xl text-xs text-muted-foreground border border-black/[0.04] dark:border-white/10 leading-relaxed">
          {summary}
        </div>
      )}

      {/* Steps List */}
      <div className="space-y-2.5 pt-1">
        {steps.map((step) => {
          const isPending = step.status === 'Pending';
          const isRunning = step.status === 'Running';
          const isCompleted = step.status === 'Completed';
          const isFailed = step.status === 'Failed';

          return (
            <div
              key={step.stepNumber}
              className={`flex items-start gap-3 p-3.5 rounded-xl border transition-all duration-150 ${
                isRunning
                  ? 'bg-blue-500/10 border-blue-500/30 shadow-xs'
                  : isCompleted
                  ? 'bg-emerald-500/5 border-emerald-500/20'
                  : isFailed
                  ? 'bg-red-500/10 border-red-500/30'
                  : 'bg-black/[0.02] dark:bg-white/[0.03] border-black/[0.04] dark:border-white/10'
              }`}
            >
              <div className="mt-0.5 flex-shrink-0">
                {isRunning && <Loader2 className="w-5 h-5 text-[#0071E3] dark:text-[#0A84FF] animate-spin" />}
                {isCompleted && <CheckCircle2 className="w-5 h-5 text-[#34C759]" />}
                {isFailed && <XCircle className="w-5 h-5 text-[#FF3B30]" />}
                {isPending && <Clock className="w-5 h-5 text-muted-foreground" />}
              </div>

              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                    <span>Step {step.stepNumber}</span>
                    <ArrowRight className="w-3 h-3 text-muted-foreground" />
                    <span className="px-1.5 py-0.5 rounded bg-black/[0.04] dark:bg-white/10 text-foreground text-[10px] font-mono border border-black/[0.04] dark:border-white/10">
                      {step.toolName}
                    </span>
                  </span>
                  <span
                    className={`text-[11px] font-semibold ${
                      isRunning
                        ? 'text-[#0071E3] dark:text-[#0A84FF]'
                        : isCompleted
                        ? 'text-[#34C759]'
                        : isFailed
                        ? 'text-[#FF3B30]'
                        : 'text-muted-foreground'
                    }`}
                  >
                    {step.status}
                  </span>
                </div>

                <p className="text-sm font-medium text-foreground mt-1">
                  {step.description || `Execute ${step.toolName}`}
                </p>

                {step.errorMessage && (
                  <p className="text-xs text-[#FF3B30] mt-1 font-mono">{step.errorMessage}</p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
