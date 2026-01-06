import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ServiceCategory, ServiceRequest, User } from '../models/service-models';
import { DashboardStats } from '../models/dashboard-stats';
import { CreateRequestDto, TechnicianTaskDto, TechnicianWorkloadDto, RespondAssignmentDto, TechnicianAvailability, TechnicianAvailabilityRange } from '../models/api-dtos';



@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  getCategories(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>('/api/ServiceRequest/categories');
  }

  createRequest(dto: CreateRequestDto): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>('/api/ServiceRequest/create', dto);
  }

  startWork(id: number): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(`/api/ServiceRequest/${id}/start`, {});
  }

  finishWork(id: number, notes: string): Observable<any> {
  // We wrap notes in an object {} to match the [FromBody] FinishWorkDto on the backend
  return this.http.post(`/api/ServiceRequest/${id}/finish`, { notes: notes });
}

  
  respondToAssignment(id: number, dto: RespondAssignmentDto): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(`/api/ServiceRequest/${id}/respond`, dto);
  }

  
  monitorAll(): Observable<ServiceRequest[]> {
    return this.http.get<ServiceRequest[]>('/api/ServiceRequest/monitor-all');
  }

  
  getMonitorAll(): Observable<ServiceRequest[]> { return this.monitorAll(); }

  
  getAvailability(scheduledUtc: string, durationHours: number): Observable<TechnicianAvailability[]> {
    const q = `/api/availability?scheduledUtc=${encodeURIComponent(scheduledUtc)}&durationHours=${encodeURIComponent(String(durationHours))}`;
    return this.http.get<TechnicianAvailability[]>(q);
  }

  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>('/api/ServiceRequest/dashboard-stats');
  }

  getTechnicianWorkload(): Observable<TechnicianWorkloadDto> {
    return this.http.get<TechnicianWorkloadDto>('/api/Technician/workload');
  }

  checkTechnicianAvailability(technicianId: string): Observable<TechnicianAvailability[]> {
    return this.http.get<TechnicianAvailability[]>(`/api/Technician/availability/${technicianId}`);
  }

  
  toUtcIso(localDate: Date): string {
    return new Date(localDate.getTime() - localDate.getTimezoneOffset() * 60000).toISOString();
  }

 
  addHoursIso(iso: string, h: number): string {
    const d = new Date(iso);
    d.setHours(d.getHours() + h);
    return d.toISOString();
  }

 
  addHoursDisplay(iso: string, h: number): string {
    const d = new Date(iso);
    d.setHours(d.getHours() + h);
    return d.toLocaleString();
  }
}
