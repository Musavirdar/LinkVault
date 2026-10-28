import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Palette, Type, Layout, Globe, Save, Loader2, Eye, Link2, Instagram, Github, Twitter, Youtube, Linkedin, ExternalLink } from 'lucide-react';
import { publicApi, linksApi, authApi, type PublicProfileTheme } from '@/api/client';

const BUTTON_STYLES = ['Rounded', 'Sharp', 'Outline', 'Shadow', 'Pill', 'Glassmorphism'];
const BG_TYPES = ['Gradient', 'Solid', 'Image'];
const LAYOUTS = ['List', 'Grid'];
const SOCIAL_POSITIONS = ['Bottom', 'Top', 'Hidden'];
const FONTS = ['Inter', 'Roboto', 'Outfit', 'Space Grotesk', 'Poppins', 'DM Sans', 'Montserrat', 'Nunito'];

const PRESET_GRADIENTS = [
  'linear-gradient(135deg, #0f0c29, #302b63, #24243e)',
  'linear-gradient(135deg, #1a1a2e, #16213e, #0f3460)',
  'linear-gradient(135deg, #000428, #004e92)',
  'linear-gradient(135deg, #2d1b69, #6b21a8)',
  'linear-gradient(135deg, #0c0c0c, #1a1a1a)',
  'linear-gradient(135deg, #1e3a5f, #0d1b2a)',
  'linear-gradient(135deg, #134e5e, #71b280)',
  'linear-gradient(135deg, #360033, #0b8793)',
];

interface SocialLinks {
  twitter?: string; instagram?: string; github?: string; youtube?: string; linkedin?: string; website?: string;
}

export function ProfileEditorPage() {
  const [theme, setTheme] = useState<Partial<PublicProfileTheme>>({
    backgroundType: 'Gradient',
    backgroundValue: PRESET_GRADIENTS[0],
    buttonStyle: 'Rounded',
    buttonColor: '#6366F1',
    buttonTextColor: '#FFFFFF',
    fontFamily: 'Inter',
    profileLayout: 'List',
    socialLinksPosition: 'Bottom',
    hideBranding: false,
  });
  const [social, setSocial] = useState<SocialLinks>({});
  const [slug, setSlug] = useState('');
  const [username, setUsername] = useState('');
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [links, setLinks] = useState<Array<{ id: string; title: string; url: string; color?: string }>>([]);
  const [activeTab, setActiveTab] = useState<'theme' | 'social' | 'slug'>('theme');

  useEffect(() => {
    publicApi.getTheme().then(r => { if (r.data) setTheme(r.data); });
    authApi.getProfile().then(r => {
      if (r.data) {
        setUsername(r.data.username);
        setSlug(r.data.username);
      }
    });
    linksApi.getAll().then(r => { if (r.data) setLinks(r.data.slice(0, 5)); });
  }, []);

  const handleSaveAll = async () => {
    setSaving(true);
    await Promise.all([
      publicApi.updateTheme(theme),
      publicApi.updateSocialLinks(social as Record<string, string>),
      slug && slug !== username ? publicApi.updateSlug(slug) : Promise.resolve(),
    ]);
    setSaving(false);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const updateTheme = (key: string, val: string | boolean) => setTheme(prev => ({ ...prev, [key]: val }));

  const tabs = [
    { id: 'theme' as const, label: 'Theme', icon: Palette },
    { id: 'social' as const, label: 'Social', icon: Globe },
    { id: 'slug' as const, label: 'URL', icon: Link2 },
  ];

  return (
    <div className="max-w-7xl mx-auto py-6 px-4">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--text-primary)]">Profile Editor</h1>
          <p className="text-sm text-[var(--text-secondary)] mt-0.5">Customize your public Link-in-Bio page</p>
        </div>
        <div className="flex items-center gap-3">
          <a href={`/u/${slug || username}`} target="_blank" rel="noopener noreferrer"
            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-strong)] text-sm text-[var(--text-primary)] hover:bg-[var(--bg-tertiary)] transition-all">
            <Eye className="w-4 h-4" /> Preview
          </a>
          <motion.button whileTap={{ scale: 0.96 }} onClick={handleSaveAll} disabled={saving}
            className="flex items-center gap-2 px-5 py-2 rounded-lg bg-[var(--accent-primary)] text-white text-sm font-medium hover:opacity-90 transition-all">
            {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : saved ? '✓ Saved' : <><Save className="w-4 h-4" /> Save</>}
          </motion.button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Editor Panel */}
        <div className="lg:col-span-2 space-y-4">
          {/* Tabs */}
          <div className="flex gap-1 p-1 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-subtle)]">
            {tabs.map(t => (
              <button key={t.id} onClick={() => setActiveTab(t.id)}
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg text-sm font-medium transition-all ${activeTab === t.id ? 'bg-[var(--accent-primary)] text-white shadow-md' : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'}`}>
                <t.icon className="w-4 h-4" /> {t.label}
              </button>
            ))}
          </div>

          {/* Theme Tab */}
          {activeTab === 'theme' && (
            <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
              className="rounded-xl p-6 bg-[var(--bg-secondary)] border border-[var(--border-subtle)] space-y-6">
              {/* Background */}
              <div>
                <label className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2 mb-3">
                  <Palette className="w-4 h-4 text-[var(--accent-primary)]" /> Background
                </label>
                <div className="flex gap-2 mb-3">
                  {BG_TYPES.map(t => (
                    <button key={t} onClick={() => updateTheme('backgroundType', t)}
                      className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${theme.backgroundType === t ? 'bg-[var(--accent-primary)] text-white' : 'bg-[var(--bg-primary)] text-[var(--text-secondary)] hover:text-[var(--text-primary)]'}`}>
                      {t}
                    </button>
                  ))}
                </div>
                {theme.backgroundType === 'Gradient' && (
                  <div className="grid grid-cols-4 gap-2">
                    {PRESET_GRADIENTS.map(g => (
                      <button key={g} onClick={() => updateTheme('backgroundValue', g)}
                        className={`h-16 rounded-lg transition-all ${theme.backgroundValue === g ? 'ring-2 ring-[var(--accent-primary)] ring-offset-2 ring-offset-[var(--bg-secondary)]' : 'hover:opacity-80'}`}
                        style={{ background: g }} />
                    ))}
                  </div>
                )}
                {theme.backgroundType === 'Solid' && (
                  <input type="color" value={theme.backgroundValue || '#090B0F'} onChange={e => updateTheme('backgroundValue', e.target.value)}
                    className="w-full h-12 rounded-lg cursor-pointer border-0" />
                )}
                {theme.backgroundType === 'Image' && (
                  <input type="url" placeholder="https://example.com/bg.jpg" value={theme.backgroundValue || ''} onChange={e => updateTheme('backgroundValue', e.target.value)}
                    className="input-field w-full" />
                )}
              </div>

              {/* Button Style */}
              <div>
                <label className="text-sm font-semibold text-[var(--text-primary)] mb-3 block">Button Style</label>
                <div className="grid grid-cols-3 gap-2">
                  {BUTTON_STYLES.map(s => (
                    <button key={s} onClick={() => updateTheme('buttonStyle', s)}
                      className={`py-3 rounded-lg text-xs font-medium transition-all ${theme.buttonStyle === s ? 'bg-[var(--accent-primary)] text-white' : 'bg-[var(--bg-primary)] text-[var(--text-secondary)] border border-[var(--border-subtle)] hover:border-[var(--accent-primary)]'}`}>
                      {s}
                    </button>
                  ))}
                </div>
              </div>

              {/* Colors */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-xs text-[var(--text-muted)] mb-1.5 block">Button Color</label>
                  <input type="color" value={theme.buttonColor || '#6366F1'} onChange={e => updateTheme('buttonColor', e.target.value)} className="w-full h-10 rounded-lg cursor-pointer border-0" />
                </div>
                <div>
                  <label className="text-xs text-[var(--text-muted)] mb-1.5 block">Text Color</label>
                  <input type="color" value={theme.buttonTextColor || '#FFFFFF'} onChange={e => updateTheme('buttonTextColor', e.target.value)} className="w-full h-10 rounded-lg cursor-pointer border-0" />
                </div>
              </div>

              {/* Font */}
              <div>
                <label className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2 mb-3">
                  <Type className="w-4 h-4 text-[var(--accent-primary)]" /> Font
                </label>
                <div className="grid grid-cols-4 gap-2">
                  {FONTS.map(f => (
                    <button key={f} onClick={() => updateTheme('fontFamily', f)}
                      className={`py-2 rounded-lg text-xs transition-all ${theme.fontFamily === f ? 'bg-[var(--accent-primary)] text-white' : 'bg-[var(--bg-primary)] text-[var(--text-secondary)] hover:text-[var(--text-primary)]'}`}
                      style={{ fontFamily: f }}>{f}</button>
                  ))}
                </div>
              </div>

              {/* Layout */}
              <div>
                <label className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2 mb-3">
                  <Layout className="w-4 h-4 text-[var(--accent-primary)]" /> Layout
                </label>
                <div className="flex gap-2">
                  {LAYOUTS.map(l => (
                    <button key={l} onClick={() => updateTheme('profileLayout', l)}
                      className={`flex-1 py-2.5 rounded-lg text-xs font-medium transition-all ${theme.profileLayout === l ? 'bg-[var(--accent-primary)] text-white' : 'bg-[var(--bg-primary)] text-[var(--text-secondary)]'}`}>{l}</button>
                  ))}
                </div>
              </div>

              {/* Social Position */}
              <div>
                <label className="text-xs text-[var(--text-muted)] mb-2 block">Social Links Position</label>
                <div className="flex gap-2">
                  {SOCIAL_POSITIONS.map(p => (
                    <button key={p} onClick={() => updateTheme('socialLinksPosition', p)}
                      className={`flex-1 py-2 rounded-lg text-xs font-medium transition-all ${theme.socialLinksPosition === p ? 'bg-[var(--accent-primary)] text-white' : 'bg-[var(--bg-primary)] text-[var(--text-secondary)]'}`}>{p}</button>
                  ))}
                </div>
              </div>

              {/* Hide branding */}
              <label className="flex items-center gap-3 cursor-pointer">
                <input type="checkbox" checked={theme.hideBranding || false} onChange={e => updateTheme('hideBranding', e.target.checked)} className="rounded" />
                <span className="text-sm text-[var(--text-secondary)]">Hide "Powered by LinkVault" footer</span>
              </label>
            </motion.div>
          )}

          {/* Social Tab */}
          {activeTab === 'social' && (
            <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
              className="rounded-xl p-6 bg-[var(--bg-secondary)] border border-[var(--border-subtle)] space-y-4">
              {[
                { key: 'twitter', label: 'X / Twitter', icon: Twitter, placeholder: '@username' },
                { key: 'instagram', label: 'Instagram', icon: Instagram, placeholder: 'username' },
                { key: 'github', label: 'GitHub', icon: Github, placeholder: 'username' },
                { key: 'youtube', label: 'YouTube', icon: Youtube, placeholder: 'channel' },
                { key: 'linkedin', label: 'LinkedIn', icon: Linkedin, placeholder: 'username' },
                { key: 'website', label: 'Website', icon: ExternalLink, placeholder: 'https://...' },
              ].map(s => (
                <div key={s.key} className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-lg flex items-center justify-center bg-[var(--bg-primary)]">
                    <s.icon className="w-4 h-4 text-[var(--text-muted)]" />
                  </div>
                  <input value={(social as Record<string, string>)[s.key] || ''} onChange={e => setSocial(prev => ({ ...prev, [s.key]: e.target.value }))}
                    placeholder={s.placeholder} className="input-field flex-1" />
                </div>
              ))}
            </motion.div>
          )}

          {/* Slug Tab */}
          {activeTab === 'slug' && (
            <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}
              className="rounded-xl p-6 bg-[var(--bg-secondary)] border border-[var(--border-subtle)] space-y-4">
              <div>
                <label className="text-sm font-semibold text-[var(--text-primary)] mb-2 block">Custom URL</label>
                <div className="flex items-center gap-2">
                  <span className="text-sm text-[var(--text-muted)]">{window.location.origin}/u/</span>
                  <input value={slug} onChange={e => setSlug(e.target.value.replace(/[^a-zA-Z0-9-_]/g, '').toLowerCase())}
                    placeholder={username} className="input-field flex-1" />
                </div>
                <p className="text-xs text-[var(--text-muted)] mt-2">Only letters, numbers, hyphens, and underscores.</p>
              </div>
            </motion.div>
          )}
        </div>

        {/* Live Preview */}
        <div className="hidden lg:block">
          <div className="sticky top-6">
            <div className="text-xs text-[var(--text-muted)] mb-2 text-center">Live Preview</div>
            <div className="rounded-2xl overflow-hidden border-2 border-[var(--border-subtle)] shadow-2xl" style={{ height: '580px' }}>
              <div className="h-full overflow-y-auto" style={{ background: theme.backgroundValue || '#090B0F', fontFamily: `'${theme.fontFamily || 'Inter'}', sans-serif` }}>
                <div className="px-4 py-8 flex flex-col items-center">
                  <div className="w-16 h-16 rounded-full flex items-center justify-center text-xl font-bold text-white mb-3"
                    style={{ background: `linear-gradient(135deg, ${theme.buttonColor || '#6366F1'}, #A78BFA)` }}>
                    {username?.[0]?.toUpperCase() || 'U'}
                  </div>
                  <h3 className="text-sm font-bold text-white">@{slug || username || 'username'}</h3>
                  <p className="text-xs text-white/50 mt-1">Your bio here</p>

                  <div className="w-full mt-6 space-y-2.5 px-2">
                    {(links.length > 0 ? links : [{ id: '1', title: 'My Website', url: '#', color: '#6366F1' }, { id: '2', title: 'Portfolio', url: '#', color: '#8B5CF6' }, { id: '3', title: 'Contact Me', url: '#', color: '#EC4899' }]).map(link => {
                      const btnColor = theme.buttonColor || '#6366F1';
                      const textColor = theme.buttonTextColor || '#FFFFFF';
                      const style = theme.buttonStyle || 'Rounded';

                      const btnCSS: React.CSSProperties = style === 'Sharp' ? { background: btnColor, borderRadius: '4px' }
                        : style === 'Outline' ? { background: 'transparent', border: `2px solid ${btnColor}`, borderRadius: '12px' }
                        : style === 'Shadow' ? { background: btnColor, borderRadius: '12px', boxShadow: `0 4px 14px ${btnColor}50` }
                        : style === 'Pill' ? { background: btnColor, borderRadius: '9999px' }
                        : style === 'Glassmorphism' ? { background: `${btnColor}20`, backdropFilter: 'blur(12px)', border: `1px solid ${btnColor}30`, borderRadius: '16px' }
                        : { background: `${btnColor}15`, border: `1px solid ${btnColor}30`, borderRadius: '16px' };

                      return (
                        <div key={link.id} className="w-full p-3 text-center text-xs font-medium" style={{ ...btnCSS, color: textColor }}>
                          {link.title}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
