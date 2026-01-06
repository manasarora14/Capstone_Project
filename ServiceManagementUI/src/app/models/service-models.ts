export interface User {
id: any;
  email: string;
  name?: string;
  displayName?: string;
  role: string;
  token: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  name?: string;
  role: string;
}


export enum RequestStatus {
    Requested = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4, 
    Closed = 5
}


export enum Priority {
  Low = 0,
  Medium = 1,
  High = 2
}

export interface ServiceCategory {
  id: number;
  name: string;
  description: string;
  baseCharge: number;
  slaHours: number;
  displaySla?: string;
}

export interface ServiceRequest {
  id: number;
  issueDescription: string;
  status: RequestStatus;
  priority: Priority;
  scheduledDate: string;
  createdAt: string;
  customerId: string;
  technicianId?: string;
  customerName?: string;
  technicianName?: string;
  category?: ServiceCategory;
  resolutionNotes?: string;
}

export interface TechnicianWorkload {
  hoursWorked: number;
  earnings: number;
  recentTasks?: Array<{
    id: number;
    scheduledDate: string;
    customerEmail?: string;
    amount?: number;
    status?: RequestStatus;
  }>;
}