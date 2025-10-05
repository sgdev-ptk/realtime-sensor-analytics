import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LiveViewComponent } from './components/live-view/live-view.component';
import { LiveChartComponent } from './components/live-chart/live-chart.component';
import { StatsPanelComponent } from './components/stats-panel/stats-panel.component';
import { AlertsPanelComponent } from './components/alerts-panel/alerts-panel.component';
import { StatusBarComponent } from './components/status-bar/status-bar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LiveViewComponent, LiveChartComponent, StatsPanelComponent, AlertsPanelComponent, StatusBarComponent],
  template: `
    <app-live-view />
    <section style="max-width: 960px; margin: 1rem auto;">
      <app-live-chart />
      <div style="display:grid; grid-template-columns: 1fr 1fr; gap: .75rem; margin-top:.75rem;">
        <app-stats-panel />
        <app-alerts-panel />
      </div>
      <app-status-bar style="margin-top:.5rem;" />
    </section>
    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';
}
