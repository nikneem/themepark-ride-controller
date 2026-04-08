import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GetRefundHistoryResponse, RefundPassengerRequest } from '../models/refund.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RefundsService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  issueRefund(rideId: string, workflowId: string, reason: string, passengers: RefundPassengerRequest[]): Observable<void> {
    return this.http.post<void>(`${this.base}/refunds`, { rideId, workflowId, reason, passengers });
  }

  getHistory(rideId: string): Observable<GetRefundHistoryResponse> {
    return this.http.get<GetRefundHistoryResponse>(`${this.base}/refunds/${rideId}/history`);
  }
}
