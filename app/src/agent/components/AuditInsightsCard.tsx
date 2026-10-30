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
    <div className="bg-bg-secondary rounded-xl border border-border-primary p-5 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <h4 className="text-sm font-semibold text-text-primary">
              Verifiable Agent Audit Trail
            </h4>
            <p className="text-xs text-text-muted">
              Zero-trust provenance: every step logged as "via agent"
            </p>
          </div>
        </div>

        <Link
          to="/audit?filter=agent"
          className="text-xs text-accent-primary hover:underline flex items-center gap-1 font-medium"
        >
          <span>View Audit Log</span>
          <ArrowUpRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      <div className="grid grid-cols-2 gap-3 pt-1">
        <div className="p-3 bg-bg-tertiary rounded-lg border border-border-secondary">
          <span className="text-[11px] text-text-muted block">Identity Scope</span>
          <span className="text-sm font-semibold text-text-primary flex items-center gap-1.5 mt-0.5">
            <Eye className="w-3.5 h-3.5 text-blue-400" />
            <span>User Parity</span>
          </span>
          <span className="text-[10px] text-text-muted mt-1 block">
            Agent cannot exceed user permissions
          </span>
        </div>

        <div className="p-3 bg-bg-tertiary rounded-lg border border-border-secondary">
          <span className="text-[11px] text-text-muted block">Audit Attribution</span>
          <span className="text-sm font-semibold text-text-primary flex items-center gap-1.5 mt-0.5">
            <Terminal className="w-3.5 h-3.5 text-emerald-400" />
            <span>[via agent] Tagged</span>
          </span>
          <span className="text-[10px] text-text-muted mt-1 block">
            Immutable attribution on all actions
          </span>
        </div>
      </div>
    </div>
  );
};
