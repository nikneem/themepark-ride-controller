import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WeatherResponse } from '../models/weather.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WeatherService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  getCurrentWeather(): Observable<WeatherResponse> {
    return this.http.get<WeatherResponse>(`${this.base}/weather/current`);
  }

  simulateWeather(severity: string): Observable<void> {
    return this.http.post<void>(`${this.base}/weather/simulate`, { severity });
  }
}
