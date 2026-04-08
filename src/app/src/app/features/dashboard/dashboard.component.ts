import { Component, signal, computed, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { PanelModule } from 'primeng/panel';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../shared/components/stat-card/stat-card.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { RidesService } from '../../core/services/rides.service';
import { WeatherService } from '../../core/services/weather.service';
import { MascotsService } from '../../core/services/mascots.service';
import { SseService } from '../../core/services/sse.service';
import { RideDto } from '../../core/models/ride.model';
import { WeatherResponse } from '../../core/models/weather.model';
import { MascotDto } from '../../core/models/mascot.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterModule, CardModule, ButtonModule, TagModule, PanelModule,
    PageHeaderComponent, StatCardComponent, StatusBadgeComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private ridesService = inject(RidesService);
  private weatherService = inject(WeatherService);
  private mascotsService = inject(MascotsService);
  private sseService = inject(SseService);
  private destroyRef = inject(DestroyRef);

  rides = signal<RideDto[]>([]);
  weather = signal<WeatherResponse | null>(null);
  mascots = signal<MascotDto[]>([]);
  loading = signal(true);

  restrictedMascots = computed(() => this.mascots().filter(m => m.isInRestrictedZone));
  runningRides = computed(() => this.rides().filter(r => r.status.toLowerCase() === 'running').length);
  idleRides = computed(() => this.rides().filter(r => r.status.toLowerCase() === 'idle').length);
  alertCount = computed(() => this.restrictedMascots().length);

  ngOnInit(): void {
    this.loadData();
    this.sseService.events$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
      if (event.type === 'ride-status-changed') this.loadRides();
    });
  }

  private loadData(): void {
    this.loadRides();
    this.weatherService.getCurrentWeather().subscribe({
      next: (w) => this.weather.set(w),
      error: () => {}
    });
    this.mascotsService.getMascots().subscribe({
      next: (m) => this.mascots.set(m),
      error: () => {}
    });
  }

  private loadRides(): void {
    this.ridesService.getRides().subscribe({
      next: (r) => { this.rides.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  startRide(rideId: string): void {
    this.ridesService.startRide(rideId).subscribe({
      next: () => this.loadRides(),
      error: () => {}
    });
  }

  getWeatherIcon(): string {
    switch (this.weather()?.severity?.toLowerCase()) {
      case 'calm': return '☀️';
      case 'mild': return '⛅';
      case 'severe': return '⛈️';
      default: return '🌤';
    }
  }
}
