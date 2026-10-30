export type PlanStatus =
  | 'Draft'
  | 'AwaitingApproval'
  | 'Executing'
  | 'Completed'
  | 'Rejected'
  | 'Failed';

export type StepStatus =
  | 'Pending'
  | 'Running'
  | 'Completed'
  | 'Failed'
  | 'Skipped';

export type SafeShareStatus =
  | 'Clean'
  | 'Warning'
  | 'Blocked';

export interface PlanStepDto {
  id?: string;
  stepNumber: number;
  toolName: string;
  parameters: Record<string, any>;
  requiresApproval: boolean;
  status: StepStatus;
  description?: string;
  result?: any;
  errorMessage?: string;
  startedAt?: string;
  finishedAt?: string;
}

export interface AgentActionPlanDto {
  id: string;
  planId?: string;
  organizationId: string;
  userId: string;
  userGoal: string;
  status: PlanStatus;
  summary?: string;
  createdAt: string;
  approvedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  steps: PlanStepDto[];
  requiresUserApproval?: boolean;
}

export interface CreatePlanRequest {
  userGoal: string;
  preferredModel?: string;
}

export interface ApprovePlanResponse {
  planId: string;
  status: PlanStatus;
  message: string;
}

export interface RejectPlanResponse {
  planId: string;
  status: PlanStatus;
  message: string;
}

export interface SafeShareScanRequest {
  resourceId: string;
}

export interface SafeShareScanResultDto {
  resourceId: string;
  fileName?: string;
  status: SafeShareStatus;
  riskScore: number;
  detectedThreats: string[];
  canShare: boolean;
  message: string;
  scannedAt: string;
}

export interface AgentPlanStatusEvent {
  planId: string;
  status: PlanStatus;
  message: string;
}

export interface AgentStepStartedEvent {
  planId: string;
  stepNumber: number;
  toolName: string;
  description?: string;
}

export interface AgentStepCompletedEvent {
  planId: string;
  stepNumber: number;
  toolName: string;
  result?: any;
}

export interface AgentStepFailedEvent {
  planId: string;
  stepNumber: number;
  toolName: string;
  errorMessage: string;
}

export interface AgentPlanFinishedEvent {
  planId: string;
  status: PlanStatus;
  completedSteps: number;
  totalSteps: number;
  summary?: string;
}
