import { Component, Input, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule, MatFormField } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { TechnicianTaskDto, RespondAssignmentDto } from '../../models/api-dtos';
import { toUtcIso } from '../../utils/time-utils';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatFormField],
  templateUrl: './task-detail.html',
  styleUrls: ['./task-detail.css']
})
export class TaskDetailComponent implements OnInit, OnDestroy {
  @Input() request!: TechnicianTaskDto | any;
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  running = false;
  elapsed = '';
  private _t: any = null;
  resolutionNotes: string = '';

  
  showAccept = false;
  plannedLocal: string | null = null; 
  acceptSubmitting = false;

  ngOnInit() {
    this.updateRunningState();
  }

  ngOnDestroy() {
    this.clearTimer();
  }

  private updateRunningState() {
    const started = !!this.request?.workStartedAt || !!this.request?.workStartedAt;
    const ended = !!this.request?.workEndedAt || !!this.request?.completedAt;
    this.running = !!started && !ended;
    if (this.running) this.startTimer(); else this.clearTimer();
  }

  startTimer() {
    this.clearTimer();
    this._t = setInterval(() => this.tick(), 1000);
    this.tick();
  }

  clearTimer() {
    if (this._t) { clearInterval(this._t); this._t = null; }
  }

  private tick() {
    const start = this.request?.workStartedAt || this.request?.workStartedAt || this.request?.startedAt;
    if (!start) { this.elapsed = ''; return; }
    const s = new Date(start);
    const diff = Date.now() - s.getTime();
    const hrs = Math.floor(diff / 3600000);
    const mins = Math.floor((diff % 3600000) / 60000);
    const secs = Math.floor((diff % 60000) / 1000);
    this.elapsed = `${hrs}h ${mins}m ${secs}s`;
  }

  onStart() {
    if (!this.request?.requestId && !this.request?.id) return;
    const id = this.request.requestId ?? this.request.id;
    this.api.startWork(id).subscribe({
      next: (res: any) => {
        this.request.workStartedAt = res?.workStartedAt ?? new Date().toISOString();
        this.updateRunningState();
        this.snack.open('Work started', 'Close', { duration: 2000 });
      },
      error: (err) => this.snack.open('Failed to start work', 'Close', { duration: 3000 })
    });
  }

  onFinish() {
  if (!this.request?.requestId && !this.request?.id) return;
  const id = this.request.requestId ?? this.request.id;
  
  // Pass this.resolutionNotes to the service
  this.api.finishWork(id, this.resolutionNotes).subscribe({
    next: (res: any) => {
      this.request.workEndedAt = res?.workEndedAt ?? new Date().toISOString();
      this.request.completedAt = res?.completedAt ?? this.request.workEndedAt;
      this.request.resolutionNotes = this.resolutionNotes; // Update local view
      this.updateRunningState();
      this.snack.open('Work finished', 'Close', { duration: 2000 });
    },
    error: (err) => this.snack.open('Failed to finish work', 'Close', { duration: 3000 })
  });
}

  
  openAccept() {
    this.showAccept = true;
    this.plannedLocal = null;
  }

 
  private localDatetimeToUtcIso(local: string) {
    const d = new Date(local);
    return toUtcIso(d);
  }

  submitAccept() {
    if (!this.request) return;
    const id = this.request.requestId ?? this.request.id;
    let dto: RespondAssignmentDto = { requestId: id, accept: true };
    if (this.plannedLocal) {
      // validate not in past
      const localDate = new Date(this.plannedLocal);
      if (localDate.getTime() < Date.now() - 1000) {
        this.snack.open('Planned start cannot be in the past', 'Close', { duration: 3000 });
        return;
      }
      dto.plannedStartUtc = this.localDatetimeToUtcIso(this.plannedLocal);
    }
    this.acceptSubmitting = true;
    this.api.respondToAssignment(id, dto).subscribe({
      next: (res: any) => {
        this.acceptSubmitting = false;
        this.showAccept = false;
        // update local request with plannedStartUtc if returned or from dto
        this.request.plannedStartUtc = res?.plannedStartUtc ?? dto.plannedStartUtc ?? this.request.plannedStartUtc;
        this.snack.open('Accepted assignment', 'Close', { duration: 2000 });
      },
      error: (err) => {
        this.acceptSubmitting = false;
        this.snack.open('Failed to accept assignment', 'Close', { duration: 3000 });
      }
    });
  }

  getActualDuration(): string | null {
    const started = this.request?.workStartedAt;
    const ended = this.request?.workEndedAt;
    

    if (!started || !ended) return null;
    
    const startDate = new Date(started);
    const endDate = new Date(ended);
    
    const diffMs = endDate.getTime() - startDate.getTime();
    
    if (diffMs <= 0) return '0s';
    
    const hours = Math.floor(diffMs / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);
    const seconds = Math.floor((diffMs % 60000) / 1000);
    
    if (hours > 0) {
      return `${hours}h ${minutes}m ${seconds}s`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    } else {
      return `${seconds}s`;
    }
  }
}
