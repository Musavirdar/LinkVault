import React, { useState } from 'react';
import { Send, Sparkles, Loader2, Bot, Shield, Lightbulb } from 'lucide-react';
import { VoiceCommandButton } from './VoiceCommandButton';

interface VaultAgentChatProps {
  onSubmitGoal: (goal: string) => void;
  isLoading?: boolean;
}

const SUGGESTED_PROMPTS = [
  'Organize my loose invoices into an Invoices folder and share the latest invoice for 7 days',
  'Group all financial documents and reports into Finance/Q3',
  'Scan my uploaded configuration files with Safe Share Guard before sharing',
  'Sort recent image uploads and screenshots into thematic folders by visual category',
];

export const VaultAgentChat: React.FC<VaultAgentChatProps> = ({
  onSubmitGoal,
  isLoading = false,
}) => {
  const [goal, setGoal] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!goal.trim() || isLoading) return;
    onSubmitGoal(goal.trim());
  };

  const handleSelectPrompt = (prompt: string) => {
    setGoal(prompt);
  };

  return (
    <div className="bg-card text-card-foreground rounded-2xl border border-black/[0.06] dark:border-white/10 p-6 space-y-6 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)] backdrop-blur-sm">
      {/* Header with Meta Badge */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-3 rounded-2xl bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/20 flex items-center justify-center">
            <Bot className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold tracking-tight text-foreground">LinkVault Workspace Agent</h2>
              <span className="px-2.5 py-0.5 text-[11px] font-semibold tracking-wide uppercase rounded-full bg-[#0071E3]/10 text-[#0071E3] dark:text-[#0A84FF] border border-[#0071E3]/20">
                Meta Llama 3.3
              </span>
            </div>
            <p className="text-xs text-muted-foreground leading-relaxed">
              Verifiable action planning • Safe Share credential guardrail • Real-time live execution
            </p>
          </div>
        </div>

        <div className="hidden sm:flex items-center gap-1.5 text-xs font-medium text-[#34C759] dark:text-[#30D158] bg-[#34C759]/10 px-3 py-1.5 rounded-full border border-[#34C759]/20">
          <Shield className="w-3.5 h-3.5" />
          <span>Verifiable Mode Active</span>
        </div>
      </div>

      {/* Suggested Prompts */}
      <div className="space-y-2.5">
        <span className="text-[11px] font-semibold text-muted-foreground flex items-center gap-1.5 uppercase tracking-wider">
          <Lightbulb className="w-3.5 h-3.5 text-[#FF9500]" />
          <span>Suggested Agent Commands</span>
        </span>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
          {SUGGESTED_PROMPTS.map((prompt, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => handleSelectPrompt(prompt)}
              className="text-left p-3 rounded-xl bg-black/[0.025] dark:bg-white/[0.05] hover:bg-black/[0.05] dark:hover:bg-white/[0.09] border border-black/[0.04] dark:border-white/10 text-xs text-muted-foreground hover:text-foreground transition-all duration-150 line-clamp-2 active:scale-[0.98]"
            >
              "{prompt}"
            </button>
          ))}
        </div>
      </div>

      {/* Input Form */}
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="relative rounded-2xl border border-black/[0.08] dark:border-white/12 bg-black/[0.02] dark:bg-white/[0.04] focus-within:border-[#0071E3] focus-within:ring-4 focus-within:ring-[#0071E3]/15 transition-all duration-150">
          <textarea
            value={goal}
            onChange={(e) => setGoal(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handleSubmit(e);
              }
            }}
            placeholder="Type or speak a workspace instruction (e.g., 'Organize my invoices into an Invoices folder and share the latest invoice for 7 days')..."
            rows={3}
            disabled={isLoading}
            className="w-full bg-transparent px-4 py-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none resize-none"
          />

          <div className="p-2.5 border-t border-black/[0.04] dark:border-white/10 flex items-center justify-between bg-black/[0.02] dark:bg-white/[0.02] rounded-b-2xl">
            <div className="flex items-center gap-2">
              <VoiceCommandButton
                onTranscript={(text) => setGoal(text)}
                disabled={isLoading}
              />
              <span className="text-[11px] text-muted-foreground hidden sm:inline">
                Press Enter to plan • Click mic to dictate
              </span>
            </div>

            <button
              type="submit"
              disabled={isLoading || !goal.trim()}
              className="min-h-[44px] px-5 py-2.5 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] active:bg-[#0066CC] disabled:opacity-40 text-white text-sm font-semibold flex items-center gap-2 transition-all shadow-sm shadow-[#0071E3]/25 active:scale-[0.98]"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Planning with Llama...</span>
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4" />
                  <span>Synthesize Plan</span>
                  <Send className="w-3.5 h-3.5" />
                </>
              )}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};
