export interface UserDto {
  id: string | number;
  email: string;
  name?: string; // maps to ApplicationUser.FullName or similar
  role: string;
}

export interface AuthResponseDto {
  token: string;
  email: string;
  name?: string;
  role: string;
}

export interface ServiceRequestDto {
  id: number;
  issueDescription: string;
  statusName?: string;
  priority?: number;
  scheduledDate?: string;
  createdAt?: string;
  customerId?: string | number;
  technicianId?: string | number | null;
  customerName?: string;
  technicianName?: string | null;
  totalPrice?: number;
  categoryId?: number;
  categoryName?: string;
}

export interface ServiceCategoryDto {
  id: number;
  name: string;
  description?: string;
  baseCharge?: number;
  slaHours?: number;
}

export interface ProblemDetailsDto {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
