import React, { useState, useEffect, useRef } from 'react';
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
  ChatMessage,
  PlanStepDto,
  SafeShareScanResultDto,
} from '../types/agent.types';
import { VaultAgentChat } from '../components/VaultAgentChat';
import { AgentMessageBubble } from '../components/AgentMessageBubble';
import { SafeShareAlertModal } from '../components/SafeShareAlertModal';
import { AuditInsightsCard } from '../components/AuditInsightsCard';
import { invalidateDashboardCache } from '@/hooks/useDashboardData';
import { Bot, Shield, Trash2 } from 'lucide-react';

const INITIAL_MESSAGES: ChatMessage[] = [
  {
    id: 'welcome-msg',
    role: 'assistant',
    content: "👋 Hello! I am your **LinkVault Verifiable Workspace Agent** powered by Meta Llama.\n\nI can help you:\n- **Explore & summarize** your workspace files and directories\n- **Organize & move** files into structured folders\n- **Generate secure share links** with expiration limits\n- **Audit & scan** documents with Safe Share Guard\n\nAsk me anything or choose a suggested workspace command below!",
    timestamp: new Date().toISOString(),
  },
];

export const AgentPage: React.FC = () => {
  const { token } = useStore();
  const [messages, setMessages] = useState<ChatMessage[]>(INITIAL_MESSAGES);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false);
  const [safeShareScan, setSafeShareScan] = useState<SafeShareScanResultDto | null>(null);
  const [isSafeShareModalOpen, setIsSafeShareModalOpen] = useState(false);
  const [activePlanId, setActivePlanId] = useState<string | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom of conversation
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  // Initialize SignalR Hub Connection
  useEffect(() => {
    if (!token) return;

    let unsubscribe: (() => void) | null = null;

    const setupHub = async () => {
      try {
        await startAgentHubConnection(token);
        unsubscribe = subscribeToAgentEvents({
          onPlanStatusChanged: (e) => {
            setMessages((prev) =>
              prev.map((msg) =>
                msg.plan && (msg.plan.id === e.planId || msg.plan.planId === e.planId)
                  ? { ...msg, plan: { ...msg.plan, status: e.status } }
                  : msg
              )
            );
          },
          onStepStarted: (e) => {
            setMessages((prev) =>
              prev.map((msg) => {
                if (!msg.plan || (msg.plan.id !== e.planId && msg.plan.planId !== e.planId)) return msg;
                const updatedSteps: PlanStepDto[] = msg.plan.steps.map((s) =>
                  s.stepNumber === e.stepNumber
                    ? { ...s, status: 'Running', description: e.description || s.description }
                    : s
                );
                return { ...msg, plan: { ...msg.plan, steps: updatedSteps } };
              })
            );
          },
          onStepCompleted: (e) => {
            setMessages((prev) =>
              prev.map((msg) => {
                if (!msg.plan || (msg.plan.id !== e.planId && msg.plan.planId !== e.planId)) return msg;
                const updatedSteps: PlanStepDto[] = msg.plan.steps.map((s) =>
                  s.stepNumber === e.stepNumber
                    ? { ...s, status: 'Completed', result: e.result }
                    : s
                );
                return { ...msg, plan: { ...msg.plan, steps: updatedSteps } };
              })
            );
          },
          onStepFailed: (e) => {
            setMessages((prev) =>
              prev.map((msg) => {
                if (!msg.plan || (msg.plan.id !== e.planId && msg.plan.planId !== e.planId)) return msg;
                const updatedSteps: PlanStepDto[] = msg.plan.steps.map((s) =>
                  s.stepNumber === e.stepNumber
                    ? { ...s, status: 'Failed', errorMessage: e.errorMessage }
                    : s
                );
                return { ...msg, plan: { ...msg.plan, status: 'Failed', steps: updatedSteps } };
              })
            );

            if (e.errorMessage && e.errorMessage.toLowerCase().includes('safe share guard')) {
              setSafeShareScan({
                resourceId: 'blocked',
                fileName: 'Protected Resource',
                status: 'Blocked',
                riskScore: 98,
                detectedThreats: ['Sensitive Configuration or Secret Intercepted'],
                canShare: false,
                message: e.errorMessage,
                scannedAt: new Date().toISOString(),
              });
              setIsSafeShareModalOpen(true);
            }
          },
          onPlanFinished: (e) => {
            setMessages((prev) =>
              prev.map((msg) =>
                msg.plan && (msg.plan.id === e.planId || msg.plan.planId === e.planId)
                  ? { ...msg, plan: { ...msg.plan, status: e.status } }
                  : msg
              )
            );
            // Invalidate dashboard and workspace caches so newly created folders or moved files appear immediately
            invalidateDashboardCache();
          },
        });
      } catch (err) {
        console.warn('SignalR Agent Hub warning:', err);
      }
    };

    setupHub();

    return () => {
      if (unsubscribe) unsubscribe();
      stopAgentHubConnection();
    };
  }, [token]);

  // Handle plan creation / chat prompt submission
  const handleGeneratePlan = async (userGoal: string) => {
    const userMessageId = `user-${Date.now()}`;
    const assistantMessageId = `assistant-${Date.now()}`;

    // 1. Add User Message
    const userMessage: ChatMessage = {
      id: userMessageId,
      role: 'user',
      content: userGoal,
      timestamp: new Date().toISOString(),
    };

    // 2. Add Assistant Thinking Placeholder
    const thinkingMessage: ChatMessage = {
      id: assistantMessageId,
      role: 'assistant',
      isThinking: true,
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage, thinkingMessage]);
    setIsLoading(true);

    try {
      const plan: AgentActionPlanDto = await agentApi.createPlan({ userGoal });
      const planId = plan.planId || plan.id;
      setActivePlanId(planId);

      // Subscribe to SignalR group for live updates
      if (planId) {
        await joinAgentPlanGroup(planId);
      }

      // Update the assistant message with the real model summary and verifiable plan
      setMessages((prev) =>
        prev.map((m) =>
          m.id === assistantMessageId
            ? {
                ...m,
                isThinking: false,
                content: plan.summary || 'I have analyzed your request.',
                plan: plan,
              }
            : m
        )
      );
    } catch (err: any) {
      setMessages((prev) =>
        prev.map((m) =>
          m.id === assistantMessageId
            ? {
                ...m,
                isThinking: false,
                errorMessage: err.message || 'Failed to synthesize response with Meta model.',
              }
            : m
        )
      );
    } finally {
      setIsLoading(false);
    }
  };

  // Handle approval of an action plan
  const handleApprovePlan = async (planId: string) => {
    setIsSubmittingApproval(true);

    // Optimistically update plan status to 'Executing'
    setMessages((prev) =>
      prev.map((msg) =>
        msg.plan && (msg.plan.id === planId || msg.plan.planId === planId)
          ? {
              ...msg,
              plan: {
                ...msg.plan,
                status: 'Executing',
                steps: msg.plan.steps.map((s, idx) => (idx === 0 ? { ...s, status: 'Running' } : s)),
              },
            }
          : msg
      )
    );

    try {
      const res = await agentApi.approvePlan(planId);
      setMessages((prev) =>
        prev.map((msg) =>
          msg.plan && (msg.plan.id === planId || msg.plan.planId === planId)
            ? { ...msg, plan: { ...msg.plan, status: res.status } }
            : msg
        )
      );
      // Invalidate dashboard and workspace files
      invalidateDashboardCache();
    } catch (err: any) {
      if (err.message && err.message.toLowerCase().includes('safe share guard')) {
        setSafeShareScan({
          resourceId: planId,
          fileName: 'Shared Document',
          status: 'Blocked',
          riskScore: 95,
          detectedThreats: ['Exposed Database Credentials or API Secrets'],
          canShare: false,
          message: err.message,
          scannedAt: new Date().toISOString(),
        });
        setIsSafeShareModalOpen(true);
      } else {
        setMessages((prev) =>
          prev.map((msg) =>
            msg.plan && (msg.plan.id === planId || msg.plan.planId === planId)
              ? { ...msg, plan: { ...msg.plan, status: 'Failed', errorMessage: err.message } }
              : msg
          )
        );
      }
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  // Handle rejection of an action plan
  const handleRejectPlan = async (planId: string) => {
    setIsSubmittingApproval(true);
    try {
      const res = await agentApi.rejectPlan(planId);
      setMessages((prev) =>
        prev.map((msg) =>
          msg.plan && (msg.plan.id === planId || msg.plan.planId === planId)
            ? {
                ...msg,
                plan: {
                  ...msg.plan,
                  status: res.status,
                  steps: msg.plan.steps.map((s) => (s.status === 'Pending' ? { ...s, status: 'Skipped' } : s)),
                },
              }
            : msg
        )
      );
    } catch (err: any) {
      console.error('Failed to reject plan:', err);
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  const handleClearHistory = () => {
    setMessages(INITIAL_MESSAGES);
    setActivePlanId(null);
  };

  return (
    <div className="w-full max-w-[1100px] mx-auto space-y-6 animate-in fade-in duration-150">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-black/[0.06] dark:border-white/10 pb-5">
        <div>
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-2xl bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/20 flex items-center justify-center">
              <Bot className="w-7 h-7" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-text-primary tracking-tight">
                Verifiable Workspace Agent
              </h1>
              <p className="text-sm text-text-secondary">
                Conversational assistant with one-click approval gating & zero-trust audit attribution
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {messages.length > 1 && (
            <button
              type="button"
              onClick={handleClearHistory}
              className="px-3 py-1.5 rounded-full text-xs font-medium text-text-muted hover:text-text-primary hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors flex items-center gap-1.5"
              title="Clear conversation history"
            >
              <Trash2 className="w-3.5 h-3.5" />
              <span>Clear Chat</span>
            </button>
          )}
          <span
            data-testid="agent-model-badge"
            className="px-3 py-1 text-xs font-semibold rounded-full bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/20"
          >
            Model: meta-models/muse-spark-open
          </span>
          <span className="px-3 py-1 text-xs font-semibold rounded-full bg-[#34C759]/10 text-[#34C759] border border-[#34C759]/20 flex items-center gap-1.5">
            <Shield className="w-3.5 h-3.5" />
            <span>Verifiable Agent</span>
          </span>
        </div>
      </div>

      {/* Conversation Thread Feed */}
      <div className="space-y-4 min-h-[220px] max-h-[600px] overflow-y-auto p-4 rounded-2xl bg-black/[0.015] dark:bg-white/[0.02] border border-black/[0.04] dark:border-white/8 custom-scrollbar">
        {messages.map((msg) => (
          <AgentMessageBubble
            key={msg.id}
            message={msg}
            onApprovePlan={handleApprovePlan}
            onRejectPlan={handleRejectPlan}
            onRetry={handleGeneratePlan}
            isActionLoading={isSubmittingApproval}
          />
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Agent Chat Prompt & Suggestions Input */}
      <VaultAgentChat onSubmitGoal={handleGeneratePlan} isLoading={isLoading} />

      {/* Verifiable Audit & Security Insights */}
      <AuditInsightsCard />

      {/* Safe Share Alert Modal */}
      <SafeShareAlertModal
        scanResult={safeShareScan}
        isOpen={isSafeShareModalOpen}
        onClose={() => setIsSafeShareModalOpen(false)}
      />
    </div>
  );
};
