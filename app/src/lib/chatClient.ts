import * as signalR from '@microsoft/signalr';

// Must match backend port. Uses VITE_API_URL if set, otherwise defaults to the correct dev port.
const SIGNALR_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5246').replace(/\/+$/, '');

let connection: signalR.HubConnection | null = null;

export function getChatConnection(token: string): signalR.HubConnection {
    if (connection) return connection;

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`${SIGNALR_URL}/hubs/chat`, {
            accessTokenFactory: () => token,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000]) // back-off
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    return connection;
}

export async function startChatConnection(token: string): Promise<signalR.HubConnection> {
    const conn = getChatConnection(token);
    if (conn.state === signalR.HubConnectionState.Disconnected) {
        // Race against a timeout so a slow/unreachable hub never blocks page load
        const timeout = new Promise<never>((_, reject) =>
            setTimeout(() => reject(new Error('SignalR connection timed out')), 5000)
        );
        await Promise.race([conn.start(), timeout]);
    }
    return conn;
}

export async function stopChatConnection(): Promise<void> {
    if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
        await connection.stop();
    }
    connection = null;
}
