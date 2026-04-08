import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { WeatherService } from '../../core/services/weather.service';
import { SseService } from '../../core/services/sse.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private weatherService = inject(WeatherService);
  private sseService = inject(SseService);

  currentTime = signal(new Date());
  weatherSeverity = signal<string>('Unknown');

  private timeInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.timeInterval = setInterval(() => this.currentTime.set(new Date()), 1000);
    this.loadWeather();
    this.sseService.connect();
  }

  ngOnDestroy(): void {
    if (this.timeInterval) clearInterval(this.timeInterval);
    this.sseService.disconnect();
  }

  private loadWeather(): void {
    this.weatherService.getCurrentWeather().subscribe({
      next: (w) => this.weatherSeverity.set(w.severity),
      error: () => this.weatherSeverity.set('Unknown')
    });
  }

  get weatherSeverityClass(): string {
    switch (this.weatherSeverity().toLowerCase()) {
      case 'calm': return 'success';
      case 'mild': return 'warn';
      case 'severe': return 'danger';
      default: return 'info';
    }
  }
}
