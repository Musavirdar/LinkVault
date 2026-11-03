import React from 'react';
import { ShieldAlert, AlertTriangle, X, Lock } from 'lucide-react';
import type { SafeShareScanResultDto } from '../types/agent.types';

interface SafeShareAlertModalProps {
  scanResult: SafeShareScanResultDto | null;
  isOpen: boolean;
  onClose: () => void;
}

export const SafeShareAlertModal: React.FC<SafeShareAlertModalProps> = ({
  scanResult,
  isOpen,
  onClose,
}) => {
  if (!isOpen || !scanResult) return null;

  const isBlocked = scanResult.status === 'Blocked';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-md animate-in fade-in duration-200">
      <div data-testid="safe-share-alert-modal" className="bg-white/95 dark:bg-[#1C1C1E]/95 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 w-full max-w-lg rounded-3xl shadow-[0_24px_60px_rgba(0,0,0,0.22)] overflow-hidden">
        <div className={`p-5 flex items-center justify-between border-b ${
          isBlocked ? 'bg-red-500/10 border-red-500/20' : 'bg-amber-500/10 border-amber-500/20'
        }`}>
          <div className="flex items-center gap-3">
            {isBlocked ? (
              <ShieldAlert className="w-6 h-6 text-[#FF3B30]" />
            ) : (
              <AlertTriangle className="w-6 h-6 text-[#FF9500]" />
            )}
            <div>
              <h3 className="font-bold tracking-tight text-foreground text-base">
                {isBlocked ? 'Safe Share Guard: Share Blocked' : 'Safe Share Guard: Potential Risk'}
              </h3>
              <p className="text-xs text-muted-foreground">
                Pre-share inspection caught sensitive data exposure
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

        <div className="p-6 space-y-4">
          <div className="flex items-center justify-between p-3.5 bg-black/[0.025] dark:bg-white/[0.04] rounded-2xl border border-black/[0.04] dark:border-white/10">
            <div>
              <span className="text-xs text-muted-foreground block">Target Resource</span>
              <span className="text-sm font-semibold text-foreground">
                {scanResult.fileName || scanResult.resourceId}
              </span>
            </div>
            <div className="text-right">
              <span className="text-xs text-muted-foreground block">Risk Score</span>
              <span className={`text-sm font-bold ${
                scanResult.riskScore >= 80 ? 'text-[#FF3B30]' : 'text-[#FF9500]'
              }`}>
                {scanResult.riskScore} / 100
              </span>
            </div>
          </div>

          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
              Detected Threats & Patterns
            </h4>
            <div className="space-y-2">
              {scanResult.detectedThreats.length > 0 ? (
                scanResult.detectedThreats.map((threat, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2.5 px-3.5 py-2.5 bg-red-500/10 text-[#FF3B30] rounded-xl text-sm border border-red-500/20 font-medium"
                  >
                    <Lock className="w-4 h-4 flex-shrink-0" />
                    <span>{threat}</span>
                  </div>
                ))
              ) : (
                <p className="text-xs text-muted-foreground">No high-risk threat categories listed.</p>
              )}
            </div>
          </div>

          <div className="p-3.5 bg-black/[0.025] dark:bg-white/[0.04] rounded-xl text-xs text-muted-foreground leading-relaxed border border-black/[0.04] dark:border-white/10">
            <span className="font-semibold text-foreground block mb-1">Protection Notice:</span>
            {scanResult.message}
          </div>
        </div>

        <div className="p-4 bg-black/[0.02] dark:bg-white/[0.02] border-t border-black/[0.06] dark:border-white/10 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="min-h-[44px] px-5 py-2 text-sm font-semibold rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white active:scale-[0.98] transition-all shadow-sm shadow-[#0071E3]/25"
          >
            Acknowledge & Close
          </button>
        </div>
      </div>
    </div>
  );
};
