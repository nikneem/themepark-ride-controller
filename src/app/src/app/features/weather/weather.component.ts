import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { WeatherService } from '../../core/services/weather.service';
import { WeatherResponse } from '../../core/models/weather.model';

@Component({
  selector: 'app-weather',
  standalone: true,
  imports: [CommonModule, FormsModule, CardModule, ButtonModule, SelectModule, PageHeaderComponent],
  templateUrl: './weather.component.html',
  styleUrl: './weather.component.scss'
})
export class WeatherComponent implements OnInit {
  private weatherService = inject(WeatherService);

  weather = signal<WeatherResponse | null>(null);
  simulateSeverity = 'Calm';
  severityOptions = ['Calm', 'Mild', 'Severe'];

  ngOnInit(): void {
    this.loadWeather();
  }

  loadWeather(): void {
    this.weatherService.getCurrentWeather().subscribe({
      next: (w) => this.weather.set(w),
      error: () => {}
    });
  }

  simulate(): void {
    this.weatherService.simulateWeather(this.simulateSeverity).subscribe({
      next: () => this.loadWeather()
    });
  }

  getWeatherEmoji(): string {
    switch (this.weather()?.severity?.toLowerCase()) {
      case 'calm': return '☀️';
      case 'mild': return '⛅';
      case 'severe': return '⛈️';
      default: return '🌤';
    }
  }
}
