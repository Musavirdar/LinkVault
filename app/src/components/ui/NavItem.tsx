import { motion } from 'framer-motion';
import { 
  LayoutDashboard, 
  Link2, 
  FileText, 
  BarChart3, 
  ClipboardList, 
  Settings,
  Bot,
  type LucideIcon
} from 'lucide-react';
import { cn } from '@/lib/utils';

interface NavItemProps {
  icon: string;
  label: string;
  isActive?: boolean;
  isCollapsed?: boolean;
  badge?: number | string;
  onClick?: () => void;
}

const iconMap: Record<string, LucideIcon> = {
  dashboard: LayoutDashboard,
  links: Link2,
  files: FileText,
  analytics: BarChart3,
  audit: ClipboardList,
  settings: Settings,
  agent: Bot,
  bot: Bot,
};

export function NavItem({
  icon,
  label,
  isActive = false,
  isCollapsed = false,
  badge,
  onClick,
}: NavItemProps) {
  const Icon = iconMap[icon] || LayoutDashboard;

  return (
    <motion.button
      onClick={onClick}
      className={cn(
        'w-full flex items-center gap-3 px-3 py-2 rounded-md',
        'text-sm font-medium transition-colors duration-200',
        isActive 
          ? 'bg-accent-primary/10 text-accent-primary' 
          : 'text-text-secondary hover:text-text-primary hover:bg-bg-tertiary'
      )}
      whileHover={{ x: 2 }}
      whileTap={{ scale: 0.98 }}
    >
      <Icon className="w-5 h-5 flex-shrink-0" />
      
      {!isCollapsed && (
        <>
          <span className="flex-1 text-left">{label}</span>
          {badge !== undefined && (
            <span className="
              px-2 py-0.5 text-xs rounded-full font-semibold
              bg-accent-primary text-white
            ">
              {badge}
            </span>
          )}
        </>
      )}
    </motion.button>
  );
}

// Navigation section header
export function NavSection({ 
  label, 
  isCollapsed = false 
}: { 
  label: string; 
  isCollapsed?: boolean 
}) {
  if (isCollapsed) return null;
  
  return (
    <div className="px-3 py-2">
      <span className="text-xs font-semibold text-text-muted uppercase tracking-wider">
        {label}
      </span>
    </div>
  );
}
