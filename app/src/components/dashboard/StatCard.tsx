import { motion } from 'framer-motion';
import { TrendingUp, TrendingDown, Link2, FileText, Eye, MousePointer } from 'lucide-react';
import { CountUp } from '@/components/ui/CountUp';
import { cn } from '@/lib/utils';
import { staggerItem } from '@/lib/animations';
import type { Stat } from '@/types';

interface StatCardProps {
  stat: Stat;
  index?: number;
}

const iconMap: Record<string, typeof Link2> = {
  links: Link2,
  files: FileText,
  views: Eye,
  clicks: MousePointer,
};

export function StatCard({ stat, index = 0 }: StatCardProps) {
  const Icon = iconMap[stat.icon] || Link2;
  const hasChange = stat.change !== undefined;
  const isPositive = (stat.change || 0) >= 0;

  return (
    <motion.div
      className={cn(
        'relative rounded-lg p-5',
        'bg-bg-secondary',
        'border border-border-subtle',
        'transition-colors duration-200'
      )}
      variants={staggerItem}
      whileHover={{ y: -2, borderColor: 'var(--accent-primary)' }}
      transition={{ duration: 0.2 }}
    >
      {/* Icon */}
      <div className="
        w-10 h-10 rounded-lg mb-4
        flex items-center justify-center
        bg-bg-tertiary
      ">
        <Icon className="w-5 h-5 text-text-secondary" />
      </div>

      {/* Value */}
      <div className="flex items-baseline gap-2">
        <CountUp
          end={stat.value}
          duration={800}
          delay={index * 100}
          format="compact"
          className="text-3xl font-bold text-text-primary"
        />
        {hasChange && (
          <span className={cn(
            'flex items-center text-sm font-medium',
            isPositive ? 'text-accent-success' : 'text-accent-danger'
          )}>
            {isPositive ? <TrendingUp className="w-3 h-3 mr-0.5" /> : <TrendingDown className="w-3 h-3 mr-0.5" />}
            {Math.abs(stat.change || 0)}%
          </span>
        )}
      </div>

      {/* Label */}
      <p className="text-sm text-text-secondary mt-1">{stat.label}</p>
      <p className="text-xs text-text-muted mt-0.5">{stat.subtext}</p>
    </motion.div>
  );
}
