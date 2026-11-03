import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Folder, FolderOpen, Plus, Trash2, Loader2, ChevronRight, Files, AlertCircle } from 'lucide-react';
import { directoriesApi } from '@/api/client';
import { useStore } from '@/store/useStore';
import { DirectoryDetailPanel } from '@/components/directories/DirectoryDetailPanel';
import { cn } from '@/lib/utils';
import { appleSpring } from '@/lib/animations';

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

const DIR_COLORS = ['#0071E3', '#34C759', '#FF9500', '#FF3B30', '#AF52DE', '#5856D6', '#0A84FF'];

export function DirectoriesPage() {
  const { permissions } = useStore();
  const [dirs, setDirs] = useState<Directory[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', description: '', color: '#0071E3', isPublic: false });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [selected, setSelected] = useState<string | null>(null);

  const load = async (background = false) => {
    if (!background) setLoading(true);
    setError(null);
    const res = await directoriesApi.getAll();
    if (res.data) setDirs(res.data);
    else if (res.error && !background) setError(res.error);
    if (!background) setLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

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
    setForm({ name: '', description: '', color: '#0071E3', isPublic: false });
    load();
  };

  const handleDelete = async (id: string) => {
    await directoriesApi.delete(id);
    if (selected === id) setSelected(null);
    load();
  };

  return (
    <div className="max-w-6xl mx-auto py-6 px-4 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between pb-2 border-b border-black/[0.06] dark:border-white/10">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Directories</h1>
          <p className="text-xs text-muted-foreground mt-0.5">{dirs.length} vaults organized</p>
        </div>
        {permissions.canUpload && (
          <motion.button
            whileTap={{ scale: 0.96 }}
            onClick={() => setShowForm(!showForm)}
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-sm transition-all"
          >
            <Plus className="w-4 h-4" /> New Directory
          </motion.button>
        )}
      </div>

      {/* Create Form */}
      <AnimatePresence>
        {showForm && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="overflow-hidden mb-6"
          >
            <div className="rounded-2xl p-6 bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 shadow-lg">
              <h3 className="font-semibold text-sm text-foreground mb-4">Create New Directory</h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-4">
                <input
                  value={form.name}
                  onChange={(e) => {
                    setForm((p) => ({ ...p, name: e.target.value }));
                    setFormError('');
                  }}
                  placeholder="Directory name *"
                  className="px-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3]"
                />
                <input
                  value={form.description}
                  onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
                  placeholder="Description (optional)"
                  className="px-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3]"
                />
              </div>

              {/* Color picker */}
              <div className="flex items-center gap-2 mb-4">
                <span className="text-xs text-muted-foreground mr-1">Accent:</span>
                {DIR_COLORS.map((c) => (
                  <button
                    key={c}
                    onClick={() => setForm((p) => ({ ...p, color: c }))}
                    className={cn(
                      'w-6 h-6 rounded-full transition-transform',
                      form.color === c && 'scale-125 ring-2 ring-offset-2 ring-[#0071E3]'
                    )}
                    style={{ backgroundColor: c }}
                  />
                ))}
              </div>

              {formError && <p className="text-xs text-red-500 mb-3">{formError}</p>}

              <div className="flex gap-2 justify-end">
                <button
                  onClick={() => setShowForm(false)}
                  className="px-3.5 py-1.5 rounded-xl text-xs font-medium text-muted-foreground hover:text-foreground bg-black/[0.04] dark:bg-white/[0.06]"
                >
                  Cancel
                </button>
                <button
                  onClick={handleCreate}
                  disabled={saving}
                  className="px-4 py-1.5 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-xs disabled:opacity-50"
                >
                  {saving ? 'Creating...' : 'Create'}
                </button>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Directory Grid */}
      {loading ? (
        <div className="flex justify-center py-16">
          <Loader2 className="w-8 h-8 animate-spin text-[#0071E3]" />
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3 text-red-500">
          <AlertCircle className="w-8 h-8" />
          <p className="text-xs font-medium">{error}</p>
        </div>
      ) : dirs.length === 0 ? (
        <div className="text-center py-16 text-muted-foreground">
          <Folder className="w-12 h-12 mx-auto mb-3 opacity-30 text-[#0071E3]" />
          <p className="text-sm">No directories yet. Create your first vault above!</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <AnimatePresence>
            {dirs.map((dir, i) => (
              <React.Fragment key={dir.id}>
                <motion.div
                  initial={{ opacity: 0, scale: 0.96 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.9 }}
                  transition={{ delay: i * 0.03, ...appleSpring }}
                  onClick={() => setSelected(selected === dir.id ? null : dir.id)}
                  className={cn(
                    'group relative cursor-pointer rounded-2xl p-5 select-none',
                    'bg-white/70 dark:bg-[#1C1C1E]/70 backdrop-blur-xl border transition-all duration-200 shadow-sm hover:shadow-md',
                    selected === dir.id
                      ? 'border-[#0071E3] ring-2 ring-[#0071E3]/20 shadow-md'
                      : 'border-black/[0.06] dark:border-white/10 hover:border-[#0071E3]/50'
                  )}
                >
                  {/* Subtle color bar */}
                  <div
                    className="absolute top-0 left-0 right-0 h-1.5 rounded-t-2xl"
                    style={{ backgroundColor: dir.color || '#0071E3' }}
                  />

                  <div className="flex items-start justify-between mt-1">
                    <div className="flex items-center gap-3 min-w-0">
                      <div
                        className="p-2.5 rounded-xl flex-shrink-0"
                        style={{ backgroundColor: `${dir.color || '#0071E3'}15` }}
                      >
                        {selected === dir.id ? (
                          <FolderOpen className="w-5 h-5" style={{ color: dir.color || '#0071E3' }} />
                        ) : (
                          <Folder className="w-5 h-5" style={{ color: dir.color || '#0071E3' }} />
                        )}
                      </div>
                      <div className="min-w-0">
                        <div className="font-semibold text-sm text-foreground truncate">{dir.name}</div>
                        {dir.description && (
                          <div className="text-xs text-muted-foreground mt-0.5 line-clamp-1">{dir.description}</div>
                        )}
                      </div>
                    </div>
                    {permissions.canDelete && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDelete(dir.id);
                        }}
                        className="opacity-0 group-hover:opacity-100 p-1.5 rounded-lg hover:bg-red-500/15 text-muted-foreground hover:text-red-500 transition-all flex-shrink-0"
                        title="Delete directory"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    )}
                  </div>

                  <div className="flex items-center justify-between mt-4 pt-3 border-t border-black/[0.04] dark:border-white/06 text-xs text-muted-foreground">
                    <div className="flex items-center gap-1.5">
                      <Files className="w-3.5 h-3.5 text-muted-foreground" />
                      <span>{dir.resourceCount} files</span>
                    </div>
                    <div className="flex items-center gap-1">
                      {dir.isPublic && (
                        <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-500">
                          Public
                        </span>
                      )}
                      <ChevronRight
                        className={cn(
                          'w-4 h-4 text-muted-foreground transition-transform',
                          selected === dir.id && 'rotate-90 text-[#0071E3]'
                        )}
                      />
                    </div>
                  </div>
                </motion.div>

                {/* Directory Content Panel — expands below selected card */}
                {selected === dir.id && (
                  <motion.div
                    key={`detail-${dir.id}`}
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    transition={{ duration: 0.2 }}
                    className="col-span-full overflow-hidden"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <DirectoryDetailPanel
                      directoryId={dir.id}
                      directoryName={dir.name}
                      directoryColor={dir.color}
                      onFilesChanged={() => load(true)}
                    />
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
