import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Folder,
  Shield,
  Layers,
  Search,
  Settings,
  MoreVertical,
  CheckCircle2,
  FileText,
  Table,
  Code,
  ArrowRight,
  Sun,
  Moon,
  Lock,
  Brain,
  SlidersHorizontal,
  History,
  AlertTriangle,
} from 'lucide-react';

const appleSpring = {
  type: 'spring' as const,
  stiffness: 400,
  damping: 32,
  mass: 0.8,
};

export function LandingPage() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'overview' | 'agent' | 'files' | 'activity' | 'components'>('overview');
  const [isDarkMode, setIsDarkMode] = useState(false);
  const [batchApproved, setBatchApproved] = useState(false);

  const toggleDarkMode = (dark: boolean) => {
    setIsDarkMode(dark);
    if (dark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  return (
    <div className={`min-h-screen font-sans ${isDarkMode ? 'dark bg-[#000000] text-[#F5F5F7]' : 'bg-[#F5F5F7] text-[#1D1D1F]'} transition-colors duration-200 antialiased`}>
      {/* ── Fixed Translucent Sidebar ── */}
      <aside className="fixed left-0 top-0 h-full w-64 bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border-r border-black/[0.06] dark:border-white/10 z-50 flex flex-col justify-between py-4 select-none">
        <div className="flex flex-col gap-4">
          {/* Brand */}
          <div className="flex items-center gap-2.5 px-5 h-11">
            <img
              alt="LinkVault Logo"
              className="h-6 w-auto object-contain"
              src="https://lh3.googleusercontent.com/aida/AEtjO1UbcIUStZ-tEXrVwMgFcaav3KfhdgICONmzYt_ToqD3l-T0tPzgHcI9k8F76U7pDjTFWbaPw4_sD7IDBSW3HikuZEV4GwW-xfKkT9YPeo5PYWovV-VucNeTaG4rFr9V_0E5B_fuZimAh3bpdzprVPzhXd3fFRjwGvpcsaJU-c1umqXWTyXFuO0EQgYcOvx2cQxdDXUbcgETOUN6uyqBO_VE4G_XgDCL7PbbE0EIUFw"
            />
            <span className="text-[17px] font-semibold text-[#1D1D1F] dark:text-white tracking-[-0.015em]">LinkVault</span>
          </div>

          {/* Quick Search */}
          <div className="px-3.5">
            <button
              onClick={() => navigate('/agent')}
              className="w-full flex items-center justify-between px-3 py-1.5 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] text-[#86868B] hover:text-[#1D1D1F] dark:hover:text-white hover:bg-black/[0.07] dark:hover:bg-white/[0.09] active:scale-[0.98] transition-all border border-black/[0.04] dark:border-white/[0.04]"
              type="button"
            >
              <div className="flex items-center gap-2">
                <Search className="w-4 h-4 text-[#86868B]" />
                <span className="text-[13px] font-normal">Quick Jump</span>
              </div>
              <kbd className="px-1.5 py-0.5 rounded-[6px] bg-white dark:bg-neutral-800 text-[10px] font-medium text-[#6E6E73] dark:text-neutral-300 shadow-[0_1px_2px_rgba(0,0,0,0.06),inset_0_0.5px_0_rgba(255,255,255,0.8)] border border-black/[0.08] dark:border-white/10">⌘K</kbd>
            </button>
          </div>

          {/* Nav Items */}
          <nav className="flex flex-col gap-0.5 px-2.5">
            {[
              { id: 'overview', label: 'Overview', icon: Layers, path: '/' },
              { id: 'agent', label: 'Agent Workspace', icon: Brain, path: '/agent' },
              { id: 'files', label: 'Files', icon: Folder, path: '/files' },
              { id: 'activity', label: 'Activity', icon: History, path: '/audit' },
              { id: 'components', label: 'Component Library', icon: Layers, path: '/links' },
            ].map((item) => {
              const Icon = item.icon;
              const isActive = activeTab === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => {
                    setActiveTab(item.id as any);
                    if (item.path !== '/') navigate(item.path);
                  }}
                  className={`relative flex items-center gap-3 px-3 py-2 rounded-xl text-[13px] font-medium transition-all active:scale-[0.98] ${
                    isActive
                      ? 'text-white'
                      : 'text-[#515154] dark:text-neutral-300 hover:text-[#1D1D1F] dark:hover:text-white hover:bg-black/[0.04] dark:hover:bg-white/[0.05]'
                  }`}
                >
                  {isActive && (
                    <motion.div
                      layoutId="active-sidebar-pill"
                      transition={appleSpring}
                      className="absolute inset-0 bg-[#0071E3] rounded-xl shadow-[0_1px_3px_rgba(0,113,227,0.3)] -z-10"
                    />
                  )}
                  <Icon className="w-[18px] h-[18px]" />
                  <span>{item.label}</span>
                </button>
              );
            })}
          </nav>
        </div>

        {/* Bottom Profile */}
        <div className="px-2.5 flex flex-col gap-1.5">
          <button
            onClick={() => navigate('/settings')}
            className="flex items-center gap-3 px-3 py-2 rounded-xl text-[#515154] dark:text-neutral-300 hover:text-[#1D1D1F] dark:hover:text-white hover:bg-black/[0.04] dark:hover:bg-white/[0.05] active:scale-[0.98] transition-all text-[13px] font-medium"
          >
            <Settings className="w-[18px] h-[18px]" />
            <span>Preferences</span>
          </button>

          <div
            onClick={() => navigate('/profile-editor')}
            className="cursor-pointer p-2 rounded-2xl bg-white/80 dark:bg-white/[0.04] border border-black/[0.05] dark:border-white/10 shadow-[0_2px_8px_rgba(0,0,0,0.03)] flex items-center justify-between transition-all hover:bg-white dark:hover:bg-white/[0.06] active:scale-[0.98]"
          >
            <div className="flex items-center gap-2.5">
              <div className="relative">
                <img
                  alt="Profile"
                  className="w-8 h-8 rounded-full object-cover ring-1 ring-black/[0.06]"
                  src="https://lh3.googleusercontent.com/aida-public/AB6AXuBXk6WO0d__04TrXmc0C359wF1gVekPDKGLJP0FL8KpFBYI8ZCgDIorQ3s_JF5D-4RfjpOG00H9rapImyAt4hufBJl77U6f4ghNROXDTbh9sW9HEeo7gNqBGZ_mUWubBIeX2O5O1GdHbwugO5NOB47cyFQuY0vppTUdejz4nJ2rT0mPn5lYVF1L3qTqOzoB01R1lWXOvLJE1vUsEb9vxIJilf6r8KOSetzmM6PKqMg"
                />
                <span className="absolute bottom-0 right-0 w-2.5 h-2.5 rounded-full bg-[#34C759] ring-2 ring-white dark:ring-neutral-900" />
              </div>
              <div className="flex flex-col text-left">
                <span className="text-[13px] font-medium text-[#1D1D1F] dark:text-white leading-tight">Executive Admin</span>
                <span className="text-[11px] font-normal text-[#86868B] leading-tight">Autonomous Synced</span>
              </div>
            </div>
            <MoreVertical className="w-4 h-4 text-[#86868B]" />
          </div>
        </div>
      </aside>

      {/* ── Main Canvas ── */}
      <div className="pl-64">
        {/* Top Header */}
        <header className="fixed top-0 left-64 right-0 h-14 bg-[#F5F5F7]/80 dark:bg-black/75 backdrop-blur-2xl border-b border-black/[0.06] dark:border-white/10 z-40 flex items-center justify-between px-6">
          <nav className="hidden sm:flex items-center p-0.5 bg-black/[0.05] dark:bg-white/[0.08] rounded-full border border-black/[0.03] dark:border-white/[0.04]">
            {[
              { id: 'overview', label: 'Overview', path: '/' },
              { id: 'agent', label: 'Agent Workspace', path: '/agent' },
              { id: 'files', label: 'Files', path: '/files' },
              { id: 'activity', label: 'Activity', path: '/audit' },
              { id: 'components', label: 'Component Library', path: '/links' },
            ].map((tab) => {
              const isSelected = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => {
                    setActiveTab(tab.id as any);
                    if (tab.path !== '/') navigate(tab.path);
                  }}
                  className={`relative px-3.5 py-1 text-[13px] font-medium rounded-full transition-colors ${
                    isSelected
                      ? 'text-[#1D1D1F] dark:text-white'
                      : 'text-[#6E6E73] dark:text-neutral-400 hover:text-[#1D1D1F] dark:hover:text-white'
                  }`}
                >
                  {isSelected && (
                    <motion.div
                      layoutId="active-top-pill"
                      transition={appleSpring}
                      className="absolute inset-0 bg-white dark:bg-neutral-800 rounded-full shadow-[0_1px_3px_rgba(0,0,0,0.08),0_1px_1px_rgba(0,0,0,0.04)] -z-10"
                    />
                  )}
                  {tab.label}
                </button>
              );
            })}
          </nav>

          <div className="flex items-center gap-3 ml-auto">
            <button
              onClick={() => toggleDarkMode(!isDarkMode)}
              className="p-1.5 rounded-full bg-white/80 dark:bg-neutral-800/80 border border-black/[0.06] dark:border-white/10 text-[#1D1D1F] dark:text-white shadow-sm active:scale-[0.96] transition-all"
              title="Toggle theme"
            >
              {isDarkMode ? <Sun className="w-4 h-4 text-amber-400" /> : <Moon className="w-4 h-4 text-neutral-600" />}
            </button>
            <button
              onClick={() => navigate('/files')}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-white/80 dark:bg-neutral-800/80 border border-black/[0.06] dark:border-white/10 text-[13px] font-medium text-[#1D1D1F] dark:text-white hover:bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)] active:scale-[0.97] transition-all h-8"
            >
              <SlidersHorizontal className="w-3.5 h-3.5 text-[#6E6E73]" />
              <span>Filters</span>
            </button>
          </div>
        </header>

        {/* Content Body */}
        <main className="relative pt-14 min-h-screen">
          <div className="w-full max-w-[1100px] mx-auto px-6 py-12">
            {/* ── Hero ── */}
            <section className="relative pt-10 pb-16 flex flex-col items-center text-center">
              <motion.div
                initial={{ opacity: 0, y: -8 }}
                animate={{ opacity: 1, y: 0 }}
                transition={appleSpring}
                className="inline-flex items-center gap-2 px-3.5 py-1 rounded-full bg-white/90 dark:bg-neutral-800/90 border border-black/[0.06] dark:border-white/10 shadow-[0_1px_3px_rgba(0,0,0,0.03)] mb-6 cursor-default"
              >
                <span className="inline-block w-2 h-2 rounded-full bg-[#0071E3] animate-pulse" />
                <span className="text-[11px] font-semibold text-[#1D1D1F] dark:text-neutral-200 uppercase tracking-wider">
                  Autonomous Engine 2.4 Ready
                </span>
                <ArrowRight className="w-3 h-3 text-[#86868B]" />
              </motion.div>

              <motion.h1
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ ...appleSpring, delay: 0.05 }}
                className="text-[48px] sm:text-[56px] leading-[1.08] text-[#1D1D1F] dark:text-white max-w-4xl tracking-[-0.03em] font-semibold mb-4"
              >
                Your workspace. Handled.
              </motion.h1>

              <motion.p
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ ...appleSpring, delay: 0.1 }}
                className="text-[19px] sm:text-[20px] leading-[1.4] text-[#86868B] dark:text-neutral-400 max-w-xl mx-auto mb-9 tracking-[-0.01em]"
              >
                Files, links, and folders organized autonomously by your team's AI agent — with human approval for every change.
              </motion.p>

              <div className="flex flex-wrap items-center justify-center gap-3.5 mb-14">
                <motion.button
                  whileTap={{ scale: 0.97 }}
                  onClick={() => navigate('/agent')}
                  className="inline-flex items-center justify-center gap-2 px-6 py-2.5 rounded-full bg-[#0071E3] hover:bg-[#0077ED] text-white text-[15px] font-medium shadow-[0_1px_3px_rgba(0,113,227,0.3),inset_0_1px_0_rgba(255,255,255,0.25)] transition-all min-h-[44px]"
                >
                  <span>Try the demo</span>
                  <ArrowRight className="w-4 h-4" />
                </motion.button>
                <motion.button
                  whileTap={{ scale: 0.97 }}
                  onClick={() => navigate('/login')}
                  className="inline-flex items-center justify-center gap-2 px-6 py-2.5 rounded-full bg-white dark:bg-neutral-800 text-[#1D1D1F] dark:text-white text-[15px] font-medium border border-black/[0.08] dark:border-white/10 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:bg-neutral-50 dark:hover:bg-neutral-700 transition-all min-h-[44px]"
                >
                  <span>Sign in</span>
                </motion.button>
              </div>

              {/* ── Native Window Mockup ── */}
              <motion.div
                initial={{ opacity: 0, scale: 0.97, y: 20 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                transition={{ ...appleSpring, delay: 0.15 }}
                className="w-full max-w-[1040px] rounded-3xl bg-white dark:bg-[#1C1C1E] border border-black/[0.08] dark:border-white/10 shadow-[0_20px_50px_rgba(0,0,0,0.08),0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden text-left relative transition-all"
              >
                {/* Traffic lights Header */}
                <div className="h-11 bg-neutral-100/80 dark:bg-neutral-800/80 backdrop-blur-xl px-4 flex items-center justify-between border-b border-black/[0.06] dark:border-white/10">
                  <div className="flex items-center gap-2">
                    <div className="w-3 h-3 rounded-full bg-[#FF5F56] border border-[#E0443E]/50 shadow-[inset_0_0.5px_0.5px_rgba(255,255,255,0.4)]" />
                    <div className="w-3 h-3 rounded-full bg-[#FFBD2E] border border-[#DEA123]/50 shadow-[inset_0_0.5px_0.5px_rgba(255,255,255,0.4)]" />
                    <div className="w-3 h-3 rounded-full bg-[#27C93F] border border-[#1AAB29]/50 shadow-[inset_0_0.5px_0.5px_rgba(255,255,255,0.4)]" />
                    <span className="ml-3 text-[11px] font-medium text-[#86868B] tracking-wider uppercase">
                      LinkVault Agent Suite — Autonomous Run #842
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-black/[0.04] dark:bg-white/[0.06] text-[#6E6E73] dark:text-neutral-300 border border-black/[0.04] dark:border-white/[0.04] flex items-center gap-1">
                      <Lock className="w-3 h-3" /> Read-Write Sandboxed
                    </span>
                    <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-[#0071E3]/10 text-[#0071E3] border border-[#0071E3]/20 flex items-center gap-1">
                      <CheckCircle2 className="w-3 h-3" /> Safe Share Verified
                    </span>
                  </div>
                </div>

                {/* Body */}
                <div className="grid grid-cols-12 min-h-[420px]">
                  {/* Directives Sub-Sidebar */}
                  <div className="col-span-3 bg-neutral-50/70 dark:bg-black/30 border-r border-black/[0.05] dark:border-white/[0.06] p-4 flex flex-col justify-between">
                    <div className="space-y-4">
                      <div className="flex items-center gap-2 px-2 text-[#86868B]">
                        <Folder className="w-4 h-4" />
                        <span className="text-[12px] font-semibold uppercase tracking-wider">Directives</span>
                      </div>
                      <div className="space-y-1">
                        <div className="flex items-center justify-between px-3 py-2 rounded-xl bg-white dark:bg-neutral-800 shadow-[0_1px_3px_rgba(0,0,0,0.04)] border border-black/[0.04] dark:border-white/10 text-[#1D1D1F] dark:text-white text-[13px] font-medium">
                          <span className="flex items-center gap-2">
                            <Brain className="w-4 h-4 text-[#0071E3]" />
                            Reorg Stale Drive
                          </span>
                          <span className="w-1.5 h-1.5 rounded-full bg-[#0071E3]" />
                        </div>
                        <div className="flex items-center justify-between px-3 py-2 rounded-xl hover:bg-black/[0.03] dark:hover:bg-white/[0.04] text-[#6E6E73] text-[13px] transition-all">
                          <span className="flex items-center gap-2">
                            <Shield className="w-4 h-4" />
                            PII Vault Scans
                          </span>
                          <span className="text-[10px] font-semibold text-[#86868B] uppercase">Idle</span>
                        </div>
                        <div className="flex items-center justify-between px-3 py-2 rounded-xl hover:bg-black/[0.03] dark:hover:bg-white/[0.04] text-[#6E6E73] text-[13px] transition-all">
                          <span className="flex items-center gap-2">
                            <Layers className="w-4 h-4" />
                            Revoke Expired
                          </span>
                          <span className="text-[10px] font-semibold text-[#86868B] uppercase">Synced</span>
                        </div>
                      </div>
                    </div>

                    <div className="p-3 rounded-2xl bg-white dark:bg-neutral-800/80 border border-black/[0.05] dark:border-white/10 shadow-[0_1px_3px_rgba(0,0,0,0.03)] flex items-center gap-3">
                      <div className="w-8 h-8 rounded-xl bg-[#0071E3]/10 flex items-center justify-center text-[#0071E3]">
                        <Brain className="w-4 h-4" />
                      </div>
                      <div className="flex flex-col">
                        <span className="text-[10px] font-semibold text-[#86868B] uppercase tracking-wider">Agent Memory</span>
                        <span className="text-[13px] font-medium text-[#1D1D1F] dark:text-white">1,402 artifacts filed</span>
                      </div>
                    </div>
                  </div>

                  {/* Main Operations List */}
                  <div className="col-span-9 p-6 flex flex-col justify-between bg-white dark:bg-[#1C1C1E]">
                    <div className="space-y-4">
                      <div className="flex items-center justify-between pb-3 border-b border-black/[0.05] dark:border-white/[0.06]">
                        <div>
                          <h3 className="text-[17px] font-semibold text-[#1D1D1F] dark:text-white tracking-[-0.01em]">
                            Pending Autonomous Operations
                          </h3>
                          <p className="text-[13px] text-[#86868B] mt-0.5">
                            The agent proposed 3 file migrations based on contextual semantic relevance.
                          </p>
                        </div>
                        <span className="px-3 py-1 rounded-full bg-[#F56300]/10 text-[#F56300] border border-[#F56300]/20 text-[11px] font-semibold flex items-center gap-1.5">
                          <span className="w-1.5 h-1.5 rounded-full bg-[#F56300]" />
                          {batchApproved ? 'Approved' : 'Needs approval'}
                        </span>
                      </div>

                      <div className="space-y-2">
                        {/* Row 1 */}
                        <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] flex items-center justify-between transition-all hover:bg-neutral-100/90 dark:hover:bg-white/[0.06]">
                          <div className="flex items-center gap-3.5 min-w-0">
                            <div className="w-9 h-9 rounded-xl bg-white dark:bg-neutral-800 shadow-[0_1px_3px_rgba(0,0,0,0.06)] border border-black/[0.04] dark:border-white/10 flex items-center justify-center text-[#0071E3]">
                              <FileText className="w-4 h-4" />
                            </div>
                            <div className="flex flex-col min-w-0">
                              <span className="text-[14px] font-medium text-[#1D1D1F] dark:text-white truncate">
                                Q3_Investor_Deck_vFinal_annotated.pdf
                              </span>
                              <span className="text-[11px] text-[#86868B] flex items-center gap-1">
                                <span>Root / Downloads</span>
                                <ArrowRight className="w-2.5 h-2.5" />
                                <span className="text-[#0071E3] font-medium">Shared / Series B / Decks</span>
                              </span>
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => navigate('/agent')}
                              className="px-3 py-1.5 rounded-lg bg-white dark:bg-neutral-800 text-[#1D1D1F] dark:text-white hover:bg-neutral-50 text-[13px] font-medium border border-black/[0.06] dark:border-white/10 shadow-[0_1px_2px_rgba(0,0,0,0.04)] active:scale-[0.96] transition-all min-h-[32px]"
                            >
                              Diff
                            </button>
                            <button
                              onClick={() => setBatchApproved(true)}
                              className="px-3.5 py-1.5 rounded-lg bg-[#0071E3] hover:bg-[#0077ED] text-white text-[13px] font-medium shadow-[0_1px_3px_rgba(0,113,227,0.3)] active:scale-[0.96] transition-all min-h-[32px]"
                            >
                              Approve
                            </button>
                          </div>
                        </div>

                        {/* Row 2 */}
                        <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] flex items-center justify-between transition-all hover:bg-neutral-100/90 dark:hover:bg-white/[0.06]">
                          <div className="flex items-center gap-3.5 min-w-0">
                            <div className="w-9 h-9 rounded-xl bg-white dark:bg-neutral-800 shadow-[0_1px_3px_rgba(0,0,0,0.06)] border border-black/[0.04] dark:border-white/10 flex items-center justify-center text-[#F56300]">
                              <Table className="w-4 h-4" />
                            </div>
                            <div className="flex flex-col min-w-0">
                              <span className="text-[14px] font-medium text-[#1D1D1F] dark:text-white truncate">
                                compensation_bands_FY25_internal.xlsx
                              </span>
                              <span className="text-[11px] text-[#86868B] flex items-center gap-1">
                                <span>Public Link Generation</span>
                                <ArrowRight className="w-2.5 h-2.5" />
                                <span className="text-[#FF3B30] font-medium">Halted: PII token detected</span>
                              </span>
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="px-2.5 py-1 rounded-md bg-[#FF3B30]/10 text-[#FF3B30] border border-[#FF3B30]/20 text-[11px] font-semibold">
                              Restricted
                            </span>
                            <button className="px-3 py-1.5 rounded-lg bg-white dark:bg-neutral-800 text-[#6E6E73] hover:text-[#1D1D1F] text-[13px] font-medium border border-black/[0.06] dark:border-white/10 shadow-[0_1px_2px_rgba(0,0,0,0.04)] active:scale-[0.96] transition-all min-h-[32px]">
                              Dismiss
                            </button>
                          </div>
                        </div>

                        {/* Row 3 */}
                        <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] flex items-center justify-between transition-all">
                          <div className="flex items-center gap-3.5 min-w-0">
                            <div className="w-9 h-9 rounded-xl bg-white dark:bg-neutral-800 shadow-[0_1px_3px_rgba(0,0,0,0.06)] border border-black/[0.04] dark:border-white/10 flex items-center justify-center text-[#6E6E73]">
                              <Code className="w-4 h-4" />
                            </div>
                            <div className="flex flex-col min-w-0">
                              <span className="text-[14px] font-medium text-[#1D1D1F] dark:text-white truncate">
                                prod_auth_spec.swagger.yaml
                              </span>
                              <span className="text-[11px] text-[#86868B] flex items-center gap-1">
                                <span>Auto-tagged with metadata: API, Infrastructure, Auth-V2</span>
                              </span>
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-black/[0.04] dark:bg-white/[0.06] text-[#6E6E73]">
                              Auto-Applied
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className="pt-4 flex items-center justify-between border-t border-black/[0.05] dark:border-white/[0.06]">
                      <div className="flex items-center gap-2 text-[#86868B]">
                        <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                        <span className="text-[12px]">LinkVault Sandbox Engine active. No changes execute without human token.</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setBatchApproved(false)}
                          className="px-4 py-2 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] hover:bg-black/[0.07] text-[#1D1D1F] dark:text-white text-[13px] font-medium active:scale-[0.97] transition-all min-h-[36px]"
                        >
                          Reject All
                        </button>
                        <button
                          onClick={() => setBatchApproved(true)}
                          className="px-4 py-2 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-[13px] font-medium shadow-[0_1px_3px_rgba(0,113,227,0.3)] active:scale-[0.97] transition-all min-h-[36px]"
                        >
                          {batchApproved ? 'Approved ✓' : 'Approve Batch (1)'}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </motion.div>
            </section>

            {/* ── Feature 1: Oversight Layer ── */}
            <section className="py-16 space-y-24">
              <div className="grid grid-cols-1 md:grid-cols-12 gap-12 items-center">
                <div className="md:col-span-5 space-y-4">
                  <div className="w-10 h-10 rounded-2xl bg-[#0071E3]/10 flex items-center justify-center text-[#0071E3] mb-1">
                    <Shield className="w-5 h-5" />
                  </div>
                  <span className="text-[11px] font-semibold text-[#0071E3] uppercase tracking-wider">Oversight Layer</span>
                  <h2 className="text-[32px] sm:text-[36px] font-semibold text-[#1D1D1F] dark:text-white tracking-[-0.02em] leading-tight">
                    Agent with human approval
                  </h2>
                  <p className="text-[17px] leading-relaxed text-[#86868B] dark:text-neutral-400">
                    You retain complete oversight. Every proposed move, rename, or link generation is previewed before execution.
                    The autonomous agent acts as a world-class curator that never oversteps boundaries.
                  </p>
                  <ul className="pt-2 space-y-2.5 text-[#515154] dark:text-neutral-300 text-[14px]">
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>Explicit granular approval queues for every file move</span>
                    </li>
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>One-click bulk authorisations with smart semantic rollback</span>
                    </li>
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>Configurable auto-delegation thresholds per department</span>
                    </li>
                  </ul>
                </div>

                <div className="md:col-span-7">
                  <div className="p-6 rounded-3xl bg-white dark:bg-[#1C1C1E] border border-black/[0.06] dark:border-white/10 shadow-[0_12px_32px_rgba(0,0,0,0.04)] overflow-hidden relative">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-[11px] font-semibold text-[#86868B] uppercase tracking-wider">Intent Proposal Canvas</span>
                      <span className="text-[12px] text-[#0071E3] font-semibold">99.4% confidence score</span>
                    </div>
                    <img
                      className="w-full h-72 object-cover rounded-2xl shadow-inner mb-4 border border-black/[0.04]"
                      alt="Workspace UI preview"
                      src="https://lh3.googleusercontent.com/aida-public/AB6AXuBGJI_Rz5-CIHen43L4Erdm40VMH2zjOMLQIhYjUJufV1gHfT28EiE9HZP-gTX6pNA7H2xGeb2P-6AanrYX_oWct9tESYCAu14yjzpoHUtvsoWJxeMQ62sTsQYwLnpOCr127iUFSNUvaf6zkZryuPmFEU_a3z6h9AsDNTqugeTLIguKMux9SMyqlrIWLt1AntgCha3WAkp7xpWgMYYLoa_XeVIsojazMOu1vxyY6Nc"
                    />
                    <div className="p-4 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Brain className="w-5 h-5 text-[#0071E3]" />
                        <div className="flex flex-col">
                          <span className="text-[13px] font-medium text-[#1D1D1F] dark:text-white">Reorganize design workspace hierarchy</span>
                          <span className="text-[11px] text-[#86868B]">84 assets grouped by design tokens & release milestones</span>
                        </div>
                      </div>
                      <button
                        onClick={() => navigate('/agent')}
                        className="px-3.5 py-1.5 rounded-xl bg-white dark:bg-neutral-800 text-[#1D1D1F] dark:text-white text-[13px] font-medium hover:bg-neutral-50 border border-black/[0.06] dark:border-white/10 shadow-[0_1px_2px_rgba(0,0,0,0.04)] active:scale-[0.96] transition-all min-h-[32px]"
                      >
                        Review Diff
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              {/* ── Feature 2: Safe Share Protection ── */}
              <div className="grid grid-cols-1 md:grid-cols-12 gap-12 items-center">
                <div className="md:col-span-7 order-2 md:order-1">
                  <div className="p-6 rounded-3xl bg-white dark:bg-[#1C1C1E] border border-black/[0.06] dark:border-white/10 shadow-[0_12px_32px_rgba(0,0,0,0.04)] overflow-hidden relative">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-[11px] font-semibold text-[#86868B] uppercase tracking-wider">Proactive Interceptor</span>
                      <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-[#34C759]/10 text-[#34C759] border border-[#34C759]/20">
                        Zero Leaks Active
                      </span>
                    </div>
                    <img
                      className="w-full h-72 object-cover rounded-2xl shadow-inner mb-4 border border-black/[0.04]"
                      alt="Cybersecurity preview"
                      src="https://lh3.googleusercontent.com/aida-public/AB6AXuBk-ma8a7auS3oxWbmhCMauS4pXpEJ5xOJYKEb4_IHLSBd-VuvaZiruNANw_juJXIy516n6fYnf2N_dpe9C1dKuZ-ZmS5-bC7stpgO0sTNyH6zdpRAGXG6tgsFlNEU2qbokVst5YgB0RSbM8QrBxw98ImhNNVNlfUq1HBeA3MbbCRTydw_1gY5-X7EopyIjfMxPMfr_Ucam1q8LQxQuIc9P9eCR0whcGCNBDmA1VR8"
                    />
                    <div className="grid grid-cols-3 gap-3">
                      <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] text-center">
                        <span className="block text-[22px] font-semibold text-[#1D1D1F] dark:text-white tracking-tight">100%</span>
                        <span className="text-[11px] text-[#86868B]">PII Pre-Screen</span>
                      </div>
                      <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] text-center">
                        <span className="block text-[22px] font-semibold text-[#1D1D1F] dark:text-white tracking-tight">&lt;2ms</span>
                        <span className="text-[11px] text-[#86868B]">Scan Latency</span>
                      </div>
                      <div className="p-3.5 rounded-2xl bg-[#F5F5F7] dark:bg-white/[0.04] border border-black/[0.03] dark:border-white/[0.04] text-center">
                        <span className="block text-[22px] font-semibold text-[#1D1D1F] dark:text-white tracking-tight">0</span>
                        <span className="text-[11px] text-[#86868B]">Unintended Leaks</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="md:col-span-5 space-y-4 order-1 md:order-2">
                  <div className="w-10 h-10 rounded-2xl bg-[#F56300]/10 flex items-center justify-center text-[#F56300] mb-1">
                    <AlertTriangle className="w-5 h-5" />
                  </div>
                  <span className="text-[11px] font-semibold text-[#F56300] uppercase tracking-wider">Autonomous Guardrail</span>
                  <h2 className="text-[32px] sm:text-[36px] font-semibold text-[#1D1D1F] dark:text-white tracking-[-0.02em] leading-tight">
                    Safe Share protection
                  </h2>
                  <p className="text-[17px] leading-relaxed text-[#86868B] dark:text-neutral-400">
                    Proactive leak prevention. Automatically halts links if sensitive API tokens, passwords, or PII are found inside targeted documents or unencrypted notes.
                  </p>
                  <ul className="pt-2 space-y-2.5 text-[#515154] dark:text-neutral-300 text-[14px]">
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>Real-time heuristic scanning of codebases, PDFs, and docs</span>
                    </li>
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>Instant redaction suggestions with smart token masking</span>
                    </li>
                    <li className="flex items-center gap-2.5">
                      <CheckCircle2 className="w-4 h-4 text-[#0071E3]" />
                      <span>Automated expiry timers on sensitive multi-party links</span>
                    </li>
                  </ul>
                </div>
              </div>
            </section>

            {/* ── Call to Action ── */}
            <section className="py-12">
              <div className="p-12 sm:p-16 rounded-3xl bg-gradient-to-b from-white to-[#F5F5F7] dark:from-[#1C1C1E] dark:to-[#121214] border border-black/[0.08] dark:border-white/10 shadow-[0_20px_48px_rgba(0,0,0,0.06)] text-center relative overflow-hidden">
                <div className="relative z-10 max-w-2xl mx-auto space-y-6">
                  <h2 className="text-[34px] sm:text-[42px] font-semibold text-[#1D1D1F] dark:text-white tracking-[-0.025em] leading-tight">
                    Ready to put your digital workspace on autopilot?
                  </h2>
                  <p className="text-[18px] text-[#86868B] dark:text-neutral-400 leading-relaxed">
                    Deploy LinkVault alongside Google Drive, Microsoft OneDrive, or Notion in under five minutes.
                  </p>
                  <div className="pt-2 flex flex-wrap items-center justify-center gap-3.5">
                    <motion.button
                      whileTap={{ scale: 0.97 }}
                      onClick={() => navigate('/login')}
                      className="px-8 py-3.5 rounded-full bg-[#0071E3] hover:bg-[#0077ED] text-white text-[15px] font-medium shadow-[0_2px_8px_rgba(0,113,227,0.3)] transition-all min-h-[44px]"
                    >
                      Start Free 14-Day Trial
                    </motion.button>
                    <motion.button
                      whileTap={{ scale: 0.97 }}
                      onClick={() => navigate('/login')}
                      className="px-8 py-3.5 rounded-full bg-white dark:bg-neutral-800 text-[#1D1D1F] dark:text-white text-[15px] font-medium border border-black/[0.08] dark:border-white/10 shadow-[0_1px_3px_rgba(0,0,0,0.04)] hover:bg-neutral-50 dark:hover:bg-neutral-700 transition-all min-h-[44px]"
                    >
                      Book Enterprise Demo
                    </motion.button>
                  </div>
                </div>
              </div>
            </section>

            {/* ── Footer ── */}
            <footer className="pt-12 pb-16 mt-8 space-y-8 border-t border-black/[0.06] dark:border-white/10">
              <div className="grid grid-cols-2 md:grid-cols-5 gap-8 pb-8">
                <div className="col-span-2 space-y-3">
                  <div className="flex items-center gap-2.5">
                    <span className="text-[17px] font-semibold text-[#1D1D1F] dark:text-white tracking-tight">LinkVault</span>
                    <span className="px-2 py-0.5 rounded-full bg-black/[0.05] dark:bg-white/[0.08] text-[11px] font-medium text-[#86868B]">v2.4</span>
                  </div>
                  <p className="text-[13px] leading-relaxed text-[#86868B] max-w-xs">
                    Autonomous organizational intelligence built for precision teams with human oversight at its core.
                  </p>
                </div>

                <div className="space-y-3">
                  <span className="text-[11px] font-semibold text-[#86868B] uppercase tracking-wider">Product</span>
                  <ul className="space-y-2 text-[13px] text-[#6E6E73] dark:text-neutral-400">
                    <li><button onClick={() => navigate('/agent')} className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors">Agent Workspace</button></li>
                    <li><button onClick={() => navigate('/agent')} className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors">Safe Share Engine</button></li>
                    <li><button onClick={() => navigate('/audit')} className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors">Immutable Audits</button></li>
                    <li><button onClick={() => navigate('/subscription')} className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors">Pricing Tiers</button></li>
                  </ul>
                </div>

                <div className="space-y-3">
                  <span className="text-[11px] font-semibold text-[#86868B] uppercase tracking-wider">Security & Trust</span>
                  <ul className="space-y-2 text-[13px] text-[#6E6E73] dark:text-neutral-400">
                    <li><span className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors cursor-pointer">SOC 2 Type II</span></li>
                    <li><span className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors cursor-pointer">ISO 27001 Certified</span></li>
                    <li><span className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors cursor-pointer">GDPR & HIPAA</span></li>
                    <li><span className="hover:text-[#1D1D1F] dark:hover:text-white transition-colors cursor-pointer">Security Whitepaper</span></li>
                  </ul>
                </div>

                <div className="space-y-3">
                  <span className="text-[11px] font-semibold text-[#86868B] uppercase tracking-wider">Appearance</span>
                  <div className="p-0.5 rounded-full bg-black/[0.05] dark:bg-white/[0.08] inline-flex items-center border border-black/[0.03] dark:border-white/[0.04]">
                    <button
                      onClick={() => toggleDarkMode(false)}
                      className={`px-3 py-1 rounded-full text-[12px] font-medium flex items-center gap-1.5 transition-all ${
                        !isDarkMode
                          ? 'bg-white dark:bg-neutral-800 text-[#1D1D1F] dark:text-white shadow-[0_1px_3px_rgba(0,0,0,0.08)]'
                          : 'text-[#86868B] hover:text-[#1D1D1F]'
                      }`}
                    >
                      <Sun className="w-3.5 h-3.5" /> Light
                    </button>
                    <button
                      onClick={() => toggleDarkMode(true)}
                      className={`px-3 py-1 rounded-full text-[12px] font-medium flex items-center gap-1.5 transition-all ${
                        isDarkMode
                          ? 'bg-neutral-800 text-white shadow-[0_1px_3px_rgba(0,0,0,0.08)]'
                          : 'text-[#86868B] hover:text-[#1D1D1F]'
                      }`}
                    >
                      <Moon className="w-3.5 h-3.5" /> Dark
                    </button>
                  </div>
                </div>
              </div>

              <div className="pt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-[12px] text-[#86868B] border-t border-black/[0.04] dark:border-white/[0.06]">
                <div className="flex items-center gap-6">
                  <span>© 2026 LinkVault Inc. All rights reserved.</span>
                  <span className="hover:text-[#1D1D1F] cursor-pointer">Privacy Policy</span>
                  <span className="hover:text-[#1D1D1F] cursor-pointer">Terms of Service</span>
                  <span className="hover:text-[#1D1D1F] cursor-pointer">System Status</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-[#34C759]" />
                  <span className="text-[#6E6E73] dark:text-neutral-400 font-medium">All Systems Operational — US-East Cloud</span>
                </div>
              </div>
            </footer>
          </div>
        </main>
      </div>
    </div>
  );
}
export default LandingPage;

