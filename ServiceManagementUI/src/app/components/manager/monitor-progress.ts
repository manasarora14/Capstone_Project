import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; 
import { RequestService } from '../../services/request-service';
import { AdminService } from '../../services/admin-service';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator'; 
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field'; 
import { MatSelectModule } from '@angular/material/select';
import { RouterModule } from '@angular/router';
import { ServiceRequest, RequestStatus, Priority } from '../../models/service-models';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs'; 

@Component({
  selector: 'app-monitor-progress',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatTableModule, MatChipsModule, MatIconModule, 
    MatCardModule, MatButtonModule, MatPaginatorModule, MatInputModule, 
    MatFormFieldModule, MatSelectModule, FormsModule
  ],
  templateUrl: './monitor-progress.html',
  styleUrl: './monitor-progress.css'
})
export class MonitorProgress implements OnInit {
  private requestService = inject(RequestService);
  private adminService = inject(AdminService);
  private categoryPriceMap: Map<number, number> = new Map();
  
  
  allRequests = signal<any[]>([]);
  totalRecords = signal<number>(0);
  pageSize = 10;
  currentPage = 1;
  searchQuery = '';
  selectedPriority: number | null = null;
  Priority = Priority;
  private searchSubject = new Subject<string>();

  displayedColumns: string[] = ['id', 'customer', 'technician', 'priority', 'status', 'pricing', 'notes', 'actions'];

  constructor() {
    
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(value => {
      this.searchQuery = value;
      this.currentPage = 1;
      this.loadMonitorData();
    });
  }

  ngOnInit() {
    this.loadCategories();
    this.loadMonitorData();
  }

  private loadCategories() {
    this.requestService.getCategories().subscribe({
      next: (cats: any[]) => {
        (cats || []).forEach(c => {
          if (c && c.id != null) this.categoryPriceMap.set(Number(c.id), Number(c.baseCharge ?? 0));
        });
      },
      error: () => { }
    });
  }

  onSearchChange(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onPriorityChange(value: number | null) {
    this.selectedPriority = value;
    this.currentPage = 1;
    this.loadMonitorData();
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadMonitorData();
  }

  loadMonitorData() {
    this.adminService.getAllUsers().subscribe({
      next: (users) => {
        const userMap = new Map<string, any>();
        users.forEach((u: { id: any; }) => userMap.set(String(u.id), u));

       
        console.debug('Loading monitor data', { page: this.currentPage, size: this.pageSize, search: this.searchQuery, priority: this.selectedPriority });
        this.requestService.getAllRequests(this.currentPage, this.pageSize, this.searchQuery, undefined, this.selectedPriority ?? undefined).subscribe({
          next: (res) => {
            const enriched = res.items.map(r => {
              const cust = userMap.get(String(r.customerId));
              const tech = userMap.get(String(r.technicianId));
              
              const serverTotal = (r as any).totalPrice ?? ((r as any).totalPrice === 0 ? 0 : undefined);
              let total = serverTotal;
              if (total === undefined || Number(total) === 0) {
                total = r.category?.baseCharge ?? this.categoryPriceMap.get(Number((r as any).categoryId)) ?? 0;
              }
              return {
                ...r,
                totalPrice: total,
                customerEmail: cust ? (cust.email || cust.name) : r.customerId,
                technicianEmail: tech ? (tech.email || tech.name) : r.technicianId,
                // prefer server-provided names, otherwise fallback to user map or id
                customerName: r.customerName || (cust ? this.formatName((cust.email || cust.name) as string) : String(r.customerId)),
                technicianName: r.technicianName || (tech ? this.formatName((tech.email || tech.name) as string) : '')
              };
            });
            // Apply client-side priority filter as a fallback if backend doesn't filter
            let filtered = enriched;
            if (this.selectedPriority !== null && this.selectedPriority !== undefined) {
              filtered = enriched.filter(it => Number(it.priority) === Number(this.selectedPriority));
            }

            this.allRequests.set(filtered);
            // If we applied client-side filtering, use the filtered length for paginator; otherwise use server total
            const total = (this.selectedPriority !== null && this.selectedPriority !== undefined) ? filtered.length : (res.totalCount ?? filtered.length);
            this.totalRecords.set(total);
          },
          error: (err) => console.error('Monitor error fetching requests:', err)
        });
      },
      error: (err) => console.error('Monitor error fetching users:', err)
    });
  }

  private formatName(raw?: string | null): string {
    if (!raw) return '';
    // strip domain if email
    const beforeAt = raw.split('@')[0];
    if (!beforeAt) return raw;
    const trimmed = String(beforeAt).trim();
    return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
  }

 
  getPriorityLabel(priority: Priority | number | null | undefined): string {
    if (priority === null || priority === undefined) return 'Not set';
    const p = Number(priority);
    if (p === Priority.Medium || p === 1) return 'Medium';
    if (p === Priority.High || p === 2) return 'High';
    return 'Low';
  }

  getStatusName(reqOrStatus: ServiceRequest | RequestStatus | number): string {
    const status = typeof reqOrStatus === 'object' ? Number(reqOrStatus.status) : Number(reqOrStatus);
    const statuses: Record<number, string> = {
        [RequestStatus.Requested]: 'Requested',
        [RequestStatus.Assigned]: 'Assigned',
        [RequestStatus.InProgress]: 'In-Progress',
        [RequestStatus.Completed]: 'Completed',
        [RequestStatus.Cancelled]: 'Cancelled',
        [5]: 'Closed'
    };
    return statuses[status] || 'Unknown';
  }

  getStatusClass(reqOrStatus: ServiceRequest | RequestStatus | number): string {
    const status = typeof reqOrStatus === 'object' ? Number(reqOrStatus.status) : Number(reqOrStatus);
    switch (status) {
        case 5: case RequestStatus.Completed: return 'status-completed';
        case RequestStatus.InProgress: return 'status-progress';
        case RequestStatus.Assigned: return 'status-assigned';
        case RequestStatus.Requested: return 'status-requested';
        case RequestStatus.Cancelled: return 'status-cancelled';
        default: return 'status-default';
    }
  }

  getTotalPrice(r: ServiceRequest): number | string {
    const serverTotal = (r as any).totalPrice;
    if (serverTotal !== undefined && serverTotal !== null) return serverTotal;
    const base = r.category?.baseCharge ?? (r as any).categoryId ? (this.categoryPriceMap.get(Number((r as any).categoryId)) ?? 0) : 0;
    return base;
  }

  isAssigned(r: ServiceRequest | any): boolean {
    const status = typeof r === 'object' ? Number(r.status) : Number(r);
    if (!isNaN(status)) return status === RequestStatus.Assigned;
    const sName = (r?.statusName || '').toString().toLowerCase();
    return sName.includes('assign');
  }
}