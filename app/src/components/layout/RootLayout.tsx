import { motion, AnimatePresence } from 'framer-motion';
import { useEffect, useState } from 'react';
import { Menu, X } from 'lucide-react';
import { AppleSidebar } from './AppleSidebar';
import { MainCanvas } from './MainCanvas';
import { GlobalChatBox } from '@/components/chat/GlobalChatBox';
import { useChatConnection } from '@/hooks/useChatConnection';
import { usePreferencesStore } from '@/store/usePreferencesStore';

interface RootLayoutProps {
  children: React.ReactNode;
}

export function RootLayout({ children }: RootLayoutProps) {
  const { accentColor } = usePreferencesStore();
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  useChatConnection(); // Start SignalR when logged in

  // Apply user accent color preference globally
  useEffect(() => {
    document.documentElement.style.setProperty('--accent-primary', accentColor);
  }, [accentColor]);

  // Close mobile nav on resize to desktop
  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth >= 768) setMobileNavOpen(false);
    };
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  return (
    <div className="w-full h-screen overflow-hidden flex flex-row bg-background text-foreground">
      {/* ── Mobile hamburger button ── */}
      <button
        className="md:hidden fixed top-3.5 left-3.5 z-50 p-2.5 min-h-[44px] min-w-[44px] rounded-full bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-xl border border-black/[0.08] dark:border-white/12 text-foreground shadow-md flex items-center justify-center transition-transform active:scale-95"
        onClick={() => setMobileNavOpen((v) => !v)}
        aria-label="Toggle navigation"
      >
        {mobileNavOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
      </button>

      {/* ── Mobile nav drawer overlay ── */}
      <AnimatePresence>
        {mobileNavOpen && (
          <motion.div
            key="mobile-overlay"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="md:hidden fixed inset-0 z-40 bg-black/50 backdrop-blur-md"
            onClick={() => setMobileNavOpen(false)}
          />
        )}
      </AnimatePresence>

      {/* ── Mobile nav drawer ── */}
      <AnimatePresence>
        {mobileNavOpen && (
          <motion.div
            key="mobile-drawer"
            initial={{ x: '-100%' }}
            animate={{ x: 0 }}
            exit={{ x: '-100%' }}
            transition={{ type: 'spring', stiffness: 350, damping: 32 }}
            className="md:hidden fixed top-0 left-0 h-full z-50 shadow-2xl flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            <AppleSidebar onNavigate={() => setMobileNavOpen(false)} />
          </motion.div>
        )}
      </AnimatePresence>

      {/* Desktop Apple Sidebar */}
      <div className="hidden md:flex flex-shrink-0 h-full">
        <AppleSidebar />
      </div>

      {/* Main Canvas Workspace */}
      <div className="flex-1 h-full min-w-0 flex flex-col overflow-hidden bg-background">
        <MainCanvas>{children}</MainCanvas>
      </div>

      {/* Global Realtime Chat Trigger */}
      <GlobalChatBox />
    </div>
  );
}
