import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { RidesService } from '../../../core/services/rides.service';
import { RideDto } from '../../../core/models/ride.model';

@Component({
  selector: 'app-rides-list',
  standalone: true,
  imports: [CommonModule, RouterModule, TableModule, ButtonModule, CardModule, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './rides-list.component.html',
  styleUrl: './rides-list.component.scss'
})
export class RidesListComponent implements OnInit {
  private ridesService = inject(RidesService);
  rides = signal<RideDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadRides();
  }

  loadRides(): void {
    this.ridesService.getRides().subscribe({
      next: (r) => { this.rides.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  startRide(rideId: string): void {
    this.ridesService.startRide(rideId).subscribe({ next: () => this.loadRides() });
  }
}
