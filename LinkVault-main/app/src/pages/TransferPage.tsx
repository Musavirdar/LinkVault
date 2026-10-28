import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Download, FileIcon, AlertCircle, Loader2, LinkIcon, Clock, Package, File, Image, Film, Music, FileText, Archive } from 'lucide-react';
import { apiClient, API_BASE_URL } from '@/api/client';

interface TransferFile {
  id: string;
  fileName: string;
  mimeType?: string;
  size: string;
  sizeBytes: number;
}

interface TransferInfo {
  token: string;
  title: string;
  fileCount: number;
  totalSize: string;
  totalSizeBytes: number;
  downloadCount: number;
  expiresAt: string;
  createdAt: string;
  createdBy: string;
  files: TransferFile[];
}

function getFileIcon(mimeType?: string) {
  if (!mimeType) return File;
  if (mimeType.startsWith('image/')) return Image;
  if (mimeType.startsWith('video/')) return Film;
  if (mimeType.startsWith('audio/')) return Music;
  if (mimeType.includes('pdf') || mimeType.includes('document') || mimeType.includes('text')) return FileText;
  if (mimeType.includes('zip') || mimeType.includes('archive') || mimeType.includes('tar')) return Archive;
  return File;
}

export function TransferPage() {
  const { token } = useParams<{ token: string }>();
  const [info, setInfo] = useState<TransferInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    document.title = 'File Transfer · LinkVault';
    apiClient<TransferInfo>(`/transfer/${token}`, { requiresAuth: false }).then(res => {
      setLoading(false);
      if (res.error) { setError(res.error); return; }
      if (res.data) setInfo(res.data);
    });
  }, [token]);

  const handleDownload = (fileId: string, fileName: string) => {
    const a = document.createElement('a');
    a.href = `${API_BASE_URL}/api/transfer/${token}/download/${fileId}`;
    a.download = fileName;
    a.click();
  };

  const accent = '#6366F1';

  const timeLeft = info?.expiresAt
    ? Math.max(0, Math.ceil((new Date(info.expiresAt).getTime() - Date.now()) / (1000 * 60 * 60)))
    : null;

  if (loading) return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: '#090B0F' }}>
      <Loader2 className="w-8 h-8 animate-spin" style={{ color: accent }} />
    </div>
  );

  if (error || !info) return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 px-4" style={{ background: '#090B0F' }}>
      <div className="w-16 h-16 rounded-2xl flex items-center justify-center" style={{ background: `${accent}20` }}>
        <AlertCircle className="w-8 h-8" style={{ color: accent }} />
      </div>
      <div className="text-center">
        <h1 className="text-2xl font-bold text-white">Transfer Not Found</h1>
        <p className="text-white/50 mt-2 text-sm">{error || 'This transfer has expired or does not exist.'}</p>
      </div>
      <Link to="/" className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white" style={{ background: accent }}>
        <LinkIcon className="w-4 h-4" /> Go to LinkVault
      </Link>
    </div>
  );

  return (
    <div className="min-h-screen px-4 py-16" style={{ background: 'radial-gradient(ellipse at 50% 0%, #1e1b4b 0%, #090B0F 60%)' }}>
      <div className="fixed inset-0 pointer-events-none" style={{ background: `radial-gradient(ellipse 60% 40% at 50% -10%, ${accent}20 0%, transparent 70%)` }} />

      <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }}
        className="relative z-10 max-w-lg mx-auto">
        {/* Header card */}
        <div className="rounded-2xl p-8 text-center mb-6" style={{ background: 'rgba(26, 26, 46, 0.9)', border: '1px solid rgba(255,255,255,0.08)', backdropFilter: 'blur(20px)' }}>
          <div className="w-16 h-16 rounded-2xl mx-auto mb-4 flex items-center justify-center" style={{ background: `${accent}20`, border: `1px solid ${accent}30` }}>
            <Package className="w-8 h-8" style={{ color: accent }} />
          </div>

          <h1 className="text-xl font-bold text-white mb-1">{info.title}</h1>
          <p className="text-sm text-white/40 mb-4">Shared by {info.createdBy}</p>

          <div className="flex items-center justify-center gap-4 text-xs text-white/50">
            <span>{info.fileCount} file{info.fileCount !== 1 ? 's' : ''}</span>
            <span>•</span>
            <span>{info.totalSize}</span>
            {timeLeft != null && (
              <>
                <span>•</span>
                <span className="flex items-center gap-1 text-amber-400">
                  <Clock className="w-3 h-3" />
                  {timeLeft > 24 ? `${Math.ceil(timeLeft / 24)}d` : `${timeLeft}h`} left
                </span>
              </>
            )}
          </div>
        </div>

        {/* Files list */}
        <div className="rounded-2xl overflow-hidden" style={{ background: 'rgba(26, 26, 46, 0.9)', border: '1px solid rgba(255,255,255,0.08)' }}>
          <div className="px-5 py-3" style={{ borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <h2 className="text-sm font-semibold text-white/80">Files</h2>
          </div>

          {info.files.map((file, i) => {
            const Icon = getFileIcon(file.mimeType);
            return (
              <motion.div key={file.id}
                initial={{ opacity: 0, x: -10 }} animate={{ opacity: 1, x: 0 }}
                transition={{ delay: i * 0.06 }}
                className="flex items-center gap-4 px-5 py-3.5 hover:bg-white/5 transition-colors"
                style={{ borderBottom: i < info.files.length - 1 ? '1px solid rgba(255,255,255,0.04)' : undefined }}>
                <div className="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0" style={{ background: `${accent}15` }}>
                  <Icon className="w-5 h-5" style={{ color: accent }} />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-white font-medium truncate">{file.fileName}</p>
                  <p className="text-xs text-white/40">{file.size}</p>
                </div>
                <motion.button whileTap={{ scale: 0.9 }} onClick={() => handleDownload(file.id, file.fileName)}
                  className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0 transition-all hover:bg-white/10"
                  style={{ color: accent }}>
                  <Download className="w-4 h-4" />
                </motion.button>
              </motion.div>
            );
          })}
        </div>

        {/* Footer */}
        <div className="text-center mt-8">
          <Link to="/" className="inline-flex items-center gap-2 text-xs px-4 py-2 rounded-full"
            style={{ background: 'rgba(255,255,255,0.06)', color: 'rgba(255,255,255,0.4)', border: '1px solid rgba(255,255,255,0.08)' }}>
            <LinkIcon className="w-3 h-3" /> Powered by LinkVault
          </Link>
        </div>
      </motion.div>
    </div>
  );
}
