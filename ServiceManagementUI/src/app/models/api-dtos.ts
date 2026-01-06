
export interface CreateRequestDto {
  issueDescription: string;
  categoryId: number;

  priority: number;
  scheduledDate: string;
  scheduledTime?: string;
}

export interface TechnicianTaskDto {
  requestId: number;
  issueDescription: string;
  scheduledDate: string;
  completedAt?: string | null;
  totalPrice: number;
  status: string;
  customerName?: string;
  estimatedDurationHours?: number;
  plannedStartUtc?: string | null;
  workStartedAt?: string | null;
  workEndedAt?: string | null;
}

export interface TechnicianWorkloadDto {
  technicianId: string;
  totalHoursWorked: number;
  totalEarnings: number;
  previousTasks: TechnicianTaskDto[];
}

export interface RespondAssignmentDto {
  requestId: number;
  accept: boolean;
  plannedStartUtc?: string;
}

export interface TechnicianAvailabilityRange { startUtc: string; endUtc: string }
export interface TechnicianAvailability { technicianId: string; availableRanges?: TechnicianAvailabilityRange[] }
