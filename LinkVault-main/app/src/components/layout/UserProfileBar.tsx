import { motion } from 'framer-motion';
import { LogOut, Settings } from 'lucide-react';
import { StatusIndicator } from '@/components/ui/StatusIndicator';
import { useStore } from '@/store/useStore';
import { useNavigate } from 'react-router-dom';
import { cn } from '@/lib/utils';

export function UserProfileBar() {
  const { user, sidebarCollapsed, logout } = useStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!user) return null;

  return (
    <div className={cn(
      'h-[52px] flex-shrink-0',
      'flex items-center gap-2 px-3',
      'bg-bg-secondary border-t border-border-subtle'
    )}>
      {/* Avatar */}
      <div className="relative flex-shrink-0">
        <img
          src={user.avatar}
          alt={user.name}
          className="w-8 h-8 rounded-full bg-bg-tertiary"
        />
        <div className="absolute -bottom-0.5 -right-0.5">
          <StatusIndicator status={user.status} size="sm" />
        </div>
      </div>

      {/* User info */}
      {!sidebarCollapsed && (
        <motion.div
          className="flex-1 min-w-0"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <p className="text-sm font-medium text-text-primary truncate">
            {user.name}
          </p>
          <p className="text-xs text-text-muted truncate">
            {user.plan}
          </p>
        </motion.div>
      )}

      {/* Actions */}
      {!sidebarCollapsed && (
        <motion.div
          className="flex items-center gap-1"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <button
            onClick={() => navigate('/settings')}
            title="Settings"
            className="p-1.5 rounded-md text-text-muted hover:text-text-primary hover:bg-bg-tertiary transition-colors duration-200"
          >
            <Settings className="w-4 h-4" />
          </button>
          <button
            onClick={handleLogout}
            title="Log out"
            className="p-1.5 rounded-md text-text-muted hover:text-red-400 hover:bg-red-400/10 transition-colors duration-200"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </motion.div>
      )}
    </div>
  );
}
