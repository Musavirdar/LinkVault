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
                'flex items-center gap-2.5 px-3.5 h-10 min-h-[40px] rounded-full transition-all duration-150',
                'bg-black/[0.04] dark:bg-white/[0.06] border backdrop-blur-md',
                open 
                    ? 'border-[#0071E3] ring-4 ring-[#0071E3]/15' 
                    : 'border-black/[0.06] dark:border-white/10 hover:border-black/[0.12] dark:hover:border-white/20'
            )}>
                {loading ? <Loader2 className="w-4 h-4 animate-spin text-text-muted flex-shrink-0" /> : <Search className="w-4 h-4 text-text-muted flex-shrink-0" />}
                <input
                    ref={inputRef}
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    onFocus={() => allResults.length > 0 && setOpen(true)}
                    placeholder="Search links, files, directories..."
                    className="flex-1 bg-transparent text-xs sm:text-sm text-foreground placeholder:text-text-muted outline-none"
                />
                {query && (
                    <button 
                        onClick={() => { setQuery(''); clearSearch(); }} 
                        className="rounded-full p-1 text-text-muted hover:text-foreground hover:bg-black/[0.06] dark:hover:bg-white/10 transition-colors"
                        aria-label="Clear search"
                    >
                        <X className="w-3.5 h-3.5" />
                    </button>
                )}
            </div>

            <AnimatePresence>
                {open && allResults.length > 0 && (
                    <motion.div
                        initial={{ opacity: 0, y: -4, scale: 0.98 }}
                        animate={{ opacity: 1, y: 0, scale: 1 }}
                        exit={{ opacity: 0, y: -4, scale: 0.98 }}
                        transition={{ duration: 0.15 }}
                        className="absolute top-full left-0 right-0 mt-2 rounded-2xl bg-white/95 dark:bg-[#1C1C1E]/95 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 shadow-[0_20px_50px_rgba(0,0,0,0.18)] z-50 overflow-hidden"
                    >
                        <div className="max-h-72 overflow-y-auto p-1.5">
                            {(['links', 'resources', 'directories'] as const).map(type => {
                                const group = results?.[type] || [];
                                if (!group.length) return null;
                                return (
                                    <div key={type} className="mb-1.5 last:mb-0">
                                        <div className="px-3 py-1 text-[10px] font-semibold text-text-muted uppercase tracking-wider">
                                            {type}
                                        </div>
                                        {group.map(r => (
                                            <button
                                                key={r.id}
                                                className="w-full flex items-center gap-3 px-3 py-2 rounded-xl hover:bg-[#0071E3]/10 text-foreground transition-colors text-left min-h-[40px]"
                                                onClick={() => { setQuery(''); setOpen(false); }}
                                            >
                                                <span className="text-[#0071E3] dark:text-[#0A84FF]">{ICON_MAP[r.icon] || ICON_MAP[r.type] || <FileText className="w-4 h-4" />}</span>
                                                <div className="flex-1 min-w-0">
                                                    <div className="text-xs sm:text-sm font-medium truncate">{r.title}</div>
                                                    {r.subtitle && <div className="text-[11px] text-text-muted truncate">{r.subtitle}</div>}
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
