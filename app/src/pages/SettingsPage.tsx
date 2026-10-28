import { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  User, Lock, Bell, Shield, Save, Camera, Loader2, CheckCircle2,
  Key, Eye, EyeOff, Smartphone, AlertTriangle, ChevronRight, Palette,
} from 'lucide-react';
import { DashboardHeader } from '@/components/dashboard/Header';
import { authApi, fileUploadApi } from '@/api/client';
import { useStore } from '@/store/useStore';

const tabs = [
  { id: 'profile',       label: 'Profile',       icon: User,         desc: 'Your public identity' },
  { id: 'security',      label: 'Security',      icon: Lock,         desc: 'Password & 2FA' },
  { id: 'notifications', label: 'Notifications', icon: Bell,         desc: 'Alert preferences' },
  { id: 'privacy',       label: 'Privacy',       icon: Shield,       desc: 'Visibility & data' },
  { id: 'appearance',    label: 'Appearance',    icon: Palette,      desc: 'Theme & display' },
] as const;
type Tab = typeof tabs[number]['id'];

// Reusable functional toggle component
function Toggle({ on, onToggle, label }: { on: boolean; onToggle: () => void; label: string }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={on}
      aria-label={label}
      onClick={onToggle}
      className={`toggle-track ${on ? 'on' : ''}`}
    >
      <span className="toggle-thumb" />
    </button>
  );
}

// Reusable setting row
function SettingRow({
  title, desc, children,
}: { title: string; desc: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between py-4 border-b border-[var(--border-subtle)] last:border-0">
      <div className="pr-4">
        <p className="text-sm font-medium text-[var(--text-primary)]">{title}</p>
        <p className="text-xs text-[var(--text-muted)] mt-0.5">{desc}</p>
      </div>
      {children}
    </div>
  );
}

const accentOptions = [
  { label: 'Indigo',  value: '#6366F1' },
  { label: 'Violet',  value: '#8B5CF6' },
  { label: 'Pink',    value: '#EC4899' },
  { label: 'Sky',     value: '#0EA5E9' },
  { label: 'Emerald', value: '#10B981' },
  { label: 'Amber',   value: '#F59E0B' },
  { label: 'Rose',    value: '#F43F5E' },
];

export function SettingsPage() {
  const { user, setUser } = useStore();
  const [activeTab, setActiveTab]           = useState<Tab>('profile');
  const [saving, setSaving]                 = useState(false);
  const [saved, setSaved]                   = useState(false);
  const [avatarUploading, setAvatarUploading] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement>(null);

  // Profile form
  const [form, setForm] = useState({ displayName: user?.name || '', bio: '', avatarUrl: user?.avatar || '' });

  // Password form
  const [pwForm, setPwForm]   = useState({ current: '', newPw: '', confirm: '' });
  const [pwShow, setPwShow]   = useState({ current: false, newPw: false, confirm: false });
  const [pwError, setPwError] = useState('');
  const [pwSaving, setPwSaving] = useState(false);
  const [pwSaved, setPwSaved]   = useState(false);

  // Notification preferences (stored locally until backend endpoint added)
  const [notifs, setNotifs] = useState({
    linkVisits:       true,
    fileDownloads:    true,
    teamActivity:     false,
    securityAlerts:   true,
    weeklyDigest:     false,
    productUpdates:   true,
  });

  // Privacy preferences
  const [privacy, setPrivacy] = useState({
    publicProfile:  true,
    onlineStatus:   false,
    allowIndexing:  true,
    shareAnalytics: false,
  });

  // Appearance
  const [accent, setAccent] = useState('#6366F1');

  useEffect(() => {
    authApi.getProfile().then(res => {
      if (res.data) {
        const p = res.data;
        setForm(f => ({ ...f, displayName: p.displayName || user?.name || '', bio: p.bio || '', avatarUrl: p.avatarUrl || user?.avatar || '' }));
      }
    });
    // Load persisted prefs
    const savedAccent = localStorage.getItem('lv_accent');
    if (savedAccent) {
      setAccent(savedAccent);
      document.documentElement.style.setProperty('--accent-primary', savedAccent);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleProfileSave = async () => {
    setSaving(true); setSaved(false);
    const res = await authApi.updateProfile({ displayName: form.displayName, bio: form.bio, avatarUrl: form.avatarUrl });
    setSaving(false);
    if (!res.error) {
      setUser({ ...user!, name: form.displayName, avatar: form.avatarUrl });
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    }
  };

  const handleAvatarUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setAvatarUploading(true);
    const res = await fileUploadApi.upload(file);
    e.target.value = '';
    setAvatarUploading(false);
    if (res.data?.url) setForm(f => ({ ...f, avatarUrl: res.data!.url }));
  };

  const handlePasswordChange = async () => {
    if (pwForm.newPw !== pwForm.confirm) { setPwError('Passwords do not match'); return; }
    if (pwForm.newPw.length < 8) { setPwError('New password must be at least 8 characters'); return; }
    setPwError(''); setPwSaving(true);
    const res = await authApi.changePassword({ currentPassword: pwForm.current, newPassword: pwForm.newPw });
    setPwSaving(false);
    if (res.error) { setPwError(res.error); return; }
    setPwSaved(true);
    setPwForm({ current: '', newPw: '', confirm: '' });
    setTimeout(() => setPwSaved(false), 2500);
  };

  const toggleNotif = (key: keyof typeof notifs) =>
    setNotifs(n => ({ ...n, [key]: !n[key] }));

  const togglePrivacy = (key: keyof typeof privacy) =>
    setPrivacy(p => ({ ...p, [key]: !p[key] }));

  const handleAccentChange = (color: string) => {
    setAccent(color);
    document.documentElement.style.setProperty('--accent-primary', color);
    localStorage.setItem('lv_accent', color);
  };

  const panelVariants = {
    hidden: { opacity: 0, x: 12 },
    visible: { opacity: 1, x: 0, transition: { duration: 0.22, ease: 'easeOut' as const } },
    exit: { opacity: 0, x: -12, transition: { duration: 0.15 } },
  };

  return (
    <div className="max-w-5xl mx-auto">
      <DashboardHeader title="Settings" subtitle="Manage your account, security, and preferences." />

      <div className="flex gap-6">
        {/* Sidebar tabs */}
        <div className="flex-shrink-0 w-52 space-y-0.5">
          {tabs.map(tab => {
            const active = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                id={`settings-tab-${tab.id}`}
                onClick={() => setActiveTab(tab.id)}
                className="w-full flex items-center gap-3 px-3 py-3 rounded-xl text-sm font-medium text-left transition-all duration-200 group"
                style={active
                  ? { background: `${accent}18`, color: accent, boxShadow: `inset 2px 0 0 ${accent}` }
                  : { color: 'var(--text-secondary)' }
                }
              >
                <tab.icon className="w-4 h-4 flex-shrink-0" />
                <div>
                  <div>{tab.label}</div>
                  <div className="text-[10px] font-normal" style={{ color: active ? `${accent}99` : 'var(--text-muted)' }}>
                    {tab.desc}
                  </div>
                </div>
                {active && <ChevronRight className="w-3 h-3 ml-auto opacity-60" />}
              </button>
            );
          })}
        </div>

        {/* Panel */}
        <div className="flex-1 min-w-0">
          <AnimatePresence mode="wait">
            <motion.div
              key={activeTab}
              variants={panelVariants}
              initial="hidden"
              animate="visible"
              exit="exit"
              className="rounded-2xl p-6"
              style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border-subtle)' }}
            >

              {/* ── PROFILE ── */}
              {activeTab === 'profile' && (
                <div className="space-y-6">
                  <div>
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">Profile Information</h3>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">This is how others see you on LinkVault.</p>
                  </div>

                  {/* Avatar */}
                  <div className="flex items-center gap-5">
                    <div className="relative flex-shrink-0">
                      <input ref={avatarInputRef} type="file" accept="image/jpeg,image/png,image/gif,image/webp" className="hidden" onChange={handleAvatarUpload} />
                      <button
                        type="button"
                        aria-label="Change profile photo"
                        onClick={() => avatarInputRef.current?.click()}
                        className="w-20 h-20 rounded-full flex items-center justify-center text-2xl font-bold text-white overflow-hidden ring-4 ring-[var(--border-subtle)] hover:ring-[var(--accent-primary)] transition-all duration-200"
                        style={{ background: form.avatarUrl ? undefined : `linear-gradient(135deg, ${accent}, #A78BFA)` }}
                      >
                        {form.avatarUrl
                          ? <img src={form.avatarUrl} alt="avatar" className="w-full h-full object-cover" />
                          : (user?.name?.[0]?.toUpperCase() || 'U')}
                      </button>
                      <button
                        type="button"
                        onClick={() => avatarInputRef.current?.click()}
                        className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full flex items-center justify-center shadow-lg transition-transform hover:scale-110"
                        style={{ background: accent, color: 'white' }}
                      >
                        {avatarUploading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Camera className="w-3.5 h-3.5" />}
                      </button>
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-[var(--text-primary)]">{form.displayName || user?.name}</p>
                      <p className="text-xs text-[var(--text-muted)]">JPG, PNG or GIF · Max 2 MB</p>
                      <button onClick={() => avatarInputRef.current?.click()} className="text-xs mt-1.5 font-medium transition-colors hover:underline" style={{ color: accent }}>
                        Upload new photo
                      </button>
                      {form.avatarUrl && (
                        <button onClick={() => setForm(f => ({ ...f, avatarUrl: '' }))} className="text-xs ml-3 text-red-400 hover:text-red-300 transition-colors">
                          Remove
                        </button>
                      )}
                    </div>
                  </div>

                  {/* Fields */}
                  <div className="grid grid-cols-1 gap-4">
                    {[
                      { label: 'Display Name', key: 'displayName', placeholder: 'Your full name', type: 'text' },
                    ].map(f => (
                      <div key={f.key}>
                        <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-wide">{f.label}</label>
                        <input
                          type={f.type}
                          value={(form as Record<string, string>)[f.key]}
                          onChange={e => setForm(p => ({ ...p, [f.key]: e.target.value }))}
                          placeholder={f.placeholder}
                          className="input-field"
                        />
                      </div>
                    ))}
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-wide">Bio</label>
                      <textarea
                        rows={3}
                        value={form.bio}
                        onChange={e => setForm(p => ({ ...p, bio: e.target.value }))}
                        placeholder="Tell people a bit about yourself…"
                        maxLength={500}
                        className="input-field resize-none"
                      />
                      <p className="text-[11px] text-[var(--text-muted)] mt-1 text-right">{form.bio.length}/500</p>
                    </div>
                  </div>

                  <button
                    id="settings-save-profile"
                    onClick={handleProfileSave}
                    disabled={saving}
                    className="btn-shine flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-semibold text-white transition-all disabled:opacity-60 disabled:cursor-not-allowed"
                    style={{ background: `linear-gradient(135deg, ${accent}, #A78BFA)`, boxShadow: `0 4px 16px ${accent}40` }}
                  >
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : saved ? <CheckCircle2 className="w-4 h-4 text-emerald-300" /> : <Save className="w-4 h-4" />}
                    {saved ? 'Saved!' : 'Save Changes'}
                  </button>
                </div>
              )}

              {/* ── SECURITY ── */}
              {activeTab === 'security' && (
                <div className="space-y-8">
                  {/* Change Password */}
                  <div className="space-y-4">
                    <div>
                      <h3 className="text-base font-semibold text-[var(--text-primary)] flex items-center gap-2"><Key className="w-4 h-4" />Change Password</h3>
                      <p className="text-xs text-[var(--text-muted)] mt-0.5">Use a strong password of at least 8 characters.</p>
                    </div>
                    {([
                      { label: 'Current Password', key: 'current' },
                      { label: 'New Password', key: 'newPw' },
                      { label: 'Confirm New Password', key: 'confirm' },
                    ] as const).map(f => (
                      <div key={f.key}>
                        <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-wide">{f.label}</label>
                        <div className="relative">
                          <input
                            type={pwShow[f.key] ? 'text' : 'password'}
                            value={pwForm[f.key]}
                            onChange={e => setPwForm(p => ({ ...p, [f.key]: e.target.value }))}
                            className="input-field pr-10"
                            placeholder="••••••••"
                          />
                          <button
                            type="button"
                            onClick={() => setPwShow(s => ({ ...s, [f.key]: !s[f.key] }))}
                            className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-colors"
                          >
                            {pwShow[f.key] ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                          </button>
                        </div>
                      </div>
                    ))}
                    {pwError && (
                      <div className="flex items-center gap-2 px-3 py-2.5 rounded-xl text-sm text-red-400" style={{ background: '#EF444415', border: '1px solid #EF444430' }}>
                        <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                        {pwError}
                      </div>
                    )}
                    <button
                      id="settings-change-password"
                      onClick={handlePasswordChange}
                      disabled={pwSaving}
                      className="btn-shine flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-semibold text-white transition-all disabled:opacity-60"
                      style={{ background: `linear-gradient(135deg, ${accent}, #A78BFA)`, boxShadow: `0 4px 16px ${accent}40` }}
                    >
                      {pwSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : pwSaved ? <CheckCircle2 className="w-4 h-4 text-emerald-300" /> : <Lock className="w-4 h-4" />}
                      {pwSaved ? 'Password Updated!' : 'Update Password'}
                    </button>
                  </div>

                  {/* 2FA */}
                  <div className="rounded-xl p-5 space-y-3" style={{ background: 'var(--bg-tertiary)', border: '1px solid var(--border-subtle)' }}>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl flex items-center justify-center" style={{ background: `${accent}20` }}>
                          <Smartphone className="w-5 h-5" style={{ color: accent }} />
                        </div>
                        <div>
                          <p className="text-sm font-semibold text-[var(--text-primary)]">Two-Factor Authentication</p>
                          <p className="text-xs text-[var(--text-muted)]">Add an extra layer of security with an authenticator app</p>
                        </div>
                      </div>
                      <span className="text-[11px] font-semibold px-2.5 py-1 rounded-full" style={{ background: '#10B98120', color: '#10B981' }}>
                        Available
                      </span>
                    </div>
                    <p className="text-xs text-[var(--text-muted)] leading-relaxed">
                      Two-factor authentication adds an extra layer of security to your account. Once enabled, you'll need both your password and a one-time code from your authenticator app to sign in.
                    </p>
                    <button
                      id="settings-setup-2fa"
                      className="text-sm font-medium px-4 py-2 rounded-lg transition-all hover:opacity-90"
                      style={{ background: `${accent}20`, color: accent }}
                    >
                      Set up 2FA →
                    </button>
                  </div>

                  {/* Danger Zone */}
                  <div className="rounded-xl p-5 space-y-3" style={{ background: '#EF444408', border: '1px solid #EF444425' }}>
                    <h4 className="text-sm font-semibold text-red-400 flex items-center gap-2"><AlertTriangle className="w-4 h-4" />Danger Zone</h4>
                    <p className="text-xs text-[var(--text-muted)]">Permanently delete your account and all associated data. This action cannot be undone.</p>
                    <button className="text-sm font-medium text-red-400 hover:text-red-300 transition-colors border border-red-500/30 hover:border-red-400/50 px-4 py-2 rounded-lg">
                      Delete Account
                    </button>
                  </div>
                </div>
              )}

              {/* ── NOTIFICATIONS ── */}
              {activeTab === 'notifications' && (
                <div className="space-y-2">
                  <div className="mb-5">
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">Notification Preferences</h3>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">Choose what you want to be notified about.</p>
                  </div>
                  {([
                    { key: 'linkVisits',     label: 'Link visits',      desc: 'When someone clicks one of your shared links' },
                    { key: 'fileDownloads',  label: 'File downloads',   desc: 'When someone downloads one of your shared files' },
                    { key: 'teamActivity',   label: 'Team activity',    desc: 'Updates from your team members' },
                    { key: 'securityAlerts', label: 'Security alerts',  desc: 'Unusual sign-in or account activity' },
                    { key: 'weeklyDigest',   label: 'Weekly digest',    desc: 'A weekly summary of your account activity' },
                    { key: 'productUpdates', label: 'Product updates',  desc: 'New features and improvements to LinkVault' },
                  ] as { key: keyof typeof notifs; label: string; desc: string }[]).map(item => (
                    <SettingRow key={item.key} title={item.label} desc={item.desc}>
                      <Toggle on={notifs[item.key]} onToggle={() => toggleNotif(item.key)} label={item.label} />
                    </SettingRow>
                  ))}
                  <div className="pt-4">
                    <button
                      id="settings-save-notifs"
                      className="btn-shine flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-semibold text-white"
                      style={{ background: `linear-gradient(135deg, ${accent}, #A78BFA)`, boxShadow: `0 4px 16px ${accent}40` }}
                    >
                      <Save className="w-4 h-4" /> Save Preferences
                    </button>
                  </div>
                </div>
              )}

              {/* ── PRIVACY ── */}
              {activeTab === 'privacy' && (
                <div className="space-y-2">
                  <div className="mb-5">
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">Privacy Settings</h3>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">Control your visibility and data sharing.</p>
                  </div>
                  {([
                    { key: 'publicProfile',  label: 'Public profile',   desc: 'Let others view your public link page at /u/username' },
                    { key: 'onlineStatus',   label: 'Online status',    desc: 'Show when you are active on LinkVault' },
                    { key: 'allowIndexing',  label: 'Search indexing',  desc: 'Allow search engines to index your public profile' },
                    { key: 'shareAnalytics', label: 'Analytics sharing', desc: 'Share anonymized usage data to improve LinkVault' },
                  ] as { key: keyof typeof privacy; label: string; desc: string }[]).map(item => (
                    <SettingRow key={item.key} title={item.label} desc={item.desc}>
                      <Toggle on={privacy[item.key]} onToggle={() => togglePrivacy(item.key)} label={item.label} />
                    </SettingRow>
                  ))}
                  <div className="pt-4">
                    <button
                      id="settings-save-privacy"
                      className="btn-shine flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-semibold text-white"
                      style={{ background: `linear-gradient(135deg, ${accent}, #A78BFA)`, boxShadow: `0 4px 16px ${accent}40` }}
                    >
                      <Save className="w-4 h-4" /> Save Settings
                    </button>
                  </div>
                </div>
              )}

              {/* ── APPEARANCE ── */}
              {activeTab === 'appearance' && (
                <div className="space-y-6">
                  <div>
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">Appearance</h3>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">Personalize your LinkVault experience.</p>
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-3 uppercase tracking-wide">Accent Color</label>
                    <div className="flex flex-wrap gap-3">
                      {accentOptions.map(opt => (
                        <button
                          key={opt.value}
                          id={`accent-${opt.label.toLowerCase()}`}
                          title={opt.label}
                          onClick={() => handleAccentChange(opt.value)}
                          className="w-9 h-9 rounded-full flex items-center justify-center transition-all hover:scale-110"
                          style={{
                            background: opt.value,
                            boxShadow: accent === opt.value ? `0 0 0 3px var(--bg-secondary), 0 0 0 5px ${opt.value}` : 'none',
                          }}
                        >
                          {accent === opt.value && <CheckCircle2 className="w-4 h-4 text-white" />}
                        </button>
                      ))}
                    </div>
                    <p className="text-xs text-[var(--text-muted)] mt-2">Selected: <span className="font-medium" style={{ color: accent }}>{accentOptions.find(o => o.value === accent)?.label ?? 'Custom'}</span></p>
                  </div>

                  <div className="rounded-xl p-4" style={{ background: 'var(--bg-tertiary)', border: '1px solid var(--border-subtle)' }}>
                    <p className="text-sm font-medium text-[var(--text-primary)] mb-1">Preview</p>
                    <p className="text-xs text-[var(--text-muted)] mb-3">See how your accent color looks across UI elements.</p>
                    <div className="flex flex-wrap gap-2">
                      <button className="px-4 py-2 rounded-lg text-sm font-semibold text-white" style={{ background: accent }}>Primary Button</button>
                      <button className="px-4 py-2 rounded-lg text-sm font-medium border" style={{ color: accent, borderColor: accent, background: `${accent}15` }}>Outline Button</button>
                      <span className="px-3 py-1.5 rounded-full text-xs font-semibold" style={{ background: `${accent}20`, color: accent }}>Badge</span>
                    </div>
                  </div>
                </div>
              )}

            </motion.div>
          </AnimatePresence>
        </div>
      </div>
    </div>
  );
}
