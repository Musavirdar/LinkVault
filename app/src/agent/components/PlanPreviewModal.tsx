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
        return <FolderPlus className="w-4 h-4 text-amber-400" />;
      case 'move_resource':
        return <Move className="w-4 h-4 text-blue-400" />;
      case 'create_share_link':
        return <Share2 className="w-4 h-4 text-purple-400" />;
      case 'search':
        return <Search className="w-4 h-4 text-teal-400" />;
      default:
        return <ListFilter className="w-4 h-4 text-text-muted" />;
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-bg-primary border border-border-primary w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh] animate-in fade-in duration-200">
        {/* Header */}
        <div className="p-5 border-b border-border-primary bg-bg-secondary/50 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-accent-primary/10 text-accent-primary">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-semibold text-text-primary text-base">
                Verifiable Plan Review (One-Click Approval)
              </h3>
              <p className="text-xs text-text-secondary">
                Review proposed workspace operations before granting execution permission
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-md text-text-muted hover:text-text-primary hover:bg-bg-tertiary transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">
          <div className="p-4 bg-bg-secondary rounded-xl border border-border-secondary space-y-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-text-muted block">
              User Directive
            </span>
            <p className="text-sm font-medium text-text-primary">
              "{plan.userGoal}"
            </p>
            {plan.summary && (
              <p className="text-xs text-text-secondary pt-1 border-t border-border-secondary">
                {plan.summary}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wider text-text-muted">
                Proposed Sequence ({plan.steps.length} Steps)
              </span>
              <span className="text-xs text-amber-400 bg-amber-400/10 px-2 py-0.5 rounded-full border border-amber-400/20">
                Requires Approval
              </span>
            </div>

            <div className="space-y-2.5">
              {plan.steps.map((step) => (
                <div
                  key={step.stepNumber}
                  className="flex items-start gap-3 p-3.5 bg-bg-secondary/70 rounded-xl border border-border-secondary hover:border-border-primary transition-all"
                >
                  <div className="mt-0.5 p-2 rounded-lg bg-bg-tertiary flex-shrink-0">
                    {getToolIcon(step.toolName)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-text-secondary">
                        Step {step.stepNumber}
                      </span>
                      <ArrowRight className="w-3 h-3 text-text-muted" />
                      <span className="px-2 py-0.5 rounded-md bg-bg-tertiary text-text-primary text-xs font-mono font-medium">
                        {step.toolName}
                      </span>
                      {step.requiresApproval && (
                        <span className="text-[10px] text-amber-500 font-medium">
                          • State Modification
                        </span>
                      )}
                    </div>
                    <p className="text-sm font-medium text-text-primary mt-1">
                      {step.description || `Invoke ${step.toolName}`}
                    </p>
                    {step.parameters && Object.keys(step.parameters).length > 0 && (
                      <pre className="text-[11px] text-text-muted mt-1.5 p-2 rounded-md bg-bg-tertiary/70 font-mono overflow-x-auto">
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
        <div className="p-4 bg-bg-secondary border-t border-border-primary flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={onReject}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium rounded-xl border border-red-500/30 text-red-400 hover:bg-red-500/10 transition-colors flex items-center gap-2"
          >
            <X className="w-4 h-4" /> Reject Plan
          </button>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 text-sm font-medium rounded-xl text-text-secondary hover:bg-bg-tertiary transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={onApprove}
              disabled={isSubmitting}
              className="px-5 py-2 text-sm font-semibold rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-600/20 transition-all flex items-center gap-2"
            >
              <Check className="w-4 h-4" /> Approve & Execute Plan
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
