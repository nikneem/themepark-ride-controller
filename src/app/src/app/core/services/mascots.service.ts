import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MascotDto, ClearMascotResponse } from '../models/mascot.model';

@Injectable({ providedIn: 'root' })
export class MascotsService {
  private http = inject(HttpClient);

  getMascots(): Observable<MascotDto[]> {
    return this.http.get<MascotDto[]>('/mascots');
  }

  clearMascot(mascotId: string): Observable<ClearMascotResponse> {
    return this.http.post<ClearMascotResponse>(`/mascots/${mascotId}/clear`, {});
  }

  simulateIntrusion(mascotId: string, targetRideId: string): Observable<void> {
    return this.http.post<void>('/mascots/simulate-intrusion', { mascotId, targetRideId });
  }
}
