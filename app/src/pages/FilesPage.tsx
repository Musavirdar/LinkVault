import { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Upload,
    File,
    Folder,
    Image,
    FileText,
    Archive,
    Search,
    Loader2,
    RefreshCw,
    Trash2,
    Download,
    FolderPlus,
    Eye,
} from 'lucide-react';
import { FilePreviewModal } from '@/components/files/FilePreviewModal';
import { DashboardHeader } from '@/components/dashboard/Header';
import { directoriesApi, resourcesApi, fileUploadApi, filesApi } from '@/api/client';
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
    directoryId?: string;
    directoryName?: string;
}

interface DirectoryOption {
    id: string;
    name: string;
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
    const [directories, setDirectories] = useState<DirectoryOption[]>([]);
    const [selectedDirectoryFilter, setSelectedDirectoryFilter] = useState<string>('all');
    const [targetDirectoryId, setTargetDirectoryId] = useState<string>(''); // '' = root
    const [search, setSearch] = useState('');
    const [loading, setLoading] = useState(true);
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [previewFile, setPreviewFile] = useState<ResourceItem | null>(null);
    const [page, setPage] = useState(1);
    const PAGE_SIZE = 20;

    const fetchFiles = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const [dirsRes, resRes] = await Promise.all([
                directoriesApi.getAll(),
                resourcesApi.getAll(),
            ]);

            if (dirsRes.data) {
                setDirectories(dirsRes.data.map(d => ({ id: d.id, name: d.name })));
            }

            if (resRes.data) {
                const dirMap = new Map((dirsRes.data ?? []).map(d => [d.id, d.name]));
                const allItems: ResourceItem[] = resRes.data.map(r => ({
                    ...r,
                    directoryName: r.directoryName || (r.directoryId ? dirMap.get(r.directoryId) : undefined),
                }));
                allItems.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
                setResources(allItems);
            } else if (resRes.error) {
                setError(resRes.error);
            }
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to load files');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchFiles(); }, [fetchFiles]);

    const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const fileList = Array.from(e.target.files ?? []);
        if (!fileList.length) return;
        setUploading(true);
        setError(null);
        try {
            const destDir = targetDirectoryId.trim() ? targetDirectoryId : undefined;
            for (const file of fileList) {
                const res = await fileUploadApi.upload(file, destDir);
                if (res.error) {
                    setError(res.error);
                }
            }
            e.target.value = '';
            await fetchFiles();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Upload failed');
        } finally {
            setUploading(false);
        }
    };

    const handleDelete = async (id: string) => {
        try {
            await resourcesApi.delete(id);
            setResources((prev) => prev.filter((r) => r.id !== id));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to delete file');
        }
    };

    const handleDownload = async (file: ResourceItem) => {
        await filesApi.downloadWithTicket(file.id, file.name);
    };

    // Filter logic
    const filtered = resources.filter((r) => {
        const matchesSearch =
            r.name.toLowerCase().includes(search.toLowerCase()) ||
            (r.directoryName ?? '').toLowerCase().includes(search.toLowerCase());

        if (!matchesSearch) return false;

        if (selectedDirectoryFilter === 'all') return true;
        if (selectedDirectoryFilter === 'root') return !r.directoryId;
        return r.directoryId === selectedDirectoryFilter;
    });

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const paginated = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    const handleSearchChange = (val: string) => {
        setSearch(val);
        setPage(1);
    };

    return (
        <div className="w-full max-w-[1100px] mx-auto space-y-6">
            <div className="flex items-start justify-between">
                <DashboardHeader title="My Files" subtitle={`${resources.length} files total across all folders.`} />
                <motion.button
                    whileTap={{ scale: 0.94 }}
                    onClick={fetchFiles}
                    disabled={loading}
                    className="mt-2 p-2.5 rounded-xl bg-black/[0.04] dark:bg-white/10 text-muted-foreground hover:text-foreground hover:bg-black/[0.08] dark:hover:bg-white/15 border border-black/[0.04] dark:border-white/10 transition-colors min-h-[40px] min-w-[40px] flex items-center justify-center"
                    title="Refresh files"
                >
                    <RefreshCw className={cn('w-4 h-4', loading && 'animate-spin')} />
                </motion.button>
            </div>

            {/* Upload Zone with Destination Folder Selector */}
            {permissions.canUpload && (
                <div className="rounded-2xl border-2 border-dashed border-black/[0.1] dark:border-white/15 p-6 bg-card transition-all space-y-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold text-foreground">
                            <FolderPlus className="w-4 h-4 text-[#0071E3]" />
                            <span>Destination Directory:</span>
                            <select
                                value={targetDirectoryId}
                                onChange={(e) => setTargetDirectoryId(e.target.value)}
                                className="px-3 py-1.5 rounded-lg bg-black/[0.04] dark:bg-white/10 border border-black/[0.06] dark:border-white/10 text-foreground text-xs font-medium focus:ring-2 focus:ring-[#0071E3] outline-none"
                            >
                                <option value="">Root / General (No folder)</option>
                                {directories.map((d) => (
                                    <option key={d.id} value={d.id}>
                                        {d.name}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <span className="text-[11px] text-muted-foreground">Max 10MB · Images, PDFs, Docs, Archives</span>
                    </div>

                    <label className="block p-6 rounded-xl border border-black/[0.06] dark:border-white/10 bg-black/[0.015] dark:bg-white/[0.02] flex flex-col items-center gap-2 cursor-pointer hover:border-[#0071E3] hover:bg-[#0071E3]/5 transition-all">
                        <input type="file" multiple className="hidden" disabled={uploading} onChange={handleUpload} />
                        <div className="w-10 h-10 rounded-full flex items-center justify-center bg-[#0071E3]/15 text-[#0071E3]">
                            {uploading ? <Loader2 className="w-5 h-5 animate-spin" /> : <Upload className="w-5 h-5" />}
                        </div>
                        <p className="font-semibold text-sm text-foreground">
                            {uploading
                                ? 'Uploading file…'
                                : targetDirectoryId
                                ? `Click or drop to upload into "${directories.find(d => d.id === targetDirectoryId)?.name || 'folder'}"`
                                : 'Click or drop to upload into General Files'}
                        </p>
                    </label>
                </div>
            )}

            {/* Search & Directory Filter Pills */}
            <div className="space-y-3">
                <div className="flex gap-3">
                    <div className="relative flex-1">
                        <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                        <input
                            type="text"
                            placeholder="Search by file name or folder…"
                            value={search}
                            onChange={(e) => handleSearchChange(e.target.value)}
                            className="w-full pl-10 pr-4 h-11 min-h-[44px] rounded-xl text-sm bg-card border border-black/[0.08] dark:border-white/12 text-foreground placeholder:text-muted-foreground outline-none focus:ring-4 focus:ring-[#0071E3]/15 focus:border-[#0071E3] transition-all"
                        />
                    </div>
                </div>

                {/* Filter Pills */}
                <div className="flex items-center gap-2 overflow-x-auto pb-1 text-xs">
                    <span className="text-muted-foreground font-medium flex-shrink-0">Filter by:</span>
                    <button
                        onClick={() => { setSelectedDirectoryFilter('all'); setPage(1); }}
                        className={cn(
                            'px-3 py-1 rounded-lg transition-colors flex-shrink-0',
                            selectedDirectoryFilter === 'all'
                                ? 'bg-[#0071E3] text-white font-medium shadow-xs'
                                : 'bg-black/[0.04] dark:bg-white/10 text-muted-foreground hover:text-foreground'
                        )}
                    >
                        All ({resources.length})
                    </button>
                    <button
                        onClick={() => { setSelectedDirectoryFilter('root'); setPage(1); }}
                        className={cn(
                            'px-3 py-1 rounded-lg transition-colors flex-shrink-0',
                            selectedDirectoryFilter === 'root'
                                ? 'bg-[#0071E3] text-white font-medium shadow-xs'
                                : 'bg-black/[0.04] dark:bg-white/10 text-muted-foreground hover:text-foreground'
                        )}
                    >
                        General / Root ({resources.filter(r => !r.directoryId).length})
                    </button>
                    {directories.map((d) => {
                        const count = resources.filter(r => r.directoryId === d.id).length;
                        return (
                            <button
                                key={d.id}
                                onClick={() => { setSelectedDirectoryFilter(d.id); setPage(1); }}
                                className={cn(
                                    'px-3 py-1 rounded-lg transition-colors flex-shrink-0 flex items-center gap-1.5',
                                    selectedDirectoryFilter === d.id
                                        ? 'bg-[#0071E3] text-white font-medium shadow-xs'
                                        : 'bg-black/[0.04] dark:bg-white/10 text-muted-foreground hover:text-foreground'
                                )}
                            >
                                <Folder className="w-3 h-3" />
                                <span>{d.name} ({count})</span>
                            </button>
                        );
                    })}
                </div>
            </div>

            {/* File Table */}
            <div className="rounded-2xl overflow-hidden bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]">
                <div className="grid grid-cols-[2fr_1.2fr_1fr_1fr_auto] gap-4 px-5 py-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground border-b border-black/[0.06] dark:border-white/10">
                    <span>Name</span>
                    <span>Directory</span>
                    <span>Size</span>
                    <span>Uploaded</span>
                    <span>Actions</span>
                </div>

                {loading && (
                    <div className="flex items-center justify-center py-16">
                        <Loader2 className="w-6 h-6 animate-spin text-[#0071E3]" />
                    </div>
                )}
                {error && !loading && (
                    <div className="py-12 text-center text-red-400 text-sm">{error}</div>
                )}
                {!loading && !error && filtered.length === 0 && (
                    <div className="py-16 text-center text-muted-foreground text-sm">
                        {resources.length === 0 ? 'No files uploaded yet. Upload one above!' : 'No files match your filter.'}
                    </div>
                )}
                <AnimatePresence>
                    {paginated.map((file, i) => (
                        <motion.div
                            key={file.id}
                            initial={{ opacity: 0, x: -8 }}
                            animate={{ opacity: 1, x: 0 }}
                            exit={{ opacity: 0, x: 8 }}
                            transition={{ delay: i * 0.02 }}
                            className="grid grid-cols-[2fr_1.2fr_1fr_1fr_auto] gap-4 px-5 py-3.5 items-center group hover:bg-black/[0.025] dark:hover:bg-white/[0.04] transition-colors border-b border-black/[0.04] dark:border-white/10"
                        >
                            <div 
                                onClick={() => setPreviewFile(file)}
                                className="flex items-center gap-3 min-w-0 cursor-pointer group/title"
                            >
                                {iconForType(file.type, file.fileType)}
                                <span className="text-sm font-medium text-foreground truncate group-hover/title:text-[#0071E3] transition-colors">{file.name}</span>
                            </div>

                            <span className="text-xs text-muted-foreground truncate">
                                {file.directoryName ? (
                                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-[#0071E3]/10 text-[#0071E3] font-medium">
                                        <Folder className="w-3 h-3" />
                                        {file.directoryName}
                                    </span>
                                ) : (
                                    <span className="text-muted-foreground/60 italic">Root / General</span>
                                )}
                            </span>

                            <span className="text-sm text-muted-foreground">{file.sizeFormatted ?? '—'}</span>
                            <span className="text-sm text-muted-foreground">{format(new Date(file.createdAt), 'MMM d, yyyy')}</span>

                            <div className="flex items-center gap-1.5 flex-shrink-0">
                                <button
                                    onClick={() => setPreviewFile(file)}
                                    className="p-1.5 rounded-lg hover:bg-black/[0.05] dark:hover:bg-white/10 text-muted-foreground hover:text-[#0071E3] transition-colors"
                                    title="Open / Preview file"
                                >
                                    <Eye className="w-4 h-4" />
                                </button>
                                <button
                                    onClick={() => handleDownload(file)}
                                    className="p-1.5 rounded-lg hover:bg-black/[0.05] dark:hover:bg-white/10 text-muted-foreground hover:text-foreground transition-colors"
                                    title="Download with secure ticket"
                                >
                                    <Download className="w-4 h-4" />
                                </button>
                                {permissions.canDelete && (
                                    <button
                                        onClick={() => handleDelete(file.id)}
                                        className="p-1.5 rounded-lg hover:bg-red-500/15 text-muted-foreground hover:text-red-400 transition-colors"
                                        title="Delete file"
                                    >
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
                    <span className="text-xs text-muted-foreground">
                        Showing {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, filtered.length)} of {filtered.length}
                    </span>
                    <div className="flex gap-2">
                        <motion.button
                            whileTap={{ scale: 0.96 }}
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="px-3 py-1.5 rounded-lg text-xs font-medium bg-black/[0.04] dark:bg-white/10 text-foreground transition-all disabled:opacity-40"
                        >
                            Previous
                        </motion.button>
                        <span className="flex items-center px-3 text-xs text-muted-foreground">{page} / {totalPages}</span>
                        <motion.button
                            whileTap={{ scale: 0.96 }}
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page === totalPages}
                            className="px-3 py-1.5 rounded-lg text-xs font-medium bg-black/[0.04] dark:bg-white/10 text-foreground transition-all disabled:opacity-40"
                        >
                            Next
                        </motion.button>
                    </div>
                </div>
            )}

            <FilePreviewModal
                isOpen={!!previewFile}
                onClose={() => setPreviewFile(null)}
                file={previewFile}
                onDownload={() => previewFile && handleDownload(previewFile)}
            />
        </div>
    );
}
