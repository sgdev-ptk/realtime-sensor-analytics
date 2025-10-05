import { Component } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LiveViewComponent } from './components/live-view/live-view.component';
import { LiveChartComponent } from './components/live-chart/live-chart.component';
import { StatsPanelComponent } from './components/stats-panel/stats-panel.component';
import { AlertsPanelComponent, AlertVm } from './components/alerts-panel/alerts-panel.component';
import { StatusBarComponent } from './components/status-bar/status-bar.component';
import { MetricsService } from './services/metrics.service';
import { AlertsService } from './services/alerts.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LiveViewComponent, LiveChartComponent, StatsPanelComponent, AlertsPanelComponent, StatusBarComponent, AsyncPipe],
  template: `
    <app-live-view />
    <section style="max-width: 960px; margin: 1rem auto;">
      <app-live-chart />
      <div style="display:grid; grid-template-columns: 1fr 1fr; gap: .75rem; margin-top:.75rem;">
        <app-stats-panel
          [ingestRate]="(metrics.stats$() | async)?.ingestRate ?? 0"
          [latencyP95]="(metrics.stats$() | async)?.latencyP95 ?? 0"
          [dropsPct]="(metrics.stats$() | async)?.dropsPct ?? 0"
        />
        <app-alerts-panel [alerts]="alerts" (ack)="onAck($event)" />
      </div>
      <app-status-bar
        style="margin-top:.5rem;"
        [ingestRate]="(metrics.stats$() | async)?.ingestRate ?? 0"
        [latencyP95]="(metrics.stats$() | async)?.latencyP95 ?? 0"
        [dropsPct]="(metrics.stats$() | async)?.dropsPct ?? 0"
      />
    </section>
    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';
  alerts: AlertVm[] = [];

  constructor(public metrics: MetricsService, private readonly alertsSvc: AlertsService) {}

  onAck(id: string) {
    // optimistic update
    const idx = this.alerts.findIndex(a => a.id === id);
    if (idx >= 0) this.alerts[idx] = { ...this.alerts[idx], ack: true };
    this.alerts = [...this.alerts];

    this.alertsSvc.ackAlert(id).subscribe({
      error: () => {
        // revert on error
        if (idx >= 0) this.alerts[idx] = { ...this.alerts[idx], ack: false };
        this.alerts = [...this.alerts];
        alert('Ack failed');
      }
    });
  }
}
