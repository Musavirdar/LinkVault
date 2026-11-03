import { useCallback, useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Upload, X, CheckCircle, AlertCircle, FileText, Image, FileArchive, Loader2, Folder } from 'lucide-react';
import { fileUploadApi, directoriesApi } from '@/api/client';
import { cn } from '@/lib/utils';

interface UploadedFile {
    id: string;
    name: string;
    url: string;
    size: number;
    mimeType: string;
}

function fileIcon(mime: string) {
    if (mime.startsWith('image/')) return <Image className="w-4 h-4" />;
    if (mime.includes('zip') || mime.includes('archive')) return <FileArchive className="w-4 h-4" />;
    return <FileText className="w-4 h-4" />;
}

function formatBytes(b: number) {
    if (b < 1024) return `${b} B`;
    if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} KB`;
    return `${(b / 1024 / 1024).toFixed(1)} MB`;
}

interface Props {
    directoryId?: string;
    onUploaded?: (file: UploadedFile) => void;
}

export function FileUploadZone({ directoryId, onUploaded }: Props) {
    const [dragging, setDragging] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [uploaded, setUploaded] = useState<UploadedFile[]>([]);
    const [error, setError] = useState('');
    const [progress, setProgress] = useState(0);
    const [directories, setDirectories] = useState<{ id: string; name: string }[]>([]);
    const [chosenDirectoryId, setChosenDirectoryId] = useState<string>(directoryId || '');

    useEffect(() => {
        if (!directoryId) {
            directoriesApi.getAll().then((res) => {
                if (res.data) setDirectories(res.data.map((d) => ({ id: d.id, name: d.name })));
            });
        }
    }, [directoryId]);

    const handleFiles = useCallback(async (files: FileList | null) => {
        if (!files || files.length === 0) return;
        const file = files[0];
        if (file.size > 10 * 1024 * 1024) { setError('File exceeds 10MB limit.'); return; }

        setError('');
        setUploading(true);
        setProgress(0);

        // Simulate progress
        const interval = setInterval(() => setProgress(p => Math.min(p + 15, 85)), 200);

        const targetDir = directoryId || (chosenDirectoryId.trim() ? chosenDirectoryId : undefined);
        const res = await fileUploadApi.upload(file, targetDir);
        clearInterval(interval);
        setProgress(100);

        if (res.error) {
            setError(res.error);
        } else if (res.data) {
            const f = res.data;
            setUploaded(prev => [{ id: f.id, name: f.name, url: f.url, size: f.size, mimeType: f.mimeType }, ...prev]);
            onUploaded?.(f);
        }

        setTimeout(() => { setUploading(false); setProgress(0); }, 600);
    }, [directoryId, chosenDirectoryId, onUploaded]);

    const onDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setDragging(false);
        handleFiles(e.dataTransfer.files);
    }, [handleFiles]);

    return (
        <div className="rounded-2xl overflow-hidden bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)]">
            {/* Optional Folder Selection */}
            {directories.length > 0 && !directoryId && (
                <div className="px-5 pt-3.5 pb-1 flex flex-wrap items-center justify-between gap-2 text-xs border-b border-black/[0.04] dark:border-white/05 bg-black/[0.01] dark:bg-white/[0.02]">
                    <div className="flex items-center gap-1.5 text-muted-foreground font-medium">
                        <Folder className="w-3.5 h-3.5 text-[#0071E3]" />
                        <span>Upload into folder:</span>
                    </div>
                    <select
                        value={chosenDirectoryId}
                        onChange={(e) => setChosenDirectoryId(e.target.value)}
                        className="px-2.5 py-1 rounded-lg bg-black/[0.04] dark:bg-white/10 border border-black/[0.06] dark:border-white/10 text-foreground text-xs font-medium outline-none focus:ring-2 focus:ring-[#0071E3]"
                    >
                        <option value="">Root / General (No folder)</option>
                        {directories.map((d) => (
                            <option key={d.id} value={d.id}>
                                {d.name}
                            </option>
                        ))}
                    </select>
                </div>
            )}

            {/* Drop Zone */}
            <motion.div
                className={cn(
                    'relative p-8 flex flex-col items-center justify-center gap-3 cursor-pointer transition-all',
                    'border-2 border-dashed rounded-xl m-2.5',
                    dragging ? 'border-[#0071E3] bg-[#0071E3]/5' : 'border-black/[0.1] dark:border-white/15 hover:border-[#0071E3]/50'
                )}
                animate={dragging ? { scale: 1.01 } : { scale: 1 }}
                onDragOver={e => { e.preventDefault(); setDragging(true); }}
                onDragLeave={() => setDragging(false)}
                onDrop={onDrop}
                onClick={() => document.getElementById('file-input')?.click()}
            >
                <input id="file-input" type="file" className="hidden" onChange={e => handleFiles(e.target.files)} />
                <div className={cn('w-12 h-12 rounded-full flex items-center justify-center transition-all', dragging ? 'bg-[var(--accent-primary)]/20' : 'bg-[var(--bg-primary)]')}>
                    <Upload className={cn('w-6 h-6 transition-all', dragging ? 'text-[var(--accent-primary)]' : 'text-[var(--text-muted)]')} />
                </div>
                <div className="text-center">
                    <p className="text-sm font-medium text-[var(--text-primary)]">
                        {dragging ? 'Drop to upload' : 'Drag & drop or click to upload'}
                    </p>
                    <p className="text-xs text-[var(--text-muted)] mt-1">
                        {chosenDirectoryId
                            ? `Target: ${directories.find(d => d.id === chosenDirectoryId)?.name || 'Folder'}`
                            : 'Max 10MB · Images, PDFs, Documents, Archives'}
                    </p>
                </div>
            </motion.div>

            {/* Upload Progress */}
            <AnimatePresence>
                {uploading && (
                    <motion.div initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} className="px-4 pb-3">
                        <div className="flex items-center gap-3">
                            <Loader2 className="w-4 h-4 animate-spin text-[var(--accent-primary)] flex-shrink-0" />
                            <div className="flex-1">
                                <div className="h-1.5 rounded-full bg-[var(--bg-primary)] overflow-hidden">
                                    <motion.div
                                        className="h-full rounded-full bg-[var(--accent-primary)]"
                                        animate={{ width: `${progress}%` }}
                                        transition={{ duration: 0.2 }}
                                    />
                                </div>
                            </div>
                            <span className="text-xs text-[var(--text-muted)]">{progress}%</span>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Error */}
            {error && (
                <div className="mx-4 mb-3 flex items-center gap-2 text-xs text-red-400 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2">
                    <AlertCircle className="w-3.5 h-3.5 flex-shrink-0" />
                    {error}
                </div>
            )}

            {/* Uploaded Files */}
            <AnimatePresence>
                {uploaded.length > 0 && (
                    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="px-4 pb-4 space-y-2">
                        <p className="text-xs font-medium text-[var(--text-secondary)]">Recently uploaded</p>
                        {uploaded.map(f => (
                            <div key={f.id} className="flex items-center justify-between p-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-subtle)] text-xs">
                                <div className="flex items-center gap-2 min-w-0">
                                    <span className="text-[var(--accent-primary)]">{fileIcon(f.mimeType)}</span>
                                    <span className="font-medium text-[var(--text-primary)] truncate max-w-[200px]">{f.name}</span>
                                    <span className="text-[var(--text-muted)]">({formatBytes(f.size)})</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    <CheckCircle className="w-3.5 h-3.5 text-green-400" />
                                    <button onClick={() => setUploaded(prev => prev.filter(x => x.id !== f.id))} className="text-[var(--text-muted)] hover:text-[var(--text-primary)]">
                                        <X className="w-3.5 h-3.5" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
}
