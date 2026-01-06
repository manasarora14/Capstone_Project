import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatListModule } from '@angular/material/list';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator'; // Added
import { RequestService } from '../../services/request-service';
import { AuthService } from '../../services/auth-service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  selector: 'app-tech-workload',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatIconModule, MatTableModule, 
    MatListModule, MatSnackBarModule, MatPaginatorModule
  ],
  templateUrl: './workload.html',
  styleUrls: ['./workload.css']
})
export class TechnicianWorkload implements OnInit {
  private req = inject(RequestService);
  private snack = inject(MatSnackBar);
  
  private categoryPriceMap: Map<number, number> = new Map();

  data = signal<{ hoursWorked: number; earnings: number } | null>(null);
  recentTasks = signal<any[]>([]);
  confirmedEarnings = signal<number>(0);
  displayedColumns = ['id', 'scheduled', 'customer', 'status', 'amount'];

  
  totalTasks = signal<number>(0);
  pageSize = 5; 
  currentPage = 1;

  ngOnInit() {
    this.loadCategories();
    this.load();
  }

  private loadCategories() {
    
    this.req.getCategories().subscribe({
      next: (cats: any[]) => {
        (cats || []).forEach(c => {
          if (c && c.id != null) this.categoryPriceMap.set(Number(c.id), Number(c.baseCharge ?? 0));
        });
      },
      error: () => { /* ignore */ }
    });
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  load() {
    
    this.req.getTechnicianWorkload(this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res) {
          const rawTasks = res.recentTasks?.items || res.previousTasks || [];
          
          const processedTasks = rawTasks.map((it: any) => {
            const raw = it.customerName || it.customer?.name || it.customerEmail || it.customer?.email || it.customerId;
            const rawDur = Number(it.duration ?? it.durationHours ?? 0);
            const dur = (rawDur < 0) ? 2 : rawDur;
            return {
              id: it.requestId,
              scheduledDate: it.scheduledDate,
              customerName: this.formatDisplayName(raw),
              amount: it.totalPrice || 0,
              status: Number(it.status),
              duration: dur
            };
          });

          const hw = Number(res.totalHoursWorked ?? 0);
          this.data.set({ 
            hoursWorked: (hw < 0) ? 2 : hw,
            earnings: Math.max(0, Number(res.totalEarnings ?? 0)) 
          });
          
          this.recentTasks.set(processedTasks);
          
          this.totalTasks.set(res.recentTasks?.totalCount || rawTasks.length);
                  const missing = processedTasks.filter((t: any) => !t.amount || Number(t.amount) === 0);
          if (missing.length > 0) {
            missing.forEach((m: any) => {
              this.req.getServiceRequestById(m.id).subscribe({
                next: (detail: any) => {
                  
                  let amt = Number(detail?.invoice?.amount ?? detail?.totalPrice ?? 0);
                  if (!amt || Number(amt) === 0) {
                    amt = Number(detail?.category?.baseCharge ?? 0);
                  }
                  if ((!amt || Number(amt) === 0) && detail?.categoryId) {
                    const bc = this.categoryPriceMap.get(Number(detail.categoryId));
                    if (bc != null) amt = bc;
                  }
                  amt = Math.max(0, Number(amt));

                 
                  this.recentTasks.update(prev => prev.map(p => p.id === m.id ? { ...p, amount: amt } : p));

                 
                  const current = this.recentTasks();
                  const confirmedNow = current
                    .filter((t: any) => Number(t.status) === 5)
                    .reduce((s: number, t: any) => s + Math.max(0, Number(t.amount) || 0), 0);
                  this.confirmedEarnings.set(confirmedNow);
                },
                error: () => { /* ignore per-row failures */ }
              });
            });
          }

          
          const confirmed = processedTasks
            .filter((t: any) => Number(t.status) === 5)
            .reduce((s: number, t: any) => s + (Number(t.amount) || 0), 0);
          this.confirmedEarnings.set(confirmed);
        }
      },
      error: (err) => {
        this.snack.open('Failed to load workload', 'Close', { duration: 3000 });
      }
    });
  }

  formatStatus(s: number): string {
    const statuses: Record<number, string> = {
      0: 'Requested', 1: 'Assigned', 2: 'In Progress',
      3: 'Completed (Pending Pay)', 4: 'Cancelled', 5: 'Closed (Paid)'
    };
    return statuses[s] || 'Unknown';
  }

  formatName(raw?: string | null): string {
    if (!raw) return '';
    return this.formatDisplayName(raw);
  }

  private formatDisplayName(raw?: string | null): string {
    if (!raw) return '';
    const s = String(raw).trim();
    // if email, strip domain
    if (s.includes('@')) {
      const namePart = s.split('@')[0].trim();
      return namePart.length === 0 ? s : (namePart.charAt(0).toUpperCase() + namePart.slice(1));
    }
    // capitalize first letter of the string
    return s.charAt(0).toUpperCase() + s.slice(1);
  }
}