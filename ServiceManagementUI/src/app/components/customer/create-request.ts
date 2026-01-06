import { Component, inject, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RequestService } from '../../services/request-service';
import { CategoryService } from '../../services/category-service';
import { NotificationService } from '../../services/notification-service';
import { toUtcIso, timespanFromDate } from '../../utils/time-utils';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ServiceCategory, Priority } from '../../models/service-models';
import { ServiceRequest } from '../../models/service-models';

@Component({
  selector: 'app-create-request',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    MatFormFieldModule, 
    MatInputModule, 
    MatSelectModule, 
    MatButtonModule, 
    MatDatepickerModule, 
    MatNativeDateModule, 
    MatCardModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './create-request.html',
  styleUrl: './create-request.css'
})
export class CreateRequest implements OnInit {
  
  categories = signal<ServiceCategory[]>([]);
  
  request = {
    issueDescription: '',
    categoryId: null as number | null,
    priority: Priority.Medium, 
    scheduledDate: new Date(),
    scheduledTime: new Date().toTimeString().slice(0,5)
  };

  private reqService = inject(RequestService);
  private categoryService = inject(CategoryService);
  private notifService = inject(NotificationService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  ngOnInit() {
    
    this.categoryService.categories$.subscribe({
      next: (data) => {
        this.categories.set(data);
        
        this.cdr.detectChanges();
      },
      error: () => this.showMsg('Failed to load categories')
    });
    this.categoryService.refresh();
  }

  getSelectedCategory(): ServiceCategory | undefined {
    return this.categories().find(c => c.id === this.request.categoryId);
  }

  submit() {
    if (!this.request.issueDescription || !this.request.categoryId) {
      this.showMsg('Please fill in all required fields');
      return;
    }

    
    const scheduledLocal = new Date(this.request.scheduledDate as any);
    if (this.request.scheduledTime) {
      const parts = (this.request.scheduledTime as string).split(':').map(Number);
      if (parts.length >= 2) scheduledLocal.setHours(parts[0], parts[1], 0, 0);
    }
    const scheduledDateUtc = toUtcIso(scheduledLocal);

    
    let scheduledTimeNorm: string | undefined = undefined;
    if (this.request.scheduledTime) {
      if (/^\d{2}:\d{2}:\d{2}$/.test(this.request.scheduledTime as string)) scheduledTimeNorm = this.request.scheduledTime as string;
      else if (/^\d{2}:\d{2}$/.test(this.request.scheduledTime as string)) scheduledTimeNorm = `${this.request.scheduledTime}:00`;
      else scheduledTimeNorm = timespanFromDate(scheduledLocal);
    }

    const payload = { issueDescription: this.request.issueDescription, categoryId: this.request.categoryId, priority: this.request.priority, scheduledDate: scheduledDateUtc, scheduledTime: scheduledTimeNorm };

    this.reqService.createRequest(payload as any).subscribe({
      next: () => {
        this.showMsg('Service Request Submitted Successfully! ✅');
        try {
          this.notifService.notify(`New service request submitted: ${this.request.issueDescription?.toString().slice(0,80)}`);
        } catch {
          // swallow any notification errors to avoid breaking the happy path
        }
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.error(err);
        this.showMsg('Submission failed. Please try again. ❌');
      }
    });
  }

  private showMsg(message: string) {
    this.snackBar.open(message, 'Close', {
      duration: 4000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }
}