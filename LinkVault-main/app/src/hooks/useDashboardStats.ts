import { useEffect, useState } from 'react';
import { linksApi, directoriesApi } from '@/api/client';

export interface DashboardStats {
    totalLinks: number;
    activeLinks: number;
    totalFiles: number;
    totalViews: number;
    totalClicks: number;
    loading: boolean;
    error: string | null;
}

/**
 * Aggregates real stats from the Links and Directories APIs.
 * - totalLinks / activeLinks from linksApi.getAll()
 * - totalFiles / totalViews / totalClicks from directoriesApi.getAll()  (sum of resourceCount)
 */
export function useDashboardStats(): DashboardStats {
    const [stats, setStats] = useState<DashboardStats>({
        totalLinks: 0,
        activeLinks: 0,
        totalFiles: 0,
        totalViews: 0,
        totalClicks: 0,
        loading: true,
        error: null,
    });

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const [linksRes, dirsRes] = await Promise.all([
                    linksApi.getAll(),
                    directoriesApi.getAll(),
                ]);

                if (cancelled) return;

                const links = linksRes.data ?? [];
                const dirs = dirsRes.data ?? [];

                const totalLinks = links.length;
                const activeLinks = links.filter((l) => l.isActive).length;
                const totalClicks = links.reduce((acc, l) => acc + (l.clickCount ?? 0), 0);
                const totalFiles = dirs.reduce((acc, d) => acc + (d.resourceCount ?? 0), 0);

                // Fetch resources per directory to sum real viewCounts
                const resourceResults = await Promise.all(
                    dirs.map((d) => directoriesApi.getResources(d.id))
                );
                if (cancelled) return;

                let totalViews = 0;
                for (const res of resourceResults) {
                    for (const r of res.data ?? []) {
                        totalViews += r.viewCount ?? 0;
                    }
                }

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
        return () => { cancelled = true; };
    }, []);

    return stats;
}
