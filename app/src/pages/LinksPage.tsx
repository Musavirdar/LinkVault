import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Plus,
  Trash2,
  ExternalLink,
  Pencil,
  Star,
  Link2,
  Loader2,
  Copy,
  Check,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { linksApi } from '@/api/client';
import { cn } from '@/lib/utils';
import { appleSpring } from '@/lib/animations';

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
  const navigate = useNavigate();
  const [links, setLinks] = useState<Link[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [copied, setCopied] = useState<string | null>(null);
  const [form, setForm] = useState({
    title: '',
    url: '',
    description: '',
    color: '#0071E3',
    isFeatured: false,
  });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const setField = (key: string, value: string | boolean) => {
    setForm((p) => ({ ...p, [key]: value }));
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

  useEffect(() => {
    loadLinks();
  }, []);

  const handleSave = async () => {
    if (!form.title.trim()) {
      setFormError('Link title is required.');
      return;
    }
    if (!form.url.trim()) {
      setFormError('URL is required.');
      return;
    }
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
    setForm({ title: '', url: '', description: '', color: '#0071E3', isFeatured: false });
    loadLinks();
  };

  const handleDelete = async (id: string) => {
    await linksApi.delete(id);
    loadLinks();
  };

  const handleEdit = (link: Link) => {
    setForm({
      title: link.title,
      url: link.url,
      description: link.description || '',
      color: link.color || '#0071E3',
      isFeatured: link.isFeatured,
    });
    setEditId(link.id);
    setShowForm(true);
  };

  const handleCopy = (url: string) => {
    navigator.clipboard.writeText(url);
    setCopied(url);
    setTimeout(() => setCopied(null), 2000);
  };

  const handleExternalClick = (id: string) => {
    setLinks((prev) =>
      prev.map((l) => (l.id === id ? { ...l, clickCount: l.clickCount + 1 } : l))
    );
    linksApi.trackClick(id);
  };

  return (
    <div className="max-w-5xl mx-auto py-6 px-4 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between pb-2 border-b border-black/[0.06] dark:border-white/10">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">My Links</h1>
          <p className="text-xs text-muted-foreground mt-0.5">{links.length} links saved</p>
        </div>
        <motion.button
          whileTap={{ scale: 0.96 }}
          onClick={() => {
            setShowForm(!showForm);
            setEditId(null);
            setForm({ title: '', url: '', description: '', color: '#0071E3', isFeatured: false });
          }}
          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-sm transition-all"
        >
          <Plus className="w-4 h-4" /> Add Link
        </motion.button>
      </div>

      {/* Add / Edit Form */}
      <AnimatePresence>
        {showForm && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="overflow-hidden"
          >
            <div className="rounded-2xl p-6 bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 shadow-lg space-y-4">
              <h3 className="font-semibold text-sm text-foreground">
                {editId ? 'Edit Link' : 'Add New Link'}
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  value={form.title}
                  onChange={(e) => setField('title', e.target.value)}
                  placeholder="Title *"
                  className="px-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3]"
                />
                <input
                  value={form.url}
                  onChange={(e) => setField('url', e.target.value)}
                  placeholder="URL (https://...) *"
                  className="px-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3]"
                />
              </div>

              <input
                value={form.description}
                onChange={(e) => setField('description', e.target.value)}
                placeholder="Description (optional)"
                className="w-full px-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3]"
              />

              <div className="flex items-center gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.isFeatured}
                    onChange={(e) => setField('isFeatured', e.target.checked)}
                    className="rounded accent-[#0071E3]"
                  />
                  <span className="text-xs text-muted-foreground">Feature on profile</span>
                </label>
              </div>

              {formError && <p className="text-xs text-red-500">{formError}</p>}

              <div className="flex gap-2 justify-end">
                <button
                  onClick={() => {
                    setShowForm(false);
                    setEditId(null);
                  }}
                  className="px-3.5 py-1.5 rounded-xl text-xs font-medium text-muted-foreground hover:text-foreground bg-black/[0.04] dark:bg-white/[0.06]"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSave}
                  disabled={saving}
                  className="px-4 py-1.5 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-xs disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editId ? 'Save Changes' : 'Create Link'}
                </button>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Links List */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="w-8 h-8 animate-spin text-[#0071E3]" />
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center gap-3">
          <AlertCircle className="w-8 h-8 text-red-400" />
          <p className="text-xs text-muted-foreground">{error}</p>
          {error.toLowerCase().includes('sign in') ? (
            <button
              onClick={() => navigate('/login')}
              className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#0071E3] text-white text-xs font-semibold hover:bg-[#0077ED]"
            >
              Sign In
            </button>
          ) : (
            <button
              onClick={loadLinks}
              className="flex items-center gap-2 px-4 py-2 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] border border-black/[0.06] dark:border-white/10 text-xs font-medium"
            >
              <RefreshCw className="w-3.5 h-3.5" /> Retry
            </button>
          )}
        </div>
      ) : links.length === 0 ? (
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-col items-center justify-center py-20 text-center"
        >
          <div className="w-16 h-16 rounded-2xl bg-[#0071E3]/10 text-[#0071E3] flex items-center justify-center mb-4 shadow-sm">
            <Link2 className="w-8 h-8" />
          </div>
          <h2 className="text-base font-semibold text-foreground mb-1">No links yet</h2>
          <p className="text-xs text-muted-foreground max-w-sm mb-6">
            Start building your link collection. Add bookmarks, web pages, repositories, or documents.
          </p>
          <button
            onClick={() => {
              setShowForm(true);
              setEditId(null);
            }}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-md transition-all active:scale-95"
          >
            <Plus className="w-4 h-4" /> Add your first link
          </button>
        </motion.div>
      ) : (
        <div className="space-y-2.5">
          <AnimatePresence>
            {links.map((link, i) => (
              <motion.div
                key={link.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ delay: i * 0.03, ...appleSpring }}
                className={cn(
                  'group flex items-center gap-4 p-4 rounded-2xl select-none',
                  'bg-white/70 dark:bg-[#1C1C1E]/70 backdrop-blur-xl border border-black/[0.06] dark:border-white/10',
                  'hover:border-[#0071E3]/40 shadow-xs hover:shadow-sm transition-all duration-150'
                )}
              >
                {/* Accent Swatch */}
                <div
                  className="w-2.5 h-10 rounded-full flex-shrink-0"
                  style={{ backgroundColor: link.color || '#0071E3' }}
                />

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-xs text-foreground truncate">{link.title}</span>
                    {link.isFeatured && <Star className="w-3.5 h-3.5 text-[#FF9500] fill-[#FF9500] flex-shrink-0" />}
                    {!link.isActive && (
                      <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-red-500/10 text-red-500">
                        Inactive
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground truncate mt-0.5 font-mono">{link.url}</p>
                  {link.description && (
                    <p className="text-xs text-muted-foreground/80 mt-0.5 truncate">{link.description}</p>
                  )}
                </div>

                {/* Stats */}
                <div className="text-center hidden sm:block flex-shrink-0 px-2">
                  <div className="text-xs font-semibold text-foreground">{link.clickCount}</div>
                  <div className="text-[10px] text-muted-foreground">clicks</div>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-1 flex-shrink-0">
                  <button
                    onClick={() => handleCopy(link.url)}
                    className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Copy URL"
                  >
                    {copied === link.url ? <Check className="w-3.5 h-3.5 text-green-500" /> : <Copy className="w-3.5 h-3.5" />}
                  </button>
                  <a
                    href={link.url}
                    target="_blank"
                    rel="noreferrer"
                    onClick={() => handleExternalClick(link.id)}
                    className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Open Link"
                  >
                    <ExternalLink className="w-3.5 h-3.5" />
                  </a>
                  <button
                    onClick={() => handleEdit(link)}
                    className="p-1.5 rounded-lg text-muted-foreground hover:text-[#0071E3] hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Edit"
                  >
                    <Pencil className="w-3.5 h-3.5" />
                  </button>
                  <button
                    onClick={() => handleDelete(link.id)}
                    className="p-1.5 rounded-lg text-muted-foreground hover:text-red-500 hover:bg-red-500/10 transition-colors"
                    title="Delete"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
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
