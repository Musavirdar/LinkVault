import React, { useState, useEffect } from 'react';
import { useStore } from '@/store/useStore';
import { agentApi } from '../services/agentApi';
import {
  startAgentHubConnection,
  joinAgentPlanGroup,
  subscribeToAgentEvents,
  stopAgentHubConnection,
} from '../services/agentHubClient';
import type {
  AgentActionPlanDto,
  PlanStepDto,
  PlanStatus,
  SafeShareScanResultDto,
} from '../types/agent.types';
import { VaultAgentChat } from '../components/VaultAgentChat';
import { PlanPreviewModal } from '../components/PlanPreviewModal';
import { LiveProgressStream } from '../components/LiveProgressStream';
import { SafeShareAlertModal } from '../components/SafeShareAlertModal';
import { AuditInsightsCard } from '../components/AuditInsightsCard';
import { Bot, Shield, AlertCircle } from 'lucide-react';

export const AgentPage: React.FC = () => {
  const { token } = useStore();
  const [currentPlan, setCurrentPlan] = useState<AgentActionPlanDto | null>(null);
  const [steps, setSteps] = useState<PlanStepDto[]>([]);
  const [planStatus, setPlanStatus] = useState<PlanStatus>('Draft');
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false);
  const [isPlanModalOpen, setIsPlanModalOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [safeShareScan, setSafeShareScan] = useState<SafeShareScanResultDto | null>(null);
  const [isSafeShareModalOpen, setIsSafeShareModalOpen] = useState(false);

  // Initialize SignalR Hub Connection
  useEffect(() => {
    if (!token) return;

    let unsubscribe: (() => void) | null = null;

    const setupHub = async () => {
      try {
        await startAgentHubConnection(token);
        unsubscribe = subscribeToAgentEvents({
          onPlanStatusChanged: (e) => {
            setPlanStatus(e.status);
          },
          onStepStarted: (e) => {
            setSteps((prev) =>
              prev.map((s) =>
                s.stepNumber === e.stepNumber
                  ? { ...s, status: 'Running', description: e.description || s.description }
                  : s
              )
            );
          },
          onStepCompleted: (e) => {
            setSteps((prev) =>
              prev.map((s) =>
                s.stepNumber === e.stepNumber
                  ? { ...s, status: 'Completed', result: e.result }
                  : s
              )
            );
          },
          onStepFailed: (e) => {
            setSteps((prev) =>
              prev.map((s) =>
                s.stepNumber === e.stepNumber
                  ? { ...s, status: 'Failed', errorMessage: e.errorMessage }
                  : s
              )
            );
            setPlanStatus('Failed');
          },
          onPlanFinished: (e) => {
            setPlanStatus(e.status);
          },
        });
      } catch (err) {
        console.warn('SignalR Agent Hub connection warning:', err);
      }
    };

    setupHub();

    return () => {
      if (unsubscribe) unsubscribe();
      stopAgentHubConnection();
    };
  }, [token]);

  // Handle plan creation
  const handleGeneratePlan = async (userGoal: string) => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const plan = await agentApi.createPlan({ userGoal });
      setCurrentPlan(plan);
      setSteps(plan.steps || []);
      setPlanStatus(plan.status);

      // Subscribe to SignalR group for live updates
      if (plan.id || plan.planId) {
        await joinAgentPlanGroup(plan.id || plan.planId!);
      }

      // If plan requires approval, present preview modal
      if (plan.requiresUserApproval || plan.status === 'AwaitingApproval') {
        setIsPlanModalOpen(true);
      }
    } catch (err: any) {
      setErrorMessage(err.message || 'Failed to synthesize plan with Meta Llama.');
    } finally {
      setIsLoading(false);
    }
  };

  // Handle approval
  const handleApprovePlan = async () => {
    if (!currentPlan) return;
    const planId = currentPlan.id || currentPlan.planId;
    if (!planId) return;

    setIsSubmittingApproval(true);
    try {
      const res = await agentApi.approvePlan(planId);
      setPlanStatus(res.status);
      setIsPlanModalOpen(false);
    } catch (err: any) {
      // Check if Safe Share blocked it
      if (err.message && err.message.toLowerCase().includes('safe share guard')) {
        setSafeShareScan({
          resourceId: currentPlan.id,
          fileName: 'Shared Document',
          status: 'Blocked',
          riskScore: 95,
          detectedThreats: ['Exposed Database Credentials or API Secrets'],
          canShare: false,
          message: err.message,
          scannedAt: new Date().toISOString(),
        });
        setIsSafeShareModalOpen(true);
        setIsPlanModalOpen(false);
      } else {
        setErrorMessage(err.message || 'Failed to approve plan.');
      }
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  // Handle rejection
  const handleRejectPlan = async () => {
    if (!currentPlan) return;
    const planId = currentPlan.id || currentPlan.planId;
    if (!planId) return;

    setIsSubmittingApproval(true);
    try {
      const res = await agentApi.rejectPlan(planId);
      setPlanStatus(res.status);
      setSteps((prev) =>
        prev.map((s) => (s.status === 'Pending' ? { ...s, status: 'Skipped' } : s))
      );
      setIsPlanModalOpen(false);
    } catch (err: any) {
      setErrorMessage(err.message || 'Failed to reject plan.');
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6 animate-in fade-in duration-200">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border-primary pb-5">
        <div>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-accent-primary/10 text-accent-primary">
              <Bot className="w-7 h-7" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-text-primary">
                Verifiable Workspace Agent
              </h1>
              <p className="text-sm text-text-secondary">
                Autonomous task execution with one-click approval gating & zero-trust audit attribution
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="px-3 py-1 text-xs font-semibold rounded-full bg-blue-500/10 text-blue-400 border border-blue-500/30">
            Powered by Meta Llama 3.3
          </span>
          <span className="px-3 py-1 text-xs font-semibold rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 flex items-center gap-1.5">
            <Shield className="w-3.5 h-3.5" />
            <span>Verifiable Agent</span>
          </span>
        </div>
      </div>

      {/* Error Banner */}
      {errorMessage && (
        <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 flex items-start gap-3">
          <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
          <div className="text-sm">
            <span className="font-semibold block">Agent Error</span>
            {errorMessage}
          </div>
        </div>
      )}

      {/* Agent Chat Prompt Card */}
      <VaultAgentChat onSubmitGoal={handleGeneratePlan} isLoading={isLoading} />

      {/* Live Progress Stream (if plan exists) */}
      {steps.length > 0 && (
        <LiveProgressStream
          steps={steps}
          status={planStatus}
          summary={currentPlan?.summary}
        />
      )}

      {/* Verifiable Audit & Security Insights */}
      <AuditInsightsCard />

      {/* Plan Preview Modal (Approval Gate) */}
      <PlanPreviewModal
        plan={currentPlan}
        isOpen={isPlanModalOpen}
        isSubmitting={isSubmittingApproval}
        onApprove={handleApprovePlan}
        onReject={handleRejectPlan}
        onClose={() => setIsPlanModalOpen(false)}
      />

      {/* Safe Share Alert Modal */}
      <SafeShareAlertModal
        scanResult={safeShareScan}
        isOpen={isSafeShareModalOpen}
        onClose={() => setIsSafeShareModalOpen(false)}
      />
    </div>
  );
};
