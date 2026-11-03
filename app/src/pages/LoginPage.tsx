import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Lock,
  Mail,
  User,
  Eye,
  EyeOff,
  Building2,
  Loader2,
  CheckCircle2,
  XCircle,
  Github,
  Linkedin,
  Shield,
  Sparkles,
} from 'lucide-react';
import { LinkVaultLogo } from '@/components/ui/LinkVaultLogo';
import { authApi, parseJwtPermissions } from '@/api/client';
import { useStore } from '@/store/useStore';
import { appleSpring } from '@/lib/animations';

type Mode = 'login' | 'register';
type AccountKind = 'individual' | 'team';

interface StrengthRule {
  label: string;
  pass: boolean;
}

function evaluatePassword(pw: string): StrengthRule[] {
  return [
    { label: 'Minimum 8 characters', pass: pw.length >= 8 },
    { label: 'One uppercase letter (A–Z)', pass: /[A-Z]/.test(pw) },
    { label: 'One number (0–9)', pass: /[0-9]/.test(pw) },
    { label: 'One special character (!@#…)', pass: /[!@#$%^&*()_+\-={}[\];':"\\|,.<>/?]/.test(pw) },
  ];
}

function strengthScore(rules: StrengthRule[]) {
  return rules.filter((r) => r.pass).length;
}
const strengthLabels = ['', 'Weak', 'Fair', 'Strong', 'Very Strong'];
const strengthColors = ['', '#EF4444', '#F59E0B', '#3B82F6', '#10B981'];

export function LoginPage() {
  const navigate = useNavigate();
  const { setToken, setPermissions, setUser } = useStore();

  const [mode, setMode] = useState<Mode>('login');
  const [accountKind, setAccountKind] = useState<AccountKind>('individual');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showPass, setShowPass] = useState(false);
  const [showRules, setShowRules] = useState(false);
  const [form, setForm] = useState({
    email: '',
    username: '',
    password: '',
    organizationName: '',
    displayName: '',
    bio: '',
  });

  const rules = useMemo(() => evaluatePassword(form.password), [form.password]);
  const score = strengthScore(rules);
  const allRulesMet = score === 4;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm((p) => ({ ...p, [e.target.name]: e.target.value }));
    setError('');
  };

  const handleDemoLogin = async (role: 'Admin' | 'Member' = 'Admin') => {
    setError('');
    setLoading(true);
    try {
      const res = await authApi.demoLogin(role);
      if (res.error) throw new Error(res.error);
      if (res.data?.token) {
        setToken(res.data.token);
        setPermissions(parseJwtPermissions(res.data.token));
        setUser({
          id: String(res.data.userId),
          name: res.data.displayName || res.data.username || (role === 'Admin' ? 'Admin' : 'Member'),
          username: res.data.username || (role === 'Admin' ? 'demo_admin' : 'demo_member'),
          avatar: res.data.avatarUrl || '',
          status: 'online',
          role: res.data.role || role,
          accountType: res.data.accountType || 'Enterprise',
        });
        navigate('/dashboard');
      }
    } catch (err: unknown) {
      setError((err as Error).message || 'Demo login failed.');
    } finally {
      setLoading(false);
    }
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
          setUser({
            id: String(res.data.userId),
            name: res.data.displayName || res.data.username,
            username: res.data.username,
            avatar: res.data.avatarUrl || '',
            status: 'online',
            role: 'User',
            accountType: 'Personal',
          });
          navigate('/dashboard');
        }
      } else {
        const regRes = await authApi.register({
          email: form.email,
          username: form.username,
          password: form.password,
          organizationName: accountKind === 'team' ? form.organizationName || 'Default Team' : undefined,
          displayName: form.displayName || form.username,
        });
        if (regRes.error) throw new Error(regRes.error);

        // Auto-login after registration
        const loginRes = await authApi.login(form.email, form.password);
        if (loginRes.error) throw new Error(loginRes.error);
        if (loginRes.data?.token) {
          setToken(loginRes.data.token);
          setPermissions(parseJwtPermissions(loginRes.data.token));
          setUser({
            id: String(loginRes.data.userId),
            name: loginRes.data.displayName || form.displayName || form.username,
            username: loginRes.data.username || form.username,
            avatar: loginRes.data.avatarUrl || '',
            status: 'online',
            role: 'User',
            accountType: accountKind === 'team' ? 'Enterprise' : 'Personal',
          });
          navigate('/dashboard');
        }
      }
    } catch (err: unknown) {
      setError((err as Error).message || 'Authentication failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-[#F5F5F7] dark:bg-[#000000] text-foreground relative overflow-hidden transition-colors duration-200">
      {/* Ambient background glows */}
      <motion.div
        animate={{ scale: [1, 1.1, 1], opacity: [0.15, 0.25, 0.15] }}
        transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut' }}
        className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] rounded-full blur-[140px] bg-[#0071E3] pointer-events-none"
      />
      <motion.div
        animate={{ scale: [1, 1.15, 1], opacity: [0.1, 0.2, 0.1] }}
        transition={{ duration: 10, repeat: Infinity, ease: 'easeInOut', delay: 2 }}
        className="absolute bottom-10 right-1/4 w-[400px] h-[400px] rounded-full blur-[120px] bg-[#34C759] pointer-events-none"
      />

      {/* Main Glass Card */}
      <motion.div
        initial={{ opacity: 0, y: 20, scale: 0.98 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={appleSpring}
        className="relative z-10 w-full max-w-md bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border border-black/[0.08] dark:border-white/12 rounded-3xl shadow-2xl p-7 sm:p-9"
      >
        {/* Brand Header */}
        <div className="flex flex-col items-center text-center mb-6">
          <div 
            onClick={() => navigate('/')} 
            className="cursor-pointer mb-3 p-2 rounded-2xl hover:scale-105 transition-transform"
          >
            <LinkVaultLogo size={48} showText={false} />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            {mode === 'login' ? 'Welcome Back' : 'Create Your Account'}
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            {mode === 'login'
              ? 'Sign in to access your vaults, files, and AI workspace.'
              : 'Join LinkVault for high-performance bookmark and document management.'}
          </p>
        </div>

        {/* Apple Segmented Control */}
        <div className="flex p-1 mb-6 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] border border-black/[0.04] dark:border-white/[0.04]">
          <button
            type="button"
            onClick={() => {
              setMode('login');
              setError('');
            }}
            className={`relative flex-1 py-1.5 text-xs font-semibold rounded-lg transition-all ${
              mode === 'login'
                ? 'text-[#1D1D1F] dark:text-white shadow-xs'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            {mode === 'login' && (
              <motion.div
                layoutId="login-mode-pill"
                transition={appleSpring}
                className="absolute inset-0 bg-white dark:bg-[#2C2C2E] rounded-lg shadow-sm -z-10"
              />
            )}
            Sign In
          </button>
          <button
            type="button"
            onClick={() => {
              setMode('register');
              setError('');
            }}
            className={`relative flex-1 py-1.5 text-xs font-semibold rounded-lg transition-all ${
              mode === 'register'
                ? 'text-[#1D1D1F] dark:text-white shadow-xs'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            {mode === 'register' && (
              <motion.div
                layoutId="login-mode-pill"
                transition={appleSpring}
                className="absolute inset-0 bg-white dark:bg-[#2C2C2E] rounded-lg shadow-sm -z-10"
              />
            )}
            Register
          </button>
        </div>

        {/* Error Alert */}
        {error && (
          <motion.div
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            className="mb-5 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-xs text-red-500 flex items-center gap-2"
          >
            <XCircle className="w-4 h-4 flex-shrink-0" />
            <span>{error}</span>
          </motion.div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'register' && (
            <>
              <div>
                <label className="block text-xs font-medium text-muted-foreground mb-1">Username</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                  <input
                    name="username"
                    type="text"
                    required
                    value={form.username}
                    onChange={handleChange}
                    placeholder="johndoe"
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3] transition-colors"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-muted-foreground mb-1">Display Name</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                  <input
                    name="displayName"
                    type="text"
                    value={form.displayName}
                    onChange={handleChange}
                    placeholder="John Doe"
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3] transition-colors"
                  />
                </div>
              </div>
            </>
          )}

          <div>
            <label className="block text-xs font-medium text-muted-foreground mb-1">
              {mode === 'login' ? 'Email or Username' : 'Email Address'}
            </label>
            <div className="relative">
              <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <input
                name="email"
                type="text"
                required
                value={form.email}
                onChange={handleChange}
                placeholder={mode === 'login' ? 'admin@linkvault.local' : 'john@example.com'}
                className="w-full pl-9 pr-3 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3] transition-colors"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-muted-foreground mb-1">Password</label>
            <div className="relative">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <input
                name="password"
                type={showPass ? 'text' : 'password'}
                required
                value={form.password}
                onChange={handleChange}
                onFocus={() => mode === 'register' && setShowRules(true)}
                placeholder="••••••••"
                className="w-full pl-9 pr-9 py-2 text-xs rounded-xl bg-black/[0.03] dark:bg-white/[0.05] border border-black/[0.08] dark:border-white/10 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:border-[#0071E3] transition-colors"
              />
              <button
                type="button"
                onClick={() => setShowPass(!showPass)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {showPass ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>

            {mode === 'register' && form.password && (
              <div className="mt-2 space-y-1.5">
                <div className="flex gap-1 h-1 w-full bg-black/5 dark:bg-white/10 rounded-full overflow-hidden">
                  {[1, 2, 3, 4].map((step) => (
                    <div
                      key={step}
                      className="flex-1 transition-colors duration-300"
                      style={{
                        backgroundColor: score >= step ? strengthColors[score] : 'transparent',
                      }}
                    />
                  ))}
                </div>
                <div className="flex justify-between items-center text-[11px]">
                  <span className="text-muted-foreground">Strength:</span>
                  <span style={{ color: strengthColors[score] }} className="font-semibold">
                    {strengthLabels[score]}
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Submit Button */}
          <button
            type="submit"
            disabled={loading}
            className="w-full mt-2 py-2.5 px-4 rounded-xl bg-[#0071E3] hover:bg-[#0077ED] text-white text-xs font-semibold shadow-md transition-all active:scale-[0.98] disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
            <span>{mode === 'login' ? 'Sign In to LinkVault' : 'Create Account'}</span>
          </button>
        </form>

        {/* Demo One-Click Access (Admin & Member) */}
        <div className="mt-6 pt-5 border-t border-black/[0.06] dark:border-white/10">
          <div className="flex items-center justify-between mb-2.5">
            <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
              <Sparkles className="w-3.5 h-3.5 text-[#0071E3]" />
              Quick Demo Access
            </span>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => handleDemoLogin('Admin')}
              disabled={loading}
              className="py-1.5 px-3 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] hover:bg-black/[0.07] dark:hover:bg-white/[0.1] text-foreground text-xs font-medium transition-all border border-black/[0.04] dark:border-white/06"
            >
              Demo Admin
            </button>
            <button
              type="button"
              onClick={() => handleDemoLogin('Member')}
              disabled={loading}
              className="py-1.5 px-3 rounded-xl bg-black/[0.04] dark:bg-white/[0.06] hover:bg-black/[0.07] dark:hover:bg-white/[0.1] text-foreground text-xs font-medium transition-all border border-black/[0.04] dark:border-white/06"
            >
              Demo Member
            </button>
          </div>
        </div>

        {/* Back to Home Link */}
        <div className="text-center mt-5">
          <button
            onClick={() => navigate('/')}
            className="text-xs text-muted-foreground hover:text-foreground transition-colors"
          >
            ← Back to Overview
          </button>
        </div>
      </motion.div>
    </div>
  );
}
