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
  glowColor = '#8B5CF6',
}: GlassCardProps) {
  return (
    <motion.div
      className={cn(
        'relative rounded-3xl overflow-hidden',
        'bg-white/70 backdrop-blur-xl',
        'border border-white/50',
        'shadow-soft',
        className
      )}
      style={{
        boxShadow: glow ? `0 0 40px ${glowColor}20, 0 8px 32px rgba(0,0,0,0.08)` : undefined,
      }}
      whileHover={hover ? { y: -2, boxShadow: '0 4px 12px rgba(0,0,0,0.1)' } : undefined}
      transition={{ duration: 0.2 }}
    >
      {children}
    </motion.div>
  );
}

// Gradient background card
export function GradientCard({
  children,
  className = '',
  gradient = 'from-violet-500/10 to-pink-500/10',
}: GlassCardProps & { gradient?: string }) {
  return (
    <div
      className={cn(
        'relative rounded-3xl overflow-hidden',
        'bg-gradient-to-br',
        gradient,
        'border border-white/30',
        className
      )}
    >
      <div className="absolute inset-0 backdrop-blur-sm" />
      <div className="relative z-10">{children}</div>
    </div>
  );
}
