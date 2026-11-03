import { useState, useEffect, useCallback } from 'react';
import { linksApi, resourcesApi } from '@/api/client';
import { format } from 'date-fns';
import type { FileItem } from '@/types';

export interface DashboardStats {
  totalLinks: number;
  activeLinks: number;
  totalFiles: number;
  totalViews: number;
  totalClicks: number;
}

export interface DashboardData {
  stats: DashboardStats;
  recentFiles: FileItem[];
  isLoading: boolean;
  isInitialLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

interface CacheStore {
  stats: DashboardStats;
  recentFiles: FileItem[];
  lastFetchedAt: number;
}

let globalCache: CacheStore | null = null;
const CACHE_TTL_MS = 30_000; // 30 seconds

// Event emitter / callback subscribers for instant synchronization across components
const listeners = new Set<() => void>();

export function invalidateDashboardCache() {
  globalCache = null;
  listeners.forEach((notify) => notify());
}

export function useDashboardData(): DashboardData {
  const hasCache = !!globalCache && Date.now() - globalCache.lastFetchedAt < CACHE_TTL_MS;

  const [stats, setStats] = useState<DashboardStats>(
    globalCache?.stats ?? {
      totalLinks: 0,
      activeLinks: 0,
      totalFiles: 0,
      totalViews: 0,
      totalClicks: 0,
    }
  );

  const [recentFiles, setRecentFiles] = useState<FileItem[]>(globalCache?.recentFiles ?? []);
  const [isLoading, setIsLoading] = useState<boolean>(!hasCache);
  const [isInitialLoading, setIsInitialLoading] = useState<boolean>(!hasCache);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async (isSilent = false) => {
    if (!isSilent) setIsLoading(true);
    setError(null);

    try {
      // Execute single consolidated parallel query
      const [linksRes, resourcesRes] = await Promise.all([
        linksApi.getAll(),
        resourcesApi.getAll(),
      ]);

      const links = linksRes.data ?? [];
      const resources = resourcesRes.data ?? [];

      const totalLinks = links.length;
      const activeLinks = links.filter((l) => l.isActive).length;
      const totalFiles = resources.length;
      const totalClicks =
        links.reduce((acc, l) => acc + (l.clickCount ?? 0), 0) +
        resources.reduce((acc, r) => acc + (r.clickCount ?? 0), 0);
      const totalViews = resources.reduce((acc, r) => acc + (r.viewCount ?? 0), 0);

      const calculatedStats: DashboardStats = {
        totalLinks,
        activeLinks,
        totalFiles,
        totalViews,
        totalClicks,
      };

      // Process recent files (sort newest first, top 5)
      const mappedFiles: FileItem[] = resources.map((r) => {
        const VALID = ['image', 'document', 'folder', 'video', 'audio'] as const;
        type FT = typeof VALID[number];
        const typeLower = (r.type || '').toLowerCase();
        const safeType: FT = VALID.includes(typeLower as FT) ? (typeLower as FT) : 'document';
        return {
          id: r.id,
          name: r.name,
          type: safeType,
          date: format(new Date(r.createdAt), "MMM d, yyyy 'at' h:mm a"),
          size: r.sizeFormatted ?? undefined,
          directoryName: r.directoryName || undefined,
        };
      });

      mappedFiles.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
      const topRecentFiles = mappedFiles.slice(0, 5);

      // Update global cache
      globalCache = {
        stats: calculatedStats,
        recentFiles: topRecentFiles,
        lastFetchedAt: Date.now(),
      };

      setStats(calculatedStats);
      setRecentFiles(topRecentFiles);
    } catch (err: any) {
      setError(err?.message || 'Failed to load dashboard data');
    } finally {
      setIsLoading(false);
      setIsInitialLoading(false);
    }
  }, []);

  useEffect(() => {
    const notify = () => {
      fetchData(true);
    };
    listeners.add(notify);

    if (!hasCache) {
      fetchData(false);
    } else {
      // Revalidate in background if cache is older than 10 seconds
      if (Date.now() - globalCache!.lastFetchedAt > 10_000) {
        fetchData(true);
      }
    }

    return () => {
      listeners.delete(notify);
    };
  }, [fetchData, hasCache]);

  const refresh = useCallback(async () => {
    globalCache = null;
    await fetchData(false);
  }, [fetchData]);

  return {
    stats,
    recentFiles,
    isLoading,
    isInitialLoading,
    error,
    refresh,
  };
}

