import { motion, AnimatePresence } from 'framer-motion';
import { useEffect, useState } from 'react';
import { Menu, X } from 'lucide-react';
import { ServerBar } from './ServerBar';
import { ChannelSidebar } from './ChannelSidebar';
import { MainCanvas } from './MainCanvas';
import { ActivityPanel } from './ActivityPanel';
import { UserProfileBar } from './UserProfileBar';
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
    const onResize = () => { if (window.innerWidth >= 768) setMobileNavOpen(false); };
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  return (
    <div className="w-full h-screen overflow-hidden flex flex-row">
      {/* ── Mobile hamburger button ── */}
      <button
        className="md:hidden fixed top-3 left-3 z-50 p-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-subtle)] text-[var(--text-primary)] shadow-lg"
        onClick={() => setMobileNavOpen(v => !v)}
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
            className="md:hidden fixed inset-0 z-40 bg-black/60"
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
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className="md:hidden fixed top-0 left-0 h-full z-50 flex flex-row"
            style={{ maxWidth: '280px' }}
            onClick={e => e.stopPropagation()}
          >
            <ServerBar />
            <div className="flex flex-col h-full">
              <ChannelSidebar />
              <UserProfileBar />
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Column 1: Server Bar — desktop only */}
      <div className="hidden md:flex">
        <ServerBar />
      </div>

      {/* Column 2: Channel Sidebar — desktop only */}
      <div className="hidden md:flex flex-col h-full">
        <ChannelSidebar />
        <UserProfileBar />
      </div>

      {/* Column 3: Main Canvas — always visible */}
      <MainCanvas>{children}</MainCanvas>

      {/* Column 4: Activity Panel — desktop only */}
      <div className="hidden md:flex">
        <ActivityPanel />
      </div>

      {/* Global Chat Bubble — always visible */}
      <GlobalChatBox />
    </div>
  );
}
