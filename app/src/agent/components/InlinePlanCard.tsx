import React from 'react';
import {
  CheckCircle2,
  Clock,
  AlertTriangle,
  Play,
  XCircle,
  Loader2,
  ShieldCheck,
  FolderPlus,
  Move,
  Share2,
  Trash2,
  Eye,
  FileSearch,
} from 'lucide-react';
import type { AgentActionPlanDto, PlanStepDto } from '../types/agent.types';
import { cn } from '@/lib/utils';

interface InlinePlanCardProps {
  plan: AgentActionPlanDto;
  onApprove?: (planId: string) => void;
  onReject?: (planId: string) => void;
  isLoading?: boolean;
}

const toolIcons: Record<string, typeof Play> = {
  create_directory: FolderPlus,
  move_resource: Move,
  create_share_link: Share2,
  delete_resource: Trash2,
  classify_image: Eye,
  search: FileSearch,
  list_directory: FolderPlus,
};

export function InlinePlanCard({
  plan,
  onApprove,
  onReject,
  isLoading = false,
}: InlinePlanCardProps) {
  const planId = plan.id || plan.planId;
  const isAwaitingApproval = plan.status === 'AwaitingApproval' || (plan.requiresUserApproval && plan.status === 'Draft');
  const isExecuting = plan.status === 'Executing';
  const isCompleted = plan.status === 'Completed';
  const isRejected = plan.status === 'Rejected';
  const isFailed = plan.status === 'Failed';

  return (
    <div className="rounded-xl border border-black/[0.08] dark:border-white/12 bg-black/[0.02] dark:bg-white/[0.03] p-4 space-y-3 mt-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <ShieldCheck className="w-4 h-4 text-[#0071E3] dark:text-[#0A84FF]" />
          <span className="text-xs font-bold uppercase tracking-wider text-text-primary">
            Verifiable Action Plan ({plan.steps.length} {plan.steps.length === 1 ? 'step' : 'steps'})
          </span>
        </div>

        {/* Status Badge */}
        <span
          className={cn(
            'px-2.5 py-0.5 rounded-full text-[11px] font-semibold flex items-center gap-1',
            isAwaitingApproval && 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20',
            isExecuting && 'bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/20 animate-pulse',
            isCompleted && 'bg-[#34C759]/10 text-[#34C759] border border-[#34C759]/20',
            isRejected && 'bg-black/[0.05] dark:bg-white/10 text-text-muted border border-black/[0.05]',
            isFailed && 'bg-red-500/10 text-red-500 border border-red-500/20'
          )}
        >
          {isExecuting && <Loader2 className="w-3 h-3 animate-spin" />}
          {isCompleted && <CheckCircle2 className="w-3 h-3" />}
          {isRejected && <XCircle className="w-3 h-3" />}
          {isFailed && <AlertTriangle className="w-3 h-3" />}
          <span>{plan.status}</span>
        </span>
      </div>

      {/* Step Breakdown */}
      <div className="space-y-2">
        {plan.steps.map((step) => {
          const Icon = toolIcons[step.toolName] || Play;
          const isStepRunning = step.status === 'Running';
          const isStepCompleted = step.status === 'Completed';
          const isStepFailed = step.status === 'Failed';

          return (
            <div
              key={step.stepNumber}
              className={cn(
                'flex items-center justify-between p-2.5 rounded-lg border text-xs transition-all',
                isStepRunning && 'bg-[#0071E3]/5 border-[#0071E3]/30',
                isStepCompleted && 'bg-[#34C759]/5 border-[#34C759]/20',
                isStepFailed && 'bg-red-500/5 border-red-500/30 text-red-500',
                !isStepRunning && !isStepCompleted && !isStepFailed && 'bg-card border-black/[0.04] dark:border-white/8'
              )}
            >
              <div className="flex items-center gap-2.5 min-w-0">
                <div
                  className={cn(
                    'w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0',
                    isStepCompleted ? 'bg-[#34C759]/15 text-[#34C759]' : 'bg-black/[0.04] dark:bg-white/10 text-text-secondary'
                  )}
                >
                  {isStepRunning ? (
                    <Loader2 className="w-3.5 h-3.5 animate-spin text-[#0071E3]" />
                  ) : isStepCompleted ? (
                    <CheckCircle2 className="w-3.5 h-3.5" />
                  ) : (
                    <Icon className="w-3.5 h-3.5" />
                  )}
                </div>

                <div className="min-w-0">
                  <span className="font-semibold text-text-primary block truncate">
                    {step.description || `Step ${step.stepNumber}: ${step.toolName}`}
                  </span>
                  {step.parameters && Object.keys(step.parameters).length > 0 && (
                    <span className="text-[10px] text-text-muted font-mono truncate block">
                      {JSON.stringify(step.parameters).slice(0, 60)}
                      {JSON.stringify(step.parameters).length > 60 ? '...' : ''}
                    </span>
                  )}
                </div>
              </div>

              <span className="text-[10px] text-text-muted uppercase font-medium ml-2 flex-shrink-0">
                {step.status}
              </span>
            </div>
          );
        })}
      </div>

      {/* Action Buttons for AwaitingApproval */}
      {isAwaitingApproval && onApprove && onReject && planId && (
        <div className="flex items-center justify-end gap-2 pt-2 border-t border-black/[0.04] dark:border-white/5">
          <button
            type="button"
            disabled={isLoading}
            onClick={() => onReject(planId)}
            className="px-3 py-1.5 rounded-lg text-xs font-medium text-text-secondary hover:text-text-primary hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors disabled:opacity-40"
          >
            Reject Plan
          </button>
          <button
            type="button"
            disabled={isLoading}
            onClick={() => onApprove(planId)}
            className="px-4 py-1.5 rounded-lg text-xs font-semibold bg-[#0071E3] hover:bg-[#0077ED] text-white transition-all shadow-sm shadow-[#0071E3]/20 disabled:opacity-40 flex items-center gap-1.5 active:scale-95"
          >
            {isLoading ? <Loader2 className="w-3 h-3 animate-spin" /> : <Play className="w-3 h-3 fill-current" />}
            <span>Approve & Execute</span>
          </button>
        </div>
      )}
    </div>
  );
}


export default InlinePlanCard;
