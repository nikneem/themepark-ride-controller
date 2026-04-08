import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { QueueService } from '../../core/services/queue.service';
import { RidesService } from '../../core/services/rides.service';
import { QueueStateResponse } from '../../core/models/queue.model';
import { RideDto } from '../../core/models/ride.model';

interface RideQueueState extends QueueStateResponse {
  rideName: string;
}

@Component({
  selector: 'app-queues',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputNumberModule, InputTextModule, SelectModule, PageHeaderComponent],
  templateUrl: './queues.component.html',
  styleUrl: './queues.component.scss'
})
export class QueuesComponent implements OnInit {
  private queueService = inject(QueueService);
  private ridesService = inject(RidesService);

  rides = signal<RideDto[]>([]);
  queueStates = signal<RideQueueState[]>([]);
  simulateRideId = '';
  simulateCount = 10;
  simulateVipProb = 0.1;
  loadRideId = '';
  loadCapacity = 20;

  ngOnInit(): void {
    this.ridesService.getRides().subscribe({
      next: (rides) => {
        this.rides.set(rides);
        this.loadQueues(rides);
      }
    });
  }

  private loadQueues(rides: RideDto[]): void {
    const states: RideQueueState[] = [];
    let loaded = 0;
    rides.forEach(ride => {
      this.queueService.getQueueState(ride.rideId).subscribe({
        next: (q) => {
          states.push({ ...q, rideName: ride.name });
          loaded++;
          if (loaded === rides.length) this.queueStates.set([...states]);
        },
        error: () => { loaded++; if (loaded === rides.length) this.queueStates.set([...states]); }
      });
    });
  }

  simulateQueue(): void {
    if (this.simulateRideId) {
      this.queueService.simulateQueue(this.simulateRideId, this.simulateCount, this.simulateVipProb).subscribe({
        next: () => this.ridesService.getRides().subscribe({ next: (r) => this.loadQueues(r) })
      });
    }
  }

  loadPassengers(): void {
    if (this.loadRideId) {
      this.queueService.loadPassengers(this.loadRideId, this.loadCapacity).subscribe({
        next: () => this.ridesService.getRides().subscribe({ next: (r) => this.loadQueues(r) })
      });
    }
  }
}
