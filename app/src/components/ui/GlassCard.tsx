import { motion } from 'framer-motion';
import { cn } from '@/lib/utils';

interface GlassCardProps {
  children: React.ReactNode;
  className?: string;
  hover?: boolean;
  glow?: boolean;
  glowColor?: string;
}

export function GlassCard({
  children,
  className = '',
  hover = true,
  glow = false,
  glowColor = '#0071E3',
}: GlassCardProps) {
  return (
    <motion.div
      className={cn(
        'relative rounded-2xl overflow-hidden',
        'bg-white/75 dark:bg-[#1C1C1E]/75 backdrop-blur-2xl',
        'border border-black/[0.06] dark:border-white/10',
        'shadow-[0_8px_24px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]',
        'before:absolute before:inset-x-0 before:top-0 before:h-px before:bg-gradient-to-r before:from-transparent before:via-white/20 dark:before:via-white/10 before:to-transparent',
        className
      )}
      style={{
        boxShadow: glow ? `0 0 32px ${glowColor}25, 0 8px 24px rgba(0,0,0,0.06)` : undefined,
      }}
      whileHover={hover ? { y: -2, boxShadow: '0 12px 32px rgba(0,0,0,0.08)' } : undefined}
      transition={{ type: 'spring', stiffness: 400, damping: 28 }}
    >
      {children}
    </motion.div>
  );
}

// Gradient background card
export function GradientCard({
  children,
  className = '',
  gradient = 'from-blue-500/10 to-indigo-500/10',
}: GlassCardProps & { gradient?: string }) {
  return (
    <div
      className={cn(
        'relative rounded-2xl overflow-hidden',
        'bg-gradient-to-br',
        gradient,
        'border border-black/[0.06] dark:border-white/10',
        'shadow-[0_8px_24px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]',
        className
      )}
    >
      <div className="absolute inset-0 backdrop-blur-xl bg-white/40 dark:bg-black/20" />
      <div className="relative z-10">{children}</div>
    </div>
  );
}
