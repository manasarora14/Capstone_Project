import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { RequestService } from '../../services/request-service';
import { AuthService } from '../../services/auth-service';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ServiceRequest, RequestStatus, Priority } from '../../models/service-models';

@Component({
  selector: 'app-service-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatDividerModule,
    MatSnackBarModule
  ],
  templateUrl: './service-details.html',
  styleUrls: ['./service-details.css']
})
export class ServiceDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private requestService = inject(RequestService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  request = signal<ServiceRequest | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadServiceRequest(parseInt(id, 10));
    } else {
      this.error.set('Invalid service request ID');
      this.loading.set(false);
    }
  }

  loadServiceRequest(id: number) {
    this.loading.set(true);
    this.error.set(null);
    
    this.requestService.getServiceRequestById(id).subscribe({
      next: (data) => {
        const anyD = data as any;
        const withNames = {
          ...data,
          customerName: anyD.customerName || anyD.customer?.name || anyD.customerEmail || anyD.customerId,
          technicianName: anyD.technicianName || anyD.technician?.name || anyD.technicianEmail || anyD.technicianId
        };
        this.request.set(withNames as ServiceRequest);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Error loading service request', err);
        this.error.set('Failed to load service request. You may not have permission to view this request.');
        this.loading.set(false);
      }
    });
  }

  getStatusName(status: RequestStatus): string {
    switch (status) {
      case RequestStatus.Requested: return 'Requested';
      case RequestStatus.Assigned: return 'Assigned';
      case RequestStatus.InProgress: return 'In Progress';
      case RequestStatus.Completed: return 'Completed';
      case RequestStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  // Prefer backend-provided `statusName` when available
  getStatusDisplay(reqOrStatus: any): string {
    const anyR = reqOrStatus ?? {};
    if (anyR?.statusName) {
      return String(anyR.statusName).replace(/([a-z])([A-Z])/g, '$1 $2');
    }
    const status = typeof reqOrStatus === 'number' ? reqOrStatus : anyR?.status;
    return this.getStatusName(status as RequestStatus);
  }

  getStatusClass(reqOrStatus: any): string {
    const anyR = reqOrStatus ?? {};
    const sName = anyR?.statusName ? String(anyR.statusName).toLowerCase() : null;
    if (sName) {
      if (sName.includes('closed') || sName.includes('completed')) return 'status-completed';
      if (sName.includes('inprogress') || sName.includes('in progress')) return 'status-progress';
      if (sName.includes('assigned')) return 'status-assigned';
      if (sName.includes('cancel')) return 'status-cancelled';
    }
    const status = typeof reqOrStatus === 'number' ? reqOrStatus : anyR?.status;
    switch (status) {
      case RequestStatus.Completed: return 'status-completed';
      case RequestStatus.InProgress: return 'status-progress';
      case RequestStatus.Assigned: return 'status-assigned';
      case RequestStatus.Cancelled: return 'status-cancelled';
      default: return 'status-requested';
    }
  }

  getPriorityLabel(priority: Priority | number | null | undefined): string {
    if (priority === null || priority === undefined) return 'Not set';
    const p = Number(priority);
    switch (p) {
      case Priority.Medium: return 'Medium';
      case Priority.High: return 'High';
      case Priority.Low: return 'Low';
      default: return 'Low';
    }
  }

  getPriorityClass(priority: Priority | number | null | undefined): string {
    if (priority === null || priority === undefined) return 'priority-low';
    const p = Number(priority);
    switch (p) {
      case Priority.High: return 'priority-high';
      case Priority.Medium: return 'priority-medium';
      default: return 'priority-low';
    }
  }

  getTotalPrice(r: ServiceRequest): number {
    const serverTotal = (r as any).totalPrice;
    if (serverTotal !== undefined && serverTotal !== null) return serverTotal;
    const base = r.category?.baseCharge ?? 0;
    // Priority no longer adds any extra charge (backend updated to ignore priority fees)
    return base;
  }

  getPrioritySurcharge(priority: Priority | number): number {
    // kept for compatibility but priority surcharge is deprecated
    return 0;
  }

  canCancel(r: ServiceRequest | null): boolean {
    if (!r) return false;
    return r.status === RequestStatus.Requested || r.status === RequestStatus.Assigned;
  }

  cancelRequest() {
    const req = this.request();
    if (!req) return;

    const ref = this.snackBar.open('Cancel this request?', 'Confirm', { duration: 6000 });
    ref.onAction().subscribe(() => {
      this.requestService.cancelRequest(req.id).subscribe({
        next: () => {
          this.snackBar.open('Request cancelled successfully', 'Close', { duration: 2500 });
          this.loadServiceRequest(req.id);
        },
        error: (err: any) => {
          console.error(err);
          this.snackBar.open('Failed to cancel request', 'Close', { duration: 3000 });
        }
      });
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  formatDate(dateString: string | undefined): string {
  if (!dateString) return 'N/A';
  
  const date = new Date(dateString);
  return date.toLocaleString(); 
}

  getActualDuration(): string | null {
  const req = this.request() as any; 
  if (!req) return null;
  
  if (req.duration) {
    return req.duration;
  }
  
  
  const started = req.workStartedAt;
  const ended = req.workEndedAt;
  
  if (!started || !ended) return null;
  
  const startDate = new Date(started);
  const endDate = new Date(ended);
  const diffMs = endDate.getTime() - startDate.getTime();
  
  if (diffMs <= 0) return '0s';
  
  const hours = Math.floor(diffMs / 3600000);
  const minutes = Math.floor((diffMs % 3600000) / 60000);
  
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
}
  getUserRole(): string {
    return this.authService.userRole();
  }

  isAssigned(r: ServiceRequest | any): boolean {
    if (!r) return false;
    if (r.status !== undefined && r.status !== null) {
      return r.status === RequestStatus.Assigned;
    }
    const sName = (r.statusName || '').toString().toLowerCase();
    return sName.includes('assigned');
  }

  

  isCustomer(): boolean {
    return this.getUserRole() === 'Customer';
  }

  isTechnician(): boolean {
    return this.getUserRole() === 'Technician';
  }

  isManagerOrAdmin(): boolean {
    const role = this.getUserRole();
    return role === 'Manager' || role === 'Admin';
  }
}

