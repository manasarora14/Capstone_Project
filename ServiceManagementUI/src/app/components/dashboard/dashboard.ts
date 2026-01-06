import { Component, inject } from '@angular/core';
import { AuthService } from '../../services/auth-service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon"; 

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatCardModule, 
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard {
  auth = inject(AuthService);
  formatName(raw?: string | null): string {
    if (!raw) return '';
    const beforeAt = String(raw).split('@')[0] || String(raw);
    const trimmed = beforeAt.trim();
    return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
  }
  
}