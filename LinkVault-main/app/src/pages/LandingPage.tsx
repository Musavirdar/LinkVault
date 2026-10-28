import { useNavigate } from 'react-router-dom';
import { motion, useScroll, useTransform, type Variants } from 'framer-motion';
import { useRef } from 'react';
import {
  Link2, FolderOpen, BarChart3, Shield, Users, Zap,
  ArrowRight, Star, ChevronDown, Share2, Lock, Globe,
  TrendingUp, FileText, Folder, BookOpen, Layers,
  CheckCircle, UserCircle, Building2
} from 'lucide-react';

/* ─── mock public content grid items ─── */
const GRID_ITEMS = [
  { id: 1, type: 'link', title: 'Design Resources Hub', desc: 'Curated Figma, icons & fonts', views: '12.4k', color: '#5865F2', icon: Link2 },
  { id: 2, type: 'doc', title: 'API Integration Guide', desc: 'Step-by-step REST docs', views: '8.1k', color: '#10B981', icon: FileText },
  { id: 3, type: 'folder', title: 'Brand Assets 2024', desc: 'Logos, colors, typography', views: '5.3k', color: '#F59E0B', icon: Folder },
  { id: 4, type: 'link', title: 'SaaS Marketing Toolkit', desc: 'Templates & copy resources', views: '21.7k', color: '#EC4899', icon: Link2 },
  { id: 5, type: 'doc', title: 'Engineering Handbook', desc: 'Best practices & runbooks', views: '9.8k', color: '#8B5CF6', icon: BookOpen },
  { id: 6, type: 'folder', title: 'Open Source Libraries', desc: 'Curated JS/TS libraries', views: '33.2k', color: '#06B6D4', icon: Layers },
  { id: 7, type: 'link', title: 'Startup Resources', desc: 'Pitch decks & playbooks', views: '14.0k', color: '#EF4444', icon: Link2 },
  { id: 8, type: 'doc', title: 'UX Research Reports', desc: 'User interviews & findings', views: '6.5k', color: '#84CC16', icon: FileText },
  { id: 9, type: 'folder', title: 'Product Screenshots', desc: 'App previews & demos', views: '4.2k', color: '#F97316', icon: Folder },
];

const FEATURES = [
  {
    icon: Link2, color: '#5865F2',
    title: 'Smart Link Management',
    desc: 'Organize, tag, and share your links with powerful filtering and analytics.'
  },
  {
    icon: FolderOpen, color: '#10B981',
    title: 'Doc Share Tree',
    desc: 'Build rich directory trees for your documents and share them instantly.'
  },
  {
    icon: BarChart3, color: '#F59E0B',
    title: 'Deep Analytics',
    desc: 'Track clicks, views, and engagement across all your shared content.'
  },
  {
    icon: Shield, color: '#EC4899',
    title: 'Enterprise Security',
    desc: 'Role-based access control, audit logs, and SSO for your whole team.'
  },
  {
    icon: Users, color: '#8B5CF6',
    title: 'Team Workspaces',
    desc: 'Collaborate with your team in shared workspaces with real-time sync.'
  },
  {
    icon: Zap, color: '#06B6D4',
    title: 'Lightning Fast',
    desc: 'Instant search, real-time updates, and a snappy experience everywhere.'
  },
];

const PLANS = [
  {
    name: 'Individual',
    icon: UserCircle,
    price: 'Free',
    color: '#5865F2',
    desc: 'Perfect for personal link & doc sharing',
    features: ['Link management', 'Doc Share Tree', 'Public sharing', '5 GB storage'],
    cta: 'Get Started Free',
  },
  {
    name: 'Team',
    icon: Building2,
    price: '$12',
    per: '/mo per user',
    color: '#8B5CF6',
    desc: 'Everything for your whole organisation',
    features: ['Everything in Individual', 'Full analytics & audit logs', 'Team workspaces & chat', 'Unlimited storage', 'Priority support', 'SSO & advanced security'],
    cta: 'Start Team Trial',
    popular: true,
  },
];

const STATS = [
  { value: '2M+', label: 'Links Managed' },
  { value: '50K+', label: 'Active Users' },
  { value: '99.9%', label: 'Uptime SLA' },
  { value: '150+', label: 'Countries' },
];

/* ─── animation variants ─── */
const fadeUp: Variants = {
  hidden: { opacity: 0, y: 32 },
  show: { opacity: 1, y: 0, transition: { duration: 0.6, ease: 'easeOut' } },
};
const stagger: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.08 } },
};

export function LandingPage() {
  const navigate = useNavigate();
  const heroRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({ target: heroRef });
  const heroY = useTransform(scrollYProgress, [0, 1], [0, -80]);
  const heroOpacity = useTransform(scrollYProgress, [0, 0.8], [1, 0]);

  return (
    <div
      className="min-h-screen overflow-x-hidden"
      style={{ background: 'var(--bg-primary)', color: 'var(--text-primary)' }}
    >
      {/* ── NAVBAR ── */}
      <nav
        className="fixed top-0 inset-x-0 z-50 flex items-center justify-between px-6 md:px-10 h-16 backdrop-blur-xl"
        style={{
          background: 'color-mix(in srgb, var(--bg-primary) 80%, transparent)',
          borderBottom: '1px solid var(--border-subtle)',
        }}
      >
        <div className="flex items-center gap-2.5">
          <img src="/logo.png" alt="LinkVault" className="w-8 h-8 rounded-lg" />
          <span className="font-bold text-lg tracking-tight" style={{ color: 'var(--text-primary)' }}>
            Link<span style={{ color: '#5865F2' }}>Vault</span>
          </span>
        </div>

        <div className="hidden md:flex items-center gap-6 text-sm font-medium" style={{ color: 'var(--text-secondary)' }}>
          <a href="#features" className="hover:text-white transition-colors">Features</a>
          <a href="#explore" className="hover:text-white transition-colors">Explore</a>
          <a href="#pricing" className="hover:text-white transition-colors">Pricing</a>
        </div>

        <div className="flex items-center gap-3">
          <motion.button
            whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.97 }}
            onClick={() => navigate('/login')}
            className="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
            style={{ color: 'var(--text-secondary)', border: '1px solid var(--border-strong)' }}
          >
            Sign In
          </motion.button>
          <motion.button
            whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.97 }}
            onClick={() => navigate('/login')}
            className="px-4 py-2 rounded-lg text-sm font-semibold text-white"
            style={{ background: 'linear-gradient(135deg, #5865F2, #7C3AED)', boxShadow: '0 4px 16px #5865F240' }}
          >
            Get Started
          </motion.button>
        </div>
      </nav>

      {/* ── HERO ── */}
      <section ref={heroRef} className="relative min-h-screen flex items-center justify-center overflow-hidden pt-16">
        {/* Background orbs */}
        <motion.div
          animate={{ x: [0, 60, 0], y: [0, -40, 0], scale: [1, 1.15, 1] }}
          transition={{ duration: 16, repeat: Infinity, ease: 'easeInOut' }}
          className="absolute -top-40 -left-40 w-[600px] h-[600px] rounded-full blur-[120px] pointer-events-none"
          style={{ background: '#5865F218' }}
        />
        <motion.div
          animate={{ x: [0, -60, 0], y: [0, 50, 0], scale: [1, 1.1, 1] }}
          transition={{ duration: 20, repeat: Infinity, ease: 'easeInOut', delay: 4 }}
          className="absolute -bottom-40 -right-40 w-[700px] h-[700px] rounded-full blur-[120px] pointer-events-none"
          style={{ background: '#7C3AED18' }}
        />
        {/* Grid overlay */}
        <div
          className="absolute inset-0 opacity-[0.025] pointer-events-none"
          style={{ backgroundImage: 'linear-gradient(var(--text-primary) 1px, transparent 1px), linear-gradient(90deg, var(--text-primary) 1px, transparent 1px)', backgroundSize: '48px 48px' }}
        />

        <motion.div
          style={{ y: heroY, opacity: heroOpacity }}
          className="relative z-10 text-center px-6 max-w-5xl mx-auto"
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.5 }}
            className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold mb-8"
            style={{ background: '#5865F215', border: '1px solid #5865F230', color: '#818CF8' }}
          >
            <Star className="w-3.5 h-3.5" />
            Now with real-time team collaboration
          </motion.div>

          <motion.h1
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.1, ease: 'easeOut' }}
            className="text-5xl md:text-7xl font-extrabold tracking-tight mb-6 leading-none"
          >
            Your digital world,{' '}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: 'linear-gradient(135deg, #5865F2, #8B5CF6, #EC4899)' }}
            >
              organised.
            </span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.2 }}
            className="text-lg md:text-xl max-w-2xl mx-auto leading-relaxed mb-10"
            style={{ color: 'var(--text-secondary)' }}
          >
            LinkVault is the all-in-one workspace for managing links, sharing documents, and collaborating with your team. Beautiful. Fast. Secure.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.3 }}
            className="flex flex-col sm:flex-row items-center justify-center gap-4"
          >
            <motion.button
              whileHover={{ scale: 1.04, boxShadow: '0 8px 32px #5865F250' }}
              whileTap={{ scale: 0.97 }}
              onClick={() => navigate('/login')}
              className="flex items-center gap-2 px-7 py-4 rounded-2xl font-semibold text-base text-white"
              style={{ background: 'linear-gradient(135deg, #5865F2, #7C3AED)', boxShadow: '0 4px 24px #5865F240' }}
            >
              Start for free
              <ArrowRight className="w-4 h-4" />
            </motion.button>
            <motion.button
              whileHover={{ scale: 1.03 }}
              whileTap={{ scale: 0.97 }}
              onClick={() => document.getElementById('explore')?.scrollIntoView({ behavior: 'smooth' })}
              className="flex items-center gap-2 px-7 py-4 rounded-2xl font-semibold text-base"
              style={{ color: 'var(--text-secondary)', border: '1px solid var(--border-strong)' }}
            >
              <Globe className="w-4 h-4" />
              Explore public content
            </motion.button>
          </motion.div>

          {/* Stats bar */}
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.5 }}
            className="mt-16 flex flex-wrap items-center justify-center gap-8 md:gap-12"
          >
            {STATS.map((s) => (
              <div key={s.label} className="text-center">
                <div className="text-2xl md:text-3xl font-extrabold" style={{ color: 'var(--text-primary)' }}>{s.value}</div>
                <div className="text-xs font-medium mt-0.5" style={{ color: 'var(--text-muted)' }}>{s.label}</div>
              </div>
            ))}
          </motion.div>
        </motion.div>

        {/* Scroll indicator */}
        <motion.div
          animate={{ y: [0, 8, 0] }}
          transition={{ duration: 2, repeat: Infinity }}
          className="absolute bottom-8 left-1/2 -translate-x-1/2"
          style={{ color: 'var(--text-muted)' }}
        >
          <ChevronDown className="w-6 h-6" />
        </motion.div>
      </section>

      {/* ── EXPLORE / INSTAGRAM GRID ── */}
      <section id="explore" className="py-24 px-6">
        <motion.div
          variants={stagger}
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, margin: '-80px' }}
          className="max-w-6xl mx-auto"
        >
          <motion.div variants={fadeUp} className="text-center mb-14">
            <div
              className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold mb-4"
              style={{ background: '#10B98115', border: '1px solid #10B98130', color: '#34D399' }}
            >
              <Share2 className="w-3.5 h-3.5" />
              Public Content Feed
            </div>
            <h2 className="text-3xl md:text-5xl font-extrabold tracking-tight mb-4">
              Discover what people are sharing
            </h2>
            <p className="text-base md:text-lg max-w-xl mx-auto" style={{ color: 'var(--text-secondary)' }}>
              Browse publicly shared links, documents, and directories from the community — no login required.
            </p>
          </motion.div>

          {/* Grid */}
          <motion.div
            variants={stagger}
            className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4"
          >
            {GRID_ITEMS.map((item) => (
              <motion.div
                key={item.id}
                variants={fadeUp}
                whileHover={{ y: -4, scale: 1.01 }}
                transition={{ duration: 0.2 }}
                className="rounded-2xl p-5 cursor-pointer group"
                style={{
                  background: 'var(--bg-secondary)',
                  border: '1px solid var(--border-subtle)',
                  boxShadow: '0 2px 12px rgba(0,0,0,0.12)',
                }}
              >
                <div className="flex items-start justify-between mb-4">
                  <div
                    className="w-11 h-11 rounded-xl flex items-center justify-center flex-shrink-0"
                    style={{ background: `${item.color}18`, border: `1px solid ${item.color}30` }}
                  >
                    <item.icon className="w-5 h-5" style={{ color: item.color }} />
                  </div>
                  <span
                    className="text-[10px] font-semibold uppercase px-2 py-1 rounded-full"
                    style={{ background: `${item.color}15`, color: item.color }}
                  >
                    {item.type}
                  </span>
                </div>
                <h3 className="font-semibold text-sm mb-1 group-hover:text-[#818CF8] transition-colors" style={{ color: 'var(--text-primary)' }}>
                  {item.title}
                </h3>
                <p className="text-xs mb-4" style={{ color: 'var(--text-muted)' }}>{item.desc}</p>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5">
                    <TrendingUp className="w-3.5 h-3.5" style={{ color: 'var(--text-muted)' }} />
                    <span className="text-xs font-medium" style={{ color: 'var(--text-muted)' }}>{item.views} views</span>
                  </div>
                  <motion.button
                    whileHover={{ x: 3 }}
                    className="text-xs font-semibold flex items-center gap-1"
                    style={{ color: item.color }}
                    onClick={() => navigate('/login')}
                  >
                    Open <ArrowRight className="w-3 h-3" />
                  </motion.button>
                </div>
              </motion.div>
            ))}
          </motion.div>

          <motion.div variants={fadeUp} className="text-center mt-10">
            <motion.button
              whileHover={{ scale: 1.03 }}
              whileTap={{ scale: 0.97 }}
              onClick={() => navigate('/login')}
              className="inline-flex items-center gap-2 px-6 py-3 rounded-xl font-semibold text-sm"
              style={{ border: '1px solid var(--border-strong)', color: 'var(--text-secondary)' }}
            >
              Sign up to see more <ArrowRight className="w-4 h-4" />
            </motion.button>
          </motion.div>
        </motion.div>
      </section>

      {/* ── FEATURES ── */}
      <section id="features" className="py-24 px-6" style={{ background: 'var(--bg-secondary)' }}>
        <motion.div
          variants={stagger}
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, margin: '-80px' }}
          className="max-w-6xl mx-auto"
        >
          <motion.div variants={fadeUp} className="text-center mb-16">
            <div
              className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold mb-4"
              style={{ background: '#5865F215', border: '1px solid #5865F230', color: '#818CF8' }}
            >
              <Zap className="w-3.5 h-3.5" />
              Built for productivity
            </div>
            <h2 className="text-3xl md:text-5xl font-extrabold tracking-tight mb-4">
              Everything you need, nothing you don't
            </h2>
            <p className="text-base md:text-lg max-w-xl mx-auto" style={{ color: 'var(--text-secondary)' }}>
              Powerful features for individuals and enterprise teams alike.
            </p>
          </motion.div>

          <motion.div variants={stagger} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {FEATURES.map((f) => (
              <motion.div
                key={f.title}
                variants={fadeUp}
                whileHover={{ y: -4 }}
                className="rounded-2xl p-6"
                style={{ background: 'var(--bg-tertiary)', border: '1px solid var(--border-subtle)' }}
              >
                <div
                  className="w-12 h-12 rounded-xl flex items-center justify-center mb-5"
                  style={{ background: `${f.color}18`, border: `1px solid ${f.color}25` }}
                >
                  <f.icon className="w-6 h-6" style={{ color: f.color }} />
                </div>
                <h3 className="font-semibold text-base mb-2" style={{ color: 'var(--text-primary)' }}>{f.title}</h3>
                <p className="text-sm leading-relaxed" style={{ color: 'var(--text-secondary)' }}>{f.desc}</p>
              </motion.div>
            ))}
          </motion.div>
        </motion.div>
      </section>

      {/* ── PRICING ── */}
      <section id="pricing" className="py-24 px-6">
        <motion.div
          variants={stagger}
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, margin: '-80px' }}
          className="max-w-4xl mx-auto"
        >
          <motion.div variants={fadeUp} className="text-center mb-16">
            <div
              className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold mb-4"
              style={{ background: '#EC489915', border: '1px solid #EC489930', color: '#F472B6' }}
            >
              <Lock className="w-3.5 h-3.5" />
              Simple pricing
            </div>
            <h2 className="text-3xl md:text-5xl font-extrabold tracking-tight mb-4">
              Pick your plan
            </h2>
            <p className="text-base max-w-md mx-auto" style={{ color: 'var(--text-secondary)' }}>
              Individuals get a powerful free tier. Teams unlock everything.
            </p>
          </motion.div>

          <motion.div variants={stagger} className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {PLANS.map((plan) => (
              <motion.div
                key={plan.name}
                variants={fadeUp}
                whileHover={{ y: -4 }}
                className="rounded-3xl p-8 relative overflow-hidden"
                style={{
                  background: plan.popular ? `linear-gradient(145deg, color-mix(in srgb, ${plan.color} 12%, var(--bg-secondary)), var(--bg-secondary))` : 'var(--bg-secondary)',
                  border: `1px solid ${plan.popular ? plan.color + '50' : 'var(--border-subtle)'}`,
                  boxShadow: plan.popular ? `0 8px 40px ${plan.color}20` : 'none',
                }}
              >
                {plan.popular && (
                  <div
                    className="absolute top-4 right-4 text-[10px] font-bold px-2.5 py-1 rounded-full uppercase tracking-wider text-white"
                    style={{ background: plan.color }}
                  >
                    Most Popular
                  </div>
                )}

                <div
                  className="w-12 h-12 rounded-xl flex items-center justify-center mb-5"
                  style={{ background: `${plan.color}18`, border: `1px solid ${plan.color}30` }}
                >
                  <plan.icon className="w-6 h-6" style={{ color: plan.color }} />
                </div>

                <h3 className="text-xl font-bold mb-1" style={{ color: 'var(--text-primary)' }}>{plan.name}</h3>
                <p className="text-sm mb-5" style={{ color: 'var(--text-muted)' }}>{plan.desc}</p>

                <div className="flex items-end gap-1 mb-6">
                  <span className="text-4xl font-extrabold" style={{ color: 'var(--text-primary)' }}>{plan.price}</span>
                  {plan.per && <span className="text-sm mb-1" style={{ color: 'var(--text-muted)' }}>{plan.per}</span>}
                </div>

                <ul className="space-y-3 mb-8">
                  {plan.features.map((f) => (
                    <li key={f} className="flex items-center gap-2.5">
                      <CheckCircle className="w-4 h-4 flex-shrink-0" style={{ color: plan.color }} />
                      <span className="text-sm" style={{ color: 'var(--text-secondary)' }}>{f}</span>
                    </li>
                  ))}
                </ul>

                <motion.button
                  whileHover={{ scale: 1.02 }}
                  whileTap={{ scale: 0.97 }}
                  onClick={() => navigate('/login')}
                  className="w-full py-3.5 rounded-xl font-semibold text-sm"
                  style={
                    plan.popular
                      ? { background: `linear-gradient(135deg, ${plan.color}, #7C3AED)`, color: 'white', boxShadow: `0 4px 20px ${plan.color}40` }
                      : { border: `1px solid ${plan.color}50`, color: plan.color, background: `${plan.color}08` }
                  }
                >
                  {plan.cta}
                </motion.button>
              </motion.div>
            ))}
          </motion.div>
        </motion.div>
      </section>

      {/* ── FOOTER CTA ── */}
      <section className="py-24 px-6 relative overflow-hidden" style={{ background: 'var(--bg-secondary)' }}>
        <motion.div
          animate={{ scale: [1, 1.1, 1], opacity: [0.15, 0.25, 0.15] }}
          transition={{ duration: 8, repeat: Infinity }}
          className="absolute inset-0 rounded-full blur-[140px] pointer-events-none"
          style={{ background: 'radial-gradient(circle at center, #5865F2, transparent 70%)' }}
        />
        <motion.div
          initial="hidden"
          whileInView="show"
          viewport={{ once: true }}
          variants={stagger}
          className="relative z-10 max-w-3xl mx-auto text-center"
        >
          <motion.h2 variants={fadeUp} className="text-3xl md:text-5xl font-extrabold tracking-tight mb-4">
            Ready to get organised?
          </motion.h2>
          <motion.p variants={fadeUp} className="text-base md:text-lg mb-10" style={{ color: 'var(--text-secondary)' }}>
            Join thousands of individuals and teams already using LinkVault to manage their digital world.
          </motion.p>
          <motion.button
            variants={fadeUp}
            whileHover={{ scale: 1.04, boxShadow: '0 8px 40px #5865F260' }}
            whileTap={{ scale: 0.97 }}
            onClick={() => navigate('/login')}
            className="inline-flex items-center gap-2 px-8 py-4 rounded-2xl font-bold text-base text-white"
            style={{ background: 'linear-gradient(135deg, #5865F2, #7C3AED)', boxShadow: '0 4px 24px #5865F240' }}
          >
            Start for free — no credit card needed
            <ArrowRight className="w-5 h-5" />
          </motion.button>
        </motion.div>
      </section>

      {/* ── FOOTER ── */}
      <footer className="py-8 px-6 text-center text-xs" style={{ borderTop: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
        <div className="flex items-center justify-center gap-2 mb-2">
          <img src="/logo.png" alt="LinkVault" className="w-5 h-5 rounded" />
          <span className="font-semibold" style={{ color: 'var(--text-secondary)' }}>LinkVault</span>
        </div>
        <p>© {new Date().getFullYear()} LinkVault. All rights reserved.</p>
      </footer>
    </div>
  );
}
