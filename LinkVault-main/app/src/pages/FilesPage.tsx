import { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Upload, File, Folder, Image, FileText, Archive, Search, Filter, Loader2, RefreshCw, Trash2, ExternalLink } from 'lucide-react';
import { DashboardHeader } from '@/components/dashboard/Header';
import { directoriesApi, resourcesApi, fileUploadApi } from '@/api/client';
import { useStore } from '@/store/useStore';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';


interface ResourceItem {
    id: string;
    name: string;
    type: string;
    fileType?: string;
    url?: string;
    sizeFormatted?: string;
    viewCount: number;
    clickCount: number;
    isPublic: boolean;
    createdAt: string;
    directoryName?: string;
}

const iconForType = (type: string, fileType?: string) => {
    const ft = (fileType ?? '').toLowerCase();
    if (ft.includes('image') || type === 'image') return <Image className="w-5 h-5 text-blue-400" />;
    if (ft.includes('pdf') || ft.includes('word') || ft.includes('doc') || type === 'document') return <FileText className="w-5 h-5 text-orange-400" />;
    if (ft.includes('zip') || ft.includes('archive') || type === 'archive') return <Archive className="w-5 h-5 text-purple-400" />;
    if (type === 'folder' || type === 'directory') return <Folder className="w-5 h-5 text-yellow-400" />;
    return <File className="w-5 h-5 text-[var(--text-muted)]" />;
};

export function FilesPage() {
    const { permissions } = useStore();
    const [resources, setResources] = useState<ResourceItem[]>([]);
    const [search, setSearch] = useState('');
    const [loading, setLoading] = useState(true);
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [page, setPage] = useState(1);
    const PAGE_SIZE = 20;


    const fetchFiles = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            // Fetch all directories, then get resources from each
            const dirsRes = await directoriesApi.getAll();
            if (!dirsRes.data) throw new Error(dirsRes.error ?? 'Failed to load directories');

            const all: ResourceItem[] = [];
            await Promise.all(
                dirsRes.data.map(async (dir) => {
                    const rRes = await directoriesApi.getResources(dir.id);
                    (rRes.data ?? []).forEach((r) => all.push({ ...r, directoryName: dir.name }));
                })
            );

            // Sort by createdAt desc
            all.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
            setResources(all);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to load files');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchFiles(); }, [fetchFiles]);

    const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(e.target.files ?? []);
        if (!files.length) return;
        setUploading(true);
        await Promise.all(files.map((f) => fileUploadApi.upload(f)));
        // Reset input so the same file can be re-uploaded
        e.target.value = '';
        setUploading(false);
        fetchFiles(); // refresh
    };

    const handleDelete = async (id: string) => {
        await resourcesApi.delete(id);
        setResources((prev) => prev.filter((r) => r.id !== id));
    };

    const filtered = resources.filter((r) =>
        r.name.toLowerCase().includes(search.toLowerCase()) ||
        (r.directoryName ?? '').toLowerCase().includes(search.toLowerCase())
    );
    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const paginated = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    const handleSearchChange = (val: string) => {
        setSearch(val);
        setPage(1); // reset to first page on new search
    };

    return (
        <div className="max-w-7xl mx-auto">
            <div className="flex items-start justify-between">
                <DashboardHeader title="My Files" subtitle={`${resources.length} files across all directories.`} />
                <motion.button
                    whileTap={{ scale: 0.94 }}
                    onClick={fetchFiles}
                    disabled={loading}
                    className="mt-2 p-2 rounded-lg text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-secondary)] transition-colors"
                    title="Refresh"
                >
                    <RefreshCw className={cn('w-4 h-4', loading && 'animate-spin')} />
                </motion.button>
            </div>

            {/* Upload Zone — only shown to users with upload permission */}
            {permissions.canUpload && (
            <motion.label
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className="block rounded-xl border-2 border-dashed border-[var(--border-strong)] p-8 mb-6 flex flex-col items-center gap-3 cursor-pointer hover:border-[var(--accent-primary)] hover:bg-[var(--accent-primary)]/5 transition-all"
                style={{ background: 'var(--bg-secondary)' }}
            >
                <input type="file" multiple className="hidden" onChange={handleUpload} />
                <div className="w-12 h-12 rounded-full flex items-center justify-center" style={{ background: 'var(--accent-primary)20' }}>
                    {uploading ? <Loader2 className="w-6 h-6 text-[var(--accent-primary)] animate-spin" /> : <Upload className="w-6 h-6 text-[var(--accent-primary)]" />}
                </div>
                <p className="font-semibold text-[var(--text-primary)]">{uploading ? 'Uploading…' : 'Drag & drop or click to upload'}</p>
                <p className="text-xs text-[var(--text-muted)]">Max 10MB · Images, PDFs, Documents, Archives</p>
            </motion.label>
            )}


            {/* Search bar */}
            <div className="flex gap-3 mb-5">
                <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
                    <input
                        type="text"
                        placeholder="Search files…"
                        value={search}
                        onChange={(e) => handleSearchChange(e.target.value)}
                        className="w-full pl-9 pr-4 py-2 rounded-lg text-sm outline-none"
                        style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)', color: 'var(--text-primary)' }}
                    />
                </div>
                <button className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium"
                    style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)', color: 'var(--text-secondary)' }}>
                    <Filter className="w-4 h-4" /> Filter
                </button>
            </div>

            {/* File table */}
            <div className="rounded-xl overflow-hidden" style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)' }}>
                <div className="grid grid-cols-[2fr_1fr_1fr_1fr_auto] gap-4 px-5 py-3 text-xs font-semibold uppercase tracking-wider text-[var(--text-muted)]"
                    style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <span>Name</span><span>Directory</span><span>Size</span><span>Uploaded</span><span></span>
                </div>

                {loading && (
                    <div className="flex items-center justify-center py-16">
                        <Loader2 className="w-6 h-6 animate-spin text-[var(--accent-primary)]" />
                    </div>
                )}
                {error && !loading && (
                    <div className="py-12 text-center text-red-400 text-sm">{error}</div>
                )}
                {!loading && !error && filtered.length === 0 && (
                    <div className="py-16 text-center text-[var(--text-muted)]">
                        {resources.length === 0 ? 'No files yet. Upload something!' : 'No files match your search.'}
                    </div>
                )}
                <AnimatePresence>
                    {paginated.map((file, i) => (
                        <motion.div
                            key={file.id}
                            initial={{ opacity: 0, x: -8 }}
                            animate={{ opacity: 1, x: 0 }}
                            exit={{ opacity: 0, x: 8 }}
                            transition={{ delay: i * 0.03 }}
                            className="grid grid-cols-[2fr_1fr_1fr_1fr_auto] gap-4 px-5 py-3.5 items-center group hover:bg-[var(--bg-tertiary)] transition-colors"
                            style={{ borderBottom: '1px solid var(--border-subtle)' }}
                        >
                            <div className="flex items-center gap-3">
                                {iconForType(file.type, file.fileType)}
                                <span className="text-sm font-medium text-[var(--text-primary)] truncate">{file.name}</span>
                            </div>
                            <span className="text-xs text-[var(--text-muted)] truncate">{file.directoryName ?? '—'}</span>
                            <span className="text-sm text-[var(--text-muted)]">{file.sizeFormatted ?? '—'}</span>
                            <span className="text-sm text-[var(--text-muted)]">{format(new Date(file.createdAt), 'MMM d, yyyy')}</span>
                            <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                {file.url && (
                                    <a href={file.url} target="_blank" rel="noopener noreferrer"
                                        className="p-1.5 rounded-md hover:bg-[var(--bg-primary)] text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-colors">
                                        <ExternalLink className="w-4 h-4" />
                                    </a>
                                )}
                                {permissions.canDelete && (
                                <button
                                    onClick={() => handleDelete(file.id)}
                                    className="p-1.5 rounded-md hover:bg-[var(--bg-primary)] text-[var(--text-muted)] hover:text-red-400 transition-colors">
                                    <Trash2 className="w-4 h-4" />
                                </button>
                                )}
                            </div>

                        </motion.div>
                    ))}
                </AnimatePresence>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex items-center justify-between mt-4 px-1">
                    <span className="text-xs text-[var(--text-muted)]">
                        Showing {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, filtered.length)} of {filtered.length}
                    </span>
                    <div className="flex gap-2">
                        <motion.button
                            whileTap={{ scale: 0.96 }}
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="px-3 py-1.5 rounded-lg text-xs font-medium transition-all disabled:opacity-40"
                            style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)', color: 'var(--text-secondary)' }}
                        >
                            Previous
                        </motion.button>
                        <span className="flex items-center px-3 text-xs text-[var(--text-muted)]">{page} / {totalPages}</span>
                        <motion.button
                            whileTap={{ scale: 0.96 }}
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page === totalPages}
                            className="px-3 py-1.5 rounded-lg text-xs font-medium transition-all disabled:opacity-40"
                            style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)', color: 'var(--text-secondary)' }}
                        >
                            Next
                        </motion.button>
                    </div>
                </div>
            )}
        </div>
    );
}
