import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RideDto, RideStatusResponse, StartRideResponse, RideHistoryEntry } from '../models/ride.model';

@Injectable({ providedIn: 'root' })
export class RidesService {
  private http = inject(HttpClient);

  getRides(): Observable<RideDto[]> {
    return this.http.get<RideDto[]>('/api/rides');
  }

  getRideStatus(rideId: string): Observable<RideStatusResponse> {
    return this.http.get<RideStatusResponse>(`/api/rides/${rideId}/status`);
  }

  startRide(rideId: string): Observable<StartRideResponse> {
    return this.http.post<StartRideResponse>(`/api/rides/${rideId}/start`, {});
  }

  approveMaintenance(rideId: string): Observable<void> {
    return this.http.post<void>(`/api/rides/${rideId}/maintenance/approve`, {});
  }

  resolveEvent(rideId: string, eventId: string, eventType: string): Observable<void> {
    return this.http.post<void>(`/api/rides/${rideId}/events/${eventId}/resolve?eventType=${eventType}`, {});
  }

  getRideHistory(rideId: string): Observable<RideHistoryEntry[]> {
    return this.http.get<RideHistoryEntry[]>(`/api/rides/${rideId}/history`);
  }

  simulateMalfunction(rideId: string): Observable<void> {
    return this.http.post<void>(`/rides/${rideId}/simulate-malfunction`, {});
  }
}
