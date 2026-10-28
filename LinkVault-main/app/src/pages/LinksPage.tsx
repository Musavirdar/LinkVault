import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Plus, Trash2, ExternalLink, Pencil, Star, Link2, Loader2, Copy, Check, AlertCircle, RefreshCw } from 'lucide-react';
import { linksApi } from '@/api/client';
import { cn } from '@/lib/utils';

interface Link {
    id: string;
    title: string;
    url: string;
    description?: string;
    color?: string;
    icon?: string;
    clickCount: number;
    isActive: boolean;
    isFeatured: boolean;
    createdAt: string;
}

export function LinksPage() {
    const [links, setLinks] = useState<Link[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);
    const [editId, setEditId] = useState<string | null>(null);
    const [copied, setCopied] = useState<string | null>(null);
    const [form, setForm] = useState({ title: '', url: '', description: '', color: '#5865F2', isFeatured: false });
    const [saving, setSaving] = useState(false);
    const [formError, setFormError] = useState('');

    const setField = (key: string, value: string | boolean) => {
        setForm(p => ({ ...p, [key]: value }));
        setFormError('');
    };

    const loadLinks = async () => {
        setLoading(true);
        setError(null);
        const res = await linksApi.getAll();
        if (res.data) setLinks(res.data);
        else if (res.error) setError(res.error);
        setLoading(false);
    };

    // eslint-disable-next-line react-hooks/set-state-in-effect
    useEffect(() => { loadLinks(); }, []);

    const handleSave = async () => {
        if (!form.title.trim()) { setFormError('Link title is required.'); return; }
        if (!form.url.trim()) { setFormError('URL is required.'); return; }
        setSaving(true);
        setFormError('');
        if (editId) {
            await linksApi.update(editId, form);
        } else {
            await linksApi.create(form);
        }
        setSaving(false);
        setShowForm(false);
        setEditId(null);
        setForm({ title: '', url: '', description: '', color: '#5865F2', isFeatured: false });
        loadLinks();
    };

    const handleDelete = async (id: string) => {
        await linksApi.delete(id);
        loadLinks();
    };

    const handleEdit = (link: Link) => {
        setForm({ title: link.title, url: link.url, description: link.description || '', color: link.color || '#5865F2', isFeatured: link.isFeatured });
        setEditId(link.id);
        setShowForm(true);
    };

    const handleCopy = (url: string) => {
        navigator.clipboard.writeText(url);
        setCopied(url);
        setTimeout(() => setCopied(null), 2000);
    };

    const handleExternalClick = (id: string) => {
        // Optimistically increment click count in local state
        setLinks(prev => prev.map(l => l.id === id ? { ...l, clickCount: l.clickCount + 1 } : l));
        // Fire-and-forget to backend
        linksApi.trackClick(id);
    };

    return (
        <div className="max-w-4xl mx-auto py-6 px-4">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)]">My Links</h1>
                    <p className="text-sm text-[var(--text-secondary)] mt-0.5">{links.length} links total</p>
                </div>
                <motion.button
                    whileTap={{ scale: 0.96 }}
                    onClick={() => { setShowForm(!showForm); setEditId(null); setForm({ title: '', url: '', description: '', color: '#5865F2', isFeatured: false }); }}
                    className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90 transition-all"
                >
                    <Plus className="w-4 h-4" />
                    Add Link
                </motion.button>
            </div>

            {/* Create/Edit Form */}
            <AnimatePresence>
                {showForm && (
                    <motion.div
                        initial={{ opacity: 0, height: 0 }}
                        animate={{ opacity: 1, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        className="overflow-hidden mb-6"
                    >
                        <div className="rounded-xl p-5 bg-[var(--bg-secondary)] border border-[var(--border-strong)]">
                            <h3 className="font-semibold text-[var(--text-primary)] mb-4">{editId ? 'Edit Link' : 'New Link'}</h3>
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
                                <input
                                    value={form.title}
                                    onChange={e => setField('title', e.target.value)}
                                    placeholder="Link Title *"
                                    className="input-field"
                                />
                                <input
                                    value={form.url}
                                    onChange={e => setField('url', e.target.value)}
                                    placeholder="https://... *"
                                    type="url"
                                    className="input-field"
                                />
                                <input
                                    value={form.description}
                                    onChange={e => setField('description', e.target.value)}
                                    placeholder="Description (optional)"
                                    className="input-field sm:col-span-2"
                                />
                            </div>
                            {formError && (
                                <p className="text-xs text-red-400 mb-3 px-1">{formError}</p>
                            )}
                            <div className="flex items-center gap-4 mb-4">
                                <label className="flex items-center gap-2 cursor-pointer">
                                    <input type="color" value={form.color} onChange={e => setForm(p => ({ ...p, color: e.target.value }))} className="w-8 h-8 rounded cursor-pointer border-0" />
                                    <span className="text-sm text-[var(--text-secondary)]">Color</span>
                                </label>
                                <label className="flex items-center gap-2 cursor-pointer">
                                    <input type="checkbox" checked={form.isFeatured} onChange={e => setForm(p => ({ ...p, isFeatured: e.target.checked }))} className="rounded" />
                                    <span className="text-sm text-[var(--text-secondary)]">Featured</span>
                                </label>
                            </div>
                            <div className="flex gap-2">
                                <motion.button whileTap={{ scale: 0.97 }} onClick={handleSave} disabled={saving} className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90">
                                    {saving && <Loader2 className="w-3 h-3 animate-spin" />}
                                    {editId ? 'Save Changes' : 'Create Link'}
                                </motion.button>
                                <button onClick={() => { setShowForm(false); setEditId(null); }} className="px-4 py-2 rounded-lg text-sm text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition-all">Cancel</button>
                            </div>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Links List */}
            {loading ? (
                <div className="flex items-center justify-center py-16">
                    <Loader2 className="w-6 h-6 animate-spin text-[var(--accent-primary)]" />
                </div>
            ) : error ? (
                <div className="flex flex-col items-center justify-center py-16 text-center gap-3">
                    <AlertCircle className="w-10 h-10 text-red-400 opacity-70" />
                    <p className="text-sm text-[var(--text-secondary)]">{error}</p>
                    <motion.button whileTap={{ scale: 0.96 }} onClick={loadLinks} className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-strong)] text-sm text-[var(--text-primary)] hover:bg-[var(--bg-tertiary)]">
                        <RefreshCw className="w-3.5 h-3.5" /> Retry
                    </motion.button>
                </div>
            ) : links.length === 0 ? (
                <motion.div
                    initial={{ opacity: 0, y: 16 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="flex flex-col items-center justify-center py-20 text-center"
                >
                    <div className="w-20 h-20 rounded-2xl flex items-center justify-center mb-6"
                        style={{ background: 'linear-gradient(135deg, #5865F2 0%, #7C3AED 100%)', boxShadow: '0 8px 32px #5865F240' }}>
                        <Link2 className="w-10 h-10 text-white" />
                    </div>
                    <h2 className="text-xl font-bold text-[var(--text-primary)] mb-2">No links yet</h2>
                    <p className="text-sm text-[var(--text-secondary)] max-w-sm mb-1">
                        Start building your personal link hub. Add links to websites, docs, tools — anything you want to share or track.
                    </p>
                    <ul className="text-xs text-[var(--text-muted)] space-y-1 mb-8 mt-4">
                        {['Track clicks and engagement', 'Colour-code & organise by category', 'Feature your most important links'].map(f => (
                            <li key={f} className="flex items-center gap-2 justify-center">
                                <span className="w-1.5 h-1.5 rounded-full bg-[var(--accent-primary)]" />
                                {f}
                            </li>
                        ))}
                    </ul>
                    <motion.button
                        whileTap={{ scale: 0.96 }}
                        onClick={() => { setShowForm(true); setEditId(null); setForm({ title: '', url: '', description: '', color: '#5865F2', isFeatured: false }); }}
                        className="flex items-center gap-2 px-6 py-3 rounded-xl text-white font-semibold text-sm transition-all hover:opacity-90"
                        style={{ background: 'linear-gradient(135deg,#5865F2,#7C3AED)', boxShadow: '0 4px 16px #5865F240' }}
                    >
                        <Plus className="w-4 h-4" />
                        Add your first link
                    </motion.button>
                </motion.div>
            ) : (
                <div className="space-y-3">
                    <AnimatePresence>
                        {links.map((link, i) => (
                            <motion.div
                                key={link.id}
                                initial={{ opacity: 0, y: 12 }}
                                animate={{ opacity: 1, y: 0 }}
                                exit={{ opacity: 0, x: -20 }}
                                transition={{ delay: i * 0.05 }}
                                className={cn(
                                    'group flex items-center gap-4 p-4 rounded-xl',
                                    'bg-[var(--bg-secondary)] border border-[var(--border-strong)]',
                                    'hover:border-[var(--accent-primary)] transition-all'
                                )}
                            >
                                {/* Color swatch */}
                                <div className="w-3 h-12 rounded-full flex-shrink-0" style={{ backgroundColor: link.color || '#5865F2' }} />

                                {/* Content */}
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2">
                                        <span className="font-semibold text-[var(--text-primary)] truncate">{link.title}</span>
                                        {link.isFeatured && <Star className="w-3.5 h-3.5 text-[var(--accent-warning)] flex-shrink-0" />}
                                        {!link.isActive && <span className="text-xs px-2 py-0.5 rounded bg-red-500/20 text-red-400">Inactive</span>}
                                    </div>
                                    <p className="text-xs text-[var(--text-muted)] truncate mt-0.5">{link.url}</p>
                                    {link.description && <p className="text-xs text-[var(--text-secondary)] mt-0.5 truncate">{link.description}</p>}
                                </div>

                                {/* Stats */}
                                <div className="text-center hidden sm:block">
                                    <div className="text-sm font-semibold text-[var(--text-primary)]">{link.clickCount}</div>
                                    <div className="text-xs text-[var(--text-muted)]">clicks</div>
                                </div>

                                {/* Actions */}
                                <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <button aria-label={copied === link.url ? 'Copied!' : 'Copy URL'} onClick={() => handleCopy(link.url)} className="p-1.5 rounded-lg hover:bg-[var(--bg-primary)] text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-all">
                                        {copied === link.url ? <Check className="w-4 h-4 text-green-400" /> : <Copy className="w-4 h-4" />}
                                    </button>

                                    <a
                                        href={link.url}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        aria-label={`Open ${link.title} in new tab`}
                                        onClick={() => handleExternalClick(link.id)}
                                        className="p-1.5 rounded-lg hover:bg-[var(--bg-primary)] text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-all"
                                    >

                                        <ExternalLink className="w-4 h-4" />
                                    </a>
                                    <button aria-label={`Edit ${link.title}`} onClick={() => handleEdit(link)} className="p-1.5 rounded-lg hover:bg-[var(--bg-primary)] text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-all">

                                        <Pencil className="w-4 h-4" />
                                    </button>
                                    <button aria-label={`Delete ${link.title}`} onClick={() => handleDelete(link.id)} className="p-1.5 rounded-lg hover:bg-red-500/20 text-[var(--text-muted)] hover:text-red-400 transition-all">

                                        <Trash2 className="w-4 h-4" />
                                    </button>
                                </div>
                            </motion.div>
                        ))}
                    </AnimatePresence>
                </div>
            )}
        </div>
    );
}
