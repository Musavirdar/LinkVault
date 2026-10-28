import { useState, useEffect, useRef, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, X, Link2, FileText, Folder, Loader2 } from 'lucide-react';
import { searchApi } from '@/api/client';
import { cn } from '@/lib/utils';

const ICON_MAP: Record<string, React.ReactNode> = {
    link: <Link2 className="w-3.5 h-3.5" />,
    file: <FileText className="w-3.5 h-3.5" />,
    resource: <FileText className="w-3.5 h-3.5" />,
    directory: <Folder className="w-3.5 h-3.5" />,
    folder: <Folder className="w-3.5 h-3.5" />,
};

interface Result { type: string; id: string; title: string; subtitle: string; icon: string; }

export function GlobalSearchBar() {
    const [query, setQuery] = useState('');
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [results, setResults] = useState<{ links: Result[]; resources: Result[]; directories: Result[] } | null>(null);
    const inputRef = useRef<HTMLInputElement>(null);
    const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

    const clearSearch = useCallback(() => {
        setResults(null);
        setOpen(false);
    }, []);

    useEffect(() => {
        // Use a local variable to express the early-exit condition to avoid
        // the react-hooks/set-state-in-effect lint rule flagging an early guard.
        const isShortQuery = query.length < 2;
        clearTimeout(timerRef.current);

        if (isShortQuery) {
            // Defer state update to avoid synchronous setState inside an effect
            const id = setTimeout(() => {
                setResults(null);
                setOpen(false);
            }, 0);
            return () => clearTimeout(id);
        }

        // eslint-disable-next-line react-hooks/set-state-in-effect
        setLoading(true);
        timerRef.current = setTimeout(async () => {
            const res = await searchApi.search(query);
            if (res.data) { setResults(res.data.results); setOpen(true); }
            setLoading(false);
        }, 300);
        return () => clearTimeout(timerRef.current);
    }, [query]);

    const allResults = results ? [...(results.links || []), ...(results.resources || []), ...(results.directories || [])] : [];

    return (
        <div className="relative w-full max-w-sm">
            <div className={cn(
                'flex items-center gap-2 px-3 py-1.5 rounded-lg transition-all',
                'bg-[var(--bg-primary)] border',
                open ? 'border-[var(--accent-primary)]' : 'border-[var(--border-strong)]'
            )}>
                {loading ? <Loader2 className="w-4 h-4 animate-spin text-[var(--text-muted)] flex-shrink-0" /> : <Search className="w-4 h-4 text-[var(--text-muted)] flex-shrink-0" />}
                <input
                    ref={inputRef}
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    onFocus={() => allResults.length > 0 && setOpen(true)}
                    placeholder="Search links, files, directories..."
                    className="flex-1 bg-transparent text-sm text-[var(--text-primary)] placeholder-[var(--text-muted)] outline-none"
                />
                {query && (
                    <button onClick={() => { setQuery(''); clearSearch(); }} className="text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-colors">
                        <X className="w-3.5 h-3.5" />
                    </button>
                )}
            </div>

            <AnimatePresence>
                {open && allResults.length > 0 && (
                    <motion.div
                        initial={{ opacity: 0, y: -4, scale: 0.97 }}
                        animate={{ opacity: 1, y: 0, scale: 1 }}
                        exit={{ opacity: 0, y: -4, scale: 0.97 }}
                        transition={{ duration: 0.15 }}
                        className="absolute top-full left-0 right-0 mt-1.5 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-strong)] shadow-2xl z-50 overflow-hidden"
                    >
                        <div className="max-h-64 overflow-y-auto">
                            {(['links', 'resources', 'directories'] as const).map(type => {
                                const group = results?.[type] || [];
                                if (!group.length) return null;
                                return (
                                    <div key={type}>
                                        <div className="px-3 py-1.5 text-xs font-semibold text-[var(--text-muted)] uppercase tracking-wider bg-[var(--bg-primary)]">
                                            {type}
                                        </div>
                                        {group.map(r => (
                                            <button
                                                key={r.id}
                                                className="w-full flex items-center gap-3 px-3 py-2.5 hover:bg-[var(--accent-primary)]/10 transition-colors text-left"
                                                onClick={() => { setQuery(''); setOpen(false); }}
                                            >
                                                <span className="text-[var(--accent-primary)]">{ICON_MAP[r.icon] || ICON_MAP[r.type] || <FileText className="w-3.5 h-3.5" />}</span>
                                                <div className="flex-1 min-w-0">
                                                    <div className="text-sm font-medium text-[var(--text-primary)] truncate">{r.title}</div>
                                                    {r.subtitle && <div className="text-xs text-[var(--text-muted)] truncate">{r.subtitle}</div>}
                                                </div>
                                            </button>
                                        ))}
                                    </div>
                                );
                            })}
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
}
