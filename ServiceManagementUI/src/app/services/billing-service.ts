import { HttpClient, HttpParams } from '@angular/common/http'; // Import HttpParams
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BillingService {
  private apiUrl = 'https://localhost:7115/api/Billing';

  constructor(private http: HttpClient) {}

  getInvoicesAsync(userId: string, userRole: string): Observable<any[]> {
    
    const params = new HttpParams()
      .set('userId', userId || '')
      .set('userRole', userRole || '');

    
    return this.http.get<any[]>(`${this.apiUrl}/my-invoices`, { params });
  }

  payInvoiceAsync(invoiceId: number, method?: string): Observable<any> {
    const payload: any = {};
    if (method) payload.method = method;
    return this.http.post(`${this.apiUrl}/pay/${invoiceId}`, payload);
  }
}