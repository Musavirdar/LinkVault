import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Theme, User, Workspace, Permissions } from '@/types';


interface AppState {
  // Theme
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;

  // Layout state
  sidebarCollapsed: boolean;
  activityPanelCollapsed: boolean;
  toggleSidebar: () => void;
  toggleActivityPanel: () => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  setActivityPanelCollapsed: (collapsed: boolean) => void;

  // Active workspace
  activeWorkspace: string;
  setActiveWorkspace: (id: string) => void;

  // Auth
  token: string | null;
  setToken: (token: string | null) => void;
  logout: () => void;

  // User
  user: User | null;
  setUser: (user: User | null) => void;

  // Permissions (parsed from JWT)
  permissions: Permissions;
  setPermissions: (permissions: Permissions) => void;

  // Workspaces
  workspaces: Workspace[];
  setWorkspaces: (workspaces: Workspace[]) => void;

  // Search
  searchQuery: string;
  setSearchQuery: (query: string) => void;

  // Notifications
  unreadCount: number;
  setUnreadCount: (count: number) => void;
}

export const useStore = create<AppState>()(
  persist(
    (set, get) => ({
      // Theme
      theme: 'dark',
      setTheme: (theme) => {
        set({ theme });
        document.documentElement.setAttribute('data-theme', theme);
      },
      toggleTheme: () => {
        const themes: Theme[] = ['dark', 'light'];
        const currentIndex = themes.indexOf(get().theme);
        const nextTheme = themes[(currentIndex + 1) % themes.length];
        get().setTheme(nextTheme);
      },

      // Layout
      sidebarCollapsed: false,
      activityPanelCollapsed: false,
      toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),
      toggleActivityPanel: () => set((state) => ({ activityPanelCollapsed: !state.activityPanelCollapsed })),
      setSidebarCollapsed: (collapsed) => set({ sidebarCollapsed: collapsed }),
      setActivityPanelCollapsed: (collapsed) => set({ activityPanelCollapsed: collapsed }),

      // Workspace
      activeWorkspace: 'personal',
      setActiveWorkspace: (id) => set({ activeWorkspace: id }),

      // Auth
      token: localStorage.getItem('accessToken'),
      setToken: (token) => {
        if (token) localStorage.setItem('accessToken', token);
        else localStorage.removeItem('accessToken');
        set({ token });
      },
      logout: () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        set({ token: null, user: null });
      },

      // User
      user: null,
      setUser: (user) => set({ user }),

      // Permissions
      permissions: {
        canUpload: false,
        canDelete: false,
        canManageUsers: false,
        canViewAuditLogs: false,
        accountType: 'Individual',
      },
      setPermissions: (permissions) => set({ permissions }),

      // Workspaces
      workspaces: [],

      setWorkspaces: (workspaces) => set({ workspaces }),

      // Search
      searchQuery: '',
      setSearchQuery: (query) => set({ searchQuery: query }),

      // Notifications
      unreadCount: 3,
      setUnreadCount: (count) => set({ unreadCount: count }),
    }),
    {
      name: 'linkvault-storage',
      partialize: (state) => ({
        theme: state.theme,
        sidebarCollapsed: state.sidebarCollapsed,
        activityPanelCollapsed: state.activityPanelCollapsed,
      }),
    }
  )
);

// Initialize theme on load
if (typeof window !== 'undefined') {
  const stored = localStorage.getItem('linkvault-storage');
  if (stored) {
    const parsed = JSON.parse(stored);
    document.documentElement.setAttribute('data-theme', parsed.state?.theme || 'dark');
  } else {
    document.documentElement.setAttribute('data-theme', 'dark');
  }
}
