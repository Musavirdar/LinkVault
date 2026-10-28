import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Folder, FolderOpen, Plus, Trash2, Loader2, ChevronRight, Files, AlertCircle, RefreshCw } from 'lucide-react';
import { directoriesApi } from '@/api/client';
import { useStore } from '@/store/useStore';
import { DirectoryChatPanel } from '@/components/chat/DirectoryChatPanel';
import { cn } from '@/lib/utils';


interface Directory {
    id: string;
    name: string;
    description?: string;
    icon?: string;
    color?: string;
    isPublic: boolean;
    exportRule: string;
    parentDirectoryId?: string;
    resourceCount: number;
    createdAt: string;
}

const DIR_COLORS = ['#5865F2', '#3BA55D', '#FAA61A', '#ED4245', '#8B5CF6', '#10B981', '#F59E0B'];

export function DirectoriesPage() {
    const { permissions } = useStore();
    const [dirs, setDirs] = useState<Directory[]>([]);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState({ name: '', description: '', color: '#5865F2', isPublic: false });
    const [saving, setSaving] = useState(false);
    const [formError, setFormError] = useState('');
    const [selected, setSelected] = useState<string | null>(null);

    const load = async () => {
        setLoading(true);
        setError(null);
        const res = await directoriesApi.getAll();
        if (res.data) setDirs(res.data);
        else if (res.error) setError(res.error);
        setLoading(false);
    };

    // eslint-disable-next-line react-hooks/set-state-in-effect
    useEffect(() => { load(); }, []);

    const handleCreate = async () => {
        if (!form.name.trim()) {
            setFormError('Directory name is required.');
            return;
        }
        setFormError('');
        setSaving(true);
        await directoriesApi.create(form);
        setSaving(false);
        setShowForm(false);
        setForm({ name: '', description: '', color: '#5865F2', isPublic: false });
        load();
    };

    const handleDelete = async (id: string) => {
        await directoriesApi.delete(id);
        if (selected === id) setSelected(null);
        load();
    };

    return (
        <div className="max-w-5xl mx-auto py-6 px-4">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)]">Directories</h1>
                    <p className="text-sm text-[var(--text-secondary)] mt-0.5">{dirs.length} directories</p>
                </div>
                {permissions.canUpload && (
                <motion.button
                    whileTap={{ scale: 0.96 }}
                    onClick={() => setShowForm(!showForm)}
                    className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90"
                >
                    <Plus className="w-4 h-4" /> New Directory
                </motion.button>
                )}

            </div>

            {/* Create Form */}
            <AnimatePresence>
                {showForm && (
                    <motion.div initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} className="overflow-hidden mb-6">
                        <div className="rounded-xl p-5 bg-[var(--bg-secondary)] border border-[var(--border-strong)]">
                            <h3 className="font-semibold text-[var(--text-primary)] mb-4">Create Directory</h3>
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-4">
                                <input
                                    value={form.name}
                                    onChange={e => { setForm(p => ({ ...p, name: e.target.value })); setFormError(''); }}
                                    placeholder="Directory Name *"
                                    className="input-field"
                                />
                                <input value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} placeholder="Description (optional)" className="input-field" />
                            </div>
                            {formError && (
                                <p className="text-xs text-red-400 mb-3 px-1">{formError}</p>
                            )}
                            <div className="flex flex-wrap items-center gap-3 mb-4">
                                <div className="flex items-center gap-2">
                                    <span className="text-xs text-[var(--text-secondary)]">Color:</span>
                                    <div className="flex gap-1.5">
                                        {DIR_COLORS.map(c => (
                                            <button key={c} onClick={() => setForm(p => ({ ...p, color: c }))} className={cn('w-6 h-6 rounded-full transition-transform', form.color === c ? 'scale-125 ring-2 ring-white ring-offset-1 ring-offset-[var(--bg-secondary)]' : 'hover:scale-110')} style={{ backgroundColor: c }} />
                                        ))}
                                    </div>
                                </div>
                                <label className="flex items-center gap-2 cursor-pointer">
                                    <input type="checkbox" checked={form.isPublic} onChange={e => setForm(p => ({ ...p, isPublic: e.target.checked }))} />
                                    <span className="text-sm text-[var(--text-secondary)]">Public</span>
                                </label>
                            </div>
                            <div className="flex gap-2">
                                <motion.button whileTap={{ scale: 0.97 }} onClick={handleCreate} disabled={saving} className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90">
                                    {saving && <Loader2 className="w-3 h-3 animate-spin" />}
                                    Create
                                </motion.button>
                                <button onClick={() => setShowForm(false)} className="px-4 py-2 rounded-lg text-sm text-[var(--text-secondary)] hover:text-[var(--text-primary)]">Cancel</button>
                            </div>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>

            {loading ? (
                <div className="flex items-center justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-[var(--accent-primary)]" /></div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-16 text-center gap-3">
                    <AlertCircle className="w-10 h-10 text-red-400 opacity-70" />
                    <p className="text-sm text-[var(--text-secondary)]">{error}</p>
                    <motion.button whileTap={{ scale: 0.96 }} onClick={load} className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-strong)] text-sm text-[var(--text-primary)] hover:bg-[var(--bg-tertiary)]">
                        <RefreshCw className="w-3.5 h-3.5" /> Retry
                    </motion.button>
                </div>
            ) : dirs.length === 0 ? (
                <div className="text-center py-16 text-[var(--text-muted)]">
                    <Folder className="w-12 h-12 mx-auto mb-3 opacity-30" />
                    <p>No directories yet. Create your first one!</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <AnimatePresence>
                        {dirs.map((dir, i) => (
                            <React.Fragment key={dir.id}>
                                <motion.div
                                    initial={{ opacity: 0, scale: 0.95 }}
                                    animate={{ opacity: 1, scale: 1 }}
                                    exit={{ opacity: 0, scale: 0.9 }}
                                    transition={{ delay: i * 0.04 }}
                                    onClick={() => setSelected(selected === dir.id ? null : dir.id)}
                                    className={cn(
                                        'group relative cursor-pointer rounded-xl p-5',
                                        'bg-[var(--bg-secondary)] border transition-all',
                                        selected === dir.id ? 'border-[var(--accent-primary)] shadow-lg' : 'border-[var(--border-strong)] hover:border-[var(--accent-primary)]/50'
                                    )}
                                >
                                    {/* Color bar */}
                                    <div className="absolute top-0 left-0 right-0 h-1 rounded-t-xl" style={{ backgroundColor: dir.color || '#5865F2' }} />

                                    <div className="flex items-start justify-between mt-1">
                                        <div className="flex items-center gap-3">
                                            {selected === dir.id
                                                ? <FolderOpen className="w-6 h-6" style={{ color: dir.color || '#5865F2' }} />
                                                : <Folder className="w-6 h-6" style={{ color: dir.color || '#5865F2' }} />
                                            }
                                            <div>
                                                <div className="font-semibold text-[var(--text-primary)]">{dir.name}</div>
                                                {dir.description && <div className="text-xs text-[var(--text-muted)] mt-0.5 line-clamp-1">{dir.description}</div>}
                                            </div>
                                        </div>
                                        {permissions.canDelete && (
                                        <button
                                            onClick={e => { e.stopPropagation(); handleDelete(dir.id); }}
                                            className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-500/20 hover:text-red-400 text-[var(--text-muted)] transition-all"
                                        >
                                            <Trash2 className="w-3.5 h-3.5" />
                                        </button>
                                        )}

                                    </div>

                                    <div className="flex items-center justify-between mt-4">
                                        <div className="flex items-center gap-1.5 text-xs text-[var(--text-muted)]">
                                            <Files className="w-3.5 h-3.5" />
                                            <span>{dir.resourceCount} files</span>
                                        </div>
                                        <div className="flex items-center gap-1">
                                            {dir.isPublic && <span className="text-xs px-1.5 py-0.5 rounded bg-green-500/20 text-green-400">Public</span>}
                                            <ChevronRight className={cn('w-4 h-4 text-[var(--text-muted)] transition-transform', selected === dir.id && 'rotate-90')} />
                                        </div>
                                    </div>
                                </motion.div>

                                {/* Chat Panel — spans full width below the selected card */}
                                {selected === dir.id && (
                                    <motion.div
                                        key={`chat-${dir.id}`}
                                        initial={{ opacity: 0, height: 0 }}
                                        animate={{ opacity: 1, height: 'auto' }}
                                        exit={{ opacity: 0, height: 0 }}
                                        transition={{ duration: 0.2 }}
                                        className="col-span-full overflow-hidden"
                                        onClick={(e) => e.stopPropagation()}
                                    >
                                        <DirectoryChatPanel directoryId={dir.id} directoryName={dir.name} />
                                    </motion.div>
                                )}
                            </React.Fragment>
                        ))}
                    </AnimatePresence>
                </div>
            )}
        </div>
    );
}
