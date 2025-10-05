import { Injectable } from '@angular/core';
import { SignalRService, Reading } from './signalr.service';
import { BehaviorSubject } from 'rxjs';

export interface SeriesPoint { name: Date; value: number; }
export interface Series { name: string; series: SeriesPoint[]; }
export type TimeWindow = '30s' | '1m' | '5m' | '15m';

@Injectable({ providedIn: 'root' })
export class ChartService {
  private capacity = 100_000;
  private buffer: Reading[] = new Array(this.capacity);
  private head = 0;
  private count = 0;
  private window: TimeWindow = '1m';
  private series$ = new BehaviorSubject<Series[]>([]);

  constructor(signalR: SignalRService) {
    signalR.frames().subscribe((frame) => this.ingest(frame));
  }

  setWindow(w: TimeWindow) {
    this.window = w;
    this.publish();
  }

  data$() {
    return this.series$.asObservable();
  }

  private ingest(frame: Reading[]) {
    for (const r of frame) {
      this.buffer[this.head] = r;
      this.head = (this.head + 1) % this.capacity;
      if (this.count < this.capacity) this.count++;
    }
    // throttle publication at ~20fps is already done by server; push immediately
    this.publish();
  }

  private publish() {
    const now = Date.now();
    const windowMs = this.windowMs(this.window);
    const minTs = now - windowMs;
    const series: Series = { name: 'sensor', series: [] };

    // Walk buffer newest->oldest until minTs
    for (let i = 0; i < this.count; i++) {
      const idx = (this.head - 1 - i + this.capacity) % this.capacity;
      const r = this.buffer[idx];
      if (!r) break;
      const ts = new Date(r.ts).getTime();
      if (ts < minTs) break;
      series.series.push({ name: new Date(ts), value: r.value });
    }

    series.series.reverse();
    const down = this.downsample(series.series, 1200); // cap points for chart perf
    this.series$.next([{ name: series.name, series: down }]);
  }

  // Placeholder LTTB-like: simple stride sampling keeping last points
  private downsample(points: SeriesPoint[], max: number): SeriesPoint[] {
    if (points.length <= max) return points;
    const stride = Math.ceil(points.length / max);
    const out: SeriesPoint[] = [];
    for (let i = 0; i < points.length; i += stride) {
      out.push(points[i]);
    }
    // Ensure we include the last point
    if (out[out.length - 1] !== points[points.length - 1]) {
      out.push(points[points.length - 1]);
    }
    return out;
  }

  private windowMs(w: TimeWindow): number {
    switch (w) {
      case '30s':
        return 30_000;
      case '1m':
        return 60_000;
      case '5m':
        return 300_000;
      case '15m':
        return 900_000;
    }
  }
}
