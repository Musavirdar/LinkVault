import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Lock, Mail, User, Eye, EyeOff, Building2,
    Loader2, CheckCircle2, XCircle, Github,
    Users, UserCircle, Linkedin, Folder,
    FolderKanban, Share2, Sparkles, FolderLock
} from 'lucide-react';
import { authApi, API_BASE_URL, parseJwtPermissions } from '@/api/client';
import { useStore } from '@/store/useStore';

import { cn } from '@/lib/utils';

type Mode = 'login' | 'register';
type AccountKind = 'individual' | 'team';

/* ---------- password strength ---------- */
interface StrengthRule { label: string; pass: boolean }
function evaluatePassword(pw: string): StrengthRule[] {
    return [
        { label: 'Minimum 8 characters', pass: pw.length >= 8 },
        { label: 'One uppercase letter (A–Z)', pass: /[A-Z]/.test(pw) },
        { label: 'One number (0–9)', pass: /[0-9]/.test(pw) },
        { label: 'One special character (!@#…)', pass: /[!@#$%^&*()_+\-={}[\];':"\\|,.<>/?]/.test(pw) },
    ];
}
function strengthScore(rules: StrengthRule[]) { return rules.filter(r => r.pass).length; }
const strengthLabels = ['', 'Weak', 'Fair', 'Strong', 'Very Strong'];
const strengthColors = ['', '#EF4444', '#F59E0B', '#3B82F6', '#10B981'];

/* ---------- component ---------- */
export function LoginPage() {
    const navigate = useNavigate();
    const { setToken, setPermissions } = useStore();

    const [mode, setMode] = useState<Mode>('login');
    const [accountKind, setAccountKind] = useState<AccountKind>('individual');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [showPass, setShowPass] = useState(false);
    const [showRules, setShowRules] = useState(false);
    const [form, setForm] = useState({
        email: '', username: '', password: '',
        organizationName: '', displayName: '', bio: '',
    });

    const rules = useMemo(() => evaluatePassword(form.password), [form.password]);
    const score = strengthScore(rules);
    const allRulesMet = score === 4;

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm(p => ({ ...p, [e.target.name]: e.target.value }));
        setError('');
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (mode === 'register' && !allRulesMet) {
            setError('Please meet all password requirements.');
            return;
        }

        setError('');
        setLoading(true);
        try {
            if (mode === 'login') {
                const res = await authApi.login(form.email, form.password);
                if (res.error) throw new Error(res.error);
                if (res.data?.token) {
                    setToken(res.data.token);
                    setPermissions(parseJwtPermissions(res.data.token));
                }
                navigate('/dashboard');

            } else {
                const regRes = await authApi.register({
                    email: form.email,
                    username: form.username,
                    password: form.password,
                    organizationName: accountKind === 'team' ? form.organizationName : undefined,
                });
                if (regRes.error) throw new Error(regRes.error);

                // Auto-login after successful registration
                const loginRes = await authApi.login(form.email, form.password);
                if (loginRes.error) throw new Error(loginRes.error);
                if (loginRes.data?.token) {
                    setToken(loginRes.data.token);
                    setPermissions(parseJwtPermissions(loginRes.data.token));
                }
                navigate('/dashboard');

            }
        } catch (err: unknown) {
            setError((err as Error).message || 'Authentication failed. Please check your credentials.');
        } finally {
            setLoading(false);
        }
    };

    const handleSso = (provider: 'google' | 'github' | 'linkedin') => {
        window.location.href = `${API_BASE_URL}/auth/${provider}`;
    };

    return (
        <div className="min-h-screen grid lg:grid-cols-2 overflow-hidden bg-[var(--bg-primary)]">

            {/* ── LEFT SIDE: BRAND & GRAPHICS ── */}
            <div className="hidden lg:flex flex-col items-center justify-center relative overflow-hidden"
                style={{
                    background: 'radial-gradient(circle at center, color-mix(in srgb, var(--accent-primary) 15%, transparent), transparent 70%)',
                    borderRight: '1px solid var(--border-subtle)'
                }}>

                {/* animated background orbs */}
                <motion.div
                    animate={{ x: [0, 40, 0], y: [0, -30, 0], scale: [1, 1.1, 1] }}
                    transition={{ duration: 14, repeat: Infinity, ease: 'easeInOut' }}
                    className="absolute -top-32 -left-32 w-[480px] h-[480px] rounded-full blur-[100px]"
                    style={{ background: '#5865F230' }}
                />
                <motion.div
                    animate={{ x: [0, -50, 0], y: [0, 40, 0], scale: [1, 1.15, 1] }}
                    transition={{ duration: 18, repeat: Infinity, ease: 'easeInOut', delay: 3 }}
                    className="absolute -bottom-32 -right-32 w-[520px] h-[520px] rounded-full blur-[100px]"
                    style={{ background: '#8B5CF230' }}
                />

                {/* Grid Overlay */}
                <div className="absolute inset-0 opacity-[0.03]"
                    style={{ backgroundImage: 'linear-gradient(var(--text-primary) 1px, transparent 1px), linear-gradient(90deg, var(--text-primary) 1px, transparent 1px)', backgroundSize: '40px 40px' }} />

                {/* ── 3D Floating Folders Graphic ── */}
                <div className="relative z-10 w-full max-w-lg mb-12 flex items-center justify-center h-80">

                    {/* Center Focus: Custom Logo */}
                    <motion.div
                        initial={{ scale: 0, opacity: 0 }}
                        animate={{ scale: 1, opacity: 1 }}
                        transition={{ duration: 0.8, type: 'spring' }}
                        className="absolute z-30 w-32 h-32 rounded-3xl flex items-center justify-center shadow-[0_20px_60px_rgba(88,101,242,0.4)] overflow-hidden"
                    >
                        <img src="/logo.png" alt="LinkVault Logo" className="w-full h-full object-cover" />
                    </motion.div>

                    {/* Floating Folder 1: Top Left */}
                    <motion.div
                        animate={{ y: [0, -15, 0], rotate: [-5, -8, -5] }}
                        transition={{ duration: 6, repeat: Infinity, ease: 'easeInOut' }}
                        className="absolute z-20 top-8 left-12 w-32 h-32 rounded-2xl flex items-center justify-center backdrop-blur-md shadow-xl"
                        style={{
                            background: 'color-mix(in srgb, #EC4899 15%, var(--bg-secondary) 85%)',
                            border: '1px solid color-mix(in srgb, #EC4899 30%, transparent)'
                        }}
                    >
                        <Folder className="w-14 h-14 text-pink-500 opacity-80" />
                    </motion.div>

                    {/* Floating Folder 2: Bottom Right */}
                    <motion.div
                        animate={{ y: [0, 20, 0], rotate: [5, 12, 5] }}
                        transition={{ duration: 7, repeat: Infinity, ease: 'easeInOut', delay: 1 }}
                        className="absolute z-20 bottom-8 right-12 w-36 h-36 rounded-2xl flex items-center justify-center backdrop-blur-md shadow-xl"
                        style={{
                            background: 'color-mix(in srgb, #10B981 15%, var(--bg-secondary) 85%)',
                            border: '1px solid color-mix(in srgb, #10B981 30%, transparent)'
                        }}
                    >
                        <FolderKanban className="w-16 h-16 text-emerald-500 opacity-80" />
                    </motion.div>

                    {/* Floating Element 3: Top Right */}
                    <motion.div
                        animate={{ y: [0, 10, 0], x: [0, 10, 0], rotate: [10, 20, 10] }}
                        transition={{ duration: 5, repeat: Infinity, ease: 'easeInOut', delay: 2 }}
                        className="absolute z-10 top-0 right-20 w-16 h-16 rounded-xl flex items-center justify-center backdrop-blur-md shadow-lg"
                        style={{
                            background: 'color-mix(in srgb, #F59E0B 20%, var(--bg-secondary) 80%)',
                            border: '1px solid color-mix(in srgb, #F59E0B 40%, transparent)'
                        }}
                    >
                        <Share2 className="w-8 h-8 text-amber-500 opacity-90" />
                    </motion.div>

                    {/* Floating Element 4: Bottom Left */}
                    <motion.div
                        animate={{ y: [0, -20, 0], x: [0, -15, 0], rotate: [-15, -25, -15] }}
                        transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut', delay: 1.5 }}
                        className="absolute z-10 bottom-4 left-24 w-20 h-20 rounded-2xl flex items-center justify-center backdrop-blur-md shadow-lg"
                        style={{
                            background: 'color-mix(in srgb, #3B82F6 20%, var(--bg-secondary) 80%)',
                            border: '1px solid color-mix(in srgb, #3B82F6 40%, transparent)'
                        }}
                    >
                        <FolderLock className="w-10 h-10 text-blue-500 opacity-90" />
                    </motion.div>

                    {/* Sparkles */}
                    <Sparkles className="absolute top-1/4 right-1/4 w-6 h-6 text-yellow-400 opacity-50 animate-pulse" />
                    <Sparkles className="absolute bottom-1/4 left-1/4 w-8 h-8 text-purple-400 opacity-50 animate-pulse delay-700" />
                </div>

                <div className="relative z-10 text-center px-10">
                    <h2 className="text-4xl font-extrabold text-[var(--text-primary)] mb-4 tracking-tight">
                        Organize your digital life.
                    </h2>
                    <p className="text-lg text-[var(--text-secondary)] max-w-md mx-auto leading-relaxed">
                        LinkVault provides a unified workspace for your links, files, and team directories. Secure, fast, and beautiful.
                    </p>
                </div>
            </div>

            {/* ── RIGHT SIDE: LOGIN FORM ── */}
            <div className="flex items-center justify-center relative p-4 sm:p-8">

                {/* Mobile orbs (only visible on small screens) */}
                <div className="lg:hidden absolute inset-0 pointer-events-none overflow-hidden">
                    <motion.div
                        animate={{ x: [0, 40, 0], y: [0, -30, 0], scale: [1, 1.1, 1] }}
                        transition={{ duration: 14, repeat: Infinity, ease: 'easeInOut' }}
                        className="absolute -top-32 -left-32 w-[480px] h-[480px] rounded-full blur-3xl opacity-50"
                        style={{ background: 'radial-gradient(circle, #5865F220, transparent 70%)' }}
                    />
                </div>

                <motion.div
                    initial={{ opacity: 0, y: 32, scale: 0.96 }}
                    animate={{ opacity: 1, y: 0, scale: 1 }}
                    transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
                    className="relative z-10 w-full max-w-md"
                >
                    {/* SSO coming-soon toast removed as we now have real SSO buttons */}

                    {/* glass card */}
                    <div className="rounded-3xl overflow-hidden shadow-2xl"
                        style={{
                            background: 'color-mix(in srgb, var(--bg-secondary) 85%, transparent)',
                            backdropFilter: 'blur(24px)',
                            border: '1px solid color-mix(in srgb, var(--border-strong) 60%, transparent)',
                        }}>

                        {/* gradient top bar */}
                        <div className="h-1.5 w-full" style={{ background: 'linear-gradient(90deg, #5865F2, #8B5CF6, #EC4899)' }} />

                        <div className="p-8 sm:p-10">
                            {/* Logo for mobile only */}
                            <div className="lg:hidden flex flex-col items-center mb-7">
                                <img src="/logo.png" alt="LinkVault Logo" className="w-16 h-16 rounded-2xl mb-3 shadow-[0_8px_24px_rgba(88,101,242,0.4)]" />
                                <h1 className="text-xl font-bold text-[var(--text-primary)]">LinkVault</h1>
                            </div>

                            <div className="hidden lg:block mb-8">
                                <h1 className="text-2xl font-bold text-[var(--text-primary)] mb-1">
                                    {mode === 'login' ? 'Welcome back' : 'Create an account'}
                                </h1>
                                <p className="text-sm text-[var(--text-muted)]">
                                    {mode === 'login' ? 'Enter your details to sign in.' : 'Get started with your free workspace.'}
                                </p>
                            </div>

                            {/* SSO Buttons */}
                            <div className="grid grid-cols-3 gap-3 mb-6">
                                <SsoButton onClick={() => handleSso('google')} title="Google">
                                    <svg className="w-5 h-5" viewBox="0 0 24 24">
                                        <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                                        <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                                        <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
                                        <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
                                    </svg>
                                </SsoButton>
                                <SsoButton onClick={() => handleSso('github')} title="GitHub">
                                    <Github className="w-5 h-5 text-[var(--text-primary)]" />
                                </SsoButton>
                                <SsoButton onClick={() => handleSso('linkedin')} title="LinkedIn">
                                    <Linkedin className="w-5 h-5 text-[#0A66C2]" />
                                </SsoButton>
                            </div>

                            {/* Divider */}
                            <div className="relative flex items-center gap-3 mb-6">
                                <div className="flex-1 h-px bg-[var(--border-strong)]" />
                                <span className="text-xs text-[var(--text-muted)] font-medium uppercase tracking-wider">or email</span>
                                <div className="flex-1 h-px bg-[var(--border-strong)]" />
                            </div>

                            {/* Tab toggle */}
                            <div className="flex gap-1 mb-6 p-1 rounded-xl bg-[var(--bg-tertiary)] border border-[var(--border-subtle)]">
                                {(['login', 'register'] as Mode[]).map(m => (
                                    <motion.button
                                        key={m}
                                        onClick={() => { setMode(m); setError(''); setShowRules(false); setAccountKind('individual'); setForm({ email: '', username: '', password: '', organizationName: '', displayName: '', bio: '' }); }}
                                        className="flex-1 py-2 rounded-lg text-sm font-semibold transition-colors relative"
                                        style={{ color: mode === m ? 'white' : 'var(--text-secondary)' }}
                                    >
                                        {mode === m && (
                                            <motion.div layoutId="tab-pill" className="absolute inset-0 rounded-lg shadow-sm"
                                                style={{ background: 'linear-gradient(135deg, #5865F2, #7C3AED)' }} />
                                        )}
                                        <span className="relative z-10">{m === 'login' ? 'Sign In' : 'Sign Up'}</span>
                                    </motion.button>
                                ))}
                            </div>

                            {/* Account-type picker (register only) */}
                            <AnimatePresence>
                                {mode === 'register' && (
                                    <motion.div
                                        initial={{ opacity: 0, height: 0 }}
                                        animate={{ opacity: 1, height: 'auto' }}
                                        exit={{ opacity: 0, height: 0 }}
                                        className="overflow-hidden mb-5"
                                    >
                                        <p className="text-xs font-medium text-[var(--text-secondary)] mb-2">Account Type</p>
                                        <div className="grid grid-cols-2 gap-2">
                                            {([['individual', 'Individual', UserCircle], ['team', 'Team / Org', Users]] as const).map(([k, label, Icon]) => (
                                                <button
                                                    key={k} type="button"
                                                    onClick={() => setAccountKind(k)}
                                                    className="flex items-center justify-center gap-2 px-3 py-2.5 rounded-xl text-sm font-medium border transition-all"
                                                    style={{
                                                        background: accountKind === k ? '#5865F215' : 'transparent',
                                                        border: `1px solid ${accountKind === k ? '#5865F2' : 'var(--border-strong)'}`,
                                                        color: accountKind === k ? '#5865F2' : 'var(--text-secondary)',
                                                    }}
                                                >
                                                    <Icon className="w-4 h-4" />{label}
                                                </button>
                                            ))}
                                        </div>
                                    </motion.div>
                                )}
                            </AnimatePresence>

                            <form onSubmit={handleSubmit} className="space-y-4">
                                <Field icon={<Mail className="w-4 h-4" />} label="Email address" name="email" type="email" value={form.email} onChange={handleChange} placeholder="you@example.com" required />

                                <AnimatePresence>
                                    {mode === 'register' && (
                                        <motion.div initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} className="overflow-hidden space-y-4 pt-1">
                                            <Field icon={<User className="w-4 h-4" />} label="Username" name="username" type="text" value={form.username} onChange={handleChange} placeholder="yourhandle" required />

                                            {accountKind === 'team' && (
                                                <Field icon={<Building2 className="w-4 h-4" />} label="Organization Name" name="organizationName" type="text" value={form.organizationName} onChange={handleChange} placeholder="Acme Corp" required />
                                            )}
                                        </motion.div>
                                    )}
                                </AnimatePresence>

                                <div>
                                    <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1.5">Password</label>
                                    <div className="relative">
                                        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"><Lock className="w-4 h-4" /></span>
                                        <input
                                            name="password" type={showPass ? "text" : "password"} value={form.password}
                                            onChange={handleChange}
                                            onFocus={() => setShowRules(true)}
                                            placeholder="••••••••" required
                                            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
                                            className="w-full pl-9 pr-10 py-2.5 rounded-xl text-sm transition-all outline-none"

                                            style={{ background: 'var(--bg-primary)', border: '1px solid var(--border-strong)', color: 'var(--text-primary)' }}
                                            onFocusCapture={e => { e.currentTarget.style.borderColor = '#5865F2'; e.currentTarget.style.boxShadow = '0 0 0 3px #5865F220'; }}
                                            onBlurCapture={e => { e.currentTarget.style.borderColor = ''; e.currentTarget.style.boxShadow = ''; }}
                                        />
                                        <button
                                            type="button"
                                            onClick={() => setShowPass(!showPass)}
                                            aria-label={showPass ? 'Hide password' : 'Show password'}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)] hover:text-[var(--text-primary)] transition-colors"
                                        >
                                            {showPass ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                        </button>

                                    </div>

                                    {/* Password Strength Widget */}
                                    <AnimatePresence>
                                        {mode === 'register' && showRules && form.password.length > 0 && (
                                            <motion.div
                                                initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }}
                                                className="mt-3 p-4 rounded-xl border border-[var(--border-strong)] bg-[var(--bg-tertiary)] overflow-hidden"
                                            >
                                                {/* score bar */}
                                                <div className="flex gap-1 mb-3">
                                                    {[1, 2, 3, 4].map(idx => (
                                                        <div key={idx} className="h-1.5 flex-1 rounded-full transition-all duration-300"
                                                            style={{ background: idx <= score ? strengthColors[score] : 'var(--border-strong)' }} />
                                                    ))}
                                                </div>
                                                {score > 0 && (
                                                    <p className="text-[11px] mb-2 font-semibold" style={{ color: strengthColors[score] }}>
                                                        {strengthLabels[score]}
                                                    </p>
                                                )}
                                                {/* rules */}
                                                <div className="space-y-1.5">
                                                    {rules.map(r => (
                                                        <div key={r.label} className="flex items-center gap-2">
                                                            {r.pass
                                                                ? <CheckCircle2 className="w-3.5 h-3.5 text-green-400 flex-shrink-0" />
                                                                : <XCircle className="w-3.5 h-3.5 text-[var(--text-muted)] flex-shrink-0" />
                                                            }
                                                            <span className={cn('text-[11px]', r.pass ? 'text-green-400' : 'text-[var(--text-muted)]')}>{r.label}</span>
                                                        </div>
                                                    ))}
                                                </div>
                                            </motion.div>
                                        )}
                                    </AnimatePresence>
                                </div>

                                {/* Error */}
                                <AnimatePresence>
                                    {error && (
                                        <motion.p initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}
                                            className="text-xs px-4 py-3 rounded-xl font-medium"
                                            style={{ color: '#F87171', background: '#EF444415', border: '1px solid #EF444430' }}>
                                            {error}
                                        </motion.p>
                                    )}
                                </AnimatePresence>

                                {/* Submit */}
                                <motion.button
                                    whileHover={{ scale: 1.01 }}
                                    whileTap={{ scale: 0.98 }}
                                    type="submit"
                                    disabled={loading}
                                    className="w-full mt-2 py-3 rounded-xl font-semibold text-sm text-white flex items-center justify-center gap-2 transition-all shadow-lg"
                                    style={{
                                        background: 'linear-gradient(135deg, #5865F2, #7C3AED)',
                                        boxShadow: '0 4px 20px #5865F240',
                                        opacity: loading ? 0.7 : 1,
                                    }}
                                >
                                    {loading && <Loader2 className="w-4 h-4 animate-spin" />}
                                    {mode === 'login' ? 'Sign In to LinkVault' : 'Create Account'}
                                </motion.button>
                            </form>

                            {/* Footer */}
                            <div className="mt-8 text-center sm:text-left flex flex-col sm:flex-row items-center justify-between text-xs text-[var(--text-muted)]">
                                <p>
                                    {mode === 'login'
                                        ? <>No account? <button onClick={() => setMode('register')} className="text-[var(--accent-primary)] font-semibold hover:underline ml-1">Sign up free</button></>
                                        : <>Already have an account? <button onClick={() => setMode('login')} className="text-[var(--accent-primary)] font-semibold hover:underline ml-1">Sign in</button></>
                                    }
                                </p>
                                <p className="mt-4 sm:mt-0 font-medium">Secured by LinkVault SSO</p>
                            </div>
                        </div>
                    </div>
                </motion.div>
            </div>
        </div>
    );
}

/* ── helpers ── */
function SsoButton({ onClick, title, children }: { onClick: () => void; title: string; children: React.ReactNode }) {
    return (
        <motion.button
            whileHover={{ scale: 1.03, y: -2 }}
            whileTap={{ scale: 0.97 }}
            type="button" onClick={onClick} title={title}
            className="flex items-center justify-center py-3 rounded-xl transition-all shadow-sm"
            style={{
                background: 'var(--bg-primary)',
                border: '1px solid var(--border-strong)',
            }}
        >
            {children}
        </motion.button>
    );
}

function Field({ icon, label, name, type, value, onChange, placeholder, required, autoComplete }: {
    icon: React.ReactNode; label: string; name: string; type: string;
    value: string; onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
    placeholder?: string; required?: boolean; autoComplete?: string;
}) {
    return (
        <div>
            <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">{label}</label>
            <div className="relative">
                <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-[var(--text-muted)]">{icon}</span>
                <input
                    name={name} type={type} value={value} onChange={onChange}
                    placeholder={placeholder} required={required}
                    autoComplete={autoComplete ?? (type === 'email' ? 'email' : type === 'password' ? 'current-password' : 'off')}
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl text-sm transition-all outline-none"
                    style={{ background: 'var(--bg-primary)', border: '1px solid var(--border-strong)', color: 'var(--text-primary)' }}
                    onFocus={e => { e.currentTarget.style.borderColor = '#5865F2'; e.currentTarget.style.boxShadow = '0 0 0 3px #5865F220'; }}
                    onBlur={e => { e.currentTarget.style.borderColor = ''; e.currentTarget.style.boxShadow = ''; }}
                />
            </div>
        </div>
    );
}

