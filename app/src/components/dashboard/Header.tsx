import { motion } from 'framer-motion';
import { Plus, Bell } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/ui/ThemeToggle';
import { useStore } from '@/store/useStore';
import { fadeInUp } from '@/lib/animations';

interface DashboardHeaderProps {
  title: string;
  subtitle?: string;
  showAddButton?: boolean;
  onAddClick?: () => void;
}

export function DashboardHeader({
  title,
  subtitle,
  showAddButton = true,
  onAddClick
}: DashboardHeaderProps) {
  const { user, unreadCount } = useStore();

  return (
    <motion.div
      className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6 sm:mb-8 pb-4 border-b border-black/[0.06] dark:border-white/10"
      variants={fadeInUp}
    >
      {/* Left: Title */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground mb-1">
          {title}
        </h1>
        {subtitle && (
          <p className="text-sm text-muted-foreground leading-relaxed">{subtitle}</p>
        )}
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-3">
        {/* Theme Toggle */}
        <ThemeToggle />

        {/* Notifications */}
        <button
          aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
          className="relative p-2.5 rounded-full transition-all duration-150 bg-black/[0.04] dark:bg-white/10 hover:bg-black/[0.08] dark:hover:bg-white/15 text-muted-foreground hover:text-foreground min-h-[40px] min-w-[40px] flex items-center justify-center border border-black/[0.04] dark:border-white/10"
        >
          <Bell className="w-4 h-4" />
          {unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 w-4 h-4 rounded-full bg-[#FF3B30] text-white text-[10px] font-bold flex items-center justify-center shadow-xs">
              {unreadCount}
            </span>
          )}
        </button>

        {/* Add Button */}
        {showAddButton && (
          <Button
            onClick={onAddClick}
            className="gap-2 rounded-xl"
          >
            <Plus className="w-4 h-4" />
            <span className="hidden sm:inline">Add Link</span>
          </Button>
        )}

        {/* User Avatar */}
        <div className="relative">
          <img
            src={user?.avatar}
            alt={user?.name}
            className="w-10 h-10 rounded-full bg-black/[0.05] dark:bg-white/10 ring-2 ring-black/[0.06] dark:ring-white/10 cursor-pointer object-cover"
          />
          <div className="absolute bottom-0 right-0 w-3 h-3 rounded-full bg-[#34C759] border-2 border-background" />
        </div>
      </div>
    </motion.div>
  );
}
