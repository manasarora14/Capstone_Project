import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestService } from '../../services/request-service';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterModule } from '@angular/router';
import { ServiceRequest, RequestStatus, Priority } from '../../models/service-models';
import { MatPaginatorModule, PageEvent, MatPaginator } from '@angular/material/paginator';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
@Component({
  selector: 'app-my-services',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatChipsModule,
    MatCardModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    FormsModule,
    MatSnackBarModule,
    MatPaginator
],
  templateUrl: './my-service.html',
  styleUrl: './my-service.css'
})
export class MyServices implements OnInit {
 
  

  constructor() {
    
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(query => {
      this.searchQuery = query;
      this.currentPage = 1;
      this.loadMyRequests();
    });
  }
  totalCount = signal<number>(0); 
  pageSize = 10;
  currentPage = 1;
  searchQuery = '';
  private searchSubject = new Subject<string>();
  requests = signal<ServiceRequest[]>([]);
  displayedColumns: string[] = ['id', 'category', 'priority', 'date', 'status', 'total', 'actions'];
  private categoryPriceMap: Map<number, number> = new Map();

  
  reschedulingId: number | null = null;
  rescheduleDate: Date = new Date();
  rescheduleTime: string = new Date().toTimeString().slice(0,5);

  private requestService = inject(RequestService);
  private snackBar = inject(MatSnackBar);

  ngOnInit() {
    this.loadCategories();
    this.loadMyRequests();
  }

  private loadCategories() {
    this.requestService.getCategories().subscribe({
      next: (cats: any[]) => {
        (cats || []).forEach(c => {
          if (c && c.id != null) this.categoryPriceMap.set(Number(c.id), Number(c.baseCharge ?? 0));
        });
      },
      error: () => { /* ignore */ }
    });
  }
  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadMyRequests();
  }

  loadMyRequests() {
    
    this.requestService.getMyRequests(this.currentPage, this.pageSize, this.searchQuery).subscribe({
      next: (res: any) => {
        
        const items = (res.items || []).map((r: any) => {
          const rp = { ...r };
          
          let p = rp.priority;
          if (p == null) p = (rp.priorityName ?? rp.priorityLabel ?? rp.priorityText ?? null);
          if (typeof p === 'string') {
            const s = p.toLowerCase();
            if (s.includes('med')) p = 1;
            else if (s.includes('high')) p = 2;
            else if (s.includes('low')) p = 0;
            else p = null;
          }
          rp.priority = Number.isFinite(Number(p)) ? Number(p) : null;

          const serverTotal = rp.totalPrice ?? (rp.totalPrice === 0 ? rp.totalPrice : undefined);
          if (serverTotal === undefined || Number(serverTotal) === 0) {
            // prefer category baseCharge from payload, else fallback to preloaded map
            const fallback = rp.category?.baseCharge ?? this.categoryPriceMap.get(Number(rp.categoryId)) ?? 0;
            rp.totalPrice = fallback;
          }
          return rp as ServiceRequest;
        });
        this.requests.set(items);
        this.totalCount.set(res.totalCount);
      },
      error: (err) => console.error('Error fetching requests', err)
    });
  }

  getStatusName(status: RequestStatus): string {
  
  console.log('Status received:', status); 
  
  switch (status) {
    case RequestStatus.Requested: return 'Requested';
    case RequestStatus.Assigned: return 'Assigned';
    case RequestStatus.InProgress: return 'In-Progress';
    case RequestStatus.Completed: return 'Completed';
    case RequestStatus.Cancelled: return 'Cancelled'; 
    default: return 'Unknown';
  }
}

  
getStatusDisplay(r: ServiceRequest): string {
    const status = Number(r.status); // Ensure it's a number
    switch (status) {
        case 0: return 'Requested';
        case 1: return 'Assigned';
        case 2: return 'In-Progress';
        case 3: return 'Completed';
        case 4: return 'Cancelled';
        case 5: return 'Closed';
        default: return 'Unknown';
    }
}


  getStatusClass(r: ServiceRequest): string {
  const anyR = r as any;
  
  if (anyR?.statusName) {
    const sName = String(anyR.statusName).toLowerCase();
    if (sName === 'completed' || sName === 'closed') return 'status-completed';
    if (sName === 'inprogress') return 'status-progress';
    if (sName === 'assigned') return 'status-assigned';
    if (sName === 'cancelled') return 'status-cancelled'; 
  }

  
  switch (r.status) {
    case RequestStatus.Completed: return 'status-completed';
    case RequestStatus.InProgress: return 'status-progress';
    case RequestStatus.Assigned: return 'status-assigned';
    case RequestStatus.Cancelled: return 'status-cancelled'; 
    default: return 'status-requested';
  }
}

  getPriorityLabel(priority: Priority | number | null | undefined): string {
    if (priority === null || priority === undefined) return 'Not set';
    const p = typeof priority === 'number' ? priority : Number(priority);
     if (p === Priority.Medium || p === 1) return 'Medium';
     if (p === Priority.High || p === 2) return 'High';
     return 'Low';
  }

  getPrioritySurcharge(priority: Priority | number): number {
    
    return 0;
  }

  getTotalPrice(r: ServiceRequest): number | string {
    
    const serverTotal = (r as any).totalPrice;
    if (serverTotal !== undefined && serverTotal !== null) return serverTotal;
    const catId = (r as any).categoryId ?? r.category?.id;
    const base = r.category?.baseCharge ?? (catId != null ? (this.categoryPriceMap.get(Number(catId)) ?? 0) : 0);
    return base;
  }

  canCancel(r: ServiceRequest) {
    return r.status === RequestStatus.Requested || r.status === RequestStatus.Assigned;
  }

  cancelRequest(id: number): void {
  
  const ref = this.snackBar.open('Cancel this request?', 'Confirm', { duration: 6000 });
  ref.onAction().subscribe(() => {
    this.requestService.cancelRequest(id).subscribe({
      next: () => {
        this.snackBar.open('Request cancelled successfully', 'Close', { duration: 2500, panelClass: ['success-snackbar'] });
        this.loadMyRequests();
      },
      error: (err: any) => {
        console.error('Cancellation Error:', err);
        const errorMessage = err?.error?.message || 'Failed to cancel request. It might already be in progress.';
        this.snackBar.open(errorMessage, 'Close', { duration: 4000, panelClass: ['error-snackbar'] });
      }
    });
  });

  }

  startReschedule(r: ServiceRequest) {
    this.reschedulingId = r.id;
    this.rescheduleDate = r.scheduledDate ? new Date(r.scheduledDate) : new Date();
    
    const d = r.scheduledDate ? new Date(r.scheduledDate) : null;
    this.rescheduleTime = d ? d.toTimeString().slice(0,5) : new Date().toTimeString().slice(0,5);
  }

  submitReschedule(id: number) {
    const scheduled = new Date(this.rescheduleDate);
    if (this.rescheduleTime) {
      const parts = this.rescheduleTime.split(':').map(Number);
      if (parts.length >= 2) scheduled.setHours(parts[0], parts[1], 0, 0);
    }
    const iso = scheduled.toISOString();
    this.requestService.rescheduleRequest(id, iso).subscribe({
      next: () => {
        this.snackBar.open('Request rescheduled', 'Close', { duration: 2500 });
        this.reschedulingId = null;
        this.loadMyRequests();
      },
      error: (err) => {
        console.error(err);
        this.snackBar.open('Failed to reschedule', 'Close', { duration: 3000 });
      }
    });
  }

  cancelReschedule() {
    this.reschedulingId = null;
  }
}