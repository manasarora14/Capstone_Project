import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RequestService } from '../../services/request-service';
import { MatTableModule } from '@angular/material/table'; // Essential
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { ServiceRequest, RequestStatus, Priority } from '../../models/service-models';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule, 
    MatTableModule, // Ensure this is here
    MatButtonModule, 
    MatIconModule, 
    MatInputModule, 
    MatFormFieldModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './task-list.html',
  styleUrls: ['./task-list.css']
})
export class TaskList implements OnInit {
  private requestService = inject(RequestService);
  private snackBar = inject(MatSnackBar);

  RequestStatus = RequestStatus;
  tasks = signal<(ServiceRequest & { tempNotes?: string; assignmentResponse?: 'accepted' | 'rejected' | null })[]>([]);

  ngOnInit() {
    this.loadTasks();
  }

  loadTasks() {
    this.requestService.getTechnicianTasks().subscribe((res: any) => {
      const raw = Array.isArray(res) ? res : (res?.items ?? res?.data ?? res?.results ?? res?.content ?? []);
      const mappedTasks = (raw as ServiceRequest[]).map(t => {
        const anyT = t as any;
        // normalize status: handle numeric, string or statusName
        let resolvedStatus: number;
        if (anyT.status === undefined || anyT.status === null) {
          const sName = (anyT.statusName || anyT.statusName || '').toString().toLowerCase();
          if (sName.includes('assigned')) resolvedStatus = RequestStatus.Assigned;
          else if (sName.includes('inprogress') || sName.includes('in progress')) resolvedStatus = RequestStatus.InProgress;
          else if (sName.includes('completed') || sName.includes('closed')) resolvedStatus = RequestStatus.Completed;
          else if (sName.includes('cancel')) resolvedStatus = RequestStatus.Cancelled;
          else resolvedStatus = RequestStatus.Requested;
        } else {
          const num = Number(anyT.status);
          if (!isNaN(num)) resolvedStatus = num;
          else {
            const s = String(anyT.status).toLowerCase();
            if (s.includes('assigned')) resolvedStatus = RequestStatus.Assigned;
            else if (s.includes('inprogress') || s.includes('in progress')) resolvedStatus = RequestStatus.InProgress;
            else if (s.includes('completed') || s.includes('closed')) resolvedStatus = RequestStatus.Completed;
            else if (s.includes('cancel')) resolvedStatus = RequestStatus.Cancelled;
            else resolvedStatus = RequestStatus.Requested;
          }
        }
        // normalize duration: if negative, show 2 hours
        let rawDur = Number(anyT.duration ?? anyT.durationHours ?? 0);
        // if duration not present but start/end timestamps exist, compute hours
        if ((!rawDur || rawDur === 0) && anyT.workStartedAt && anyT.workEndedAt) {
          try {
            const s = Date.parse(anyT.workStartedAt);
            const e = Date.parse(anyT.workEndedAt);
            if (!isNaN(s) && !isNaN(e)) rawDur = (e - s) / 3600000;
          } catch { /* ignore */ }
        }
        const dur = (rawDur < 0) ? 2 : rawDur;
        const resolvedResolution = anyT.resolutionNotes ?? anyT.resolution_note ?? anyT.resolutionNote ?? anyT.notes ?? anyT.note ?? '';
        const resolvedTemp = anyT.tempNotes ?? anyT.temp_notes ?? resolvedResolution ?? '';
        return ({ 
          ...t,
          status: resolvedStatus,
          tempNotes: resolvedTemp,
          resolutionNotes: resolvedResolution,
          assignmentResponse: null,
          customerName: anyT.customerName || anyT.customer?.fullName || anyT.customer?.userName || null,
          duration: dur
        });
      });
      this.tasks.set(mappedTasks);
    }, () => {
      this.snackBar.open('Error loading tasks from server.', 'Close', { duration: 3000 });
    });
  }

  acceptTask(requestId: number) {
    
    const svcAny = this.requestService as any;
    if (typeof svcAny.acceptAssignment === 'function') {
      svcAny.acceptAssignment(requestId).subscribe({
        next: () => this.markAssignmentResponse(requestId, 'accepted'),
        error: () => this.markAssignmentResponse(requestId, 'accepted')
      });
    } else {
      
      this.markAssignmentResponse(requestId, 'accepted');
    }
  }

  rejectTask(requestId: number) {
    const svcAny = this.requestService as any;
    if (typeof svcAny.rejectAssignment === 'function') {
      svcAny.rejectAssignment(requestId).subscribe({
        next: () => this.markAssignmentResponse(requestId, 'rejected'),
        error: () => this.markAssignmentResponse(requestId, 'rejected')
      });
    } else {
      
      this.markAssignmentResponse(requestId, 'rejected');
    }
  }

  private markAssignmentResponse(requestId: number, resp: 'accepted' | 'rejected') {
    const updated = this.tasks().map(t => t.id === requestId ? { ...t, assignmentResponse: resp } : t);
    this.tasks.set(updated);
    const msg = resp === 'accepted' ? 'Task accepted' : 'Task rejected';
    this.snackBar.open(msg, 'OK', { duration: 2000 });
  }

 

startWork(requestId: number) {
  this.requestService.startWork(requestId).subscribe({
    next: () => {
      this.snackBar.open('Timer started! Work is now In Progress.', 'OK', { duration: 2000 });
      this.loadTasks(); 
    },
    error: () => this.snackBar.open('Could not start work.', 'Close', { duration: 3000 })
  });
}

// Update this in task-list.ts
// Update this in task-list.ts
finishWork(requestId: number) {
  const task = this.tasks().find(t => t.id === requestId);
  const notes = task ? (task.tempNotes || '') : '';

  // Now this call matches the new 2-argument signature
  this.requestService.finishWork(requestId, notes).subscribe({
    next: () => {
      this.snackBar.open('Task finished and notes saved!', 'OK', { duration: 2000 });
      this.loadTasks(); 
    },
    error: () => this.snackBar.open('Could not finish work.', 'Close', { duration: 3000 })
  });
}

  getPriorityName(priority: Priority | number | null | undefined): string {
    if (priority === null || priority === undefined) return 'Not set';
    const names = ['Low', 'Medium', 'High', 'Critical'];
    return names[Number(priority)] || 'Standard';
  }

  formatStatus(s: number | string | undefined | null): string {
    const status = Number(s);
    const statuses: Record<number, string> = {
      0: 'Requested', 1: 'Assigned', 2: 'In Progress',
      3: 'Completed (Pending Pay)', 4: 'Cancelled', 5: 'Closed (Paid)'
    };
    return statuses[status] || 'Unknown';
  }

  updateStatus(requestId: number, newStatus: RequestStatus, notes: string = "") {
    this.requestService.updateStatus(requestId, newStatus, notes).subscribe({
      next: () => {
        const action = newStatus === RequestStatus.Completed ? 'completed' : 'started';
        this.snackBar.open(`Task ${action} successfully!`, 'OK', { duration: 2000 });
        this.loadTasks(); 
      },
      error: () => {
        this.snackBar.open('Failed to update task status.', 'Close', { duration: 3000 });
      }
    });
  }
}