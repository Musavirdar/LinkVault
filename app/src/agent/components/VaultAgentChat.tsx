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
    <div className="bg-bg-secondary rounded-2xl border border-border-primary p-6 space-y-5 shadow-sm">
      {/* Header with Meta Badge */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-accent-primary/10 text-accent-primary border border-accent-primary/20">
            <Bot className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold text-text-primary">LinkVault Workspace Agent</h2>
              <span className="px-2 py-0.5 text-[10px] font-semibold tracking-wide uppercase rounded-full bg-blue-500/10 text-blue-400 border border-blue-500/30">
                Meta Llama 3.3
              </span>
            </div>
            <p className="text-xs text-text-muted">
              Verifiable action planning • Safe Share credential guardrail • Real-time live execution
            </p>
          </div>
        </div>

        <div className="hidden sm:flex items-center gap-1.5 text-xs text-emerald-400 bg-emerald-500/10 px-3 py-1.5 rounded-lg border border-emerald-500/20">
          <Shield className="w-3.5 h-3.5" />
          <span>Verifiable Mode Active</span>
        </div>
      </div>

      {/* Suggested Prompts */}
      <div className="space-y-2">
        <span className="text-[11px] font-semibold text-text-muted flex items-center gap-1.5 uppercase tracking-wider">
          <Lightbulb className="w-3.5 h-3.5 text-amber-400" />
          <span>Suggested Agent Commands</span>
        </span>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
          {SUGGESTED_PROMPTS.map((prompt, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => handleSelectPrompt(prompt)}
              className="text-left p-2.5 rounded-xl bg-bg-tertiary/60 hover:bg-bg-tertiary border border-border-secondary hover:border-border-primary text-xs text-text-secondary hover:text-text-primary transition-all duration-150 line-clamp-2"
            >
              "{prompt}"
            </button>
          ))}
        </div>
      </div>

      {/* Input Form */}
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="relative rounded-xl border border-border-primary bg-bg-primary focus-within:border-accent-primary focus-within:ring-2 focus-within:ring-accent-primary/20 transition-all">
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
            className="w-full bg-transparent px-4 py-3 text-sm text-text-primary placeholder:text-text-muted focus:outline-none resize-none"
          />

          <div className="p-2 border-t border-border-secondary/60 flex items-center justify-between bg-bg-secondary/30 rounded-b-xl">
            <div className="flex items-center gap-2">
              <VoiceCommandButton
                onTranscript={(text) => setGoal(text)}
                disabled={isLoading}
              />
              <span className="text-[11px] text-text-muted hidden sm:inline">
                Press Enter to plan • Click mic to dictate
              </span>
            </div>

            <button
              type="submit"
              disabled={isLoading || !goal.trim()}
              className="px-4 py-2 rounded-lg bg-accent-primary hover:bg-accent-primary-hover disabled:opacity-50 text-white text-sm font-medium flex items-center gap-2 transition-all shadow-md shadow-accent-primary/20"
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
