import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateMaintenanceRequestResponse,
  CompleteMaintenanceRequestResponse,
  GetMaintenanceHistoryResponse
} from '../models/maintenance.model';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private http = inject(HttpClient);

  createRequest(rideId: string, reason: string, workflowId: string | null = null): Observable<CreateMaintenanceRequestResponse> {
    return this.http.post<CreateMaintenanceRequestResponse>('/maintenance', {
      rideId,
      reason,
      workflowId,
      requestedAt: new Date().toISOString()
    });
  }

  completeRequest(id: string): Observable<CompleteMaintenanceRequestResponse> {
    return this.http.post<CompleteMaintenanceRequestResponse>(`/maintenance/${id}/complete`, {});
  }

  getHistory(rideId: string): Observable<GetMaintenanceHistoryResponse> {
    return this.http.get<GetMaintenanceHistoryResponse>(`/maintenance/history/${rideId}`);
  }
}
