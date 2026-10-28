import { motion } from 'framer-motion';
import { Copy, Check, ExternalLink, User, ArrowUpRight } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useStore } from '@/store/useStore';
import { cn } from '@/lib/utils';
import { staggerItem } from '@/lib/animations';

function CompletionBar({ value }: { value: number }) {
  return (
    <div className="h-1.5 w-full rounded-full overflow-hidden" style={{ background: 'var(--bg-tertiary)' }}>
      <motion.div
        className="h-full rounded-full"
        style={{ background: 'linear-gradient(90deg, #6366F1, #A78BFA)' }}
        initial={{ width: 0 }}
        animate={{ width: `${value}%` }}
        transition={{ duration: 0.8, ease: 'easeOut', delay: 0.2 }}
      />
    </div>
  );
}

export function ProfileCard() {
  const { user } = useStore();
  const [copied, setCopied] = useState(false);

  const username = user?.username || user?.name?.toLowerCase().replace(/\s+/g, '') || 'you';
  const profileUrl = `linkvault.app/u/${username}`;
  const profilePath = `/u/${username}`;

  // Calculate completeness
  let completeness = 40; // base: has account
  if (user?.name || user?.username) completeness += 20;
  if (user?.avatar) completeness += 20;
  if (completeness < 100) {} // bio check would add 20 if set

  const handleCopy = () => {
    navigator.clipboard.writeText(`https://${profileUrl}`);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <motion.div
      className={cn('rounded-xl p-5', 'bg-bg-secondary border border-border-subtle')}
      style={{ boxShadow: '0 4px 24px rgba(0,0,0,0.15)' }}
      variants={staggerItem}
    >
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div
            className="w-6 h-6 rounded-lg flex items-center justify-center"
            style={{ background: '#6366F120' }}
          >
            <User className="w-3.5 h-3.5" style={{ color: '#6366F1' }} />
          </div>
          <h3 className="text-sm font-semibold text-text-primary">Public Profile</h3>
        </div>
        <Link
          to={profilePath}
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-1 text-xs font-medium transition-colors hover:opacity-80"
          style={{ color: '#6366F1' }}
          title="View your public profile"
        >
          Preview <ArrowUpRight className="w-3 h-3" />
        </Link>
      </div>

      {/* URL Copy */}
      <div className="flex items-center gap-2 mb-4">
        <div className={cn(
          'flex-1 flex items-center gap-2 px-3 py-2 rounded-lg',
          'bg-bg-primary border border-border-strong'
        )}>
          <ExternalLink className="w-3.5 h-3.5 text-text-muted flex-shrink-0" />
          <span className="text-xs text-text-secondary truncate">{profileUrl}</span>
        </div>
        <motion.button
          id="profile-card-copy-url"
          onClick={handleCopy}
          title={copied ? 'Copied!' : 'Copy link'}
          className={cn(
            'px-3 py-2 rounded-lg transition-all duration-200',
            'border border-border-subtle',
            copied ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' : 'bg-bg-tertiary text-text-secondary hover:text-text-primary hover:bg-bg-floating'
          )}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
        >
          {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
        </motion.button>
      </div>

      {/* Profile Completeness */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-[10px] font-semibold text-text-muted uppercase tracking-wider">
            Profile Completeness
          </span>
          <span className="text-xs font-bold" style={{ color: '#6366F1' }}>
            {completeness}%
          </span>
        </div>
        <CompletionBar value={completeness} />
        {completeness < 100 && (
          <p className="text-[11px] text-text-muted">
            {!user?.avatar && 'Add a profile photo · '}
            Add a bio to reach 100%
          </p>
        )}
      </div>
    </motion.div>
  );
}
