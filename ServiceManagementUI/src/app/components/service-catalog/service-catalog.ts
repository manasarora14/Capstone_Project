import { Component, inject, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CategoryService } from '../../services/category-service';
import { AuthService } from '../../services/auth-service';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { ServiceCategory } from '../../models/service-models';

@Component({
  selector: 'app-service-catalog',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule
  ],
  templateUrl: './service-catalog.html',
  styleUrls: ['./service-catalog.css']
})
export class ServiceCatalog implements OnInit {
  private categoryService = inject(CategoryService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  categories = signal<ServiceCategory[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    
    this.categoryService.categories$.subscribe({ next: (data) => { this.categories.set(data); this.loading.set(false); this.cdr.detectChanges(); }, error: (err) => { console.error(err); this.error.set('Failed to load service catalog'); this.loading.set(false); this.cdr.detectChanges(); } });
    this.categoryService.refresh();
  }

  loadCategories() {
    
    this.loading.set(true);
    this.error.set(null);
    this.categoryService.refresh();
  }

  getUserRole(): string {
    return this.authService.userRole();
  }

  isCustomer(): boolean {
    return this.getUserRole() === 'Customer';
  }

  canCreateRequest(): boolean {
    return this.isCustomer();
  }

  getCategoryIcon(categoryName: string): string {
    const name = categoryName.toLowerCase();
    if (name.includes('install')) return 'build';
    if (name.includes('maintain')) return 'settings';
    if (name.includes('repair')) return 'handyman';
    return 'category';
  }
}

