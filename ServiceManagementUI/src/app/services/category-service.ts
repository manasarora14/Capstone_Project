import { Injectable, inject, NgZone, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, timer, Subscription, switchMap } from 'rxjs';
import { ServiceCategory } from '../models/service-models';

@Injectable({ providedIn: 'root' })
export class CategoryService implements OnDestroy {
  private http = inject(HttpClient);
  private zone = inject(NgZone);
  
  private readonly categoryUrl = 'https://localhost:7115/api/ServiceRequest/categories';

  private _categories = new BehaviorSubject<ServiceCategory[]>([]);
  public categories$ = this._categories.asObservable();

  private pollingSub: Subscription | null = null;

  constructor() {
    
    this.startPolling(10000);
  }

  startPolling(intervalMs: number = 10000) {
    if (this.pollingSub) return;
    this.pollingSub = timer(0, intervalMs)
      .pipe(switchMap(() => this.http.get<ServiceCategory[]>(this.categoryUrl)))
      .subscribe({ next: (data) => this.zone.run(() => this._categories.next(data)), error: () => {} });
  }

  stopPolling() {
    this.pollingSub?.unsubscribe();
    this.pollingSub = null;
  }

  refresh() {
    this.http.get<ServiceCategory[]>(this.categoryUrl).subscribe({ next: (data) => this.zone.run(() => this._categories.next(data)) });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}
