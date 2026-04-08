import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RideDto, RideStatusResponse, StartRideResponse, RideHistoryEntry } from '../models/ride.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RidesService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  getRides(): Observable<RideDto[]> {
    return this.http.get<RideDto[]>(`${this.base}/controlcenter/rides`);
  }

  getRideStatus(rideId: string): Observable<RideStatusResponse> {
    return this.http.get<RideStatusResponse>(`${this.base}/controlcenter/rides/${rideId}/status`);
  }

  startRide(rideId: string): Observable<StartRideResponse> {
    return this.http.post<StartRideResponse>(`${this.base}/controlcenter/rides/${rideId}/start`, {});
  }

  approveMaintenance(rideId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/controlcenter/rides/${rideId}/maintenance/approve`, {});
  }

  resolveEvent(rideId: string, eventId: string, eventType: string): Observable<void> {
    return this.http.post<void>(`${this.base}/controlcenter/rides/${rideId}/events/${eventId}/resolve?eventType=${eventType}`, {});
  }

  getRideHistory(rideId: string): Observable<RideHistoryEntry[]> {
    return this.http.get<RideHistoryEntry[]>(`${this.base}/controlcenter/rides/${rideId}/history`);
  }

  simulateMalfunction(rideId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/rides/${rideId}/simulate-malfunction`, {});
  }
}
