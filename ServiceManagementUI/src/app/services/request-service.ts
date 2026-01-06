import { HttpClient, HttpParams } from '@angular/common/http'; // Added HttpParams
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RequestStatus, ServiceCategory, ServiceRequest, Priority } from '../models/service-models';
import { DashboardStats } from '../models/dashboard-stats';



/**
 * Generic paged response returned by list endpoints.
 * Adjust fields to match your backend contract if necessary.
 */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class RequestService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7115/api/ServiceRequest';

  // HELPER: Private method to build query parameters
  private getQueryParams(page: number, size: number, search?: string, status?: RequestStatus, priority?: Priority | number): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', size.toString());

    if (search) params = params.set('searchTerm', search);
    if (status !== undefined && status !== null) params = params.set('statusFilter', status.toString());
    if (priority !== undefined && priority !== null) params = params.set('priorityFilter', String(priority));
    
    return params;
  }

  
  getAllRequests(page: number = 1, size: number = 10, search?: string, status?: RequestStatus, priority?: Priority | number): Observable<PagedResponse<ServiceRequest>> {
    const params = this.getQueryParams(page, size, search, status, priority);
    return this.http.get<PagedResponse<ServiceRequest>>(`${this.apiUrl}/monitor-all`, { params });
  }
  getTechnicianWorkload(page: number = 1, size: number = 5): Observable<any> {
  const params = new HttpParams()
    .set('pageNumber', page.toString())
    .set('pageSize', size.toString());

    const techUrl = this.apiUrl.replace('/ServiceRequest', '') + '/Technician/workload';
  return this.http.get<any>(techUrl, { params });
}

  
  getTechnicianTasks(page: number = 1, size: number = 10, search?: string): Observable<PagedResponse<ServiceRequest>> {
    const params = this.getQueryParams(page, size, search);
    return this.http.get<PagedResponse<ServiceRequest>>(`${this.apiUrl}/my-tasks`, { params });
  }

  
  getMyRequests(page: number = 1, size: number = 10, search?: string): Observable<PagedResponse<ServiceRequest>> {
    const params = this.getQueryParams(page, size, search);
    return this.http.get<PagedResponse<ServiceRequest>>(`${this.apiUrl}/my-requests`, { params });
  }

  

  getServiceRequestById(id: number): Observable<ServiceRequest> {
    return this.http.get<ServiceRequest>(`${this.apiUrl}/${id}`);
  }

  updateStatus(requestId: number, status: RequestStatus, resolutionNotes: string = ""): Observable<any> {
    const payload = { requestId, status, resolutionNotes };
    return this.http.put(`${this.apiUrl}/update-status`, payload);
  }

  respondToAssignment(requestId: number, accept: boolean, plannedStart?: string): Observable<any> {
    const dto = { accept, plannedStartUtc: plannedStart };
    return this.http.post(`${this.apiUrl}/${requestId}/respond`, dto);
  }

  startWork(id: number) {
    return this.http.post(`${this.apiUrl}/${id}/start`, {});
  }

  
finishWork(requestId: number, notes: string): Observable<any> {
  
  return this.http.post(`${this.apiUrl}/${requestId}/finish`, { notes: notes });
}

  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/dashboard-stats`);
  }

  assignTechnician(requestId: number, technicianId: string): Observable<void> {
    const payload = { requestId, technicianId };
    return this.http.put<void>(`${this.apiUrl}/assign`, payload);
  }

  getCategories(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>(`${this.apiUrl}/categories`);
  }

  createRequest(data: Partial<ServiceRequest>): Observable<any> {
    return this.http.post(`${this.apiUrl}/create`, data);
  }

  rescheduleRequest(requestId: number, scheduledDateIso: string): Observable<any> {
    const payload = {
      requestId,
      id: requestId,
      scheduledUtc: scheduledDateIso,
      newDate: scheduledDateIso,
      newScheduledUtc: scheduledDateIso,
      plannedStartUtc: scheduledDateIso,
      scheduledDate: scheduledDateIso
    };
    return this.http.put(`${this.apiUrl}/${requestId}/reschedule`, payload).pipe(
      catchError(err => {
            if (err && (err.status === 404 || err.status === 405)) {
          return this.http.put(`${this.apiUrl}/reschedule`, payload);
        }
        return throwError(() => err);
      })
    );
  }

  cancelRequest(requestId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${requestId}/cancel`);
  }
}