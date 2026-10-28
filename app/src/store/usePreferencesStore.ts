import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type WidgetId =
    | 'stats'
    | 'quickActions'
    | 'uploadZone'
    | 'recentFiles'
    | 'profileCard'
    | 'heatmap';

export interface WidgetConfig {
    id: WidgetId;
    label: string;
    description: string;
    icon: string;
    visible: boolean;
}

const DEFAULTS: WidgetConfig[] = [
    { id: 'stats', label: 'Stats Overview', description: 'Total links, files, views, clicks', icon: '📊', visible: true },
    { id: 'quickActions', label: 'Quick Actions', description: 'Manage Links and Upload Files shortcuts', icon: '⚡', visible: true },
    { id: 'uploadZone', label: 'Upload Zone', description: 'Drag-and-drop file upload area', icon: '📂', visible: true },
    { id: 'recentFiles', label: 'Recent Files', description: 'Latest files table', icon: '🗂️', visible: true },
    { id: 'profileCard', label: 'Public Profile', description: 'Your profile completeness card', icon: '👤', visible: true },
    { id: 'heatmap', label: 'Attendance Heatmap', 'description': 'Activity heatmap calendar', icon: '🔥', visible: true },
];

interface PreferencesState {
    widgets: WidgetConfig[];
    accentColor: string;
    compactMode: boolean;

    // Getters
    isVisible: (id: WidgetId) => boolean;

    // Actions
    toggle: (id: WidgetId) => void;
    setAccentColor: (color: string) => void;
    setCompactMode: (v: boolean) => void;
    resetToDefaults: () => void;
}

export const usePreferencesStore = create<PreferencesState>()(
    persist(
        (set, get) => ({
            widgets: DEFAULTS,
            accentColor: '#5865F2',
            compactMode: false,

            isVisible: (id) => get().widgets.find((w) => w.id === id)?.visible ?? true,

            toggle: (id) =>
                set((s) => ({
                    widgets: s.widgets.map((w) =>
                        w.id === id ? { ...w, visible: !w.visible } : w
                    ),
                })),

            setAccentColor: (color) => set({ accentColor: color }),
            setCompactMode: (v) => set({ compactMode: v }),

            resetToDefaults: () => set({ widgets: DEFAULTS, accentColor: '#5865F2', compactMode: false }),
        }),
        {
            name: 'linkvault-preferences',
        }
    )
);
