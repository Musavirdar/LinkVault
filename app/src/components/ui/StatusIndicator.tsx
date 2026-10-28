import { cn } from '@/lib/utils';
import type { UserStatus } from '@/types';

interface StatusIndicatorProps {
  status: UserStatus;
  size?: 'sm' | 'md' | 'lg';
  showPulse?: boolean;
}

const statusColors: Record<UserStatus, string> = {
  online: 'bg-accent-success',
  away: 'bg-accent-warning',
  offline: 'bg-text-muted',
  dnd: 'bg-accent-danger',
};

const sizeClasses = {
  sm: 'w-2.5 h-2.5 border-2',
  md: 'w-3 h-3 border-2',
  lg: 'w-4 h-4 border-[3px]',
};

export function StatusIndicator({ 
  status, 
  size = 'md',
  showPulse = false 
}: StatusIndicatorProps) {
  return (
    <div className="relative">
      <div
        className={cn(
          'rounded-full border-bg-secondary',
          statusColors[status],
          sizeClasses[size]
        )}
      />
      {showPulse && status === 'online' && (
        <div
          className={cn(
            'absolute inset-0 rounded-full bg-accent-success',
            'animate-pulse-status'
          )}
        />
      )}
    </div>
  );
}

// Status badge with label
export function StatusBadge({ status }: { status: UserStatus }) {
  const labels: Record<UserStatus, string> = {
    online: 'Online',
    away: 'Away',
    offline: 'Offline',
    dnd: 'Do Not Disturb',
  };

  return (
    <div className="flex items-center gap-2">
      <StatusIndicator status={status} size="sm" />
      <span className="text-sm text-text-secondary">{labels[status]}</span>
    </div>
  );
}
