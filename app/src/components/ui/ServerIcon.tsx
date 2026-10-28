import { motion } from 'framer-motion';
import { Infinity as InfinityIcon, Briefcase, Folder, Plus, Settings, User } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ServerIconProps {
  icon: string;
  color?: string;
  isActive?: boolean;
  isAddButton?: boolean;
  isSettings?: boolean;
  onClick?: () => void;
  label?: string;
}

const iconMap: Record<string, typeof InfinityIcon> = {
  infinity: InfinityIcon,
  briefcase: Briefcase,
  folder: Folder,
  plus: Plus,
  settings: Settings,
  user: User,
  P: User,
};

export function ServerIcon({
  icon,
  color = '#5865F2',
  isActive = false,
  isAddButton = false,
  isSettings = false,
  onClick,
  label,
}: ServerIconProps) {
  const Icon = iconMap[icon] || User;

  if (isAddButton) {
    return (
      <motion.button
        onClick={onClick}
        className="
          w-12 h-12 rounded-full
          flex items-center justify-center
          bg-bg-tertiary text-text-muted
          hover:text-text-primary hover:bg-bg-floating
          transition-colors duration-200
          group
        "
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        title="Add Workspace"
      >
        <Plus className="w-5 h-5 group-hover:rotate-90 transition-transform duration-200" />
      </motion.button>
    );
  }

  if (isSettings) {
    return (
      <motion.button
        onClick={onClick}
        className="
          w-12 h-12 rounded-full
          flex items-center justify-center
          bg-bg-tertiary text-text-muted
          hover:text-text-primary hover:bg-bg-floating
          transition-colors duration-200
        "
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        title="Settings"
      >
        <Settings className="w-5 h-5" />
      </motion.button>
    );
  }

  return (
    <motion.button
      onClick={onClick}
      className={cn(
        'relative group flex items-center justify-center',
        'w-12 h-12 rounded-[50%]',
        'transition-all duration-200',
        'hover:rounded-2xl',
        isActive && 'rounded-2xl'
      )}
      style={{ backgroundColor: color }}
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      title={label}
    >
      {/* Active indicator */}
      <motion.div
        className={cn(
          'absolute left-0 w-1 bg-white rounded-r-full',
          'transition-all duration-200',
          isActive ? 'h-8' : 'h-0 group-hover:h-5'
        )}
        style={{ transform: 'translateX(-8px)' }}
      />
      
      <Icon className="w-5 h-5 text-white" />
      
      {/* Tooltip */}
      <div className="
        absolute left-full ml-4 px-3 py-2 rounded-lg
        bg-bg-floating text-text-primary text-sm
        opacity-0 group-hover:opacity-100
        pointer-events-none whitespace-nowrap
        transition-opacity duration-200
        z-50
      ">
        {label}
      </div>
    </motion.button>
  );
}

