import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { startChatConnection, stopChatConnection } from '@/lib/chatClient';
import { useChatStore } from '@/store/useChatStore';
import { useStore } from '@/store/useStore';

type SignalRMessage = {
    id: string;
    content: string;
    userId: string;
    userName: string;
    userAvatarUrl?: string;
    directoryId?: string;
    createdAt: string;
    isEdited: boolean;
    type?: 'Text' | 'Image' | 'File' | 'System';
};

/**
 * Connects to the SignalR chat hub when the user is logged in.
 * Registers all global message handlers.
 * Should be mounted once inside RootLayout.
 */
export function useChatConnection() {
    const { token } = useStore();
    const connRef = useRef<signalR.HubConnection | null>(null);
    // Store actions are accessed via useChatStore.getState() inside handlers to avoid stale closures

    useEffect(() => {
        if (!token) return;

        let cancelled = false;

        const connect = async () => {
            try {
                const conn = await startChatConnection(token);
                if (cancelled) return;
                connRef.current = conn;

                conn.on('ReceiveMessage', (msg: SignalRMessage) => {
                    const state = useChatStore.getState();
                    state.addMessage(msg.directoryId ?? 'global', {
                        id: msg.id,
                        content: msg.content,
                        userId: msg.userId,
                        userName: msg.userName,
                        userAvatar: msg.userAvatarUrl,
                        directoryId: msg.directoryId ?? 'global',
                        createdAt: msg.createdAt,
                        isEdited: msg.isEdited,
                        type: msg.type ?? 'Text',
                    });
                    if (msg.directoryId !== state.activeRoomId) {
                        state.incrementUnread(msg.directoryId ?? 'global');
                    }
                });

                conn.on('MessageEdited', (msg: SignalRMessage) => {
                    useChatStore.getState().editMessage(msg.directoryId ?? 'global', {
                        id: msg.id,
                        content: msg.content,
                        userId: msg.userId,
                        userName: msg.userName,
                        directoryId: msg.directoryId ?? 'global',
                        createdAt: msg.createdAt,
                        isEdited: true,
                        type: msg.type ?? 'Text' as const,
                    });
                });

                conn.on('MessageDeleted', ({ messageId, directoryId }: { messageId: string; directoryId: string }) => {
                    useChatStore.getState().removeMessage(directoryId, messageId);
                });

                conn.on('ChatHistory', (history: { items: SignalRMessage[] }) => {
                    if (history.items?.length > 0) {
                        const roomId = history.items[0].directoryId ?? 'global';
                        useChatStore.getState().setMessages(roomId, history.items.map((m: SignalRMessage) => ({
                            id: m.id,
                            content: m.content,
                            userId: m.userId,
                            userName: m.userName,
                            userAvatar: m.userAvatarUrl,
                            directoryId: m.directoryId ?? 'global',
                            createdAt: m.createdAt,
                            isEdited: m.isEdited,
                            type: m.type ?? 'Text' as const,
                        })));
                    }
                });

                conn.on('UserTyping', ({ userName, isTyping, directoryId: dId }: { userName: string; isTyping: boolean; directoryId: string }) => {
                    useChatStore.getState().setTyping(dId, userName, isTyping);
                });
            } catch (err) {
                console.error('[Chat] Connection failed:', err);
            }
        };

        connect();

        return () => {
            cancelled = true;
            stopChatConnection();
        };
    // Token is the only stable dep — all store access happens via getState() inside effect
    }, [token]);

    return connRef;
}
