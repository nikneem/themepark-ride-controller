import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { QueueStateResponse, LoadPassengersResponse } from '../models/queue.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class QueueService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  getQueueState(rideId: string): Observable<QueueStateResponse> {
    return this.http.get<QueueStateResponse>(`${this.base}/queue/${rideId}`);
  }

  loadPassengers(rideId: string, capacity: number): Observable<LoadPassengersResponse> {
    return this.http.post<LoadPassengersResponse>(`${this.base}/queue/${rideId}/load`, { capacity });
  }

  simulateQueue(rideId: string, count: number, vipProbability: number): Observable<void> {
    return this.http.post<void>(`${this.base}/queue/${rideId}/simulate-queue`, { count, vipProbability });
  }
}
