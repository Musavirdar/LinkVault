import { motion, AnimatePresence } from 'framer-motion';
import { X, RotateCcw, Sliders } from 'lucide-react';
import { usePreferencesStore, type WidgetId } from '@/store/usePreferencesStore';

interface Props {
    open: boolean;
    onClose: () => void;
}

const ACCENT_COLORS = [
    { label: 'Indigo', value: '#5865F2' },
    { label: 'Purple', value: '#7C3AED' },
    { label: 'Rose', value: '#E11D48' },
    { label: 'Emerald', value: '#10B981' },
    { label: 'Amber', value: '#F59E0B' },
    { label: 'Cyan', value: '#06B6D4' },
    { label: 'Pink', value: '#EC4899' },
    { label: 'Orange', value: '#F97316' },
];

function Toggle({ checked, onChange }: { checked: boolean; onChange: () => void }) {
    return (
        <button
            onClick={onChange}
            className="relative flex-shrink-0 w-11 h-6 rounded-full transition-colors duration-200"
            style={{ background: checked ? 'var(--accent-primary)' : 'var(--bg-primary)' }}
            role="switch"
            aria-checked={checked}
        >
            <motion.span
                layout
                transition={{ type: 'spring', stiffness: 500, damping: 30 }}
                className="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow-sm"
                style={{ left: checked ? '22px' : '2px' }}
            />
        </button>
    );
}

export function CustomizeDashboard({ open, onClose }: Props) {
    const { widgets, toggle, accentColor, setAccentColor, compactMode, setCompactMode, resetToDefaults } = usePreferencesStore();

    return (
        <AnimatePresence>
            {open && (
                <>
                    {/* Backdrop */}
                    <motion.div
                        key="backdrop"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        className="fixed inset-0 z-40 bg-black/30 backdrop-blur-sm"
                        onClick={onClose}
                    />

                    {/* Panel */}
                    <motion.div
                        key="panel"
                        initial={{ x: 360, opacity: 0 }}
                        animate={{ x: 0, opacity: 1 }}
                        exit={{ x: 360, opacity: 0 }}
                        transition={{ type: 'spring', damping: 26, stiffness: 300 }}
                        className="fixed top-0 right-0 h-full w-[320px] z-50 flex flex-col overflow-hidden"
                        style={{
                            background: 'var(--bg-secondary)',
                            borderLeft: '1px solid var(--border-strong)',
                            boxShadow: '-16px 0 48px rgba(0,0,0,0.35)',
                        }}
                    >
                        {/* Header */}
                        <div className="flex items-center gap-3 px-5 py-4 border-b border-[var(--border-subtle)] flex-shrink-0">
                            <div className="w-8 h-8 rounded-lg flex items-center justify-center"
                                style={{ background: 'var(--accent-primary)18' }}>
                                <Sliders className="w-4 h-4" style={{ color: 'var(--accent-primary)' }} />
                            </div>
                            <div className="flex-1">
                                <p className="text-sm font-semibold text-[var(--text-primary)]">Customize Dashboard</p>
                                <p className="text-xs text-[var(--text-muted)]">Choose what you see</p>
                            </div>
                            <button
                                onClick={onClose}
                                className="p-1.5 rounded-lg text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-primary)] transition-colors"
                            >
                                <X className="w-4 h-4" />
                            </button>
                        </div>

                        <div className="flex-1 overflow-y-auto px-5 py-4 space-y-6">
                            {/* Widgets section */}
                            <div>
                                <p className="text-xs font-semibold text-[var(--text-muted)] uppercase tracking-widest mb-3">
                                    Widgets
                                </p>
                                <div className="space-y-2">
                                    {widgets.map((w) => (
                                        <motion.div
                                            key={w.id}
                                            layout
                                            className="flex items-center gap-3 px-3 py-3 rounded-xl transition-colors"
                                            style={{
                                                background: w.visible ? 'var(--bg-primary)' : 'transparent',
                                                border: `1px solid ${w.visible ? 'var(--border-strong)' : 'transparent'}`,
                                            }}
                                        >
                                            <span className="text-lg leading-none">{w.icon}</span>
                                            <div className="flex-1 min-w-0">
                                                <p className="text-sm font-medium text-[var(--text-primary)] truncate">{w.label}</p>
                                                <p className="text-xs text-[var(--text-muted)] truncate">{w.description}</p>
                                            </div>
                                            <Toggle checked={w.visible} onChange={() => toggle(w.id as WidgetId)} />
                                        </motion.div>
                                    ))}
                                </div>
                            </div>

                            {/* Accent Color */}
                            <div>
                                <p className="text-xs font-semibold text-[var(--text-muted)] uppercase tracking-widest mb-3">
                                    Accent Color
                                </p>
                                <div className="grid grid-cols-4 gap-2">
                                    {ACCENT_COLORS.map((c) => (
                                        <button
                                            key={c.value}
                                            title={c.label}
                                            onClick={() => setAccentColor(c.value)}
                                            className="flex flex-col items-center gap-1.5"
                                        >
                                            <motion.div
                                                whileTap={{ scale: 0.85 }}
                                                className="w-10 h-10 rounded-xl transition-all"
                                                style={{
                                                    background: c.value,
                                                    boxShadow: accentColor === c.value ? `0 0 0 3px var(--bg-secondary), 0 0 0 5px ${c.value}` : 'none',
                                                }}
                                            />
                                            <span className="text-[10px] text-[var(--text-muted)]">{c.label}</span>
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Layout Options */}
                            <div>
                                <p className="text-xs font-semibold text-[var(--text-muted)] uppercase tracking-widest mb-3">
                                    Layout
                                </p>
                                <div className="flex items-center justify-between px-3 py-3 rounded-xl"
                                    style={{ background: 'var(--bg-primary)', border: '1px solid var(--border-strong)' }}>
                                    <div>
                                        <p className="text-sm font-medium text-[var(--text-primary)]">Compact Mode</p>
                                        <p className="text-xs text-[var(--text-muted)]">Tighter spacing between widgets</p>
                                    </div>
                                    <Toggle checked={compactMode} onChange={() => setCompactMode(!compactMode)} />
                                </div>
                            </div>
                        </div>

                        {/* Footer */}
                        <div className="px-5 py-4 border-t border-[var(--border-subtle)] flex-shrink-0">
                            <button
                                onClick={resetToDefaults}
                                className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-primary)]"
                            >
                                <RotateCcw className="w-3.5 h-3.5" />
                                Reset to defaults
                            </button>
                        </div>
                    </motion.div>
                </>
            )}
        </AnimatePresence>
    );
}
