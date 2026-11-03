import React from 'react';
import { Bot, User, Loader2, AlertCircle, RefreshCw, ShieldAlert } from 'lucide-react';
import type { ChatMessage } from '../types/agent.types';
import { InlinePlanCard } from './InlinePlanCard';

interface AgentMessageBubbleProps {
  message: ChatMessage;
  onApprovePlan?: (planId: string) => void;
  onRejectPlan?: (planId: string) => void;
  onRetry?: (prompt: string) => void;
  isActionLoading?: boolean;
}

/**
 * Lightweight, safe markdown formatter for assistant responses.
 * Handles headings, bold, bullet lists, inline code, and code blocks.
 */
function MarkdownRenderer({ content }: { content: string }) {
  const lines = content.split('\n');

  return (
    <div className="space-y-2 text-sm leading-relaxed text-foreground">
      {lines.map((line, idx) => {
        const trimmed = line.trim();

        // Empty line
        if (!trimmed) {
          return <div key={idx} className="h-1.5" />;
        }

        // Heading 3: ###
        if (trimmed.startsWith('### ')) {
          return (
            <h4 key={idx} className="text-sm font-bold text-foreground mt-3 mb-1">
              {formatInline(trimmed.substring(4))}
            </h4>
          );
        }

        // Heading 2 / 1: ## or #
        if (trimmed.startsWith('## ') || trimmed.startsWith('# ')) {
          const text = trimmed.startsWith('## ') ? trimmed.substring(3) : trimmed.substring(2);
          return (
            <h3 key={idx} className="text-base font-bold text-foreground mt-3 mb-1">
              {formatInline(text)}
            </h3>
          );
        }

        // Bullet point: - or *
        if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
          return (
            <div key={idx} className="flex items-start gap-2 ml-2 my-1">
              <span className="text-[#0071E3] dark:text-[#0A84FF] text-base leading-none select-none">•</span>
              <span className="flex-1 text-sm">{formatInline(trimmed.substring(2))}</span>
            </div>
          );
        }

        // Numbered list item: 1. 2. etc.
        const numMatch = trimmed.match(/^(\d+)\.\s+(.*)/);
        if (numMatch) {
          return (
            <div key={idx} className="flex items-start gap-2 ml-2 my-1">
              <span className="text-xs font-semibold text-[#0071E3] dark:text-[#0A84FF] select-none pt-0.5">
                {numMatch[1]}.
              </span>
              <span className="flex-1 text-sm">{formatInline(numMatch[2])}</span>
            </div>
          );
        }

        // Standard paragraph
        return (
          <p key={idx} className="text-sm text-foreground/90">
            {formatInline(trimmed)}
          </p>
        );
      })}
    </div>
  );
}

/**
 * Formats inline bold (**text**) and code (`text`) tokens
 */
function formatInline(text: string): React.ReactNode[] {
  const parts: React.ReactNode[] = [];
  const regex = /(`[^`]+`|\*\*[^*]+\*\*)/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push(text.substring(lastIndex, match.index));
    }
    const token = match[0];
    if (token.startsWith('`') && token.endsWith('`')) {
      parts.push(
        <code
          key={match.index}
          className="px-1.5 py-0.5 rounded-md bg-black/[0.06] dark:bg-white/10 font-mono text-xs text-[#0071E3] dark:text-[#0A84FF]"
        >
          {token.slice(1, -1)}
        </code>
      );
    } else if (token.startsWith('**') && token.endsWith('**')) {
      parts.push(
        <strong key={match.index} className="font-semibold text-foreground">
          {token.slice(2, -2)}
        </strong>
      );
    }
    lastIndex = regex.lastIndex;
  }

  if (lastIndex < text.length) {
    parts.push(text.substring(lastIndex));
  }

  return parts;
}

export function AgentMessageBubble({
  message,
  onApprovePlan,
  onRejectPlan,
  onRetry,
  isActionLoading = false,
}: AgentMessageBubbleProps) {
  const isUser = message.role === 'user';

  if (isUser) {
    return (
      <div className="flex items-start justify-end gap-3 max-w-[85%] ml-auto animate-in fade-in duration-150">
        <div className="rounded-2xl rounded-tr-sm px-4 py-3 bg-[#0071E3] text-white shadow-sm">
          <p className="text-sm leading-relaxed whitespace-pre-wrap">{message.content}</p>
          <span className="text-[10px] text-white/70 block text-right mt-1.5">
            {new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
        <div className="w-8 h-8 rounded-full bg-[#0071E3]/20 border border-[#0071E3]/30 flex items-center justify-center flex-shrink-0 text-[#0071E3] dark:text-[#0A84FF]">
          <User className="w-4 h-4" />
        </div>
      </div>
    );
  }

  const isSecurityAlert = message.errorMessage?.toLowerCase().includes('safe share guard') ||
    message.errorMessage?.toLowerCase().includes('secret') ||
    message.errorMessage?.toLowerCase().includes('credential');

  return (
    <div className="flex items-start gap-3 max-w-[92%] mr-auto animate-in fade-in duration-150">
      <div className="w-8 h-8 rounded-full bg-[#0071E3]/15 border border-[#0071E3]/25 flex items-center justify-center flex-shrink-0 text-[#0071E3] dark:text-[#0A84FF] mt-1">
        <Bot className="w-4 h-4" />
      </div>

      <div className="flex-1 space-y-3">
        {/* Thinking State */}
        {message.isThinking ? (
          <div className="rounded-2xl rounded-tl-sm p-4 bg-card border border-black/[0.06] dark:border-white/10 shadow-sm flex items-center gap-3 animate-pulse">
            <Loader2 className="w-4 h-4 animate-spin text-[#0071E3] dark:text-[#0A84FF]" />
            <span className="text-xs font-medium text-text-secondary">
              Thinking with Meta Llama...
            </span>
          </div>
        ) : message.errorMessage ? (
          /* Error or Security Alert State */
          <div
            className={`rounded-2xl rounded-tl-sm p-4 border space-y-2.5 ${
              isSecurityAlert
                ? 'bg-amber-500/10 border-amber-500/30 text-amber-700 dark:text-amber-300'
                : 'bg-red-500/10 border-red-500/25 text-red-500 dark:text-red-400'
            }`}
          >
            <div className="flex items-center gap-2 text-xs font-semibold">
              {isSecurityAlert ? (
                <ShieldAlert className="w-4 h-4 text-amber-500 flex-shrink-0" />
              ) : (
                <AlertCircle className="w-4 h-4 flex-shrink-0" />
              )}
              <span>{isSecurityAlert ? 'Safe Share Guard Intercepted' : 'Agent Request Error'}</span>
            </div>
            <p className="text-xs leading-relaxed">{message.errorMessage}</p>
            {onRetry && !isSecurityAlert && (
              <button
                type="button"
                onClick={() => onRetry(message.content || '')}
                className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-medium bg-red-500/20 hover:bg-red-500/30 text-red-600 dark:text-red-300 transition-colors"
              >
                <RefreshCw className="w-3 h-3" />
                Retry
              </button>
            )}
          </div>
        ) : (
          /* Assistant Response Bubble */
          <div className="rounded-2xl rounded-tl-sm p-5 bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.03)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.25)] space-y-3">
            {message.content && <MarkdownRenderer content={message.content} />}

            {/* Inline Action Plan Card if steps exist */}
            {message.plan && message.plan.steps && message.plan.steps.length > 0 && (
              <InlinePlanCard
                plan={message.plan}
                onApprove={onApprovePlan}
                onReject={onRejectPlan}
                isLoading={isActionLoading}
              />
            )}

            <div className="flex items-center justify-between pt-2 border-t border-black/[0.04] dark:border-white/5 text-[11px] text-text-muted">
              <span>{message.plan?.modelName || 'meta-models/muse-spark-open'}</span>
              <span>
                {new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
