import { motion } from 'framer-motion';
import { ChevronRight, Link2, Upload } from 'lucide-react';
import { cn } from '@/lib/utils';
import { staggerItem } from '@/lib/animations';

interface ActionCardProps {
  title: string;
  description: string;
  icon: string;
  onClick?: () => void;
}

const iconMap: Record<string, typeof Link2> = {
  links: Link2,
  upload: Upload,
};


export function ActionCard({ title, description, icon, onClick }: ActionCardProps) {
  const Icon = iconMap[icon] || Link2;

  return (
    <motion.button
      className={cn(
        'w-full flex items-center gap-4 p-4 rounded-lg',
        'bg-bg-secondary border border-border-subtle',
        'text-left',
        'hover:bg-bg-tertiary hover:border-border-strong',
        'transition-all duration-200'
      )}
      variants={staggerItem}
      whileHover={{ y: -1 }}
      whileTap={{ scale: 0.98 }}
      onClick={onClick}
    >
      {/* Icon */}
      <div className={cn(
        'w-12 h-12 rounded-lg flex-shrink-0',
        'flex items-center justify-center',
        'bg-bg-tertiary'
      )}>
        <Icon className="w-6 h-6 text-text-secondary" />
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <h4 className="font-semibold text-text-primary">{title}</h4>
        <p className="text-sm text-text-secondary">{description}</p>
      </div>

      {/* Arrow */}
      <motion.div
        className="text-text-muted"
        whileHover={{ x: 4 }}
      >
        <ChevronRight className="w-5 h-5" />
      </motion.div>
    </motion.button>
  );
}
