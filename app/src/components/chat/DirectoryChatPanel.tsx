import { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { MessageCircle, Send, ChevronDown, ChevronUp, Loader2 } from 'lucide-react';
import { useChatStore } from '@/store/useChatStore';
import { useStore } from '@/store/useStore';
import { getChatConnection } from '@/lib/chatClient';
import { cn } from '@/lib/utils';
import { format } from 'date-fns';

interface Props {
    directoryId: string;
    directoryName: string;
}

function Avatar({ name, size = 7 }: { name: string; size?: number }) {
    return (
        <div
            className={`w-${size} h-${size} rounded-full flex-shrink-0 flex items-center justify-center text-xs font-bold text-white`}
            style={{ background: `hsl(${(name.charCodeAt(0) * 37) % 360}, 65%, 52%)` }}
        >
            {name?.[0]?.toUpperCase() ?? '?'}
        </div>
    );
}

export function DirectoryChatPanel({ directoryId, directoryName }: Props) {
    const { messages, typingUsers, addMessage, markRead, setActiveRoom, unreadCounts } = useChatStore();
    const { user, token } = useStore();
    const [open, setOpen] = useState(false);
    const [input, setInput] = useState('');
    const [sending, setSending] = useState(false);
    const bottomRef = useRef<HTMLDivElement>(null);
    const typingTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);

    const roomMessages = useMemo(() => messages[directoryId] ?? [], [messages, directoryId]);
    const typingList = typingUsers[directoryId] ?? [];
    const unread = unreadCounts[directoryId] ?? 0;

    useEffect(() => {
        if (open) {
            setActiveRoom(directoryId);
            markRead(directoryId);
            // Join room and load history
            if (token) {
                try {
                    const conn = getChatConnection(token);
                    conn.invoke('JoinDirectory', directoryId);
                    conn.invoke('LoadHistory', directoryId, { page: 1, pageSize: 50 });
                } catch { /* ignore */ }
            }
        } else {
            setActiveRoom(null);
        }
    }, [open, directoryId, token, markRead, setActiveRoom]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [roomMessages]);

    const sendMessage = useCallback(async () => {
        const text = input.trim();
        if (!text || !token || sending) return;
        setSending(true);
        setInput('');
        try {
            const conn = getChatConnection(token);
            await conn.invoke('SendMessage', directoryId, { content: text, type: 'Text' });
        } catch {
            // Optimistic fallback
            addMessage(directoryId, {
                id: crypto.randomUUID(),
                content: text,
                userId: user?.id ?? '',
                userName: user?.name ?? 'Me',
                directoryId,
                createdAt: new Date().toISOString(),
                type: 'Text',
            });
        } finally {
            setSending(false);
        }
    }, [input, token, directoryId, user, addMessage, sending]);

    const handleKey = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
    };

    const handleTyping = () => {
        if (!token) return;
        try {
            getChatConnection(token).invoke('Typing', directoryId, true);
            if (typingTimeout.current) clearTimeout(typingTimeout.current);
            typingTimeout.current = setTimeout(() => {
                getChatConnection(token).invoke('Typing', directoryId, false);
            }, 2000);
        } catch { /* */ }
    };

    return (
        <div className="rounded-xl overflow-hidden border border-[var(--border-strong)]"
            style={{ background: 'var(--bg-secondary)' }}>
            {/* Header */}
            <button
                onClick={() => setOpen(v => !v)}
                className="w-full flex items-center gap-3 px-4 py-3 hover:bg-[var(--bg-primary)] transition-colors"
            >
                <MessageCircle className="w-4 h-4 text-[var(--accent-primary)]" />
                <span className="flex-1 text-left text-sm font-semibold text-[var(--text-primary)]">
                    #{directoryName} Chat
                </span>
                {unread > 0 && !open && (
                    <span className="px-2 py-0.5 rounded-full bg-[var(--accent-primary)] text-white text-xs font-bold">
                        {unread > 9 ? '9+' : unread}
                    </span>
                )}
                {open ? <ChevronUp className="w-4 h-4 text-[var(--text-muted)]" /> : <ChevronDown className="w-4 h-4 text-[var(--text-muted)]" />}
            </button>

            <AnimatePresence>
                {open && (
                    <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: 320, opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.2 }}
                        className="overflow-hidden flex flex-col"
                    >
                        {/* Messages */}
                        <div className="flex-1 overflow-y-auto px-3 py-2 space-y-2 h-[260px]"
                            style={{ borderTop: '1px solid var(--border-subtle)' }}>
                            {roomMessages.length === 0 && (
                                <div className="flex flex-col items-center justify-center h-full text-[var(--text-muted)] text-center">
                                    <MessageCircle className="w-8 h-8 mb-2 opacity-20" />
                                    <p className="text-xs">No messages yet in #{directoryName}</p>
                                </div>
                            )}
                            {roomMessages.map((msg, i) => {
                                const isMe = msg.userId === user?.id;
                                const showMeta = i === 0 || roomMessages[i - 1].userId !== msg.userId;
                                return (
                                    <div key={msg.id} className={cn('flex gap-2', isMe && 'flex-row-reverse')}>
                                        {showMeta && !isMe && <Avatar name={msg.userName} />}
                                        {!showMeta && !isMe && <div className="w-7 flex-shrink-0" />}
                                        <div className={cn('max-w-[80%]', isMe && 'items-end flex flex-col')}>
                                            {showMeta && (
                                                <p className={cn('text-[10px] text-[var(--text-muted)] mb-0.5', isMe && 'text-right')}>
                                                    {isMe ? 'You' : msg.userName} · {format(new Date(msg.createdAt), 'HH:mm')}
                                                </p>
                                            )}
                                            <div className={cn(
                                                'px-3 py-1.5 rounded-2xl text-xs leading-relaxed',
                                                isMe ? 'text-white rounded-tr-sm' : 'bg-[var(--bg-primary)] text-[var(--text-primary)] rounded-tl-sm'
                                            )}
                                                style={isMe ? { background: 'linear-gradient(135deg,#5865F2,#7C3AED)' } : {}}
                                            >
                                                {msg.content}
                                            </div>
                                        </div>
                                    </div>
                                );
                            })}
                            {typingList.length > 0 && (
                                <p className="text-[10px] text-[var(--text-muted)] pl-1">
                                    {typingList.join(', ')} {typingList.length === 1 ? 'is' : 'are'} typing…
                                </p>
                            )}
                            <div ref={bottomRef} />
                        </div>

                        {/* Input */}
                        <div className="px-3 py-2 border-t border-[var(--border-subtle)]">
                            <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg"
                                style={{ background: 'var(--bg-primary)', border: '1px solid var(--border-strong)' }}>
                                <input
                                    value={input}
                                    onChange={e => { setInput(e.target.value); handleTyping(); }}
                                    onKeyDown={handleKey}
                                    placeholder={`Message #${directoryName}…`}
                                    className="flex-1 bg-transparent text-xs text-[var(--text-primary)] outline-none placeholder:text-[var(--text-muted)]"
                                />
                                <motion.button
                                    whileTap={{ scale: 0.88 }}
                                    disabled={!input.trim() || sending}
                                    onClick={sendMessage}
                                    className="w-7 h-7 rounded-lg flex items-center justify-center disabled:opacity-40"
                                    style={{ background: input.trim() ? 'linear-gradient(135deg,#5865F2,#7C3AED)' : 'var(--bg-tertiary)' }}
                                >
                                    {sending ? <Loader2 className="w-3 h-3 text-white animate-spin" /> : <Send className="w-3 h-3 text-white" />}
                                </motion.button>
                            </div>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
}
