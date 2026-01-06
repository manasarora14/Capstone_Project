import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { User, ServiceCategory } from '../models/service-models';

@Injectable({ providedIn: 'root' })
export class AdminService {
 
  private rootUrl = 'https://localhost:7115/api';
  private adminUrl = `${this.rootUrl}/Admin`;
  private categoryUrl = `${this.rootUrl}/Category`; 

  constructor(private http: HttpClient) {}


  updateUserRole(userId: string, newRole: string): Observable<any> {
    return this.http.post(`${this.adminUrl}/update-role`, { userId, newRole });
  }

  
  getServiceCategories(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>(this.categoryUrl);
  }

  createServiceCategory(category: Partial<ServiceCategory>): Observable<any> {
    return this.http.post(this.categoryUrl, category);
  }
  
getAllUsers(page: number = 1, size: number = 10, search?: string, role?: string): Observable<any> {
  let params = new HttpParams()
    .set('pageNumber', page.toString())
    .set('pageSize', size.toString());

  if (search) {
    params = params.set('searchTerm', search);
  }

  if (role !== undefined && role !== null && String(role).trim() !== '') {
    params = params.set('roleFilter', role);
  }

  return this.http.get<any>(`${this.adminUrl}/users`, { params });
}

  updateServiceCategory(id: number, category: ServiceCategory): Observable<any> {
    
    return this.http.put(`${this.categoryUrl}/${id}`, category);
  }

  deleteUser(userId: string): Observable<any> {
    return this.http.delete(`${this.adminUrl}/delete-user/${userId}`);
  }

  
deleteServiceCategory(id: number): Observable<any> {
 
  return this.http.delete(`${this.rootUrl}/Category/${id}`);
}
}