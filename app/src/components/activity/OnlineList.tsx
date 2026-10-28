import { motion } from 'framer-motion';
import { StatusIndicator } from '@/components/ui/StatusIndicator';
import { cn } from '@/lib/utils';
import type { User } from '@/types';
import { staggerContainer, staggerItem } from '@/lib/animations';

interface OnlineListProps {
  members: User[];
  compact?: boolean;
}

export function OnlineList({ members, compact = false }: OnlineListProps) {
  if (compact) {
    return (
      <motion.div
        className="space-y-2"
        variants={staggerContainer}
        initial="hidden"
        animate="visible"
      >
        {members.map((member) => (
          <motion.div
            key={member.id}
            className="flex items-center gap-2"
            variants={staggerItem}
          >
            <div className="relative">
              <img
                src={member.avatar}
                alt={member.name}
                className="w-6 h-6 rounded-full bg-bg-tertiary"
              />
              <div className="absolute -bottom-0.5 -right-0.5">
                <StatusIndicator status={member.status} size="sm" showPulse={member.status === 'online'} />
              </div>
            </div>
            <span className="text-sm text-text-secondary truncate">
              {member.name}
            </span>
          </motion.div>
        ))}
      </motion.div>
    );
  }

  return (
    <motion.div
      className="space-y-2"
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
    >
      {members.map((member) => (
        <motion.div
          key={member.id}
          className={cn(
            'flex items-center gap-3 p-2 rounded-md',
            'hover:bg-bg-tertiary transition-colors duration-200',
            'cursor-pointer'
          )}
          variants={staggerItem}
          whileHover={{ x: 2 }}
        >
          <div className="relative">
            <img
              src={member.avatar}
              alt={member.name}
              className="w-8 h-8 rounded-full bg-bg-tertiary"
            />
            <div className="absolute -bottom-0.5 -right-0.5">
              <StatusIndicator status={member.status} size="sm" showPulse={member.status === 'online'} />
            </div>
          </div>
          <span className="text-sm text-text-secondary">
            {member.name}
          </span>
        </motion.div>
      ))}
    </motion.div>
  );
}
