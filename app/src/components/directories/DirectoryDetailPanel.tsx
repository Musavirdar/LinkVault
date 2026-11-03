import { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Upload,
  File,
  Image,
  FileText,
  Archive,
  Download,
  Trash2,
  Loader2,
  FolderOpen,
  MessageSquare,
  Files,
  Plus,
  Eye,
} from 'lucide-react';
import { FilePreviewModal } from '@/components/files/FilePreviewModal';
import { directoriesApi, resourcesApi, fileUploadApi, filesApi } from '@/api/client';
import { DirectoryChatPanel } from '@/components/chat/DirectoryChatPanel';
import { useStore } from '@/store/useStore';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';

interface DirectoryResource {
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
}

interface Props {
  directoryId: string;
  directoryName: string;
  directoryColor?: string;
  onFilesChanged?: () => void;
}

function iconForType(type: string, fileType?: string) {
  const ft = (fileType ?? '').toLowerCase();
  if (ft.includes('image') || type === 'image') return <Image className="w-4 h-4 text-blue-400" />;
  if (ft.includes('pdf') || ft.includes('word') || ft.includes('doc') || type === 'document')
    return <FileText className="w-4 h-4 text-orange-400" />;
  if (ft.includes('zip') || ft.includes('archive') || type === 'archive')
    return <Archive className="w-4 h-4 text-purple-400" />;
  return <File className="w-4 h-4 text-[var(--text-muted)]" />;
}

function formatBytes(bytes?: number | null) {
  if (!bytes) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function DirectoryDetailPanel({
  directoryId,
  directoryName,
  directoryColor = '#0071E3',
  onFilesChanged,
}: Props) {
  const { permissions } = useStore();
  const [tab, setTab] = useState<'files' | 'chat'>('files');
  const [files, setFiles] = useState<DirectoryResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewFile, setPreviewFile] = useState<DirectoryResource | null>(null);

  const loadFiles = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await directoriesApi.getResources(directoryId);
      if (res.data && res.data.length > 0) {
        setFiles(res.data);
      } else {
        const allRes = await resourcesApi.getAll(directoryId);
        if (allRes.data) {
          setFiles(allRes.data);
        } else if (res.data) {
          setFiles(res.data);
        } else {
          setError(res.error || allRes.error || 'Failed to load files');
        }
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error loading files');
    } finally {
      setLoading(false);
    }
  }, [directoryId]);

  useEffect(() => {
    loadFiles();
  }, [loadFiles]);

  const handleUploadFiles = async (fileList: FileList | null) => {
    if (!fileList || fileList.length === 0) return;
    setUploading(true);
    setError(null);
    try {
      for (const file of Array.from(fileList)) {
        const res = await fileUploadApi.upload(file, directoryId);
        if (res.error) {
          setError(res.error);
        } else if (res.data) {
          const uploaded = res.data;
          setFiles((prev) => [
            {
              id: uploaded.id,
              name: uploaded.name,
              type: uploaded.mimeType?.startsWith('image/') ? 'image' : 'file',
              url: uploaded.url,
              sizeFormatted: formatBytes(uploaded.size),
              viewCount: 0,
              clickCount: 0,
              isPublic: false,
              createdAt: uploaded.createdAt || new Date().toISOString(),
            },
            ...prev.filter((f) => f.id !== uploaded.id),
          ]);
        }
      }
      await loadFiles();
      onFilesChanged?.();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await resourcesApi.delete(id);
      setFiles((prev) => prev.filter((f) => f.id !== id));
      onFilesChanged?.();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Delete failed');
    }
  };

  const handleDownload = async (file: DirectoryResource) => {
    await filesApi.downloadWithTicket(file.id, file.name);
  };

  return (
    <div className="rounded-2xl bg-card border border-black/[0.08] dark:border-white/12 overflow-hidden shadow-lg mt-3 mb-4">
      {/* Header with Segmented Control */}
      <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-3.5 bg-black/[0.02] dark:bg-white/[0.03] border-b border-black/[0.06] dark:border-white/10">
        <div className="flex items-center gap-2">
          <FolderOpen className="w-5 h-5" style={{ color: directoryColor }} />
          <span className="font-semibold text-sm text-foreground">{directoryName}</span>
          <span className="text-xs text-muted-foreground">({files.length} items)</span>
        </div>

        {/* Tab Switcher */}
        <div className="flex items-center p-0.5 rounded-xl bg-black/[0.05] dark:bg-white/10 border border-black/[0.04] dark:border-white/05">
          <button
            onClick={() => setTab('files')}
            className={cn(
              'flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-medium transition-all',
              tab === 'files'
                ? 'bg-white dark:bg-neutral-800 text-foreground shadow-xs'
                : 'text-muted-foreground hover:text-foreground'
            )}
          >
            <Files className="w-3.5 h-3.5" />
            <span>Files ({files.length})</span>
          </button>
          <button
            onClick={() => setTab('chat')}
            className={cn(
              'flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-medium transition-all',
              tab === 'chat'
                ? 'bg-white dark:bg-neutral-800 text-foreground shadow-xs'
                : 'text-muted-foreground hover:text-foreground'
            )}
          >
            <MessageSquare className="w-3.5 h-3.5" />
            <span>Team Chat</span>
          </button>
        </div>
      </div>

      {/* Tab Content */}
      <div className="p-4 sm:p-5">
        {tab === 'files' ? (
          <div className="space-y-4">
            {/* Inline Upload Drop Zone */}
            {permissions.canUpload && (
              <div
                onDragOver={(e) => {
                  e.preventDefault();
                  setDragging(true);
                }}
                onDragLeave={() => setDragging(false)}
                onDrop={(e) => {
                  e.preventDefault();
                  setDragging(false);
                  handleUploadFiles(e.dataTransfer.files);
                }}
                className={cn(
                  'relative rounded-xl border-2 border-dashed p-4 flex items-center justify-between gap-4 transition-all',
                  dragging
                    ? 'border-[#0071E3] bg-[#0071E3]/5'
                    : 'border-black/[0.08] dark:border-white/12 hover:border-[#0071E3]/50 bg-black/[0.01] dark:bg-white/[0.02]'
                )}
              >
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-lg flex items-center justify-center bg-[#0071E3]/10 text-[#0071E3]">
                    {uploading ? <Loader2 className="w-5 h-5 animate-spin" /> : <Upload className="w-5 h-5" />}
                  </div>
                  <div>
                    <div className="text-xs font-semibold text-foreground">
                      {uploading ? 'Uploading to this folder…' : `Upload file to ${directoryName}`}
                    </div>
                    <div className="text-[11px] text-muted-foreground">
                      Drag & drop files here or click the button to browse
                    </div>
                  </div>
                </div>

                <label className="flex-shrink-0 cursor-pointer">
                  <input
                    type="file"
                    multiple
                    className="hidden"
                    disabled={uploading}
                    onChange={(e) => {
                      handleUploadFiles(e.target.files);
                      e.target.value = '';
                    }}
                  />
                  <span className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-medium shadow-xs transition-colors">
                    <Plus className="w-3.5 h-3.5" />
                    <span>Select Files</span>
                  </span>
                </label>
              </div>
            )}

            {error && (
              <div className="text-xs text-red-500 bg-red-500/10 p-2.5 rounded-lg border border-red-500/20">
                {error}
              </div>
            )}

            {/* Files List */}
            {loading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="w-5 h-5 animate-spin text-[#0071E3]" />
              </div>
            ) : files.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground text-xs">
                No files in this directory yet. Upload one above!
              </div>
            ) : (
              <div className="divide-y divide-black/[0.04] dark:divide-white/05 rounded-xl border border-black/[0.06] dark:border-white/10 overflow-hidden bg-background">
                {files.map((file) => (
                  <div
                    key={file.id}
                    className="flex items-center justify-between gap-3 px-4 py-2.5 hover:bg-black/[0.02] dark:hover:bg-white/[0.03] transition-colors"
                  >
                    <div 
                      onClick={() => setPreviewFile(file)}
                      className="flex items-center gap-2.5 min-w-0 cursor-pointer group/title"
                    >
                      {iconForType(file.type, file.fileType)}
                      <span className="text-xs font-medium text-foreground truncate group-hover/title:text-[#0071E3] transition-colors">{file.name}</span>
                    </div>

                    <div className="flex items-center gap-3 text-xs text-muted-foreground flex-shrink-0">
                      <span>{file.sizeFormatted || '—'}</span>
                      <span className="hidden sm:inline">
                        {format(new Date(file.createdAt), 'MMM d, yyyy')}
                      </span>

                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => setPreviewFile(file)}
                          className="p-1 rounded-md hover:bg-black/[0.05] dark:hover:bg-white/10 text-muted-foreground hover:text-[#0071E3] transition-colors"
                          title="Open / Preview file"
                        >
                          <Eye className="w-3.5 h-3.5" />
                        </button>
                        <button
                          onClick={() => handleDownload(file)}
                          className="p-1 rounded-md hover:bg-black/[0.05] dark:hover:bg-white/10 text-muted-foreground hover:text-foreground transition-colors"
                          title="Download file"
                        >
                          <Download className="w-3.5 h-3.5" />
                        </button>
                        {permissions.canDelete && (
                          <button
                            onClick={() => handleDelete(file.id)}
                            className="p-1 rounded-md hover:bg-red-500/15 text-muted-foreground hover:text-red-400 transition-colors"
                            title="Delete file"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <DirectoryChatPanel directoryId={directoryId} directoryName={directoryName} />
        )}
      </div>

      <FilePreviewModal
        isOpen={!!previewFile}
        onClose={() => setPreviewFile(null)}
        file={previewFile ? { ...previewFile, directoryName } : null}
        onDownload={() => previewFile && handleDownload(previewFile)}
      />
    </div>
  );
}

