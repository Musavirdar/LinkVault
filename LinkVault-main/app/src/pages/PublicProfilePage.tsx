import { useEffect, useState, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ExternalLink, LinkIcon, AlertCircle, Loader2, ArrowLeft, Lock, X, Share2, QrCode, Star } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { publicApi, type PublicProfileData, type PublicLink, type PublicProfileTheme } from '@/api/client';

/* ── Social icon map ── */
const SOCIAL_ICONS: Record<string, { icon: string; baseUrl: string }> = {
  twitter: { icon: '𝕏', baseUrl: 'https://x.com/' },
  instagram: { icon: '📸', baseUrl: 'https://instagram.com/' },
  github: { icon: '🐙', baseUrl: 'https://github.com/' },
  youtube: { icon: '▶️', baseUrl: 'https://youtube.com/@' },
  tiktok: { icon: '🎵', baseUrl: 'https://tiktok.com/@' },
  linkedin: { icon: '💼', baseUrl: 'https://linkedin.com/in/' },
  website: { icon: '🌐', baseUrl: '' },
  email: { icon: '✉️', baseUrl: 'mailto:' },
};

/* ── Button style CSS map ── */
function getButtonCSS(style: string, color: string, textColor: string, hoverColor?: string) {
  const base = { color: textColor, transition: 'all 0.2s ease' };
  switch (style) {
    case 'Sharp': return { ...base, background: color, borderRadius: '4px' };
    case 'Outline': return { ...base, background: 'transparent', border: `2px solid ${color}`, borderRadius: '12px' };
    case 'Shadow': return { ...base, background: color, borderRadius: '12px', boxShadow: `0 4px 14px ${color}50` };
    case 'Pill': return { ...base, background: color, borderRadius: '9999px' };
    case 'Glassmorphism': return { ...base, background: `${color}20`, backdropFilter: 'blur(12px)', border: `1px solid ${color}30`, borderRadius: '16px' };
    default: return { ...base, background: `${color}15`, border: `1px solid ${color}30`, borderRadius: '16px' };
  }
}

/* ── Background renderer ── */
function getBackgroundCSS(theme?: PublicProfileTheme) {
  if (!theme) return { background: 'radial-gradient(ellipse at 50% 0%, #1e1b4b 0%, #090B0F 60%)' };
  switch (theme.backgroundType) {
    case 'Solid': return { background: theme.backgroundValue };
    case 'Image': return { backgroundImage: `url(${theme.backgroundValue})`, backgroundSize: 'cover', backgroundPosition: 'center' };
    case 'Video': return { background: '#090B0F' };
    default: return { background: theme.backgroundValue || 'radial-gradient(ellipse at 50% 0%, #1e1b4b 0%, #090B0F 60%)' };
  }
}

/* ── Code Lock Modal ── */
function CodeLockModal({ linkId, slug, onClose }: { linkId: string; slug: string; onClose: () => void }) {
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (code.length !== 4) return;
    setLoading(true);
    setError('');
    const res = await publicApi.unlockLink(slug, linkId, code);
    setLoading(false);
    if (res.data?.valid && res.data.url) {
      window.open(res.data.url, '_blank', 'noopener,noreferrer');
      onClose();
    } else {
      setError('Invalid code. Try again.');
      setCode('');
    }
  };

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm px-4" onClick={onClose}>
      <motion.div initial={{ scale: 0.9, y: 20 }} animate={{ scale: 1, y: 0 }}
        className="bg-[#1a1a2e] border border-white/10 rounded-2xl p-6 w-full max-w-sm" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2"><Lock className="w-5 h-5 text-amber-400" /><h3 className="text-white font-semibold">Enter Code</h3></div>
          <button onClick={onClose} className="text-white/40 hover:text-white"><X className="w-5 h-5" /></button>
        </div>
        <p className="text-white/50 text-sm mb-4">This link is protected. Enter the 4-digit code to continue.</p>
        <div className="flex gap-2 justify-center mb-4">
          {[0, 1, 2, 3].map(i => (
            <input key={i} type="text" maxLength={1} value={code[i] || ''} readOnly
              className="w-12 h-14 text-center text-2xl font-bold bg-white/5 border border-white/20 rounded-xl text-white focus:border-indigo-500 focus:outline-none" />
          ))}
        </div>
        <input type="text" inputMode="numeric" pattern="\d*" maxLength={4} value={code}
          onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 4))}
          className="sr-only" autoFocus onKeyDown={e => e.key === 'Enter' && handleSubmit()} />
        {error && <p className="text-red-400 text-xs text-center mb-3">{error}</p>}
        <button onClick={handleSubmit} disabled={code.length !== 4 || loading}
          className="w-full py-3 rounded-xl font-semibold text-white bg-indigo-600 hover:bg-indigo-500 disabled:opacity-40 transition-all">
          {loading ? <Loader2 className="w-4 h-4 animate-spin mx-auto" /> : 'Unlock'}
        </button>
      </motion.div>
    </motion.div>
  );
}

/* ── QR Code Modal ── */
function QRModal({ url, onClose }: { url: string; onClose: () => void }) {
  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm px-4" onClick={onClose}>
      <motion.div initial={{ scale: 0.9 }} animate={{ scale: 1 }}
        className="bg-[#1a1a2e] border border-white/10 rounded-2xl p-6 text-center" onClick={e => e.stopPropagation()}>
        <h3 className="text-white font-semibold mb-4">Scan to visit</h3>
        <div className="bg-white p-4 rounded-xl inline-block">
          <QRCodeSVG value={url} size={200} level="H" />
        </div>
        <p className="text-white/40 text-xs mt-3 max-w-[200px] mx-auto truncate">{url}</p>
        <button onClick={onClose} className="mt-4 px-6 py-2 rounded-xl bg-white/10 text-white text-sm hover:bg-white/20 transition-all">Close</button>
      </motion.div>
    </motion.div>
  );
}

/* ── Social Icons Bar ── */
function SocialBar({ socialLinks, accent }: { socialLinks: Record<string, string>; accent: string }) {
  const entries = Object.entries(socialLinks).filter(([, v]) => v);
  if (!entries.length) return null;
  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.3 }}
      className="flex items-center justify-center gap-3 flex-wrap">
      {entries.map(([platform, handle]) => {
        const info = SOCIAL_ICONS[platform];
        const url = info?.baseUrl ? `${info.baseUrl}${handle.replace('@', '')}` : handle;
        return (
          <a key={platform} href={url} target="_blank" rel="noopener noreferrer"
            className="w-10 h-10 rounded-full flex items-center justify-center text-lg transition-all hover:scale-110"
            style={{ background: `${accent}20`, border: `1px solid ${accent}30` }} title={platform}>
            {info?.icon || '🔗'}
          </a>
        );
      })}
    </motion.div>
  );
}

/* ── Single Link Card ── */
function LinkCard({ link, index, theme, slug }: { link: PublicLink; index: number; theme?: PublicProfileTheme; slug: string }) {
  const [clicked, setClicked] = useState(false);
  const [showLock, setShowLock] = useState(false);
  const accent = theme?.buttonColor || '#6366F1';
  const textColor = theme?.buttonTextColor || '#FFFFFF';
  const btnStyle = getButtonCSS(theme?.buttonStyle || 'Rounded', accent, textColor, theme?.buttonHoverColor);

  const handleClick = useCallback(() => {
    if (link.hasCodeLock) { setShowLock(true); return; }
    setClicked(true);
    publicApi.trackClick(slug, link.id).catch(() => null);
    setTimeout(() => window.open(link.url, '_blank', 'noopener,noreferrer'), 80);
  }, [link, slug]);

  // Header type renders as a divider
  if (link.linkType === 'Header') {
    return (
      <motion.div initial={{ opacity: 0, y: 15 }} animate={{ opacity: 1, y: 0 }}
        transition={{ delay: index * 0.05, duration: 0.3 }} className="pt-4 pb-1">
        <h3 className="text-sm font-bold uppercase tracking-wider" style={{ color: `${accent}CC` }}>{link.title}</h3>
      </motion.div>
    );
  }

  return (
    <>
      <motion.button id={`public-link-${link.id}`}
        initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
        transition={{ delay: index * 0.06, duration: 0.35, ease: 'easeOut' }}
        onClick={handleClick} whileHover={{ scale: 1.02, y: -2 }} whileTap={{ scale: 0.98 }}
        className="w-full text-left group relative overflow-hidden p-4 transition-all duration-200"
        style={btnStyle}>
        <div className="flex items-center gap-4 relative z-10">
          {link.icon && <span className="text-xl flex-shrink-0">{link.icon}</span>}
          {!link.icon && <div className="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0" style={{ background: `${accent}20` }}>
            {link.hasCodeLock ? <Lock className="w-4 h-4" style={{ color: accent }} /> : <LinkIcon className="w-4 h-4" style={{ color: accent }} />}
          </div>}
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold truncate" style={{ color: textColor }}>{link.title}</p>
            {link.description && <p className="text-xs truncate mt-0.5" style={{ color: `${textColor}80` }}>{link.description}</p>}
          </div>
          {link.isFeatured && <Star className="w-4 h-4 flex-shrink-0 fill-amber-400 text-amber-400" />}
          <ExternalLink className={`w-4 h-4 flex-shrink-0 transition-all ${clicked ? 'text-emerald-400' : 'opacity-30 group-hover:opacity-70'}`} style={{ color: textColor }} />
        </div>
      </motion.button>
      <AnimatePresence>{showLock && <CodeLockModal linkId={link.id} slug={slug} onClose={() => setShowLock(false)} />}</AnimatePresence>
    </>
  );
}

/* ── Grid Layout ── */
function GridLayout({ links, theme, slug }: { links: PublicLink[]; theme?: PublicProfileTheme; slug: string }) {
  return (
    <div className="grid grid-cols-2 gap-3">
      {links.map((link, i) => <LinkCard key={link.id} link={link} index={i} theme={theme} slug={slug} />)}
    </div>
  );
}

/* ── List Layout ── */
function ListLayout({ links, theme, slug }: { links: PublicLink[]; theme?: PublicProfileTheme; slug: string }) {
  return (
    <div className="space-y-3">
      {links.map((link, i) => <LinkCard key={link.id} link={link} index={i} theme={theme} slug={slug} />)}
    </div>
  );
}

/* ══════════════════ Main Page ══════════════════ */
export function PublicProfilePage() {
  const { username } = useParams<{ username: string }>();
  const [profile, setProfile] = useState<PublicProfileData | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [showQR, setShowQR] = useState(false);

  const theme = profile?.theme;
  const accent = theme?.buttonColor || '#6366F1';
  const fontFamily = theme?.fontFamily || 'Inter';
  const pageUrl = typeof window !== 'undefined' ? window.location.href : '';

  useEffect(() => {
    if (!username) { setNotFound(true); setLoading(false); return; }
    document.title = `@${username} · LinkVault`;
    publicApi.getProfile(username).then(res => {
      setLoading(false);
      if (res.error || !res.data) { setNotFound(true); return; }
      setProfile(res.data);
    });
  }, [username]);

  // Inject Google Font
  useEffect(() => {
    if (!fontFamily || fontFamily === 'Inter') return;
    const link = document.createElement('link');
    link.href = `https://fonts.googleapis.com/css2?family=${fontFamily.replace(/ /g, '+')}:wght@400;500;600;700&display=swap`;
    link.rel = 'stylesheet';
    document.head.appendChild(link);
    return () => { document.head.removeChild(link); };
  }, [fontFamily]);

  if (loading) return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: '#090B0F' }}>
      <Loader2 className="w-8 h-8 animate-spin" style={{ color: accent }} />
    </div>
  );

  if (notFound || !profile) return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 px-4" style={{ background: '#090B0F' }}>
      <div className="w-16 h-16 rounded-2xl flex items-center justify-center" style={{ background: `${accent}20`, border: `1px solid ${accent}40` }}>
        <AlertCircle className="w-8 h-8" style={{ color: accent }} />
      </div>
      <div className="text-center">
        <h1 className="text-2xl font-bold text-white">Profile not found</h1>
        <p className="text-[var(--text-muted)] mt-2">The user <strong className="text-white">@{username}</strong> doesn't exist or has been removed.</p>
      </div>
      <Link to="/" className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white transition-all hover:opacity-90" style={{ background: accent }}>
        <ArrowLeft className="w-4 h-4" /> Go to LinkVault
      </Link>
    </div>
  );

  const layout = theme?.profileLayout || 'List';
  const socialPosition = theme?.socialLinksPosition || 'Bottom';

  return (
    <div className="min-h-screen" style={{ ...getBackgroundCSS(theme), fontFamily: `'${fontFamily}', sans-serif` }}>
      {/* Background video */}
      {theme?.backgroundType === 'Video' && (
        <video autoPlay loop muted playsInline className="fixed inset-0 w-full h-full object-cover opacity-30">
          <source src={theme.backgroundValue} type="video/mp4" />
        </video>
      )}

      {/* Ambient glow */}
      <div className="fixed inset-0 pointer-events-none" style={{ background: `radial-gradient(ellipse 60% 40% at 50% -10%, ${accent}20 0%, transparent 70%)` }} />

      {/* Share + QR floating buttons */}
      <div className="fixed top-4 right-4 z-20 flex gap-2">
        <button onClick={() => navigator.clipboard?.writeText(pageUrl)} title="Copy link"
          className="w-9 h-9 rounded-full flex items-center justify-center bg-white/10 backdrop-blur-md text-white/60 hover:text-white hover:bg-white/20 transition-all">
          <Share2 className="w-4 h-4" />
        </button>
        <button onClick={() => setShowQR(true)} title="QR Code"
          className="w-9 h-9 rounded-full flex items-center justify-center bg-white/10 backdrop-blur-md text-white/60 hover:text-white hover:bg-white/20 transition-all">
          <QrCode className="w-4 h-4" />
        </button>
      </div>

      <div className="relative z-10 max-w-lg mx-auto px-4 py-16">
        {/* Social icons — top */}
        {socialPosition === 'Top' && profile.socialLinks && <div className="mb-6"><SocialBar socialLinks={profile.socialLinks} accent={accent} /></div>}

        {/* Profile header */}
        <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }} className="text-center mb-8">
          <motion.div initial={{ scale: 0.8, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} transition={{ delay: 0.1, type: 'spring', stiffness: 200 }} className="relative inline-block mb-4">
            <div className="w-24 h-24 rounded-full mx-auto flex items-center justify-center text-3xl font-bold text-white overflow-hidden"
              style={{ background: profile.avatarUrl ? undefined : `linear-gradient(135deg, ${accent}, #A78BFA)`, boxShadow: `0 0 0 4px ${accent}40, 0 8px 32px ${accent}30` }}>
              {profile.avatarUrl ? <img src={profile.avatarUrl} alt={profile.displayName || profile.username} className="w-full h-full object-cover" />
                : (profile.displayName?.[0] || profile.username[0])?.toUpperCase()}
            </div>
            <span className="absolute bottom-1 right-1 w-4 h-4 rounded-full border-2 border-[#090B0F]" style={{ background: '#10B981' }} />
          </motion.div>
          <motion.h1 initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.2 }} className="text-2xl font-bold text-white">
            {profile.displayName || `@${profile.username}`}
          </motion.h1>
          <motion.p initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.25 }} className="text-sm mt-0.5" style={{ color: accent }}>@{profile.username}</motion.p>
          {profile.bio && <motion.p initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.3 }} className="text-sm mt-3 max-w-sm mx-auto leading-relaxed" style={{ color: 'rgba(255,255,255,0.6)' }}>{profile.bio}</motion.p>}
        </motion.div>

        {/* Links */}
        {profile.links.length > 0 ? (
          layout === 'Grid'
            ? <GridLayout links={profile.links} theme={theme} slug={username!} />
            : <ListLayout links={profile.links} theme={theme} slug={username!} />
        ) : (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.4 }} className="text-center py-12" style={{ color: 'rgba(255,255,255,0.3)' }}>
            <LinkIcon className="w-8 h-8 mx-auto mb-3 opacity-40" /><p className="text-sm">No links shared yet.</p>
          </motion.div>
        )}

        {/* Social icons — bottom */}
        {socialPosition === 'Bottom' && profile.socialLinks && <div className="mt-8"><SocialBar socialLinks={profile.socialLinks} accent={accent} /></div>}

        {/* Footer branding */}
        {!theme?.hideBranding && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.6 }} className="text-center mt-12">
            <Link to="/" className="inline-flex items-center gap-2 text-xs px-4 py-2 rounded-full transition-all hover:opacity-80"
              style={{ background: 'rgba(255,255,255,0.06)', color: 'rgba(255,255,255,0.4)', border: '1px solid rgba(255,255,255,0.08)' }}>
              <LinkIcon className="w-3 h-3" /> Powered by LinkVault
            </Link>
          </motion.div>
        )}

        {/* Custom CSS injection */}
        {theme?.customCSS && <style dangerouslySetInnerHTML={{ __html: theme.customCSS }} />}
      </div>

      {/* QR Modal */}
      <AnimatePresence>{showQR && <QRModal url={pageUrl} onClose={() => setShowQR(false)} />}</AnimatePresence>
    </div>
  );
}
