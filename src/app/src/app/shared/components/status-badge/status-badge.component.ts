import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, TagModule],
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss'
})
export class StatusBadgeComponent {
  status = input.required<string>();

  get severity(): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (this.status().toLowerCase()) {
      case 'running': return 'success';
      case 'paused': return 'warn';
      case 'failed': return 'danger';
      case 'maintenance': return 'warn';
      case 'idle': return 'secondary';
      default: return 'info';
    }
  }
}
