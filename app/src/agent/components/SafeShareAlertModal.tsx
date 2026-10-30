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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-bg-primary border border-border-primary w-full max-w-lg rounded-xl shadow-2xl overflow-hidden animate-in fade-in duration-200">
        <div className={`p-4 flex items-center justify-between border-b ${
          isBlocked ? 'bg-red-500/10 border-red-500/30' : 'bg-amber-500/10 border-amber-500/30'
        }`}>
          <div className="flex items-center gap-3">
            {isBlocked ? (
              <ShieldAlert className="w-6 h-6 text-red-500" />
            ) : (
              <AlertTriangle className="w-6 h-6 text-amber-500" />
            )}
            <div>
              <h3 className="font-semibold text-text-primary text-base">
                {isBlocked ? 'Safe Share Guard: Share Blocked' : 'Safe Share Guard: Potential Risk'}
              </h3>
              <p className="text-xs text-text-secondary">
                Pre-share inspection caught sensitive data exposure
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-md text-text-muted hover:text-text-primary hover:bg-bg-secondary transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-6 space-y-4">
          <div className="flex items-center justify-between p-3 bg-bg-secondary rounded-lg border border-border-secondary">
            <div>
              <span className="text-xs text-text-muted block">Target Resource</span>
              <span className="text-sm font-medium text-text-primary">
                {scanResult.fileName || scanResult.resourceId}
              </span>
            </div>
            <div className="text-right">
              <span className="text-xs text-text-muted block">Risk Score</span>
              <span className={`text-sm font-bold ${
                scanResult.riskScore >= 80 ? 'text-red-500' : 'text-amber-500'
              }`}>
                {scanResult.riskScore} / 100
              </span>
            </div>
          </div>

          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">
              Detected Threats & Patterns
            </h4>
            <div className="space-y-1.5">
              {scanResult.detectedThreats.length > 0 ? (
                scanResult.detectedThreats.map((threat, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2 px-3 py-2 bg-red-500/10 text-red-400 rounded-md text-sm border border-red-500/20"
                  >
                    <Lock className="w-4 h-4 flex-shrink-0" />
                    <span>{threat}</span>
                  </div>
                ))
              ) : (
                <p className="text-xs text-text-muted">No high-risk threat categories listed.</p>
              )}
            </div>
          </div>

          <div className="p-3 bg-bg-tertiary rounded-lg text-xs text-text-secondary">
            <span className="font-semibold text-text-primary block mb-1">Protection Notice:</span>
            {scanResult.message}
          </div>
        </div>

        <div className="p-4 bg-bg-secondary border-t border-border-primary flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-bg-primary hover:bg-bg-tertiary border border-border-primary text-text-primary transition-colors"
          >
            Acknowledge & Close
          </button>
        </div>
      </div>
    </div>
  );
};
