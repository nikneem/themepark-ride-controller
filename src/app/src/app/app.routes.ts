import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'rides',
    loadComponent: () => import('./features/rides/rides-list/rides-list.component').then(m => m.RidesListComponent)
  },
  {
    path: 'rides/:id',
    loadComponent: () => import('./features/rides/ride-detail/ride-detail.component').then(m => m.RideDetailComponent)
  },
  {
    path: 'weather',
    loadComponent: () => import('./features/weather/weather.component').then(m => m.WeatherComponent)
  },
  {
    path: 'mascots',
    loadComponent: () => import('./features/mascots/mascots.component').then(m => m.MascotsComponent)
  },
  {
    path: 'queues',
    loadComponent: () => import('./features/queues/queues.component').then(m => m.QueuesComponent)
  },
  {
    path: 'maintenance',
    loadComponent: () => import('./features/maintenance/maintenance.component').then(m => m.MaintenanceComponent)
  },
  {
    path: 'refunds',
    loadComponent: () => import('./features/refunds/refunds.component').then(m => m.RefundsComponent)
  },
];
