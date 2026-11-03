import { motion } from 'framer-motion';
import { cn } from '@/lib/utils';
import { staggerContainer } from '@/lib/animations';

interface MainCanvasProps {
  children: React.ReactNode;
}

export function MainCanvas({ children }: MainCanvasProps) {
  return (
    <motion.main
      className={cn(
        'flex-1 h-full overflow-hidden',
        'flex flex-col',
        'bg-background text-foreground',
      )}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.15, ease: 'easeOut' }}
    >
      <div className="flex-1 overflow-y-auto custom-scrollbar">
        <motion.div
          className="w-full max-w-[1100px] mx-auto p-4 sm:p-6 lg:p-8 space-y-6"
          variants={staggerContainer}
          initial="hidden"
          animate="visible"
        >
          {children}
        </motion.div>
      </div>
    </motion.main>
  );
}
