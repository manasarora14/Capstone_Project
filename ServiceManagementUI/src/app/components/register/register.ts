import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { Router, RouterModule } from '@angular/router'; 
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon'; 
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    RouterModule,
    MatCardModule, 
    MatFormFieldModule, 
    MatInputModule, 
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  registerData = {
    name: '',
    email: '',
    password: '',
    confirmPassword: ''
  };
  
  errors = signal<string[]>([]);
  
  private auth = inject(AuthService);
  private router = inject(Router);

  onRegister() {
    this.errors.set([]); 
    if (!this.registerData.name || this.registerData.name.trim().length === 0) {
      this.errors.set(['Please enter your full name.']);
      return;
    }
    if (this.registerData.password !== this.registerData.confirmPassword) {
      this.errors.set(['Passwords do not match.']);
      return;
    }

    const payload = {
      fullName: this.registerData.name,
      email: this.registerData.email,
      password: this.registerData.password,
      confirmPassword: this.registerData.confirmPassword,
      role: 'Customer'
    };

    this.auth.register(payload).subscribe({
      next: () => {
        this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
      },
      error: (err) => {
        console.error('Registration error:', err);

        // If server returned model validation details (RFC7807 / problem+json)
        const server = err?.error;
        const collected: string[] = [];

        if (Array.isArray(server)) {
          server.forEach((s: any) => { if (s.description) collected.push(s.description); });
        } else if (server && typeof server === 'object') {
          if (server.errors && typeof server.errors === 'object') {
            // ASP.NET Core validation errors: { errors: { field: ["msg1"] } }
            Object.values(server.errors).forEach((v: any) => {
              if (Array.isArray(v)) v.forEach(i => collected.push(String(i)));
            });
          }
          if (server.title) collected.push(server.title + (server.detail ? (': ' + server.detail) : ''));
          if (server.detail && !server.title) collected.push(server.detail);
        } else if (err?.message) {
          collected.push(err.message);
        }

        if (collected.length > 0) {
          this.errors.set(collected);
        } else {
          this.errors.set([`Registration failed (${err?.status || 'unknown'})`]);
        }
      }
    });
  }
}