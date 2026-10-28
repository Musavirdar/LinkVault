import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Download, Lock, Eye, FileIcon, AlertCircle, Loader2, LinkIcon, Shield, Clock, X } from 'lucide-react';
import { apiClient } from '@/api/client';

interface ShareInfo {
  token: string;
  fileName: string;
  fileType?: string;
  mimeType?: string;
  sizeInBytes?: number;
  sizeFormatted?: string;
  hasPassword: boolean;
  allowPreview: boolean;
  requireEmail: boolean;
  expiresAt?: string;
  downloadsRemaining?: number;
  sharedBy?: string;
}

export function SharedFilePage() {
  const { token } = useParams<{ token: string }>();
  const [info, setInfo] = useState<ShareInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');
  const [authenticated, setAuthenticated] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [pwError, setPwError] = useState('');

  useEffect(() => {
    if (!token) return;
    document.title = 'Shared File · LinkVault';
    apiClient<ShareInfo>(`/share/${token}`, { requiresAuth: false }).then(res => {
      setLoading(false);
      if (res.error) { setError(res.error); return; }
      if (res.data) {
        setInfo(res.data);
        if (!res.data.hasPassword) setAuthenticated(true);
      }
    });
  }, [token]);

  const handleVerifyPassword = async () => {
    if (!token || !password) return;
    setPwError('');
    const res = await apiClient<{ valid: boolean }>(`/share/${token}/verify`, {
      method: 'POST', body: { password }, requiresAuth: false,
    });
    if (res.data?.valid) {
      setAuthenticated(true);
    } else {
      setPwError('Incorrect password.');
      setPassword('');
    }
  };

  const handleDownload = async () => {
    if (!token) return;
    setDownloading(true);
    const body: Record<string, string> = {};
    if (info?.hasPassword) body.password = password;
    if (info?.requireEmail) body.email = email;

    const res = await apiClient<{ downloadUrl: string; fileName: string }>(`/share/${token}/download`, {
      method: 'POST', body, requiresAuth: false,
    });
    setDownloading(false);

    if (res.data?.downloadUrl) {
      const a = document.createElement('a');
      a.href = res.data.downloadUrl;
      a.download = res.data.fileName;
      a.click();
    }
  };

  const accent = '#6366F1';

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
        <h1 className="text-2xl font-bold text-white">Link Expired or Not Found</h1>
        <p className="text-white/50 mt-2 text-sm">{error || 'This share link is no longer available.'}</p>
      </div>
      <Link to="/" className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white" style={{ background: accent }}>
        <LinkIcon className="w-4 h-4" /> Go to LinkVault
      </Link>
    </div>
  );

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-16" style={{ background: 'radial-gradient(ellipse at 50% 0%, #1e1b4b 0%, #090B0F 60%)' }}>
      <div className="fixed inset-0 pointer-events-none" style={{ background: `radial-gradient(ellipse 60% 40% at 50% -10%, ${accent}20 0%, transparent 70%)` }} />

      <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }}
        className="relative z-10 w-full max-w-md">
        <div className="rounded-2xl p-8 text-center" style={{ background: 'rgba(26, 26, 46, 0.9)', border: '1px solid rgba(255,255,255,0.08)', backdropFilter: 'blur(20px)' }}>
          {/* File icon */}
          <div className="w-20 h-20 rounded-2xl mx-auto mb-5 flex items-center justify-center" style={{ background: `${accent}20`, border: `1px solid ${accent}30` }}>
            <FileIcon className="w-10 h-10" style={{ color: accent }} />
          </div>

          <h1 className="text-xl font-bold text-white mb-1">{info.fileName}</h1>
          <div className="flex items-center justify-center gap-3 text-xs text-white/40 mb-6">
            {info.sizeFormatted && <span>{info.sizeFormatted}</span>}
            {info.fileType && <span>• {info.fileType}</span>}
            {info.sharedBy && <span>• by {info.sharedBy}</span>}
          </div>

          {/* Security badges */}
          <div className="flex items-center justify-center gap-2 mb-6">
            <span className="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs bg-emerald-500/10 text-emerald-400">
              <Shield className="w-3 h-3" /> Secure
            </span>
            {info.expiresAt && (
              <span className="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs bg-amber-500/10 text-amber-400">
                <Clock className="w-3 h-3" /> Expires {new Date(info.expiresAt).toLocaleDateString()}
              </span>
            )}
            {info.downloadsRemaining != null && (
              <span className="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs bg-blue-500/10 text-blue-400">
                <Download className="w-3 h-3" /> {info.downloadsRemaining} left
              </span>
            )}
          </div>

          {/* Password gate */}
          {info.hasPassword && !authenticated && (
            <div className="mb-6 space-y-3">
              <div className="flex items-center gap-2 text-white/60 text-sm mb-2">
                <Lock className="w-4 h-4 text-amber-400" /> This file is password protected
              </div>
              <input type="password" placeholder="Enter password" value={password}
                onChange={e => { setPassword(e.target.value); setPwError(''); }}
                onKeyDown={e => e.key === 'Enter' && handleVerifyPassword()}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-white/10 text-white placeholder-white/30 focus:outline-none focus:border-indigo-500" />
              {pwError && <p className="text-red-400 text-xs">{pwError}</p>}
              <button onClick={handleVerifyPassword}
                className="w-full py-3 rounded-xl font-semibold text-white bg-indigo-600 hover:bg-indigo-500 transition-all">
                Unlock
              </button>
            </div>
          )}

          {/* Email gate */}
          {authenticated && info.requireEmail && (
            <div className="mb-6">
              <input type="email" placeholder="Enter your email to download" value={email}
                onChange={e => setEmail(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-white/5 border border-white/10 text-white placeholder-white/30 focus:outline-none focus:border-indigo-500" />
            </div>
          )}

          {/* Download button */}
          {authenticated && (
            <motion.button whileTap={{ scale: 0.96 }} onClick={handleDownload}
              disabled={downloading || (info.requireEmail && !email)}
              className="w-full py-3.5 rounded-xl font-semibold text-white transition-all disabled:opacity-40"
              style={{ background: `linear-gradient(135deg, ${accent}, #8B5CF6)`, boxShadow: `0 4px 20px ${accent}40` }}>
              {downloading ? <Loader2 className="w-5 h-5 animate-spin mx-auto" /> : (
                <span className="flex items-center justify-center gap-2"><Download className="w-5 h-5" /> Download File</span>
              )}
            </motion.button>
          )}

          {/* Preview */}
          {authenticated && info.allowPreview && info.mimeType?.startsWith('image/') && (
            <div className="mt-6 rounded-xl overflow-hidden border border-white/10">
              <img src={`/api/share/${token}/download`} alt="Preview" className="w-full" />
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="text-center mt-6">
          <Link to="/" className="inline-flex items-center gap-2 text-xs px-4 py-2 rounded-full" style={{ background: 'rgba(255,255,255,0.06)', color: 'rgba(255,255,255,0.4)' }}>
            <LinkIcon className="w-3 h-3" /> Powered by LinkVault
          </Link>
        </div>
      </motion.div>
    </div>
  );
}
