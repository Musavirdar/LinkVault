import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Sliders } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { DashboardHeader } from '@/components/dashboard/Header';
import { StatsGrid } from '@/components/dashboard/StatsGrid';
import { ActionCard } from '@/components/dashboard/ActionCard';
import { ProfileCard } from '@/components/dashboard/ProfileCard';
import { FileList } from '@/components/dashboard/FileList';
import { AttendanceHeatmap } from '@/components/dashboard/AttendanceHeatmap';
import { FileUploadZone } from '@/components/dashboard/FileUpload';
import { CustomizeDashboard } from '@/components/dashboard/CustomizeDashboard';
import { useStore } from '@/store/useStore';
import { usePreferencesStore } from '@/store/usePreferencesStore';
import { useDashboardStats } from '@/hooks/useDashboardStats';
import { directoriesApi } from '@/api/client';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import type { FileItem } from '@/types';

export function DashboardPage() {
    const { user } = useStore();
    const { isVisible, compactMode } = usePreferencesStore();
    const navigate = useNavigate();
    const [customizeOpen, setCustomizeOpen] = useState(false);
    const [files, setFiles] = useState<FileItem[]>([]);
    const apiStats = useDashboardStats();

    // Fetch real recent files from all directories
    useEffect(() => {
        directoriesApi.getAll().then(async (dirsRes) => {
            const dirs = dirsRes.data ?? [];
            const all: FileItem[] = [];
            await Promise.all(dirs.map(async (dir) => {
                const rRes = await directoriesApi.getResources(dir.id);
                (rRes.data ?? []).forEach((r) => {
                    const VALID = ['image', 'document', 'folder', 'video', 'audio'] as const;
                    type FT = typeof VALID[number];
                    const safeType: FT = VALID.includes(r.type as FT) ? (r.type as FT) : 'document';
                    all.push({
                        id: r.id,
                        name: r.name,
                        type: safeType,
                        date: format(new Date(r.createdAt), "MMM d, yyyy 'at' h:mm a"),
                        size: r.sizeFormatted ?? undefined,
                    });
                });
            }));
            // Sort newest first, take top 5
            all.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
            setFiles(all.slice(0, 5));
        });
    }, []);

    const gap = compactMode ? 'gap-3' : 'gap-6';
    const mb = compactMode ? 'mb-3' : 'mb-6';

    // Build real stat cards from live data
    const stats = [
        {
            label: 'Total Links',
            value: apiStats.totalLinks,
            subtext: apiStats.loading ? 'Loading…' : `+${apiStats.activeLinks} active`,
            change: apiStats.activeLinks > 0 ? apiStats.activeLinks : undefined,
            icon: 'links' as const,
        },
        {
            label: 'Total Files',
            value: apiStats.totalFiles,
            subtext: apiStats.loading ? 'Loading…' : apiStats.totalFiles > 0 ? `${apiStats.totalFiles} items` : 'No files yet',
            icon: 'files' as const,
        },
        {
            label: 'Total Clicks',
            value: apiStats.totalClicks,
            subtext: apiStats.loading ? 'Loading…' : 'Across all links',
            icon: 'clicks' as const,
        },
        {
            label: 'Total Views',
            value: apiStats.totalViews,
            subtext: apiStats.loading ? 'Loading…' : 'Across all content',
            icon: 'views' as const,
        },
    ];

    return (
        <div className="max-w-7xl mx-auto">
            {/* Header + Customize button */}
            <div className="flex items-start justify-between mb-1">
                <DashboardHeader
                    title={`Welcome back, ${user?.name?.split(' ')[0] || 'User'}!`}
                    subtitle="Here's what's happening with your links and files today."
                    showAddButton={true}
                />
                <motion.button
                    whileTap={{ scale: 0.94 }}
                    onClick={() => setCustomizeOpen(true)}
                    className="flex items-center gap-2 mt-1 px-3 py-2 rounded-xl text-sm font-medium transition-all flex-shrink-0"
                    style={{
                        background: 'var(--bg-secondary)',
                        border: '1px solid var(--border-strong)',
                        color: 'var(--text-secondary)',
                    }}
                >
                    <Sliders className="w-4 h-4" />
                    <span className="hidden sm:inline">Customize</span>
                </motion.button>
            </div>

            {/* Stats */}
            <AnimatePresence>
                {isVisible('stats') && (
                    <motion.div
                        key="stats"
                        initial={{ opacity: 0, y: -8 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, height: 0, marginBottom: 0 }}
                        className={mb}
                    >
                        <StatsGrid stats={stats} />
                    </motion.div>
                )}
            </AnimatePresence>

            <div className={cn(`grid grid-cols-1 lg:grid-cols-3 ${gap}`, mb)}>
                <div className={cn('lg:col-span-2 space-y-6', compactMode && 'space-y-3')}>
                    {/* Quick Actions */}
                    <AnimatePresence>
                        {isVisible('quickActions') && (
                            <motion.div
                                key="quickActions"
                                initial={{ opacity: 0, y: -8 }}
                                animate={{ opacity: 1, y: 0 }}
                                exit={{ opacity: 0, height: 0 }}
                                className="rounded-lg p-5 bg-[var(--bg-secondary)] border border-[var(--border-subtle)]"
                            >
                                <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-1">Quick Actions</h3>
                                <p className="text-sm text-[var(--text-secondary)] mb-4">Common tasks you can do right now.</p>
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                    <ActionCard title="Manage Links" description="Add, edit, or organize your links" icon="links" onClick={() => navigate('/links')} />
                                    <ActionCard title="Upload Files" description="Upload and share files with others" icon="upload" onClick={() => navigate('/files')} />
                                </div>
                            </motion.div>
                        )}
                    </AnimatePresence>

                    {/* Upload Zone */}
                    <AnimatePresence>
                        {isVisible('uploadZone') && (
                            <motion.div key="uploadZone" initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }}>
                                <FileUploadZone />
                            </motion.div>
                        )}
                    </AnimatePresence>

                    {/* Recent Files */}
                    <AnimatePresence>
                        {isVisible('recentFiles') && (
                            <motion.div key="recentFiles" initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }}>
                                <FileList files={files} />
                            </motion.div>
                        )}
                    </AnimatePresence>
                </div>

                {/* Profile Card */}
                <AnimatePresence>
                    {isVisible('profileCard') && (
                        <motion.div key="profileCard" initial={{ opacity: 0, x: 8 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, height: 0 }}>
                            <ProfileCard />
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>

            {/* Heatmap */}
            <AnimatePresence>
                {isVisible('heatmap') && (
                    <motion.div key="heatmap" initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }} className="mb-8">
                        <AttendanceHeatmap />
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Customize panel */}
            <CustomizeDashboard open={customizeOpen} onClose={() => setCustomizeOpen(false)} />
        </div>
    );
}
