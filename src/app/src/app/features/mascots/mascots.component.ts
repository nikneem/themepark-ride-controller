import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { MascotsService } from '../../core/services/mascots.service';
import { RidesService } from '../../core/services/rides.service';
import { MascotDto } from '../../core/models/mascot.model';
import { RideDto } from '../../core/models/ride.model';

@Component({
  selector: 'app-mascots',
  standalone: true,
  imports: [CommonModule, FormsModule, CardModule, ButtonModule, InputTextModule, SelectModule, PageHeaderComponent],
  templateUrl: './mascots.component.html',
  styleUrl: './mascots.component.scss'
})
export class MascotsComponent implements OnInit {
  private mascotsService = inject(MascotsService);
  private ridesService = inject(RidesService);

  mascots = signal<MascotDto[]>([]);
  rides = signal<RideDto[]>([]);
  intrusionMascotId = '';
  intrusionRideId = '';

  ngOnInit(): void {
    this.loadMascots();
    this.ridesService.getRides().subscribe({ next: (r) => this.rides.set(r) });
  }

  loadMascots(): void {
    this.mascotsService.getMascots().subscribe({ next: (m) => this.mascots.set(m) });
  }

  clearMascot(mascotId: string): void {
    this.mascotsService.clearMascot(mascotId).subscribe({ next: () => this.loadMascots() });
  }

  simulateIntrusion(): void {
    if (this.intrusionMascotId && this.intrusionRideId) {
      this.mascotsService.simulateIntrusion(this.intrusionMascotId, this.intrusionRideId).subscribe({
        next: () => { this.loadMascots(); this.intrusionMascotId = ''; this.intrusionRideId = ''; }
      });
    }
  }
}
