import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RequestService } from '../../services/request-service';
import { AdminService } from '../../services/admin-service';
import { ServiceRequest, User, RequestStatus } from '../../models/service-models';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-assign-request',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSelectModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatSnackBarModule,
    MatCardModule,
    MatInputModule,
    FormsModule
],
  templateUrl: './assign-request.html',
  styleUrl: './assign-request.css'
})
export class AssignRequest implements OnInit {
  private requestService = inject(RequestService);
  private adminService = inject(AdminService);
  private api = inject(ApiService);
  
  private snackBar = inject(MatSnackBar);

  pendingRequests = signal<any[]>([]); 
  technicians = signal<User[]>([]);
  monitorRecords = signal<any[]>([]);
  private _busyMap: Map<string, Array<{start:number,end:number}>> = new Map();
  
  displayedColumns: string[] = ['id', 'description', 'assignee', 'action'];

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    
    this.requestService.getAllRequests().subscribe({
      next: (res: any) => {
        
        const list: ServiceRequest[] = Array.isArray(res) ? res : (res?.items ?? res?.data ?? []);
        
        this.pendingRequests.set(list.filter((r: ServiceRequest) => r.status === RequestStatus.Requested));
      },
      error: (err) => this.showMsg('Error loading requests')
    });

    
    this.adminService.getAllUsers().subscribe({
      next: (res: User[]) => {
        const techList = res
          .filter(u => u.role === 'Technician')
          .map(u => ({
            ...u,
            displayName: (u as any).name || (u as any).fullName || (u as any).full_name || this.formatName((u as any).email)
          }));
        this.technicians.set(techList);
      },
      error: (err) => this.showMsg('Could not load technician list')
    });

    
    this.api.monitorAll().subscribe({
      next: (res: any) => {
        const records = (res || []) as any[];
        this.monitorRecords.set(records);
        this.buildBusyMap(records);
      },
      error: (err) => {
        console.warn('monitorAll failed', err);
        this.monitorRecords.set([]);
        this._busyMap.clear();
      }
    });
  }

  private buildBusyMap(records: any[]) {
    this._busyMap.clear();
    (records || []).forEach(r => {
      const techId = r.technicianId ?? r.assignedTechnicianId ?? null;
      if (!techId) return;
      
      
      const startIso = r.plannedStartUtc ?? r.scheduledDate;
      const start = startIso ? Date.parse(startIso) : null;
      
      
      const durationHours = r.category?.slaHours ?? 0;
      if (durationHours <= 0) return; 
      
      const end = start != null ? (start + Number(durationHours) * 3600000) : null;
      if (start == null || end == null) return;
      
      const arr = this._busyMap.get(String(techId)) ?? [];
      arr.push({ start, end });
      this._busyMap.set(String(techId), arr);
    });
  }

  
  isTechAvailableForSlot(techId: string, slotStartIso: string | null, durationHours: number) {
   
    return true;
  }

  private rangesOverlap(aStart: number, aEnd: number, bStart: number, bEnd: number) {
    return aStart < bEnd && bStart < aEnd;
  }

  getAvailableTechnicians(request: any) {
    
    const slotIso = request?.selectedPlannedLocal ? request.selectedPlannedLocal : request?.scheduledDate;
    
    
    const duration = Number(request?.category?.slaHours ?? 0);
    
   
    if (!duration || duration <= 0) {
      return this.technicians();
    }
    
    const all = this.technicians();
    if (!all || all.length === 0) return [];
    
    
    const filtered = all.filter(t => this.isTechAvailableForSlot(String(t.id), slotIso, duration));
    
    
    return (filtered.length > 0) ? filtered : all;
  }

  assign(requestId: number, techId: string) {
    if (!techId) {
      this.showMsg('Please select a technician first');
      return;
    }

    this.requestService.assignTechnician(requestId, techId).subscribe({
      next: () => {
        this.showMsg(`Request #${requestId} successfully assigned!`);
        this.loadData(); 
      },
      error: (err) => {
        const errorMsg = err?.error?.message || 'Failed to assign technician. Technician may be busy for the scheduled time.';
        this.showMsg(errorMsg);
      }
    });
  }

  formatName(raw?: string | null): string {
    if (!raw) return '';
    const beforeAt = String(raw).split('@')[0] || String(raw);
    const trimmed = beforeAt.trim();
    return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
  }

  

  private showMsg(msg: string) {
    this.snackBar.open(msg, 'Close', { duration: 3000 });
  }
}