import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  X,
  Download,
  ExternalLink,
  ZoomIn,
  ZoomOut,
  RotateCw,
  Copy,
  Check,
  FileText,
  Image as ImageIcon,
  Film,
  Music,
  Archive,
  File,
  Loader2,
  Folder,
  AlertCircle,
} from 'lucide-react';
import { filesApi } from '@/api/client';
import { detectPreviewCategory, getLanguageFromFilename, type FileMetadata } from '@/lib/filePreviewUtils';
import { modalSpring } from '@/lib/animations';

interface FilePreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  file: FileMetadata | null;
  onDownload?: () => void;
}

export function FilePreviewModal({ isOpen, onClose, file, onDownload }: FilePreviewModalProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [textContent, setTextContent] = useState<string | null>(null);
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [copied, setCopied] = useState(false);

  // Reset state and load file data when file changes
  useEffect(() => {
    if (!isOpen || !file) {
      if (blobUrl) URL.revokeObjectURL(blobUrl);
      setBlobUrl(null);
      setTextContent(null);
      setLoading(false);
      setError(null);
      setZoom(1);
      setRotation(0);
      setCopied(false);
      return;
    }

    let isMounted = true;
    setLoading(true);
    setError(null);
    setZoom(1);
    setRotation(0);
    setCopied(false);

    const category = detectPreviewCategory(file.name, file.fileType);

    async function loadContent() {
      try {
        if (category === 'text') {
          const text = await filesApi.getPreviewText(file!.id);
          if (!isMounted) return;
          if (text !== null) {
            setTextContent(text);
          } else {
            setError('Could not read text content.');
          }
        } else if (category === 'image' || category === 'pdf' || category === 'media') {
          const res = await filesApi.getPreviewBlob(file!.id);
          if (!isMounted) return;
          if (res?.blob) {
            const url = URL.createObjectURL(res.blob);
            setBlobUrl(url);
          } else {
            setError('Could not load media preview.');
          }
        }
      } catch (err) {
        if (!isMounted) return;
        setError(err instanceof Error ? err.message : 'Failed to preview file.');
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    loadContent();

    return () => {
      isMounted = false;
      if (blobUrl) URL.revokeObjectURL(blobUrl);
    };
  }, [isOpen, file?.id]);

  if (!isOpen || !file) return null;

  const category = detectPreviewCategory(file.name, file.fileType);

  const handleCopyText = async () => {
    if (!textContent) return;
    await navigator.clipboard.writeText(textContent);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    if (onDownload) {
      onDownload();
    } else {
      filesApi.downloadWithTicket(file.id, file.name);
    }
  };

  const handleOpenInNewTab = async () => {
    const url = await filesApi.getDownloadUrl(file.id);
    window.open(url, '_blank');
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-6 overflow-hidden">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="fixed inset-0 bg-black/60 dark:bg-black/80 backdrop-blur-md"
        />

        {/* Modal Window */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 15 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.96, y: 10 }}
          transition={modalSpring}
          className="relative w-full max-w-5xl h-[88vh] flex flex-col bg-white/95 dark:bg-[#1C1C1E]/95 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 rounded-2xl shadow-2xl overflow-hidden z-10"
        >
          {/* Top Bar */}
          <div className="flex items-center justify-between px-5 py-3 border-b border-black/[0.06] dark:border-white/10 bg-black/[0.02] dark:bg-white/[0.02] select-none flex-shrink-0">
            {/* File Info */}
            <div className="flex items-center gap-3 min-w-0 pr-4">
              <div className="p-2 rounded-xl bg-black/[0.04] dark:bg-white/[0.08] text-foreground flex-shrink-0">
                {category === 'image' && <ImageIcon className="w-5 h-5 text-[#0071E3]" />}
                {category === 'pdf' && <FileText className="w-5 h-5 text-[#FF9500]" />}
                {category === 'text' && <FileText className="w-5 h-5 text-[#34C759]" />}
                {category === 'media' && <Film className="w-5 h-5 text-[#AF52DE]" />}
                {category === 'binary' && <Archive className="w-5 h-5 text-[#5856D6]" />}
              </div>
              <div className="flex flex-col min-w-0">
                <span className="text-sm font-semibold text-foreground truncate" title={file.name}>
                  {file.name}
                </span>
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span>{file.sizeFormatted || '—'}</span>
                  {category === 'text' && (
                    <>
                      <span>•</span>
                      <span>{getLanguageFromFilename(file.name)}</span>
                    </>
                  )}
                  {file.directoryName && (
                    <>
                      <span>•</span>
                      <span className="inline-flex items-center gap-1 text-[#0071E3]">
                        <Folder className="w-3 h-3" />
                        {file.directoryName}
                      </span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Actions Bar */}
            <div className="flex items-center gap-1.5 flex-shrink-0">
              {category === 'image' && (
                <>
                  <button
                    onClick={() => setZoom((z) => Math.max(0.5, z - 0.25))}
                    className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Zoom Out"
                  >
                    <ZoomOut className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => setZoom((z) => Math.min(3, z + 0.25))}
                    className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Zoom In"
                  >
                    <ZoomIn className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => setRotation((r) => (r + 90) % 360)}
                    className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors"
                    title="Rotate"
                  >
                    <RotateCw className="w-4 h-4" />
                  </button>
                </>
              )}

              {category === 'text' && textContent && (
                <button
                  onClick={handleCopyText}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium bg-black/[0.05] dark:bg-white/10 text-foreground hover:bg-black/[0.08] dark:hover:bg-white/15 transition-all"
                  title="Copy file text"
                >
                  {copied ? <Check className="w-3.5 h-3.5 text-green-500" /> : <Copy className="w-3.5 h-3.5" />}
                  <span>{copied ? 'Copied' : 'Copy'}</span>
                </button>
              )}

              <button
                onClick={handleOpenInNewTab}
                className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors hidden sm:inline-flex"
                title="Open in new tab"
              >
                <ExternalLink className="w-4 h-4" />
              </button>

              <button
                onClick={handleDownload}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold bg-[#0071E3] hover:bg-[#0077ED] text-white shadow-sm transition-all active:scale-95"
                title="Download file"
              >
                <Download className="w-3.5 h-3.5" />
                <span className="hidden sm:inline">Download</span>
              </button>

              <button
                onClick={onClose}
                className="p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 transition-colors ml-1"
                aria-label="Close modal"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Main Viewer Canvas */}
          <div className="flex-1 overflow-auto bg-black/[0.015] dark:bg-black/40 flex items-center justify-center p-4">
            {loading ? (
              <div className="flex flex-col items-center gap-3">
                <Loader2 className="w-7 h-7 animate-spin text-[#0071E3]" />
                <span className="text-xs text-muted-foreground font-medium">Loading preview...</span>
              </div>
            ) : error ? (
              <div className="flex flex-col items-center max-w-sm text-center p-6 rounded-2xl bg-red-500/10 border border-red-500/20">
                <AlertCircle className="w-8 h-8 text-red-500 mb-2" />
                <h4 className="text-sm font-semibold text-foreground mb-1">Preview Unavailable</h4>
                <p className="text-xs text-muted-foreground mb-4">{error}</p>
                <button
                  onClick={handleDownload}
                  className="px-4 py-2 rounded-xl bg-[#0071E3] text-white text-xs font-medium shadow-sm hover:bg-[#0077ED] transition-colors"
                >
                  Download File Instead
                </button>
              </div>
            ) : category === 'image' && blobUrl ? (
              <div className="w-full h-full flex items-center justify-center overflow-auto p-4">
                <img
                  src={blobUrl}
                  alt={file.name}
                  style={{
                    transform: `scale(${zoom}) rotate(${rotation}deg)`,
                    transition: 'transform 0.2s cubic-bezier(0.2, 0, 0, 1)',
                  }}
                  className="max-w-full max-h-full object-contain rounded-lg shadow-lg select-none"
                />
              </div>
            ) : category === 'pdf' && blobUrl ? (
              <div className="w-full h-full rounded-xl overflow-hidden border border-black/[0.08] dark:border-white/10 shadow-inner bg-white">
                <object
                  data={blobUrl}
                  type="application/pdf"
                  className="w-full h-full"
                >
                  <iframe
                    src={blobUrl}
                    title={file.name}
                    className="w-full h-full border-0"
                  />
                </object>
              </div>
            ) : category === 'text' && textContent !== null ? (
              <div className="w-full h-full flex flex-col rounded-xl overflow-hidden border border-black/[0.06] dark:border-white/10 bg-[#FAFAFC] dark:bg-[#151517]">
                <div className="flex-1 overflow-auto p-4 font-mono text-xs leading-relaxed text-[#1D1D1F] dark:text-[#E5E5E7] whitespace-pre">
                  {textContent}
                </div>
              </div>
            ) : category === 'media' && blobUrl ? (
              <div className="flex flex-col items-center justify-center max-w-2xl w-full p-4">
                {file.name.match(/\.(mp3|wav|m4a|aac|flac)$/i) ? (
                  <div className="w-full max-w-md p-6 rounded-2xl bg-white/70 dark:bg-[#2C2C2E]/70 backdrop-blur-xl border border-black/[0.06] dark:border-white/10 shadow-lg flex flex-col items-center gap-4">
                    <Music className="w-16 h-16 text-[#AF52DE]" />
                    <span className="text-sm font-semibold text-foreground text-center truncate max-w-full">
                      {file.name}
                    </span>
                    <audio controls src={blobUrl} className="w-full mt-2" autoPlay />
                  </div>
                ) : (
                  <video
                    controls
                    src={blobUrl}
                    className="w-full max-h-[70vh] rounded-xl shadow-2xl bg-black"
                    autoPlay
                  />
                )}
              </div>
            ) : (
              /* Binary / Archive Fallback Card */
              <div className="flex flex-col items-center max-w-md w-full p-8 rounded-2xl bg-white/70 dark:bg-[#2C2C2E]/70 backdrop-blur-xl border border-black/[0.08] dark:border-white/12 shadow-xl text-center">
                <div className="w-16 h-16 rounded-2xl bg-[#0071E3]/10 text-[#0071E3] flex items-center justify-center mb-4 shadow-inner">
                  {category === 'binary' ? <Archive className="w-8 h-8" /> : <File className="w-8 h-8" />}
                </div>
                <h3 className="text-base font-semibold text-foreground mb-1 truncate max-w-full" title={file.name}>
                  {file.name}
                </h3>
                <p className="text-xs text-muted-foreground mb-6">
                  This file type cannot be previewed inline. You can download and open it on your device.
                </p>

                <div className="w-full divide-y divide-black/[0.04] dark:divide-white/[0.06] text-xs text-left mb-6 bg-black/[0.02] dark:bg-white/[0.04] rounded-xl p-3 border border-black/[0.04] dark:border-white/[0.04]">
                  <div className="flex justify-between py-1.5">
                    <span className="text-muted-foreground">Size</span>
                    <span className="font-medium text-foreground">{file.sizeFormatted || '—'}</span>
                  </div>
                  <div className="flex justify-between py-1.5">
                    <span className="text-muted-foreground">Type</span>
                    <span className="font-medium text-foreground">{file.fileType || file.name.split('.').pop()?.toUpperCase() || 'Binary'}</span>
                  </div>
                  {file.createdAt && (
                    <div className="flex justify-between py-1.5">
                      <span className="text-muted-foreground">Uploaded</span>
                      <span className="font-medium text-foreground">{new Date(file.createdAt).toLocaleDateString()}</span>
                    </div>
                  )}
                </div>

                <button
                  onClick={handleDownload}
                  className="w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-md transition-all active:scale-[0.98]"
                >
                  <Download className="w-4 h-4" />
                  <span>Download File ({file.sizeFormatted || 'Ready'})</span>
                </button>
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}

