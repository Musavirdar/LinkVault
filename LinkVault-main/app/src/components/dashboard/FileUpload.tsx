import { useCallback, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Upload, X, CheckCircle, AlertCircle, FileText, Image, FileArchive, Loader2 } from 'lucide-react';
import { fileUploadApi } from '@/api/client';
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

    const handleFiles = useCallback(async (files: FileList | null) => {
        if (!files || files.length === 0) return;
        const file = files[0];
        if (file.size > 10 * 1024 * 1024) { setError('File exceeds 10MB limit.'); return; }

        setError('');
        setUploading(true);
        setProgress(0);

        // Simulate progress
        const interval = setInterval(() => setProgress(p => Math.min(p + 15, 85)), 200);

        const res = await fileUploadApi.upload(file, directoryId);
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
    }, [directoryId, onUploaded]);

    const onDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setDragging(false);
        handleFiles(e.dataTransfer.files);
    }, [handleFiles]);

    return (
        <div className="rounded-xl overflow-hidden bg-[var(--bg-secondary)] border border-[var(--border-strong)]">
            {/* Drop Zone */}
            <motion.div
                className={cn(
                    'relative p-8 flex flex-col items-center justify-center gap-3 cursor-pointer transition-all',
                    'border-2 border-dashed rounded-xl m-2',
                    dragging ? 'border-[var(--accent-primary)] bg-[var(--accent-primary)]/5' : 'border-[var(--border-strong)] hover:border-[var(--accent-primary)]/50'
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
                    <p className="text-xs text-[var(--text-muted)] mt-1">Max 10MB · Images, PDFs, Documents, Archives</p>
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
                    <div className="px-4 pb-4 space-y-2">
                        {uploaded.map(f => (
                            <motion.div
                                key={f.id}
                                initial={{ opacity: 0, x: -10 }}
                                animate={{ opacity: 1, x: 0 }}
                                exit={{ opacity: 0, x: 10 }}
                                className="flex items-center gap-3 p-2.5 rounded-lg bg-[var(--bg-primary)]"
                            >
                                <div className="w-8 h-8 rounded flex items-center justify-center bg-green-500/20 text-green-400 flex-shrink-0">
                                    {fileIcon(f.mimeType)}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className="text-xs font-medium text-[var(--text-primary)] truncate">{f.name}</p>
                                    <p className="text-xs text-[var(--text-muted)]">{formatBytes(f.size)}</p>
                                </div>
                                <CheckCircle className="w-4 h-4 text-green-400 flex-shrink-0" />
                                <button aria-label={`Remove ${f.name}`} onClick={() => setUploaded(prev => prev.filter(u => u.id !== f.id))} className="p-0.5 rounded hover:text-[var(--text-primary)] text-[var(--text-muted)]">
                                    <X className="w-3.5 h-3.5" />
                                </button>

                            </motion.div>
                        ))}
                    </div>
                )}
            </AnimatePresence>
        </div>
    );
}
