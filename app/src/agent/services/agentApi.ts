import { apiClient } from '../../api/client';
import type {
  AgentActionPlanDto,
  ApprovePlanResponse,
  CreatePlanRequest,
  RejectPlanResponse,
  SafeShareScanResultDto,
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
};
