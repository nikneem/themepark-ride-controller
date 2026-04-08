import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { MaintenanceService } from '../../core/services/maintenance.service';
import { RidesService } from '../../core/services/rides.service';
import { MaintenanceHistoryItem } from '../../core/models/maintenance.model';
import { RideDto } from '../../core/models/ride.model';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, SelectModule, PageHeaderComponent],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss'
})
export class MaintenanceComponent implements OnInit {
  private maintenanceService = inject(MaintenanceService);
  private ridesService = inject(RidesService);

  rides = signal<RideDto[]>([]);
  allHistory = signal<MaintenanceHistoryItem[]>([]);
  newRideId = '';
  newReason = '';

  ngOnInit(): void {
    this.ridesService.getRides().subscribe({
      next: (rides) => {
        this.rides.set(rides);
        this.loadHistory(rides);
      }
    });
  }

  private loadHistory(rides: RideDto[]): void {
    const history: MaintenanceHistoryItem[] = [];
    let loaded = 0;
    rides.forEach(ride => {
      this.maintenanceService.getHistory(ride.rideId).subscribe({
        next: (r) => {
          history.push(...r.history);
          loaded++;
          if (loaded === rides.length) this.allHistory.set([...history]);
        },
        error: () => { loaded++; if (loaded === rides.length) this.allHistory.set([...history]); }
      });
    });
  }

  createRequest(): void {
    if (this.newRideId && this.newReason) {
      this.maintenanceService.createRequest(this.newRideId, this.newReason).subscribe({
        next: () => {
          this.newRideId = ''; this.newReason = '';
          this.ridesService.getRides().subscribe({ next: (r) => this.loadHistory(r) });
        }
      });
    }
  }

  complete(id: string): void {
    this.maintenanceService.completeRequest(id).subscribe({
      next: () => this.ridesService.getRides().subscribe({ next: (r) => this.loadHistory(r) })
    });
  }
}
