# 🖥️ Frontend — Theme Park Control Center UI

## Overview

The Control Center UI is a modern **Angular (v21+)** single-page application. It gives theme park operators a real-time view of all rides, live workflow progress, chaos event alerts, and operator action controls.

---

## Tech Stack

| Concern | Choice |
|---------|--------|
| Framework | Angular 21+ |
| Change detection | Zoneless (`provideExperimentalZonelessChangeDetection`) |
| Component model | Standalone (moduleless) |
| Routing | Angular Router (`provideRouter`) |
| Styling | SCSS |
| UI component library | PrimeNG |
| Theming | PrimeNG theme system |

---

## Angular Configuration

### Zoneless Change Detection

The app opts out of `zone.js` entirely. Change detection is driven by signals and explicit `markForCheck` / `ChangeDetectorRef` calls where needed.

```ts
// main.ts
bootstrapApplication(AppComponent, {
  providers: [
    provideExperimentalZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(),
    providePrimeNG({ theme: { preset: Aura } }),
  ],
});
```

> ⚠️ Because the app is zoneless, all async state must flow through Angular signals, `AsyncPipe`, or explicit change detection triggers. Do **not** rely on zone-patched APIs to trigger rendering.

### Standalone Components (Moduleless)

There are no `NgModule` declarations. Every component, directive, and pipe is standalone. Dependencies are imported directly in the `imports` array of each component.

```ts
@Component({
  selector: 'app-ride-card',
  standalone: true,
  imports: [CommonModule, ButtonModule, TagModule, RouterLink],
  templateUrl: './ride-card.component.html',
  styleUrl: './ride-card.component.scss',
})
export class RideCardComponent { ... }
```

---

## Routing

Routes are defined in a flat `routes.ts` file and provided via `provideRouter`. Lazy loading is used for feature areas.

```ts
// routes.ts
export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),
  },
  {
    path: 'rides/:rideId',
    loadComponent: () =>
      import('./features/ride-detail/ride-detail.component')
        .then(m => m.RideDetailComponent),
  },
];
```

### Route Structure

| Path | Component | Description |
|------|-----------|-------------|
| `/dashboard` | `DashboardComponent` | Overview of all rides and live event feed |
| `/rides/:rideId` | `RideDetailComponent` | Ride workflow timeline and operator controls |

---

## Styling

All styles are written in **SCSS**. Global styles, variables, and theming overrides live in `src/styles/`.

```
src/
  styles/
    _variables.scss     # Design tokens (colours, spacing, fonts)
    _primeng.scss       # PrimeNG component overrides
    styles.scss         # Root stylesheet, imports all partials
```

Component-level styles use Angular's encapsulated `styleUrl` (`:host` scoping via ViewEncapsulation).

---

## PrimeNG Theming

PrimeNG is configured via `providePrimeNG` with a preset theme. The app uses the **Aura** preset as a base, customised to fit the theme park aesthetic.

```ts
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

providePrimeNG({
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: '.dark-mode',
      cssLayer: {
        name: 'primeng',
        order: 'tailwind-base, primeng, app-styles',
      },
    },
  },
})
```

### Components in Use

| PrimeNG Component | Used For |
|-------------------|----------|
| `Button` | Operator action buttons |
| `Tag` | Ride status indicators |
| `Timeline` | Ride workflow step timeline |
| `Toast` | Chaos event notifications |
| `Dialog` | Operator approval modals |
| `Card` | Ride summary cards on dashboard |
| `ProgressBar` | Ride progress indicator |
| `Badge` | Passenger count and VIP markers |
| `Toolbar` | Control Center top bar |

---

## Project Structure

```
src/
  app/
    app.component.ts          # Root standalone component
    routes.ts                 # Top-level route definitions
    features/
      dashboard/              # Ride overview and live event feed
      ride-detail/            # Ride timeline and operator controls
    shared/
      components/             # Reusable standalone components
      services/               # API clients and signal stores
      models/                 # TypeScript interfaces and types
  styles/
    _variables.scss
    _primeng.scss
    styles.scss
```

---

## State Management

The app uses **Angular Signals** as the primary state primitive. There are no NgRx or other state libraries. Each feature manages its own signal-based state via injectable services.

```ts
@Injectable({ providedIn: 'root' })
export class RideStore {
  readonly rides = signal<Ride[]>([]);
  readonly activeEvents = signal<ChaosEvent[]>([]);

  updateRideStatus(rideId: string, status: RideStatus) {
    this.rides.update(rides =>
      rides.map(r => r.id === rideId ? { ...r, status } : r)
    );
  }
}
```

Real-time updates from the backend are delivered via **Server-Sent Events (SSE)** or **SignalR**, feeding directly into signal updates.

---

## Key Design Decisions

- **Zoneless** keeps the app lean and predictable — no hidden change detection cycles from third-party code.
- **Standalone components** eliminate boilerplate NgModules and make lazy loading trivially easy.
- **Signals** pair naturally with zoneless; the framework re-renders only when a signal used in the template changes.
- **PrimeNG Aura** provides a polished, accessible component set that can be themed to the park's brand without building from scratch.
