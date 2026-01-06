export interface StatusCount {
  status: string;
  count: number;
}

export interface RevenueData {
  month: string;
  total: number;
}

export interface TechnicianLoad {
  name: string;
  taskCount: number;
}

export interface CategoryCount {
  category: string;
  count: number;
}
export interface DashboardStats {
  statusCounts: StatusCount[];
  avgResolution: number;
  revenueReport: RevenueData[];
  technicianLoad: TechnicianLoad[];
  categoryCounts?: CategoryCount[];
  totalRevenue: number;
}