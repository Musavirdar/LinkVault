import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Shield, Loader2, Filter, ChevronDown, User, FileText, Link2, Folder, Settings } from 'lucide-react';
import { auditApi } from '@/api/client';
import { cn } from '@/lib/utils';

interface AuditLog {
    id: string;
    actionType: string;
    entityType: string;
    entityName?: string;
    description?: string;
    userName?: string;
    createdAt: string;
}

const ACTION_COLORS: Record<string, string> = {
    Created: 'text-green-400 bg-green-400/10',
    Updated: 'text-blue-400 bg-blue-400/10',
    Deleted: 'text-red-400 bg-red-400/10',
    Uploaded: 'text-purple-400 bg-purple-400/10',
    Login: 'text-yellow-400 bg-yellow-400/10',
    Logout: 'text-orange-400 bg-orange-400/10',
    Viewed: 'text-cyan-400 bg-cyan-400/10',
};

const ENTITY_ICON: Record<string, React.ReactNode> = {
    User: <User className="w-3.5 h-3.5" />,
    Resource: <FileText className="w-3.5 h-3.5" />,
    Link: <Link2 className="w-3.5 h-3.5" />,
    Directory: <Folder className="w-3.5 h-3.5" />,
    Settings: <Settings className="w-3.5 h-3.5" />,
};

export function AuditPage() {
    const [logs, setLogs] = useState<AuditLog[]>([]);
    const [loading, setLoading] = useState(true);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [filters, setFilters] = useState({ actionType: '', entityType: '', fromDate: '', toDate: '' });
    const [showFilters, setShowFilters] = useState(false);

    const load = async (p = 1) => {
        setLoading(true);
        const res = await auditApi.getLogs({
            page: p,
            pageSize: 20,
            actionType: filters.actionType || undefined,
            entityType: filters.entityType || undefined,
            fromDate: filters.fromDate || undefined,
            toDate: filters.toDate || undefined,
        });
        if (res.data) {
            setLogs(res.data.items);
            setTotal(res.data.totalCount);
            setTotalPages(res.data.totalPages);
        }
        setLoading(false);
    };

    // eslint-disable-next-line react-hooks/set-state-in-effect, react-hooks/exhaustive-deps
    useEffect(() => { load(page); }, [page]);

    const formatTime = (ts: string) => {
        const d = new Date(ts);
        return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
    };

    return (
        <div className="max-w-5xl mx-auto py-6 px-4">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)]">Audit Log</h1>
                    <p className="text-sm text-[var(--text-secondary)] mt-0.5">{total} events recorded</p>
                </div>
                <button
                    onClick={() => setShowFilters(!showFilters)}
                    className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-strong)] text-sm text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition-all"
                >
                    <Filter className="w-4 h-4" />
                    Filters
                    <ChevronDown className={cn('w-3.5 h-3.5 transition-transform', showFilters && 'rotate-180')} />
                </button>
            </div>

            {/* Filters */}
            {showFilters && (
                <div className="rounded-xl p-4 bg-[var(--bg-secondary)] border border-[var(--border-strong)] mb-6 grid grid-cols-2 sm:grid-cols-4 gap-3">
                    <select value={filters.actionType} onChange={e => setFilters(p => ({ ...p, actionType: e.target.value }))} className="input-field">
                        <option value="">All Actions</option>
                        {['Created', 'Updated', 'Deleted', 'Viewed', 'Uploaded', 'Login', 'Logout'].map(a => <option key={a} value={a}>{a}</option>)}
                    </select>
                    <select value={filters.entityType} onChange={e => setFilters(p => ({ ...p, entityType: e.target.value }))} className="input-field">
                        <option value="">All Entities</option>
                        {['User', 'Resource', 'Link', 'Directory', 'Role'].map(a => <option key={a} value={a}>{a}</option>)}
                    </select>
                    <input type="date" value={filters.fromDate} onChange={e => setFilters(p => ({ ...p, fromDate: e.target.value }))} className="input-field" placeholder="From" />
                    <input type="date" value={filters.toDate} onChange={e => setFilters(p => ({ ...p, toDate: e.target.value }))} className="input-field" placeholder="To" />
                    <button onClick={() => { setPage(1); load(1); }} className="col-span-2 sm:col-span-4 px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90 transition-all">Apply Filters</button>
                </div>
            )}

            {/* Log Entries */}
            {loading ? (
                <div className="flex items-center justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-[var(--accent-primary)]" /></div>
            ) : logs.length === 0 ? (
                <div className="text-center py-16 text-[var(--text-muted)]">
                    <Shield className="w-12 h-12 mx-auto mb-3 opacity-30" />
                    <p>No audit events found.</p>
                </div>
            ) : (
                <div className="space-y-2">
                    {logs.map((log, i) => (
                        <motion.div
                            key={log.id}
                            initial={{ opacity: 0, x: -10 }}
                            animate={{ opacity: 1, x: 0 }}
                            transition={{ delay: i * 0.02 }}
                            className="flex items-start gap-4 p-4 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-strong)] hover:border-[var(--accent-primary)]/30 transition-all"
                        >
                            {/* Action badge */}
                            <span className={cn('flex-shrink-0 px-2 py-1 rounded text-xs font-semibold', ACTION_COLORS[log.actionType] || 'text-[var(--text-secondary)] bg-[var(--bg-primary)]')}>
                                {log.actionType}
                            </span>

                            {/* Details */}
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2 flex-wrap">
                                    <span className="flex items-center gap-1 text-xs text-[var(--text-muted)]">
                                        {ENTITY_ICON[log.entityType] || <FileText className="w-3.5 h-3.5" />}
                                        {log.entityType}
                                    </span>
                                    {log.entityName && <span className="font-medium text-sm text-[var(--text-primary)] truncate">{log.entityName}</span>}
                                </div>
                                {log.description && <p className="text-xs text-[var(--text-secondary)] mt-0.5 truncate">{log.description}</p>}
                            </div>

                            {/* Right side */}
                            <div className="text-right flex-shrink-0">
                                {log.userName && <div className="text-xs font-medium text-[var(--text-secondary)]">{log.userName}</div>}
                                <div className="text-xs text-[var(--text-muted)] mt-0.5">{formatTime(log.createdAt)}</div>
                            </div>
                        </motion.div>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex items-center justify-center gap-2 mt-6">
                    <button disabled={page <= 1} onClick={() => setPage(p => p - 1)} className="px-3 py-1.5 rounded text-sm bg-[var(--bg-secondary)] border border-[var(--border-strong)] disabled:opacity-40 hover:border-[var(--accent-primary)] transition-all">Prev</button>
                    <span className="text-sm text-[var(--text-secondary)]">Page {page} of {totalPages}</span>
                    <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} className="px-3 py-1.5 rounded text-sm bg-[var(--bg-secondary)] border border-[var(--border-strong)] disabled:opacity-40 hover:border-[var(--accent-primary)] transition-all">Next</button>
                </div>
            )}
        </div>
    );
}
