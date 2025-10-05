import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { SignalRService, Reading } from './signalr.service';

export interface MetricsSnapshot {
  ingestRate: number; // readings per second
  latencyP95: number; // ms
  dropsPct: number; // placeholder, not computed yet
}

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private latencies: number[] = [];
  private recentTs: number[] = []; // last 1s of reading timestamps
  private stats = new BehaviorSubject<MetricsSnapshot>({ ingestRate: 0, latencyP95: 0, dropsPct: 0 });

  constructor(signalR: SignalRService) {
    signalR.frames().subscribe((frame) => this.onFrame(frame));
  }

  stats$() {
    return this.stats.asObservable();
  }

  private onFrame(frame: Reading[]) {
    const now = Date.now();
    for (const r of frame) {
      const t = new Date(r.ts).getTime();
      this.recentTs.push(t);
      this.latencies.push(now - t);
    }

    // Trim to last 1s for ingest rate
    const cutoff = now - 1000;
    while (this.recentTs.length && this.recentTs[0] < cutoff) {
      this.recentTs.shift();
    }

    // Keep latency window bounded (e.g., last 500)
    if (this.latencies.length > 500) {
      this.latencies.splice(0, this.latencies.length - 500);
    }

    const ingestRate = this.recentTs.length;
    const latencyP95 = this.percentile(this.latencies, 0.95);
    this.stats.next({ ingestRate, latencyP95, dropsPct: 0 });
  }

  private percentile(values: number[], p: number): number {
    if (!values.length) return 0;
    const a = [...values].sort((x, y) => x - y);
    const idx = Math.max(0, Math.min(a.length - 1, Math.ceil(p * a.length) - 1));
    return a[idx];
  }
}
