import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { TrendingUp, Eye, MousePointerClick, Link2, BarChart3, Loader2, AlertCircle, RefreshCw } from 'lucide-react';
import { DashboardHeader } from '@/components/dashboard/Header';
import { linksApi } from '@/api/client';

interface LinkItem {
    id: string;
    title: string;
    url: string;
    clickCount: number;
    isActive: boolean;
}

interface AnalyticsData {
    totalLinks: number;
    activeLinks: number;
    totalClicks: number;
    topLinks: LinkItem[];
    loading: boolean;
    error: string | null;
}

function useAnalyticsData() {
    const [data, setData] = useState<AnalyticsData>({
        totalLinks: 0, activeLinks: 0, totalClicks: 0, topLinks: [], loading: true, error: null,
    });

    const load = () => {
        setData(d => ({ ...d, loading: true, error: null }));
        linksApi.getAll().then((res) => {
            if (res.error) {
                setData(d => ({ ...d, loading: false, error: res.error! }));
                return;
            }
            const links = res.data ?? [];
            const sorted = [...links].sort((a, b) => b.clickCount - a.clickCount);
            setData({
                totalLinks: links.length,
                activeLinks: links.filter((l) => l.isActive).length,
                totalClicks: links.reduce((acc, l) => acc + (l.clickCount ?? 0), 0),
                topLinks: sorted.slice(0, 5),
                loading: false,
                error: null,
            });
        });
    };

    // eslint-disable-next-line react-hooks/set-state-in-effect
    useEffect(() => { load(); }, []);
    return { ...data, retry: load };
}

export function AnalyticsPage() {
    const stats = useAnalyticsData();

    if (stats.error) return (
        <div className="max-w-7xl mx-auto">
            <DashboardHeader title="Analytics" subtitle="Real-time performance of your links." />
            <div className="flex flex-col items-center justify-center py-24 gap-3">
                <AlertCircle className="w-10 h-10 text-red-400 opacity-70" />
                <p className="text-sm text-[var(--text-secondary)]">{stats.error}</p>
                <motion.button whileTap={{ scale: 0.96 }} onClick={stats.retry}
                    className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-strong)] text-sm text-[var(--text-primary)] hover:bg-[var(--bg-tertiary)]">
                    <RefreshCw className="w-3.5 h-3.5" /> Retry
                </motion.button>
            </div>
        </div>
    );

    const statCards = [
        { label: 'Total Links', value: stats.loading ? '…' : String(stats.totalLinks), icon: Link2, color: '#5865F2' },
        { label: 'Active Links', value: stats.loading ? '…' : String(stats.activeLinks), icon: TrendingUp, color: '#10B981' },
        { label: 'Total Clicks', value: stats.loading ? '…' : String(stats.totalClicks), icon: MousePointerClick, color: '#8B5CF6' },
        { label: 'Avg Clicks/Link', value: stats.loading ? '…' : (stats.totalLinks > 0 ? (stats.totalClicks / stats.totalLinks).toFixed(1) : '0'), icon: Eye, color: '#F59E0B' },
    ];

    return (
        <div className="max-w-7xl mx-auto">
            <DashboardHeader title="Analytics" subtitle="Real-time performance of your links." />

            {/* Stats row */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                {statCards.map((card, i) => (
                    <motion.div
                        key={card.label}
                        initial={{ opacity: 0, y: 12 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: i * 0.07 }}
                        className="rounded-xl p-5"
                        style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)' }}
                    >
                        <div className="flex items-center justify-between mb-3">
                            <span className="text-xs text-[var(--text-muted)] font-medium">{card.label}</span>
                            <div className="w-8 h-8 rounded-lg flex items-center justify-center" style={{ background: card.color + '20' }}>
                                <card.icon className="w-4 h-4" style={{ color: card.color }} />
                            </div>
                        </div>
                        <p className="text-2xl font-bold text-[var(--text-primary)] mb-1">{card.value}</p>
                        <span className="text-xs text-[var(--text-muted)]">Live from API</span>
                    </motion.div>
                ))}
            </div>

            {/* Click-rate bar chart — bars sized by real click data */}
            <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.3 }}
                className="rounded-xl p-6 mb-6"
                style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)' }}
            >
                <div className="flex items-center justify-between mb-4">
                    <div>
                        <h3 className="text-base font-semibold text-[var(--text-primary)]">Click Distribution</h3>
                        <p className="text-xs text-[var(--text-muted)]">By link — top {Math.min(stats.topLinks.length, 20)} links</p>
                    </div>
                    <BarChart3 className="w-5 h-5 text-[var(--text-muted)]" />
                </div>
                {stats.loading ? (
                    <div className="flex items-center justify-center h-36"><Loader2 className="w-5 h-5 animate-spin text-[var(--accent-primary)]" /></div>
                ) : stats.topLinks.length === 0 ? (
                    <div className="flex items-center justify-center h-36 text-[var(--text-muted)] text-sm">No links yet — add some to see click data.</div>
                ) : (
                    <div className="flex items-end gap-1.5 h-36">
                        {stats.topLinks.map((link, i) => {
                            const max = Math.max(...stats.topLinks.map((l) => l.clickCount), 1);
                            const pct = Math.round((link.clickCount / max) * 100);
                            return (
                                <motion.div
                                    key={link.id}
                                    title={`${link.title}: ${link.clickCount} clicks`}
                                    initial={{ height: 0 }}
                                    animate={{ height: `${Math.max(pct, 4)}%` }}
                                    transition={{ delay: 0.35 + i * 0.04, duration: 0.5, ease: 'easeOut' }}
                                    className="flex-1 rounded-t-sm cursor-pointer hover:opacity-80 transition-opacity"
                                    style={{ background: 'linear-gradient(to top, #5865F2, #8B5CF6)' }}
                                />
                            );
                        })}
                    </div>
                )}
            </motion.div>

            {/* Top links table */}
            <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.45 }}
                className="rounded-xl overflow-hidden"
                style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)' }}
            >
                <div className="px-5 py-4" style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">Top Performing Links</h3>
                </div>
                {stats.loading ? (
                    <div className="flex items-center justify-center py-10"><Loader2 className="w-5 h-5 animate-spin text-[var(--accent-primary)]" /></div>
                ) : stats.topLinks.length === 0 ? (
                    <div className="py-10 text-center text-[var(--text-muted)] text-sm">No links found.</div>
                ) : stats.topLinks.map((link, i) => (
                    <div
                        key={link.id}
                        className="flex items-center justify-between px-5 py-3.5 hover:bg-[var(--bg-tertiary)] transition-colors"
                        style={{ borderBottom: i < stats.topLinks.length - 1 ? '1px solid var(--border-subtle)' : undefined }}
                    >
                        <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-[var(--text-primary)] truncate">{link.title}</p>
                            <p className="text-xs text-[var(--text-muted)] font-mono truncate">{link.url}</p>
                        </div>
                        <div className="flex gap-6 text-sm flex-shrink-0 ml-4">
                            <span className="text-[var(--text-muted)]">
                                <span className="font-semibold text-[var(--text-primary)]">{link.clickCount}</span> clicks
                            </span>
                            <span className={`text-xs px-2 py-0.5 rounded-full ${link.isActive ? 'bg-green-400/10 text-green-400' : 'bg-[var(--bg-primary)] text-[var(--text-muted)]'}`}>
                                {link.isActive ? 'Active' : 'Inactive'}
                            </span>
                        </div>
                    </div>
                ))}
            </motion.div>
        </div>
    );
}
