import React from 'react';
import { Check, X, ShieldAlert, ArrowRight, FolderPlus, Move, Share2, Search, ListFilter } from 'lucide-react';
import type { AgentActionPlanDto } from '../types/agent.types';

interface PlanPreviewModalProps {
  plan: AgentActionPlanDto | null;
  isOpen: boolean;
  isSubmitting?: boolean;
  onApprove: () => void;
  onReject: () => void;
  onClose: () => void;
}

export const PlanPreviewModal: React.FC<PlanPreviewModalProps> = ({
  plan,
  isOpen,
  isSubmitting = false,
  onApprove,
  onReject,
  onClose,
}) => {
  if (!isOpen || !plan) return null;

  const getToolIcon = (toolName: string) => {
    switch (toolName) {
      case 'create_directory':
        return <FolderPlus className="w-4 h-4 text-[#FF9500]" />;
      case 'move_resource':
        return <Move className="w-4 h-4 text-[#0071E3]" />;
      case 'create_share_link':
        return <Share2 className="w-4 h-4 text-purple-500" />;
      case 'search':
        return <Search className="w-4 h-4 text-teal-500" />;
      default:
        return <ListFilter className="w-4 h-4 text-muted-foreground" />;
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-md animate-in fade-in duration-200">
      <div data-testid="plan-preview-modal" className="bg-white/95 dark:bg-[#1C1C1E]/95 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 w-full max-w-2xl rounded-3xl shadow-[0_24px_60px_rgba(0,0,0,0.22)] overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-6 border-b border-black/[0.06] dark:border-white/10 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-2xl bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF]">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold tracking-tight text-foreground text-base">
                Verifiable Plan Review (One-Click Approval)
              </h3>
              <p className="text-xs text-muted-foreground">
                Review proposed workspace operations before granting execution permission
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-full bg-black/[0.05] dark:bg-white/10 text-muted-foreground hover:text-foreground transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1 custom-scrollbar">
          <div className="p-4 bg-black/[0.025] dark:bg-white/[0.04] rounded-2xl border border-black/[0.04] dark:border-white/10 space-y-2">
            <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground block">
              User Directive
            </span>
            <p className="text-sm font-medium text-foreground">
              "{plan.userGoal}"
            </p>
            {plan.modelName && (
              <span className="text-[11px] font-mono text-muted-foreground block">
                Model: {plan.modelName}
              </span>
            )}
            {plan.summary && (
              <p className="text-xs text-muted-foreground pt-1.5 border-t border-black/[0.04] dark:border-white/10">
                {plan.summary}
              </p>
            )}
          </div>

          <div className="space-y-2.5">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Proposed Sequence ({plan.steps.length} Steps)
              </span>
              <span className="text-xs text-[#FF9500] bg-[#FF9500]/10 px-2.5 py-0.5 rounded-full border border-[#FF9500]/20 font-medium">
                Requires Approval
              </span>
            </div>

            <div className="space-y-2.5">
              {plan.steps.map((step) => (
                <div
                  key={step.stepNumber}
                  className="flex items-start gap-3 p-4 bg-black/[0.02] dark:bg-white/[0.03] rounded-2xl border border-black/[0.04] dark:border-white/10 hover:border-[#0071E3]/30 transition-all duration-150"
                >
                  <div className="mt-0.5 p-2 rounded-xl bg-black/[0.04] dark:bg-white/10 flex-shrink-0">
                    {getToolIcon(step.toolName)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-foreground">
                        Step {step.stepNumber}
                      </span>
                      <ArrowRight className="w-3 h-3 text-muted-foreground" />
                      <span className="px-2 py-0.5 rounded-md bg-black/[0.04] dark:bg-white/10 text-foreground text-xs font-mono font-medium">
                        {step.toolName}
                      </span>
                      {step.requiresApproval && (
                        <span className="text-[10px] text-[#FF9500] font-medium">
                          • State Modification
                        </span>
                      )}
                    </div>
                    <p className="text-sm font-medium text-foreground mt-1">
                      {step.description || `Invoke ${step.toolName}`}
                    </p>
                    {step.parameters && Object.keys(step.parameters).length > 0 && (
                      <pre className="text-[11px] text-muted-foreground mt-2 p-2.5 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] font-mono overflow-x-auto border border-black/[0.04] dark:border-white/10">
                        {JSON.stringify(step.parameters, null, 2)}
                      </pre>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Footer Actions */}
        <div className="p-4 sm:p-5 bg-black/[0.02] dark:bg-white/[0.02] border-t border-black/[0.06] dark:border-white/10 flex items-center justify-between gap-3">
          <button
            type="button"
            data-testid="reject-plan-btn" onClick={onReject}
            disabled={isSubmitting}
            className="min-h-[44px] px-4 py-2 text-sm font-semibold rounded-xl border border-red-500/30 text-[#FF3B30] hover:bg-red-500/10 active:scale-[0.98] transition-all flex items-center gap-2"
          >
            <X className="w-4 h-4" /> Reject Plan
          </button>

          <div className="flex items-center gap-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="min-h-[44px] px-4 py-2 text-sm font-medium rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.04] dark:hover:bg-white/10 active:scale-[0.98] transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              data-testid="approve-plan-btn" onClick={onApprove}
              disabled={isSubmitting}
              className="min-h-[44px] px-5 py-2.5 text-sm font-semibold rounded-xl bg-[#34C759] hover:bg-[#30D158] active:bg-[#28B04C] text-white shadow-sm shadow-green-500/20 active:scale-[0.98] transition-all flex items-center gap-2"
            >
              <Check className="w-4 h-4" /> Approve & Execute Plan
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
