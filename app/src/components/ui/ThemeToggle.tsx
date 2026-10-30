import { motion } from 'framer-motion';
import { Moon, Sun } from 'lucide-react';
import { useStore } from '@/store/useStore';
import type { Theme } from '@/types';

const themes: { id: Theme; icon: typeof Moon; label: string }[] = [
  { id: 'dark', icon: Moon, label: 'Dark' },
  { id: 'light', icon: Sun, label: 'Light' },
];

export function ThemeToggle() {
  const { theme, setTheme } = useStore();

  return (
    <div className="flex items-center gap-0.5 p-0.5 rounded-full bg-black/[0.05] dark:bg-white/10 border border-black/[0.04] dark:border-white/10 backdrop-blur-md">
      {themes.map(({ id, icon: Icon, label }) => {
        const isActive = theme === id;
        return (
          <motion.button
            key={id}
            onClick={() => setTheme(id)}
            className={`
              relative p-1.5 rounded-full transition-colors duration-150 min-w-[32px] min-h-[32px] flex items-center justify-center
              ${isActive 
                ? 'text-[#0071E3] dark:text-white' 
                : 'text-text-muted hover:text-text-primary'
              }
            `}
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            title={label}
            aria-label={`${label} theme`}
          >
            {isActive && (
              <motion.div
                layoutId="theme-indicator"
                className="absolute inset-0 bg-white dark:bg-[#2C2C2E] rounded-full shadow-[0_1px_3px_rgba(0,0,0,0.12)] border border-black/[0.04] dark:border-white/10"
                transition={{ type: 'spring', stiffness: 500, damping: 32 }}
              />
            )}
            <Icon className="w-3.5 h-3.5 relative z-10" />
          </motion.button>
        );
      })}
    </div>
  );
}

export function ThemeDropdown() {
  const { theme, setTheme } = useStore();

  return (
    <div className="flex items-center gap-2">
      <span className="text-xs font-medium text-text-secondary">Theme</span>
      <select
        value={theme}
        onChange={(e) => setTheme(e.target.value as Theme)}
        className="
          flex items-center gap-2 px-3 py-1.5 rounded-xl
          bg-bg-tertiary text-text-primary text-xs font-medium
          border border-border-subtle
          focus:outline-none focus:ring-2 focus:ring-accent-primary/40
          cursor-pointer transition-colors duration-150
        "
      >
        <option value="dark">Dark</option>
        <option value="light">Light</option>
      </select>
    </div>
  );
}
