import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { attendanceApi } from '@/api/client';
import { Flame, CalendarDays, TrendingUp, Zap } from 'lucide-react';
import { cn } from '@/lib/utils';

interface DayData { date: string; count: number; }

function getColor(count: number, max: number): string {
    if (count === 0) return 'var(--bg-primary)';
    const intensity = Math.ceil((count / max) * 4);
    switch (intensity) {
        case 1: return '#1e3a5f';
        case 2: return '#1e5fa8';
        case 3: return '#3b82f6';
        case 4: return '#60a5fa';
        default: return '#93c5fd';
    }
}

export function AttendanceHeatmap() {
    const [days, setDays] = useState<DayData[]>([]);
    const [summary, setSummary] = useState<{ totalDaysActive: number; totalActions: number; maxSingleDay: number; currentStreak: number } | null>(null);
    const [loading, setLoading] = useState(true);
    const [tooltip, setTooltip] = useState<{ date: string; count: number; x: number; y: number } | null>(null);

    useEffect(() => {
        attendanceApi.getHeatmap().then(res => {
            if (res.data) {
                setDays(res.data.days);
                setSummary(res.data.summary);
            }
            setLoading(false);
        });
    }, []);

    const WEEKS = 52;
    const max = Math.max(...days.map(d => d.count), 1);

    // Build a 52-week x 7-day grid from the days array
    const grid: (DayData | null)[][] = Array.from({ length: WEEKS }, () => Array(7).fill(null));
    const today = new Date();
    const startDate = new Date(today);
    startDate.setDate(today.getDate() - 364);

    days.forEach(d => {
        const date = new Date(d.date);
        const diff = Math.floor((date.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24));
        const weekIdx = Math.floor(diff / 7);
        const dayIdx = diff % 7;
        if (weekIdx >= 0 && weekIdx < WEEKS) {
            grid[weekIdx][dayIdx] = d;
        }
    });

    const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

    const formatDate = (dateStr: string) => {
        const d = new Date(dateStr);
        return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    };

    return (
        <div className="rounded-xl p-5 bg-[var(--bg-secondary)] border border-[var(--border-strong)]">
            {/* Title + Summary Stats */}
            <div className="flex items-start justify-between mb-5 flex-wrap gap-3">
                <div>
                    <h3 className="font-semibold text-[var(--text-primary)] flex items-center gap-2">
                        <CalendarDays className="w-4 h-4 text-[var(--accent-primary)]" />
                        Activity Heatmap
                    </h3>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">Your actions over the past year</p>
                </div>

                {summary && (
                    <div className="flex items-center gap-4 text-sm flex-wrap">
                        <div className="flex items-center gap-1.5 text-[var(--text-secondary)]">
                            <Flame className="w-4 h-4 text-orange-400" />
                            <span className="font-semibold text-[var(--text-primary)]">{summary.currentStreak}</span> day streak
                        </div>
                        <div className="flex items-center gap-1.5 text-[var(--text-secondary)]">
                            <Zap className="w-4 h-4 text-yellow-400" />
                            <span className="font-semibold text-[var(--text-primary)]">{summary.totalActions}</span> total
                        </div>
                        <div className="flex items-center gap-1.5 text-[var(--text-secondary)]">
                            <TrendingUp className="w-4 h-4 text-green-400" />
                            <span className="font-semibold text-[var(--text-primary)]">{summary.totalDaysActive}</span> days active
                        </div>
                    </div>
                )}
            </div>

            {loading ? (
                <div className="h-28 flex items-center justify-center">
                    <div className="w-5 h-5 rounded-full border-2 border-[var(--accent-primary)] border-t-transparent animate-spin" />
                </div>
            ) : (
                <div className="overflow-x-auto pb-2">
                    {/* Month labels */}
                    <div className="flex gap-[3px] mb-1 pl-0">
                        {Array.from({ length: WEEKS }).map((_, wi) => {
                            const d = new Date(startDate);
                            d.setDate(d.getDate() + wi * 7);
                            const show = d.getDate() <= 7 ? MONTHS[d.getMonth()] : '';
                            return (
                                <div key={wi} className="w-3 text-[9px] text-[var(--text-muted)] text-center flex-shrink-0">
                                    {show}
                                </div>
                            );
                        })}
                    </div>

                    {/* Grid */}
                    <div className="flex gap-[3px]">
                        {grid.map((week, wi) => (
                            <div key={wi} className="flex flex-col gap-[3px]">
                                {week.map((day, di) => (
                                    <motion.div
                                        key={`${wi}-${di}`}
                                        whileHover={{ scale: 1.4 }}
                                        onMouseEnter={e => day && setTooltip({ date: day.date, count: day.count, x: e.clientX, y: e.clientY })}
                                        onMouseLeave={() => setTooltip(null)}
                                        className={cn('w-3 h-3 rounded-sm cursor-default flex-shrink-0', !day && 'opacity-0')}
                                        style={{ backgroundColor: day ? getColor(day.count, max) : undefined, border: day?.count === 0 ? '1px solid var(--border-strong)' : 'none' }}
                                    />
                                ))}
                            </div>
                        ))}
                    </div>

                    {/* Legend */}
                    <div className="flex items-center gap-1 mt-3 justify-end">
                        <span className="text-[10px] text-[var(--text-muted)] mr-1">Less</span>
                        {[0, 1, 2, 3, 4].map(n => (
                            <div key={n} className="w-3 h-3 rounded-sm" style={{ backgroundColor: n === 0 ? 'var(--bg-primary)' : getColor(n, 4), border: n === 0 ? '1px solid var(--border-strong)' : 'none' }} />
                        ))}
                        <span className="text-[10px] text-[var(--text-muted)] ml-1">More</span>
                    </div>
                </div>
            )}

            {/* Tooltip */}
            {tooltip && (
                <div
                    className="fixed z-50 px-2.5 py-1.5 rounded-lg bg-[var(--bg-floating)] border border-[var(--border-strong)] text-xs text-[var(--text-primary)] shadow-xl pointer-events-none"
                    style={{ left: tooltip.x + 12, top: tooltip.y - 10 }}
                >
                    <div className="font-semibold">{tooltip.count} action{tooltip.count !== 1 ? 's' : ''}</div>
                    <div className="text-[var(--text-muted)]">{formatDate(tooltip.date)}</div>
                </div>
            )}
        </div>
    );
}
