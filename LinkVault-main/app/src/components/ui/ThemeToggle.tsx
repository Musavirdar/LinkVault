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
    <div className="flex items-center gap-1 p-1 rounded-lg bg-bg-tertiary/50">
      {themes.map(({ id, icon: Icon, label }) => (
        <motion.button
          key={id}
          onClick={() => setTheme(id)}
          className={`
            relative p-2 rounded-md transition-colors duration-200
            ${theme === id 
              ? 'text-white' 
              : 'text-text-muted hover:text-text-secondary'
            }
          `}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          title={label}
        >
          {theme === id && (
            <motion.div
              layoutId="theme-indicator"
              className="absolute inset-0 bg-accent-primary rounded-md"
              transition={{ type: 'spring', stiffness: 500, damping: 30 }}
            />
          )}
          <Icon className="w-4 h-4 relative z-10" />
        </motion.button>
      ))}
    </div>
  );
}

export function ThemeDropdown() {
  const { theme, setTheme } = useStore();

  return (
    <div className="flex items-center gap-2">
      <span className="text-sm text-text-secondary">Theme</span>
      <select
        value={theme}
        onChange={(e) => setTheme(e.target.value as Theme)}
        className="
          flex items-center gap-2 px-3 py-1.5 rounded-md
          bg-bg-tertiary text-text-primary text-sm
          border border-border-strong/30
          focus:outline-none focus:ring-2 focus:ring-accent-primary/30
          cursor-pointer
        "
      >
        <option value="dark">Dark</option>
        <option value="light">Light</option>
      </select>
    </div>
  );
}
