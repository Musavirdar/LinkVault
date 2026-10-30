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
      'h-[56px] flex-shrink-0',
      'flex items-center gap-2.5 px-3.5',
      'bg-white/60 dark:bg-[#1C1C1E]/60 backdrop-blur-xl border-t border-black/[0.06] dark:border-white/10'
    )}>
      {/* Avatar */}
      <div className="relative flex-shrink-0">
        <img
          src={user.avatar}
          alt={user.name}
          className="w-8 h-8 rounded-full bg-black/[0.05] dark:bg-white/10 ring-1 ring-black/[0.06] dark:border-white/10"
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
          <p className="text-xs font-semibold text-foreground truncate tracking-tight">
            {user.name}
          </p>
          <p className="text-[11px] text-muted-foreground truncate">
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
            className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors min-h-[36px] min-w-[36px] flex items-center justify-center"
          >
            <Settings className="w-4 h-4" />
          </button>
          <button
            onClick={handleLogout}
            title="Log out"
            className="p-2 rounded-xl text-muted-foreground hover:text-[#FF3B30] hover:bg-red-500/10 transition-colors min-h-[36px] min-w-[36px] flex items-center justify-center"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </motion.div>
      )}
    </div>
  );
}
