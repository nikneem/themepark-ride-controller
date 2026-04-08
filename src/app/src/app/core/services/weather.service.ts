import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WeatherResponse } from '../models/weather.model';

@Injectable({ providedIn: 'root' })
export class WeatherService {
  private http = inject(HttpClient);

  getCurrentWeather(): Observable<WeatherResponse> {
    return this.http.get<WeatherResponse>('/weather/current');
  }

  simulateWeather(severity: string): Observable<void> {
    return this.http.post<void>('/weather/simulate', { severity });
  }
}
