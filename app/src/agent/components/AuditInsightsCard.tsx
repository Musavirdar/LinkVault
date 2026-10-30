import React from 'react';
import { ShieldCheck, Eye, Terminal, ArrowUpRight } from 'lucide-react';
import { Link } from 'react-router-dom';

interface AuditInsightsCardProps {
  agentActionsCount?: number;
}

export const AuditInsightsCard: React.FC<AuditInsightsCardProps> = ({
  agentActionsCount = 0,
}) => {
  return (
    <div className="bg-card text-card-foreground rounded-2xl border border-black/[0.06] dark:border-white/10 p-6 space-y-4 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-[#34C759]/10 text-[#34C759] border border-[#34C759]/20 flex items-center justify-center">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <h4 className="text-sm font-semibold tracking-tight text-foreground">
              Verifiable Agent Audit Trail
            </h4>
            <p className="text-xs text-muted-foreground">
              Zero-trust provenance: every step logged as "via agent"
            </p>
          </div>
        </div>

        <Link
          to="/audit?filter=agent"
          className="text-xs text-[#0071E3] dark:text-[#0A84FF] hover:underline flex items-center gap-1 font-semibold"
        >
          <span>View Audit Log</span>
          <ArrowUpRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
        <div className="p-3.5 bg-black/[0.025] dark:bg-white/[0.04] rounded-xl border border-black/[0.04] dark:border-white/10">
          <span className="text-[11px] font-medium text-muted-foreground block">Identity Scope</span>
          <span className="text-sm font-semibold text-foreground flex items-center gap-1.5 mt-0.5">
            <Eye className="w-3.5 h-3.5 text-[#0071E3] dark:text-[#0A84FF]" />
            <span>User Parity</span>
          </span>
          <span className="text-[10px] text-muted-foreground mt-1 block">
            Agent cannot exceed user permissions
          </span>
        </div>

        <div className="p-3.5 bg-black/[0.025] dark:bg-white/[0.04] rounded-xl border border-black/[0.04] dark:border-white/10">
          <span className="text-[11px] font-medium text-muted-foreground block">Audit Attribution</span>
          <span className="text-sm font-semibold text-foreground flex items-center gap-1.5 mt-0.5">
            <Terminal className="w-3.5 h-3.5 text-[#34C759]" />
            <span>[via agent] Tagged</span>
          </span>
          <span className="text-[10px] text-muted-foreground mt-1 block">
            Immutable attribution on all actions
          </span>
        </div>
      </div>
    </div>
  );
};
