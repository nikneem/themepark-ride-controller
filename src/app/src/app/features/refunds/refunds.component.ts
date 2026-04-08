import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { RefundsService } from '../../core/services/refunds.service';
import { RidesService } from '../../core/services/rides.service';
import { RefundBatchSummaryDto } from '../../core/models/refund.model';
import { RideDto } from '../../core/models/ride.model';

@Component({
  selector: 'app-refunds',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, SelectModule, PageHeaderComponent],
  templateUrl: './refunds.component.html',
  styleUrl: './refunds.component.scss'
})
export class RefundsComponent implements OnInit {
  private refundsService = inject(RefundsService);
  private ridesService = inject(RidesService);

  rides = signal<RideDto[]>([]);
  allRefunds = signal<(RefundBatchSummaryDto & { rideName?: string })[]>([]);
  newRideId = '';
  newWorkflowId = '';
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
    const all: (RefundBatchSummaryDto & { rideName?: string })[] = [];
    let loaded = 0;
    rides.forEach(ride => {
      this.refundsService.getHistory(ride.rideId).subscribe({
        next: (r) => {
          all.push(...r.history.map(h => ({ ...h, rideName: ride.name })));
          loaded++;
          if (loaded === rides.length) this.allRefunds.set([...all]);
        },
        error: () => { loaded++; if (loaded === rides.length) this.allRefunds.set([...all]); }
      });
    });
  }

  issueRefund(): void {
    if (this.newRideId && this.newWorkflowId && this.newReason) {
      this.refundsService.issueRefund(this.newRideId, this.newWorkflowId, this.newReason, []).subscribe({
        next: () => {
          this.newRideId = ''; this.newWorkflowId = ''; this.newReason = '';
          this.ridesService.getRides().subscribe({ next: (r) => this.loadHistory(r) });
        }
      });
    }
  }
}
