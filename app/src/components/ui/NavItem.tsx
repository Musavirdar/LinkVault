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
        'w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl min-h-[44px]',
        'text-sm font-medium transition-all duration-150',
        isActive 
          ? 'bg-[#0071E3] text-white shadow-sm shadow-[#0071E3]/25 font-semibold' 
          : 'text-text-secondary hover:text-text-primary hover:bg-black/[0.04] dark:hover:bg-white/[0.06] active:bg-black/[0.08] dark:active:bg-white/10'
      )}
      whileTap={{ scale: 0.98 }}
    >
      <Icon className={cn("w-4 h-4 flex-shrink-0 transition-transform", isActive ? "scale-105" : "")} />
      
      {!isCollapsed && (
        <>
          <span className="flex-1 text-left tracking-tight truncate">{label}</span>
          {badge !== undefined && (
            <span className={cn(
              "px-2 py-0.5 text-[11px] rounded-full font-semibold tracking-tight",
              isActive 
                ? "bg-white/20 text-white" 
                : "bg-black/[0.06] dark:bg-white/10 text-foreground"
            )}>
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
    <div className="px-3.5 pt-4 pb-1.5">
      <span className="text-[11px] font-semibold text-text-muted uppercase tracking-wider">
        {label}
      </span>
    </div>
  );
}
