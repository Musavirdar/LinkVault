import { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { MessageCircle, X, Send, Hash, Loader2 } from 'lucide-react';
import { useChatStore } from '@/store/useChatStore';
import { useStore } from '@/store/useStore';
import { getChatConnection } from '@/lib/chatClient';
import { cn } from '@/lib/utils';
import { format, isToday, isYesterday } from 'date-fns';

const GLOBAL_ROOM = 'global';

function formatTime(ts: string) {
    const d = new Date(ts);
    if (isToday(d)) return format(d, 'HH:mm');
    if (isYesterday(d)) return 'Yesterday ' + format(d, 'HH:mm');
    return format(d, 'MMM d, HH:mm');
}

function Avatar({ name, avatar, size = 8 }: { name: string; avatar?: string; size?: number }) {
    if (avatar) {
        return <img src={avatar} alt={name} className={`w-${size} h-${size} rounded-full object-cover flex-shrink-0`} />;
    }
    return (
        <div
            className={`w-${size} h-${size} rounded-full flex items-center justify-center text-xs font-bold text-white flex-shrink-0`}
            style={{ background: 'linear-gradient(135deg,#5865F2,#7C3AED)' }}
        >
            {name?.[0]?.toUpperCase() ?? '?'}
        </div>
    );
}

export function GlobalChatBox() {
    const { isGlobalOpen, setGlobalOpen, messages, typingUsers, addMessage, markRead, unreadCounts } = useChatStore();
    const { user, token } = useStore();
    const [input, setInput] = useState('');
    const [sending, setSending] = useState(false);
    const bottomRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);
    const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const roomMessages = useMemo(() => messages[GLOBAL_ROOM] ?? [], [messages]);
    const typingList = typingUsers[GLOBAL_ROOM] ?? [];
    const unread = unreadCounts[GLOBAL_ROOM] ?? 0;

    // Auto-scroll on new messages
    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [roomMessages]);

    // Mark read when open
    useEffect(() => {
        if (isGlobalOpen) markRead(GLOBAL_ROOM);
    }, [isGlobalOpen, roomMessages.length, markRead]);

    const sendMessage = useCallback(async () => {
        const text = input.trim();
        if (!text || !token || sending) return;
        setSending(true);
        setInput('');
        try {
            const conn = getChatConnection(token);
            await conn.invoke('SendMessage', GLOBAL_ROOM, { content: text, type: 'Text' });
        } catch (err) {
            console.error('[Chat] Send failed:', err);
            // Optimistic add for offline/demo fallback
            addMessage(GLOBAL_ROOM, {
                id: crypto.randomUUID(),
                content: text,
                userId: user?.id ?? 'me',
                userName: user?.name ?? 'Me',
                userAvatar: user?.avatar,
                directoryId: GLOBAL_ROOM,
                createdAt: new Date().toISOString(),
                type: 'Text',
            });
        } finally {
            setSending(false);
        }
    }, [input, token, sending, user, addMessage]);

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    };

    const handleTyping = () => {
        if (!token) return;
        try {
            const conn = getChatConnection(token);
            conn.invoke('Typing', GLOBAL_ROOM, true);
            if (typingTimeoutRef.current) clearTimeout(typingTimeoutRef.current);
            typingTimeoutRef.current = setTimeout(() => {
                conn.invoke('Typing', GLOBAL_ROOM, false);
            }, 2000);
        } catch { /* ignore */ }
    };

    return (
        <>
            {/* Floating bubble */}
            <motion.button
                whileTap={{ scale: 0.92 }}
                whileHover={{ scale: 1.05 }}
                onClick={() => setGlobalOpen(!isGlobalOpen)}
                className="fixed bottom-6 right-6 z-50 w-14 h-14 rounded-full shadow-2xl flex items-center justify-center"
                style={{ background: 'linear-gradient(135deg,#5865F2,#7C3AED)', boxShadow: '0 8px 32px #5865F260' }}
                aria-label="Open global chat"
            >
                <AnimatePresence mode="wait">
                    {isGlobalOpen ? (
                        <motion.div key="x" initial={{ rotate: -90, opacity: 0 }} animate={{ rotate: 0, opacity: 1 }} exit={{ rotate: 90, opacity: 0 }}>
                            <X className="w-5 h-5 text-white" />
                        </motion.div>
                    ) : (
                        <motion.div key="chat" initial={{ rotate: 90, opacity: 0 }} animate={{ rotate: 0, opacity: 1 }} exit={{ rotate: -90, opacity: 0 }}>
                            <MessageCircle className="w-5 h-5 text-white" />
                        </motion.div>
                    )}
                </AnimatePresence>
                {/* Unread badge */}
                <AnimatePresence>
                    {!isGlobalOpen && unread > 0 && (
                        <motion.span
                            key="badge"
                            initial={{ scale: 0 }} animate={{ scale: 1 }} exit={{ scale: 0 }}
                            className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-red-500 text-white text-xs font-bold flex items-center justify-center"
                        >
                            {unread > 9 ? '9+' : unread}
                        </motion.span>
                    )}
                </AnimatePresence>
            </motion.button>

            {/* Chat panel */}
            <AnimatePresence>
                {isGlobalOpen && (
                    <motion.div
                        initial={{ opacity: 0, y: 24, scale: 0.95 }}
                        animate={{ opacity: 1, y: 0, scale: 1 }}
                        exit={{ opacity: 0, y: 24, scale: 0.95 }}
                        transition={{ type: 'spring', damping: 22, stiffness: 320 }}
                        className="fixed bottom-24 right-6 z-50 w-[360px] rounded-2xl overflow-hidden flex flex-col shadow-2xl"
                        style={{
                            background: 'var(--bg-secondary)',
                            border: '1px solid var(--border-strong)',
                            height: '480px',
                            boxShadow: '0 24px 64px rgba(0,0,0,0.40)',
                        }}
                    >
                        {/* Header */}
                        <div className="flex items-center gap-3 px-4 py-3 border-b border-[var(--border-subtle)]"
                            style={{ background: 'linear-gradient(135deg,#5865F2,#7C3AED)' }}>
                            <Hash className="w-5 h-5 text-white/80" />
                            <div className="flex-1">
                                <p className="text-sm font-semibold text-white">Global Chat</p>
                                <p className="text-xs text-white/60">All members online</p>
                            </div>
                        </div>

                        {/* Messages */}
                        <div className="flex-1 overflow-y-auto px-3 py-3 space-y-3 scrollbar-thin">
                            {roomMessages.length === 0 && (
                                <div className="flex flex-col items-center justify-center h-full text-center text-[var(--text-muted)] py-8">
                                    <MessageCircle className="w-10 h-10 mb-3 opacity-20" />
                                    <p className="text-sm">No messages yet. Say hello! 👋</p>
                                </div>
                            )}
                            {roomMessages.map((msg, i) => {
                                const isMe = msg.userId === user?.id;
                                const showAvatar = i === 0 || roomMessages[i - 1].userId !== msg.userId;
                                return (
                                    <motion.div
                                        key={msg.id}
                                        initial={{ opacity: 0, y: 8 }}
                                        animate={{ opacity: 1, y: 0 }}
                                        className={cn('flex gap-2', isMe && 'flex-row-reverse')}
                                    >
                                        {showAvatar && !isMe ? (
                                            <Avatar name={msg.userName} avatar={msg.userAvatar} size={7} />
                                        ) : !isMe ? (
                                            <div className="w-7 flex-shrink-0" />
                                        ) : null}
                                        <div className={cn('max-w-[80%]', isMe && 'items-end flex flex-col')}>
                                            {showAvatar && (
                                                <p className={cn('text-[10px] text-[var(--text-muted)] mb-0.5', isMe && 'text-right')}>
                                                    {isMe ? 'You' : msg.userName} · {formatTime(msg.createdAt)}
                                                </p>
                                            )}
                                            <div
                                                className={cn(
                                                    'px-3 py-2 rounded-2xl text-sm leading-relaxed',
                                                    isMe
                                                        ? 'text-white rounded-tr-sm'
                                                        : 'text-[var(--text-primary)] rounded-tl-sm bg-[var(--bg-primary)]'
                                                )}
                                                style={isMe ? { background: 'linear-gradient(135deg,#5865F2,#7C3AED)' } : {}}
                                            >
                                                {msg.content}
                                                {msg.isEdited && <span className="text-[9px] opacity-60 ml-1">(edited)</span>}
                                            </div>
                                        </div>
                                    </motion.div>
                                );
                            })}
                            {/* Typing indicator */}
                            {typingList.length > 0 && (
                                <div className="flex items-center gap-2 text-xs text-[var(--text-muted)] pl-1">
                                    <span className="flex gap-0.5">
                                        {[0, 1, 2].map(i => (
                                            <motion.span key={i} className="w-1.5 h-1.5 rounded-full bg-[var(--accent-primary)]"
                                                animate={{ y: [0, -4, 0] }} transition={{ duration: 0.8, delay: i * 0.15, repeat: Infinity }} />
                                        ))}
                                    </span>
                                    {typingList.slice(0, 2).join(', ')} {typingList.length === 1 ? 'is' : 'are'} typing…
                                </div>
                            )}
                            <div ref={bottomRef} />
                        </div>

                        {/* Input */}
                        <div className="px-3 pb-3 pt-2 border-t border-[var(--border-subtle)]">
                            <div className="flex items-center gap-2 px-3 py-2 rounded-xl"
                                style={{ background: 'var(--bg-primary)', border: '1px solid var(--border-strong)' }}>
                                <input
                                    ref={inputRef}
                                    value={input}
                                    onChange={e => { setInput(e.target.value); handleTyping(); }}
                                    onKeyDown={handleKeyDown}
                                    placeholder="Message global chat…"
                                    className="flex-1 bg-transparent text-sm text-[var(--text-primary)] outline-none placeholder:text-[var(--text-muted)]"
                                />
                                <motion.button
                                    whileTap={{ scale: 0.88 }}
                                    disabled={!input.trim() || sending}
                                    onClick={sendMessage}
                                    className="w-8 h-8 rounded-lg flex items-center justify-center disabled:opacity-40 transition-all"
                                    style={{ background: input.trim() ? 'linear-gradient(135deg,#5865F2,#7C3AED)' : 'var(--bg-tertiary)' }}
                                >
                                    {sending
                                        ? <Loader2 className="w-3.5 h-3.5 text-white animate-spin" />
                                        : <Send className="w-3.5 h-3.5 text-white" />
                                    }
                                </motion.button>
                            </div>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>
        </>
    );
}
