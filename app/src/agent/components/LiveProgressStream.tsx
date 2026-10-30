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
    <div className="bg-bg-secondary rounded-xl border border-border-primary p-5 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-text-primary text-base flex items-center gap-2">
            <span>Execution Progress</span>
            {status === 'Executing' && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-blue-500/20 text-blue-400 border border-blue-500/30 flex items-center gap-1">
                <Loader2 className="w-3 h-3 animate-spin" /> Live Streaming
              </span>
            )}
            {status === 'Completed' && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 flex items-center gap-1">
                <ShieldCheck className="w-3 h-3" /> Verifiable Execution Done
              </span>
            )}
            {status === 'Failed' && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-red-500/20 text-red-400 border border-red-500/30">
                Execution Halted
              </span>
            )}
          </h3>
          <p className="text-xs text-text-muted mt-0.5">
            {completedCount} of {steps.length} steps completed ({progressPercent}%)
          </p>
        </div>
      </div>

      {/* Progress Bar */}
      <div className="w-full bg-bg-tertiary h-2 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all duration-300 ${
            status === 'Failed'
              ? 'bg-red-500'
              : status === 'Completed'
              ? 'bg-emerald-500'
              : 'bg-accent-primary'
          }`}
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {summary && (
        <div className="p-3 bg-bg-tertiary/70 rounded-lg text-xs text-text-secondary border border-border-secondary">
          {summary}
        </div>
      )}

      {/* Steps List */}
      <div className="space-y-2 pt-1">
        {steps.map((step) => {
          const isPending = step.status === 'Pending';
          const isRunning = step.status === 'Running';
          const isCompleted = step.status === 'Completed';
          const isFailed = step.status === 'Failed';

          return (
            <div
              key={step.stepNumber}
              className={`flex items-start gap-3 p-3 rounded-lg border transition-all duration-200 ${
                isRunning
                  ? 'bg-blue-500/10 border-blue-500/30 shadow-sm'
                  : isCompleted
                  ? 'bg-emerald-500/5 border-emerald-500/20'
                  : isFailed
                  ? 'bg-red-500/10 border-red-500/30'
                  : 'bg-bg-tertiary/40 border-border-secondary'
              }`}
            >
              <div className="mt-0.5 flex-shrink-0">
                {isRunning && <Loader2 className="w-5 h-5 text-blue-400 animate-spin" />}
                {isCompleted && <CheckCircle2 className="w-5 h-5 text-emerald-400" />}
                {isFailed && <XCircle className="w-5 h-5 text-red-400" />}
                {isPending && <Clock className="w-5 h-5 text-text-muted" />}
              </div>

              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-semibold text-text-secondary flex items-center gap-1.5">
                    <span>Step {step.stepNumber}</span>
                    <ArrowRight className="w-3 h-3 text-text-muted" />
                    <span className="px-1.5 py-0.5 rounded bg-bg-primary text-text-primary text-[10px] font-mono border border-border-primary">
                      {step.toolName}
                    </span>
                  </span>
                  <span
                    className={`text-[11px] font-medium ${
                      isRunning
                        ? 'text-blue-400'
                        : isCompleted
                        ? 'text-emerald-400'
                        : isFailed
                        ? 'text-red-400'
                        : 'text-text-muted'
                    }`}
                  >
                    {step.status}
                  </span>
                </div>

                <p className="text-sm font-medium text-text-primary mt-1">
                  {step.description || `Execute ${step.toolName}`}
                </p>

                {step.errorMessage && (
                  <p className="text-xs text-red-400 mt-1 font-mono">{step.errorMessage}</p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
