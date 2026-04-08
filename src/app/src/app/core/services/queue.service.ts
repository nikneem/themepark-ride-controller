import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { QueueStateResponse, LoadPassengersResponse } from '../models/queue.model';

@Injectable({ providedIn: 'root' })
export class QueueService {
  private http = inject(HttpClient);

  getQueueState(rideId: string): Observable<QueueStateResponse> {
    return this.http.get<QueueStateResponse>(`/queue/${rideId}`);
  }

  loadPassengers(rideId: string, capacity: number): Observable<LoadPassengersResponse> {
    return this.http.post<LoadPassengersResponse>(`/queue/${rideId}/load`, { capacity });
  }

  simulateQueue(rideId: string, count: number, vipProbability: number): Observable<void> {
    return this.http.post<void>(`/queue/${rideId}/simulate-queue`, { count, vipProbability });
  }
}
