import { Component, inject, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { AdminService } from '../../services/admin-service';
import { CategoryService } from '../../services/category-service';
import { ServiceCategory } from '../../models/service-models';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    FormsModule,
    MatSnackBarModule,
    MatIconModule
  ],
  templateUrl: './admin-categories.html',
  styleUrls: ['./admin-panel.css']
})
export class AdminCategories implements OnInit {
  private adminService = inject(AdminService);
  private categoryService = inject(CategoryService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef); // Correctly inject the service here

  categories = signal<ServiceCategory[]>([]);
  categoryColumns: string[] = ['name', 'baseCharge', 'slaHours', 'actionsCat'];

  currentCategory: ServiceCategory = { id: 0, name: '', description: '', baseCharge: 0, slaHours: 24 };
  editing = false;

  ngOnInit() {
    
    this.categoryService.categories$.subscribe({
      next: (data: ServiceCategory[]) => {
        this.categories.set(data);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
      }
    });
    
    this.categoryService.refresh();
  }

  loadCategories() {
    
    this.categoryService.refresh();
  }

  saveCategory() {
    if (this.editing) {
      this.adminService.updateServiceCategory(this.currentCategory.id, this.currentCategory).subscribe({
        next: () => {
          this.snackBar.open('Category updated', 'Close', { duration: 2000 });
          this.resetForm();
            this.categoryService.refresh();
        },
        error: (err) => {
          this.snackBar.open('Failed to update category', 'Close', { duration: 3000 });
          console.error(err);
        }
      });
    } else {
      this.adminService.createServiceCategory(this.currentCategory).subscribe({
        next: () => {
          this.snackBar.open('Category created', 'Close', { duration: 2000 });
          this.resetForm();
            this.categoryService.refresh();
        },
        error: (err) => {
          this.snackBar.open('Failed to create category', 'Close', { duration: 3000 });
          console.error(err);
        }
      });
    }
  }

  editCategory(cat: ServiceCategory) {
   
    setTimeout(() => {
      this.currentCategory = { ...cat };
      this.editing = true;
      this.cdr.detectChanges(); 
    });
  }

  resetForm() {
   
    setTimeout(() => {
      this.currentCategory = { id: 0, name: '', description: '', baseCharge: 0, slaHours: 24 };
      this.editing = false;
      this.cdr.detectChanges();
    });
  }

  deleteCategory(id: number) {
  if (!confirm('Are you sure you want to delete this category?')) return;

  this.adminService.deleteServiceCategory(id).subscribe({
    next: () => {
      this.snackBar.open('Category deleted successfully', 'Close', { duration: 2000 });
      this.categoryService.refresh();
    },
    error: (err) => {
     
      const errorMessage = err.error?.message || 'Failed to delete category';
      
      this.snackBar.open(errorMessage, 'OK', { 
        duration: 5000,
        panelClass: ['error-snackbar']
      });
      console.error('Delete error:', err);
    }
  });
}
}