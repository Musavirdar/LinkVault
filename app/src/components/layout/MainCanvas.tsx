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
        'bg-bg-primary',
      )}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.4, delay: 0.2 }}
    >
      <div className="flex-1 overflow-y-auto custom-scrollbar">
        <motion.div
          className="p-6"
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
