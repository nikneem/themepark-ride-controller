import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'pi pi-home' },
    { label: 'Rides', route: '/rides', icon: 'pi pi-star' },
    { label: 'Weather', route: '/weather', icon: 'pi pi-cloud' },
    { label: 'Mascots', route: '/mascots', icon: 'pi pi-user' },
    { label: 'Queues', route: '/queues', icon: 'pi pi-users' },
    { label: 'Maintenance', route: '/maintenance', icon: 'pi pi-wrench' },
    { label: 'Refunds', route: '/refunds', icon: 'pi pi-dollar' },
  ];
}
