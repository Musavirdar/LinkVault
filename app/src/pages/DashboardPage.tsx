import { useState } from 'react';
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
import { useDashboardData } from '@/hooks/useDashboardData';
import { cn } from '@/lib/utils';

export function DashboardPage() {
    const { user } = useStore();
    const { isVisible, compactMode } = usePreferencesStore();
    const navigate = useNavigate();
    const [customizeOpen, setCustomizeOpen] = useState(false);
    
    // Unified, consolidated dashboard data with in-memory caching
    const { stats: apiStats, recentFiles, isInitialLoading, refresh } = useDashboardData();

    const gap = compactMode ? 'gap-3' : 'gap-6';
    const mb = compactMode ? 'mb-3' : 'mb-6';

    // Build real stat cards from live data
    const stats = [
        {
            label: 'Total Links',
            value: apiStats.totalLinks,
            subtext: isInitialLoading ? 'Loading…' : `+${apiStats.activeLinks} active`,
            change: apiStats.activeLinks > 0 ? apiStats.activeLinks : undefined,
            icon: 'links' as const,
        },
        {
            label: 'Total Files',
            value: apiStats.totalFiles,
            subtext: isInitialLoading ? 'Loading…' : apiStats.totalFiles > 0 ? `${apiStats.totalFiles} items` : 'No files yet',
            icon: 'files' as const,
        },
        {
            label: 'Total Clicks',
            value: apiStats.totalClicks,
            subtext: isInitialLoading ? 'Loading…' : 'Across all links',
            icon: 'clicks' as const,
        },
        {
            label: 'Total Views',
            value: apiStats.totalViews,
            subtext: isInitialLoading ? 'Loading…' : 'Across all content',
            icon: 'views' as const,
        },
    ];

    return (
        <div className="w-full max-w-[1100px] mx-auto space-y-6">
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
                    className="flex items-center gap-2 mt-1 px-3.5 py-2 rounded-xl text-xs font-semibold bg-black/[0.04] dark:bg-white/10 hover:bg-black/[0.08] dark:hover:bg-white/15 border border-black/[0.06] dark:border-white/12 text-foreground transition-all duration-150 active:scale-[0.98] flex-shrink-0"
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
                        initial={{ opacity: 0, y: -4 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, height: 0, marginBottom: 0 }}
                        transition={{ duration: 0.15 }}
                        className={mb}
                    >
                        <StatsGrid stats={stats} isLoading={isInitialLoading} />
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
                                initial={{ opacity: 0, y: -4 }}
                                animate={{ opacity: 1, y: 0 }}
                                exit={{ opacity: 0, height: 0 }}
                                transition={{ duration: 0.15 }}
                                className="rounded-2xl p-6 bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]"
                            >
                                <h3 className="text-lg font-bold tracking-tight text-foreground mb-1">Quick Actions</h3>
                                <p className="text-sm text-muted-foreground mb-4 leading-relaxed">Common tasks you can do right now.</p>
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
                            <motion.div key="uploadZone" initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }} transition={{ duration: 0.15 }}>
                                <FileUploadZone onUploaded={refresh} />
                            </motion.div>
                        )}
                    </AnimatePresence>

                    {/* Recent Files */}
                    <AnimatePresence>
                        {isVisible('recentFiles') && (
                            <motion.div key="recentFiles" initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }} transition={{ duration: 0.15 }}>
                                <FileList files={recentFiles} isLoading={isInitialLoading} />
                            </motion.div>
                        )}
                    </AnimatePresence>
                </div>

                {/* Profile Card */}
                <AnimatePresence>
                    {isVisible('profileCard') && (
                        <motion.div key="profileCard" initial={{ opacity: 0, x: 4 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, height: 0 }} transition={{ duration: 0.15 }}>
                            <ProfileCard />
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>

            {/* Heatmap */}
            <AnimatePresence>
                {isVisible('heatmap') && (
                    <motion.div key="heatmap" initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }} transition={{ duration: 0.15 }} className="mb-8">
                        <AttendanceHeatmap />
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Customize panel */}
            <CustomizeDashboard open={customizeOpen} onClose={() => setCustomizeOpen(false)} />
        </div>
    );
}
