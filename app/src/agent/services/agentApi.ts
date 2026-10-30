import { API_BASE_URL, apiClient } from '../../api/client';
import type {
  AgentActionPlanDto,
  ApprovePlanResponse,
  CreatePlanRequest,
  RejectPlanResponse,
  SafeShareScanResultDto,
  TranscribeResultDto,
} from '../types/agent.types';

export const agentApi = {
  async createPlan(request: CreatePlanRequest): Promise<AgentActionPlanDto> {
    const res = await apiClient<AgentActionPlanDto>('/agent/plan', {
      method: 'POST',
      body: request,
    });
    if (res.error || !res.data) {
      throw new Error(res.error || 'Failed to create plan');
    }
    return res.data;
  },

  async getPlan(planId: string): Promise<AgentActionPlanDto> {
    const res = await apiClient<AgentActionPlanDto>(`/agent/plan/${planId}`, {
      method: 'GET',
    });
    if (res.error || !res.data) {
      throw new Error(res.error || 'Failed to retrieve plan');
    }
    return res.data;
  },

  async approvePlan(planId: string): Promise<ApprovePlanResponse> {
    const res = await apiClient<ApprovePlanResponse>(`/agent/plan/${planId}/approve`, {
      method: 'POST',
    });
    if (res.error || !res.data) {
      throw new Error(res.error || 'Failed to approve plan');
    }
    return res.data;
  },

  async rejectPlan(planId: string): Promise<RejectPlanResponse> {
    const res = await apiClient<RejectPlanResponse>(`/agent/plan/${planId}/reject`, {
      method: 'POST',
    });
    if (res.error || !res.data) {
      throw new Error(res.error || 'Failed to reject plan');
    }
    return res.data;
  },

  async scanResource(resourceId: string): Promise<SafeShareScanResultDto> {
    const res = await apiClient<SafeShareScanResultDto>('/agent/safe-share/scan', {
      method: 'POST',
      body: { resourceId },
    });
    if (res.error || !res.data) {
      throw new Error(res.error || 'Failed to scan resource');
    }
    return res.data;
  },

  async transcribeAudio(audioBlob: Blob): Promise<TranscribeResultDto> {
    const token = localStorage.getItem('accessToken');
    const formData = new FormData();
    formData.append('audio', audioBlob, 'recording.webm');

    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE_URL}/api/agent/transcribe`, {
      method: 'POST',
      headers,
      body: formData,
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      throw new Error(err.message || err.error || 'Transcription failed');
    }

    return await response.json();
  },
};
