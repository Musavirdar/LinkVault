import * as signalR from '@microsoft/signalr';
import type {
  AgentPlanFinishedEvent,
  AgentPlanStatusEvent,
  AgentStepCompletedEvent,
  AgentStepFailedEvent,
  AgentStepStartedEvent,
} from '../types/agent.types';

const SIGNALR_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5246').replace(/\/+$/, '');

let agentConnection: signalR.HubConnection | null = null;

export function getAgentHubConnection(token: string): signalR.HubConnection {
  if (agentConnection) return agentConnection;

  agentConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${SIGNALR_URL}/hubs/agent`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 1000, 3000, 5000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  return agentConnection;
}

export async function startAgentHubConnection(token: string): Promise<signalR.HubConnection> {
  const conn = getAgentHubConnection(token);
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    const timeout = new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error('Agent SignalR connection timed out')), 5000)
    );
    await Promise.race([conn.start(), timeout]);
  }
  return conn;
}

export async function joinAgentPlanGroup(planId: string): Promise<void> {
  if (agentConnection && agentConnection.state === signalR.HubConnectionState.Connected) {
    await agentConnection.invoke('JoinPlanGroup', planId);
  }
}

export async function leaveAgentPlanGroup(planId: string): Promise<void> {
  if (agentConnection && agentConnection.state === signalR.HubConnectionState.Connected) {
    await agentConnection.invoke('LeavePlanGroup', planId);
  }
}

export function subscribeToAgentEvents(listeners: {
  onPlanStatusChanged?: (event: AgentPlanStatusEvent) => void;
  onStepStarted?: (event: AgentStepStartedEvent) => void;
  onStepCompleted?: (event: AgentStepCompletedEvent) => void;
  onStepFailed?: (event: AgentStepFailedEvent) => void;
  onPlanFinished?: (event: AgentPlanFinishedEvent) => void;
}): () => void {
  if (!agentConnection) return () => {};

  if (listeners.onPlanStatusChanged) {
    agentConnection.on('OnPlanStatusChanged', listeners.onPlanStatusChanged);
  }
  if (listeners.onStepStarted) {
    agentConnection.on('OnStepStarted', listeners.onStepStarted);
  }
  if (listeners.onStepCompleted) {
    agentConnection.on('OnStepCompleted', listeners.onStepCompleted);
  }
  if (listeners.onStepFailed) {
    agentConnection.on('OnStepFailed', listeners.onStepFailed);
  }
  if (listeners.onPlanFinished) {
    agentConnection.on('OnPlanFinished', listeners.onPlanFinished);
  }

  return () => {
    if (agentConnection) {
      if (listeners.onPlanStatusChanged) agentConnection.off('OnPlanStatusChanged', listeners.onPlanStatusChanged);
      if (listeners.onStepStarted) agentConnection.off('OnStepStarted', listeners.onStepStarted);
      if (listeners.onStepCompleted) agentConnection.off('OnStepCompleted', listeners.onStepCompleted);
      if (listeners.onStepFailed) agentConnection.off('OnStepFailed', listeners.onStepFailed);
      if (listeners.onPlanFinished) agentConnection.off('OnPlanFinished', listeners.onPlanFinished);
    }
  };
}

export async function stopAgentHubConnection(): Promise<void> {
  if (agentConnection && agentConnection.state !== signalR.HubConnectionState.Disconnected) {
    await agentConnection.stop();
  }
  agentConnection = null;
}
