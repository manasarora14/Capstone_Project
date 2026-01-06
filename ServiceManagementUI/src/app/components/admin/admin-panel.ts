import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; 
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input'; 
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator'; 
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon'; 
import { MatButtonModule } from '@angular/material/button'; 
import { AdminService } from '../../services/admin-service'; 
import { User } from '../../models/service-models'; 
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatTableModule, MatSelectModule, 
    MatFormFieldModule, MatInputModule, MatPaginatorModule,
    MatSnackBarModule, MatIconModule, MatButtonModule
  ],
  templateUrl: './admin-panel.html',
  styleUrls: ['./admin-panel.css']
})
export class AdminPanel implements OnInit {
  private adminService = inject(AdminService);
  private snackBar = inject(MatSnackBar);

  
  users = signal<User[]>([]);
  totalUsers = signal<number>(0);
  pageSize = 10;
  currentPage = 1;
  searchQuery = '';
  private searchSubject = new Subject<string>();

  roles = ['Admin', 'Manager', 'Technician', 'Customer'];
  selectedRole: string | null = null;
  displayedColumns: string[] = ['email', 'role', 'actions'];

  pendingDeleteId = signal<string | null>(null);

  constructor() {
    
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(value => {
      this.searchQuery = value;
      this.currentPage = 1;
      this.loadUsers();
    });
  }

  ngOnInit() {
    this.loadUsers();
  }

  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  onRoleChange(value: string | null) {
    this.selectedRole = value;
    this.currentPage = 1;
    this.loadUsers();
  }

  loadUsers() {
    
    this.adminService.getAllUsers(this.currentPage, this.pageSize, this.searchQuery, this.selectedRole ?? undefined).subscribe({
      next: (res: any) => {
      
        const raw: User[] = res.items || res;

        
        if (!res.totalCount && Array.isArray(res)) {
          
          // If server returned a raw array (no paging), apply client-side search and role filter
          let filtered = this.searchQuery
            ? raw.filter(u => (u.email || '').toLowerCase().includes(this.searchQuery.toLowerCase()))
            : raw;

          if (this.selectedRole) {
            filtered = filtered.filter(u => String(u.role).toLowerCase() === String(this.selectedRole).toLowerCase());
          }

          const start = (this.currentPage - 1) * this.pageSize;
          const paged = filtered.slice(start, start + this.pageSize);

          this.users.set(paged);
          this.totalUsers.set(filtered.length);
        } else {
          
          this.users.set(raw);
          this.totalUsers.set(res.totalCount || raw.length);
        }
      },
      error: (err) => {
        this.snackBar.open('Error loading users', 'Close', { duration: 3000 });
        console.error(err);
      }
    });
  }

  changeRole(userId: string, newRole: string) {
    this.adminService.updateUserRole(userId, newRole).subscribe({
      next: () => {
        this.snackBar.open('Role updated successfully!', 'Close', { duration: 3000 });
        this.loadUsers(); 
      },
      error: (err) => {
        this.snackBar.open('Failed to update role', 'Close', { duration: 3000 });
      }
    });
  }

  requestDelete(userId: string) {
    this.pendingDeleteId.set(userId);
  }

  cancelDelete() {
    this.pendingDeleteId.set(null);
  }

  confirmDelete(userId: string) {
    this.adminService.deleteUser(userId).subscribe({
      next: () => {
        this.snackBar.open('User deleted successfully', 'Close', { duration: 3000 });
        this.pendingDeleteId.set(null);
        this.loadUsers();
      },
      error: (err) => {
        const msg = err?.error?.message || err?.error || 'Cannot delete user; user has associated work.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
        this.pendingDeleteId.set(null);
      }
    });
  }
}