import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

export interface SseEvent {
  type: string;
  data: unknown;
}

@Injectable({ providedIn: 'root' })
export class SseService implements OnDestroy {
  private eventSource: EventSource | null = null;
  private eventsSubject = new Subject<SseEvent>();
  public events$ = this.eventsSubject.asObservable();
  private reconnectTimeout: ReturnType<typeof setTimeout> | null = null;

  connect(): void {
    this.disconnect();
    this.eventSource = new EventSource('/api/events/stream');

    this.eventSource.addEventListener('ride-status-changed', (event: MessageEvent) => {
      try {
        this.eventsSubject.next({ type: 'ride-status-changed', data: JSON.parse(event.data) });
      } catch {
        this.eventsSubject.next({ type: 'ride-status-changed', data: event.data });
      }
    });

    this.eventSource.onerror = () => {
      this.disconnect();
      this.reconnectTimeout = setTimeout(() => this.connect(), 5000);
    };
  }

  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
    this.eventsSubject.complete();
  }
}
