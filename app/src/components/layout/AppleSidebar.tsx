import { useLocation, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  LayoutDashboard,
  Folder,
  Files,
  Link2,
  Brain,
  History,
  BarChart3,
  Settings,
  Search,
  Sun,
  Moon,
  LogOut,
  User,
} from 'lucide-react';
import { LinkVaultLogo } from '@/components/ui/LinkVaultLogo';
import { useStore } from '@/store/useStore';
import { appleSpring } from '@/lib/animations';

interface AppleSidebarProps {
  onSearchClick?: () => void;
  onNavigate?: () => void; // Used for closing mobile drawer
}

export function AppleSidebar({ onSearchClick, onNavigate }: AppleSidebarProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout, theme, setTheme } = useStore();

  const currentPath = location.pathname;

  const navItems = [
    { label: 'Dashboard', icon: LayoutDashboard, path: '/dashboard' },
    { label: 'Directories', icon: Folder, path: '/directories' },
    { label: 'Files', icon: Files, path: '/files' },
    { label: 'Links', icon: Link2, path: '/links' },
    { label: 'Agent Workspace', icon: Brain, path: '/agent', badge: 'Meta' },
    { label: 'Analytics', icon: BarChart3, path: '/analytics' },
    { label: 'Activity Feed', icon: History, path: '/audit' },
  ];

  const handleNav = (path: string) => {
    navigate(path);
    onNavigate?.();
  };

  const toggleTheme = () => {
    const nextTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(nextTheme);
  };

  return (
    <aside className="w-64 h-full bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border-r border-black/[0.06] dark:border-white/10 flex flex-col justify-between py-4 select-none z-30">
      <div className="flex flex-col gap-4">
        {/* Brand Header */}
        <div 
          onClick={() => handleNav('/dashboard')} 
          className="flex items-center gap-2.5 px-5 h-11 cursor-pointer group"
        >
          <LinkVaultLogo size={28} showText={true} />
        </div>

        {/* Quick Search / Command Jump */}
        <div className="px-3.5">
          <button
            onClick={() => {
              if (onSearchClick) onSearchClick();
              else handleNav('/agent');
            }}
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

        {/* Primary Navigation Items */}
        <nav className="flex flex-col gap-0.5 px-2.5">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = currentPath === item.path || (item.path !== '/dashboard' && currentPath.startsWith(item.path));
            return (
              <button
                key={item.path}
                onClick={() => handleNav(item.path)}
                className={`relative flex items-center justify-between px-3 py-2 rounded-xl text-[13px] font-medium transition-all active:scale-[0.98] ${
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
                <div className="flex items-center gap-3">
                  <Icon className="w-[18px] h-[18px]" />
                  <span>{item.label}</span>
                </div>
                {item.badge && (
                  <span className={`text-[10px] font-semibold px-1.5 py-0.5 rounded-full ${
                    isActive ? 'bg-white/20 text-white' : 'bg-[#0071E3]/15 text-[#0071E3]'
                  }`}>
                    {item.badge}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Bottom Profile, Theme & Preferences Controls */}
      <div className="px-2.5 flex flex-col gap-1 pt-2 border-t border-black/[0.05] dark:border-white/[0.06]">
        {/* Settings button */}
        <button
          onClick={() => handleNav('/settings')}
          className={`flex items-center gap-3 px-3 py-2 rounded-xl text-[13px] font-medium transition-all active:scale-[0.98] ${
            currentPath.startsWith('/settings')
              ? 'text-white bg-[#0071E3]'
              : 'text-[#515154] dark:text-neutral-300 hover:text-[#1D1D1F] dark:hover:text-white hover:bg-black/[0.04] dark:hover:bg-white/[0.05]'
          }`}
        >
          <Settings className="w-[18px] h-[18px]" />
          <span>Preferences</span>
        </button>

        {/* User Card & Quick Controls */}
        <div className="flex items-center justify-between px-2.5 py-2 mt-1 rounded-xl bg-black/[0.02] dark:bg-white/[0.04] border border-black/[0.04] dark:border-white/[0.04]">
          <div 
            onClick={() => handleNav('/profile-editor')}
            className="flex items-center gap-2.5 min-w-0 cursor-pointer group"
          >
            <div className="w-7 h-7 rounded-full bg-[#0071E3]/10 text-[#0071E3] flex items-center justify-center font-semibold text-xs border border-[#0071E3]/20 flex-shrink-0">
              {user?.avatar ? (
                <img src={user.avatar} alt={user.name || 'User'} className="w-full h-full rounded-full object-cover" />
              ) : (
                <User className="w-3.5 h-3.5" />
              )}
            </div>
            <div className="flex flex-col min-w-0">
              <span className="text-xs font-semibold text-foreground truncate group-hover:text-[#0071E3] transition-colors">
                {user?.name || user?.username || 'User'}
              </span>
              <span className="text-[10px] text-muted-foreground truncate capitalize">
                {user?.role || 'Member'}
              </span>
            </div>
          </div>

          <div className="flex items-center gap-0.5 flex-shrink-0">
            {/* Theme switcher */}
            <button
              onClick={toggleTheme}
              className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
              title={`Switch to ${theme === 'dark' ? 'Light' : 'Dark'} Mode`}
              type="button"
            >
              {theme === 'dark' ? <Sun className="w-3.5 h-3.5" /> : <Moon className="w-3.5 h-3.5" />}
            </button>

            {/* Logout button */}
            <button
              onClick={() => {
                logout();
                navigate('/login');
              }}
              className="p-1.5 rounded-lg text-muted-foreground hover:text-red-500 hover:bg-red-500/10 transition-colors"
              title="Sign Out"
              type="button"
            >
              <LogOut className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>
    </aside>
  );
}

