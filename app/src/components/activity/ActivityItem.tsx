import { motion } from 'framer-motion';
import { Upload, Eye, Settings, Share2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Activity } from '@/types';
import { slideInRight } from '@/lib/animations';

interface ActivityItemProps {
  activity: Activity;
  compact?: boolean;
}

const typeIcons = {
  upload: Upload,
  view: Eye,
  system: Settings,
  share: Share2,
};

const typeColors = {
  upload: 'text-accent-success',
  view: 'text-accent-primary',
  system: 'text-text-muted',
  share: 'text-accent-warning',
};

export function ActivityItem({ activity, compact = false }: ActivityItemProps) {
  const Icon = typeIcons[activity.type];
  const isSystem = activity.type === 'system';

  if (compact) {
    return (
      <motion.div
        className="flex items-start gap-3"
        variants={slideInRight}
        initial="hidden"
        animate="visible"
      >
        {/* Avatar or Icon */}
        <div className="flex-shrink-0">
          {isSystem ? (
            <div className="
              w-8 h-8 rounded-full
              flex items-center justify-center
              bg-bg-tertiary
            ">
              <Icon className={cn('w-4 h-4', typeColors[activity.type])} />
            </div>
          ) : (
            <img
              src={activity.user.avatar}
              alt={activity.user.name}
              className="w-8 h-8 rounded-full bg-bg-tertiary"
            />
          )}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <p className="text-sm text-text-primary">
            <span className="font-medium">{activity.user.name}</span>{' '}
            <span className="text-text-secondary">{activity.action}</span>{' '}
            <span className="text-accent-primary">{activity.target}</span>
          </p>
          <p className="text-xs text-text-muted mt-0.5">
            {activity.timestamp}
          </p>
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      className="flex items-start gap-3"
      variants={slideInRight}
      initial="hidden"
      animate="visible"
    >
      {/* Avatar or Icon */}
      <div className="flex-shrink-0">
        {isSystem ? (
          <div className="
            w-10 h-10 rounded-full
            flex items-center justify-center
            bg-bg-tertiary
          ">
            <Icon className={cn('w-5 h-5', typeColors[activity.type])} />
          </div>
        ) : (
          <img
            src={activity.user.avatar}
            alt={activity.user.name}
            className="w-10 h-10 rounded-full bg-bg-tertiary"
          />
        )}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline justify-between gap-2">
          <p className="text-sm font-medium text-text-primary truncate">
            {activity.user.name}
          </p>
          <span className="text-xs text-text-muted flex-shrink-0">
            {activity.timestamp}
          </span>
        </div>
        <p className="text-sm text-text-secondary mt-0.5">
          {activity.action}{' '}
          <span className="text-accent-primary">{activity.target}</span>
        </p>
      </div>
    </motion.div>
  );
}
