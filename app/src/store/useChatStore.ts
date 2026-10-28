import { create } from 'zustand';

export interface ChatMessage {
    id: string;
    content: string;
    userId: string;
    userName: string;
    userAvatar?: string;
    directoryId: string;
    createdAt: string;
    isEdited?: boolean;
    type: 'Text' | 'Image' | 'File' | 'System';
}

interface ChatState {
    // Global chat: directoryId = 'global'
    messages: Record<string, ChatMessage[]>;   // keyed by directoryId
    typingUsers: Record<string, string[]>;     // directoryId -> userNames typing
    isGlobalOpen: boolean;
    activeRoomId: string | null;               // which directoryId chat is open
    unreadCounts: Record<string, number>;

    // Actions
    setGlobalOpen: (v: boolean) => void;
    setActiveRoom: (id: string | null) => void;
    addMessage: (roomId: string, msg: ChatMessage) => void;
    setMessages: (roomId: string, msgs: ChatMessage[]) => void;
    editMessage: (roomId: string, msg: ChatMessage) => void;
    removeMessage: (roomId: string, messageId: string) => void;
    setTyping: (roomId: string, userName: string, isTyping: boolean) => void;
    markRead: (roomId: string) => void;
    incrementUnread: (roomId: string) => void;
}

export const useChatStore = create<ChatState>((set) => ({
    messages: {},
    typingUsers: {},
    isGlobalOpen: false,
    activeRoomId: null,
    unreadCounts: {},

    setGlobalOpen: (v) => set({ isGlobalOpen: v }),
    setActiveRoom: (id) => set({ activeRoomId: id }),

    addMessage: (roomId, msg) =>
        set((s) => ({
            messages: {
                ...s.messages,
                [roomId]: [...(s.messages[roomId] ?? []), msg],
            },
        })),

    setMessages: (roomId, msgs) =>
        set((s) => ({
            messages: { ...s.messages, [roomId]: msgs },
        })),

    editMessage: (roomId, updated) =>
        set((s) => ({
            messages: {
                ...s.messages,
                [roomId]: (s.messages[roomId] ?? []).map((m) =>
                    m.id === updated.id ? updated : m
                ),
            },
        })),

    removeMessage: (roomId, messageId) =>
        set((s) => ({
            messages: {
                ...s.messages,
                [roomId]: (s.messages[roomId] ?? []).filter((m) => m.id !== messageId),
            },
        })),

    setTyping: (roomId, userName, isTyping) =>
        set((s) => {
            const current = s.typingUsers[roomId] ?? [];
            return {
                typingUsers: {
                    ...s.typingUsers,
                    [roomId]: isTyping
                        ? [...new Set([...current, userName])]
                        : current.filter((u) => u !== userName),
                },
            };
        }),

    markRead: (roomId) =>
        set((s) => ({
            unreadCounts: { ...s.unreadCounts, [roomId]: 0 },
        })),

    incrementUnread: (roomId) =>
        set((s) => ({
            unreadCounts: {
                ...s.unreadCounts,
                [roomId]: (s.unreadCounts[roomId] ?? 0) + 1,
            },
        })),
}));
