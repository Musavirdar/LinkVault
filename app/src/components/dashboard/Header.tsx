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
      className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8"
      variants={fadeInUp}
    >
      {/* Left: Title */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-bold text-text-primary mb-1">
          {title}
        </h1>
        {subtitle && (
          <p className="text-text-secondary">{subtitle}</p>
        )}
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-3">
        {/* Theme Toggle */}
        <ThemeToggle />

        {/* Notifications */}
        <button
          aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
          className="relative p-2 rounded-lg transition-colors duration-200 bg-bg-secondary hover:bg-bg-tertiary text-text-muted hover:text-text-primary"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-accent-danger text-white text-xs flex items-center justify-center">
              {unreadCount}
            </span>
          )}
        </button>


        {/* Add Button */}
        {showAddButton && (
          <Button
            onClick={onAddClick}
            className="gap-2"
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
            className="w-9 h-9 rounded-full bg-bg-tertiary cursor-pointer"
          />
          <div className="absolute bottom-0 right-0 w-2.5 h-2.5 rounded-full bg-accent-success border-2 border-bg-primary" />
        </div>
      </div>
    </motion.div>
  );
}
