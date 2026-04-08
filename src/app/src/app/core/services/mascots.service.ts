import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MascotDto, ClearMascotResponse } from '../models/mascot.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MascotsService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  getMascots(): Observable<MascotDto[]> {
    return this.http.get<MascotDto[]>(`${this.base}/mascots`);
  }

  clearMascot(mascotId: string): Observable<ClearMascotResponse> {
    return this.http.post<ClearMascotResponse>(`${this.base}/mascots/${mascotId}/clear`, {});
  }

  simulateIntrusion(mascotId: string, targetRideId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/mascots/simulate-intrusion`, { mascotId, targetRideId });
  }
}
