import { motion, AnimatePresence } from 'framer-motion';
import { ChevronDown, PanelLeftClose, PanelLeftOpen, Search, X, Link2, File, Folder } from 'lucide-react';
import { useNavigate, useLocation } from 'react-router-dom';
import { NavItem, NavSection } from '@/components/ui/NavItem';
import { useStore } from '@/store/useStore';
import { cn } from '@/lib/utils';
import { sidebarVariants } from '@/lib/animations';
import { searchApi } from '@/api/client';
import { useState, useRef, useEffect, useCallback } from 'react';

// Sections visible to ALL authenticated users (Individual + Team)
const individualSections = [
  {
    id: 'sharing',
    label: 'My Sharing',
    items: [
      { id: 'agent', icon: 'agent', label: 'AI Agent', href: '/agent', badge: 'Meta' },
      { id: 'links', icon: 'links', label: 'My Links', href: '/links', badge: 0 },
      { id: 'profile-editor', icon: 'settings', label: 'Profile Editor', href: '/profile-editor' },
      { id: 'directories', icon: 'files', label: 'Doc Share Tree', href: '/directories' },
    ],
  },
  {
    id: 'system',
    label: 'System',
    items: [
      { id: 'settings', icon: 'settings', label: 'Settings', href: '/settings' },
      { id: 'subscription', icon: 'settings', label: 'Subscription', href: '/subscription' },
    ],
  },
];

// Sections visible ONLY to Team accounts
const teamSections = [
  {
    id: 'general',
    label: 'General',
    items: [
      { id: 'dashboard', icon: 'dashboard', label: 'Dashboard', href: '/dashboard' },
      { id: 'agent', icon: 'agent', label: 'AI Agent', href: '/agent', badge: 'Meta' },
      { id: 'links', icon: 'links', label: 'My Links', href: '/links', badge: 0 },
      { id: 'profile-editor', icon: 'settings', label: 'Profile Editor', href: '/profile-editor' },
      { id: 'files', icon: 'files', label: 'My Files', href: '/files' },
      { id: 'directories', icon: 'files', label: 'Doc Share Tree', href: '/directories' },
    ],
  },
  {
    id: 'insights',
    label: 'Insights',
    items: [
      { id: 'analytics', icon: 'analytics', label: 'Analytics', href: '/analytics' },
      { id: 'audit', icon: 'audit', label: 'Audit Logs', href: '/audit' },
    ],
  },
  {
    id: 'system',
    label: 'System',
    items: [
      { id: 'settings', icon: 'settings', label: 'Settings', href: '/settings' },
      { id: 'subscription', icon: 'settings', label: 'Subscription', href: '/subscription' },
    ],
  },
];

interface SearchResult {
  type: string;
  id: string;
  title: string;
  subtitle: string;
  icon: string;
}

function SearchBox({ onNavigate }: { onNavigate: (href: string) => void }) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const doSearch = useCallback(async (q: string) => {
    if (q.length < 2) { setResults([]); setOpen(false); return; }
    setLoading(true);
    const res = await searchApi.search(q);
    if (res.data) {
      const flat: SearchResult[] = [
        ...res.data.results.links,
        ...res.data.results.resources,
        ...res.data.results.directories,
      ];
      setResults(flat);
      setOpen(flat.length > 0);
    }
    setLoading(false);
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const q = e.target.value;
    setQuery(q);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => doSearch(q), 300);
  };

  const clear = () => { setQuery(''); setResults([]); setOpen(false); };

  // Close when clicking outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const iconFor = (type: string) => {
    if (type === 'link') return <Link2 className="w-3.5 h-3.5 text-[var(--accent-primary)]" />;
    if (type === 'directory') return <Folder className="w-3.5 h-3.5 text-yellow-400" />;
    return <File className="w-3.5 h-3.5 text-[var(--text-muted)]" />;
  };

  const hrefFor = (r: SearchResult) => {
    if (r.type === 'link') return '/links';
    if (r.type === 'directory') return '/directories';
    return '/files';
  };

  return (
    <div ref={containerRef} className="relative px-3 pb-2">
      <div className={cn(
        'flex items-center gap-2 px-3 py-2 rounded-lg transition-all',
        'bg-[var(--bg-primary)] border',
        open ? 'border-[var(--accent-primary)]/60' : 'border-[var(--border-subtle)]'
      )}>
        <Search className="w-3.5 h-3.5 text-[var(--text-muted)] flex-shrink-0" />
        <input
          value={query}
          onChange={handleChange}
          onFocus={() => results.length > 0 && setOpen(true)}
          placeholder="Search…"
          className="flex-1 bg-transparent text-xs text-[var(--text-primary)] outline-none placeholder:text-[var(--text-muted)]"
        />
        {(query || loading) && (
          <button aria-label="Clear search" onClick={clear} className="text-[var(--text-muted)] hover:text-[var(--text-primary)]">
            <X className="w-3 h-3" />
          </button>
        )}

      </div>

      <AnimatePresence>
        {open && results.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -4 }}
            transition={{ duration: 0.15 }}
            className="absolute left-3 right-3 top-full mt-1 z-50 rounded-lg overflow-hidden shadow-xl"
            style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-strong)' }}
          >
            {results.map((r) => (
              <button
                key={`${r.type}-${r.id}`}
                className="w-full flex items-center gap-2.5 px-3 py-2 text-left hover:bg-[var(--bg-tertiary)] transition-colors"
                onClick={() => { onNavigate(hrefFor(r)); clear(); }}
              >
                {iconFor(r.type)}
                <div className="min-w-0">
                  <div className="text-xs font-medium text-[var(--text-primary)] truncate">{r.title}</div>
                  <div className="text-[10px] text-[var(--text-muted)] truncate">{r.subtitle}</div>
                </div>
                <span className="ml-auto text-[10px] px-1.5 py-0.5 rounded bg-[var(--bg-primary)] text-[var(--text-muted)] capitalize flex-shrink-0">{r.type}</span>
              </button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export function ChannelSidebar() {
  const {
    sidebarCollapsed,
    toggleSidebar,
    activeWorkspace,
    workspaces,
    permissions,
  } = useStore();

  const isTeam = permissions.accountType === 'Team';
  const navSections = isTeam ? teamSections : individualSections;

  const activeWorkspaceData = workspaces.find(w => w.id === activeWorkspace);
  const navigate = useNavigate();
  const location = useLocation();

  // Dark/Light: Collapsible sidebar
  return (
    <motion.aside
      className={cn(
        'h-full flex-shrink-0',
        'flex flex-col',
        'bg-bg-secondary border-r border-border-subtle',
        'overflow-hidden'
      )}
      variants={sidebarVariants}
      initial="expanded"
      animate={sidebarCollapsed ? 'collapsed' : 'expanded'}
    >
      {/* Header */}
      <div className={cn(
        'flex items-center justify-between p-4',
        'border-b border-border-subtle'
      )}>
        {!sidebarCollapsed && (
          <motion.div
            className="flex items-center gap-2"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            <span className="font-semibold text-text-primary">
              {activeWorkspaceData?.name ?? 'LinkVault'}
            </span>
            {isTeam && (
              <span
                className="text-[9px] font-bold uppercase px-1.5 py-0.5 rounded-full"
                style={{ background: '#5865F220', color: '#818CF8', border: '1px solid #5865F230' }}
              >
                Team
              </span>
            )}
            <ChevronDown className="w-4 h-4 text-text-muted" />
          </motion.div>
        )}
        <motion.button
          onClick={toggleSidebar}
          aria-label={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          className={cn(
            'p-1.5 rounded-md',
            'text-text-muted hover:text-text-primary hover:bg-bg-tertiary',
            'transition-colors duration-200',
            sidebarCollapsed && 'mx-auto'
          )}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
        >

          {sidebarCollapsed ? (
            <PanelLeftOpen className="w-4 h-4" />
          ) : (
            <PanelLeftClose className="w-4 h-4" />
          )}
        </motion.button>
      </div>

      {/* Search box (only visible when expanded) */}
      {!sidebarCollapsed && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="pt-3"
        >
          <SearchBox onNavigate={navigate} />
        </motion.div>
      )}

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto custom-scrollbar p-3">
        {navSections.map((section) => (
          <div key={section.id} className="mb-4">
            <NavSection
              label={section.label}
              isCollapsed={sidebarCollapsed}
            />
            <div className="space-y-1">
              {section.items.map((item) => {
                const isActive = location.pathname === item.href ||
                  (location.pathname === '/' && item.href === '/dashboard');
                return (
                  <NavItem
                    key={item.id}
                    icon={item.icon}
                    label={item.label}
                    isActive={isActive}
                    isCollapsed={sidebarCollapsed}
                    badge={item.badge}
                    onClick={() => navigate(item.href)}
                  />
                );
              })}
            </div>
          </div>
        ))}
      </nav>
    </motion.aside>
  );
}
