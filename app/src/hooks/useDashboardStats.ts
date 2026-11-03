import { useEffect, useState, useCallback } from 'react';
import { linksApi, resourcesApi } from '@/api/client';

export interface DashboardStats {
    totalLinks: number;
    activeLinks: number;
    totalFiles: number;
    totalViews: number;
    totalClicks: number;
    loading: boolean;
    error: string | null;
    refresh: () => void;
}

/**
 * Aggregates live stats from Links and Resources APIs.
 * Includes both folder files and root/general files.
 */
export function useDashboardStats(): DashboardStats {
    const [stats, setStats] = useState<Omit<DashboardStats, 'refresh'>>({
        totalLinks: 0,
        activeLinks: 0,
        totalFiles: 0,
        totalViews: 0,
        totalClicks: 0,
        loading: true,
        error: null,
    });

    const [tick, setTick] = useState(0);
    const refresh = useCallback(() => setTick((t) => t + 1), []);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const [linksRes, resourcesRes] = await Promise.all([
                    linksApi.getAll(),
                    resourcesApi.getAll(),
                ]);

                if (cancelled) return;

                const links = linksRes.data ?? [];
                const resources = resourcesRes.data ?? [];

                const totalLinks = links.length;
                const activeLinks = links.filter((l) => l.isActive).length;
                const totalFiles = resources.length;
                const totalClicks =
                    links.reduce((acc, l) => acc + (l.clickCount ?? 0), 0) +
                    resources.reduce((acc, r) => acc + (r.clickCount ?? 0), 0);
                const totalViews = resources.reduce((acc, r) => acc + (r.viewCount ?? 0), 0);

                setStats({
                    totalLinks,
                    activeLinks,
                    totalFiles,
                    totalViews,
                    totalClicks,
                    loading: false,
                    error: null,
                });
            } catch (err) {
                if (!cancelled) {
                    setStats((s) => ({
                        ...s,
                        loading: false,
                        error: err instanceof Error ? err.message : 'Failed to load stats',
                    }));
                }
            }
        }

        load();
        return () => {
            cancelled = true;
        };
    }, [tick]);

    return { ...stats, refresh };
}
