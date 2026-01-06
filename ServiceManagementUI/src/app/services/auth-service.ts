import { HttpClient } from '@angular/common/http';
import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, User } from '../models/service-models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = 'https://localhost:7115/api/Auth';
  
  
  currentUser = signal<User | null>(this.getUserFromStorage());

 
  isLoggedIn = computed(() => !!this.currentUser());
  userRole = computed(() => this.currentUser()?.role || '');

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: any): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => this.setUser(res))
    );
  }

  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  logout(): void {
    localStorage.removeItem('user');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private setUser(res: AuthResponse): void {
    const user: User = {
      email: res.email,
      name: (res as any).name || undefined,
      role: res.role,
      token: res.token,
      id: undefined
    };
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  private getUserFromStorage(): User | null {
    const data = localStorage.getItem('user');
    try {
        return data ? JSON.parse(data) : null;
    } catch {
        return null;
    }
  }

  getToken(): string | undefined { 
    return this.currentUser()?.token; 
  }
}