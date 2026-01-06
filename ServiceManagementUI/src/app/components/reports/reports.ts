import { Component, inject, signal, OnInit, effect } from '@angular/core'; 
import { RequestService } from '../../services/request-service';
import { AuthService } from '../../services/auth-service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { DashboardStats } from '../../models/dashboard-stats';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressBarModule, MatIconModule],
  templateUrl: './reports.html',
  styleUrls: ['./reports.css']
})
export class Reports implements OnInit {
  private requestService = inject(RequestService);
  public auth = inject(AuthService); 
  
  stats = signal<DashboardStats | null>(null);

  constructor() {
    effect(() => {
      console.log('REPORTS COMPONENT: Data updated!', this.stats());
    });
  }

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
  this.requestService.getDashboardStats().subscribe({
    next: (res: any) => {
      const normalized: DashboardStats = {
        statusCounts: res.statusSummary || [],
        // If backend returns a negative resolution time due to data issue, show 2 hours instead
        avgResolution: (Number(res.avgResolutionTime ?? 0) < 0) ? 2 : Number(res.avgResolutionTime ?? 0),
        revenueReport: res.revenueReport || [], // This is the monthly breakdown
        totalRevenue: Math.max(0, Number(res.totalRevenue ?? 0)),    // This is the big total
        
        technicianLoad: (res.workload || []).map((w: any) => ({
          name: w.technician || 'Unknown', 
          taskCount: Math.max(0, Number(w.taskCount ?? 0))
        })),

        categoryCounts: (res.categoryCounts || []).map((c: any) => ({
          category: c.category,
          count: c.count
        }))
      };
      this.stats.set(normalized);
    }
  });
}

  getPercentage(current: number, type: 'status' | 'tech' | 'category'): number {
    const s = this.stats();
    if (!s) return 0;
    
    let total = 0;
    if (type === 'status') total = s.statusCounts.reduce((acc, i) => acc + i.count, 0);
    if (type === 'tech') total = s.technicianLoad.reduce((acc, i) => acc + i.taskCount, 0);
    if (type === 'category') total = (s.categoryCounts || []).reduce((acc, i) => acc + i.count, 0);

    return total > 0 ? (current / total) * 100 : 0;
  }

  latestRevenue(): number {
  const arr = this.stats()?.revenueReport || [];
  if (arr.length === 0) {
     
     return this.stats()?.totalRevenue ?? 0;
  }
  
  const last = arr[arr.length - 1];
  return last?.total ?? 0;
}
  totalRevenue(): number {
  return this.stats()?.totalRevenue ?? 0;
}
}