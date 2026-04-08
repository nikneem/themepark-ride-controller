import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { PanelModule } from 'primeng/panel';
import { TagModule } from 'primeng/tag';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { RidesService } from '../../../core/services/rides.service';
import { QueueService } from '../../../core/services/queue.service';
import { MaintenanceService } from '../../../core/services/maintenance.service';
import { RefundsService } from '../../../core/services/refunds.service';
import { RideStatusResponse, RideHistoryEntry } from '../../../core/models/ride.model';
import { QueueStateResponse } from '../../../core/models/queue.model';
import { MaintenanceHistoryItem } from '../../../core/models/maintenance.model';
import { RefundBatchSummaryDto } from '../../../core/models/refund.model';

@Component({
  selector: 'app-ride-detail',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, TableModule, PanelModule, TagModule, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './ride-detail.component.html',
  styleUrl: './ride-detail.component.scss'
})
export class RideDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private ridesService = inject(RidesService);
  private queueService = inject(QueueService);
  private maintenanceService = inject(MaintenanceService);
  private refundsService = inject(RefundsService);

  rideId = signal<string>('');
  rideStatus = signal<RideStatusResponse | null>(null);
  queue = signal<QueueStateResponse | null>(null);
  history = signal<RideHistoryEntry[]>([]);
  maintenanceHistory = signal<MaintenanceHistoryItem[]>([]);
  refundHistory = signal<RefundBatchSummaryDto[]>([]);
  simulating = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.rideId.set(id);
    this.loadAll(id);
  }

  private loadAll(id: string): void {
    this.ridesService.getRideStatus(id).subscribe({ next: (r) => this.rideStatus.set(r), error: () => {} });
    this.queueService.getQueueState(id).subscribe({ next: (q) => this.queue.set(q), error: () => {} });
    this.ridesService.getRideHistory(id).subscribe({ next: (h) => this.history.set(h), error: () => {} });
    this.maintenanceService.getHistory(id).subscribe({ next: (m) => this.maintenanceHistory.set(m.history), error: () => {} });
    this.refundsService.getHistory(id).subscribe({ next: (r) => this.refundHistory.set(r.history), error: () => {} });
  }

  startRide(): void {
    this.ridesService.startRide(this.rideId()).subscribe({ next: () => this.reload() });
  }

  approveMaintenance(): void {
    this.ridesService.approveMaintenance(this.rideId()).subscribe({ next: () => this.reload() });
  }

  /** eventType must be one of: WeatherAlert | MascotIntrusion | RideMalfunction */
  resolveEvent(eventType: string): void {
    this.ridesService.resolveEvent(this.rideId(), eventType, eventType).subscribe({ next: () => this.reload() });
  }

  simulateMalfunction(): void {
    this.simulating.set(true);
    this.ridesService.simulateMalfunction(this.rideId()).subscribe({
      next: () => { this.simulating.set(false); this.reload(); },
      error: () => this.simulating.set(false)
    });
  }

  reload(): void {
    this.loadAll(this.rideId());
  }
}
