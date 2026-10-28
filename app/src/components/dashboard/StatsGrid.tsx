import { motion } from 'framer-motion';
import { StatCard } from './StatCard';
import { staggerContainer } from '@/lib/animations';
import type { Stat } from '@/types';

interface StatsGridProps {
  stats: Stat[];
}

export function StatsGrid({ stats }: StatsGridProps) {
  return (
    <motion.div
      className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8"
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
    >
      {stats.map((stat, index) => (
        <StatCard key={stat.label} stat={stat} index={index} />
      ))}
    </motion.div>
  );
}
