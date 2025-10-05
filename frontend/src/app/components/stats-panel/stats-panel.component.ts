import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-stats-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section style="padding:.5rem; border:1px solid #ddd; border-radius:8px;">
      <h3 style="margin:0 0 .5rem 0; font-size: 1rem;">Stats</h3>
      <div>Ingest: <strong>{{ ingestRate }}/s</strong></div>
      <div>Latency p95: <strong>{{ latencyP95 }} ms</strong></div>
      <div>Drops: <strong>{{ dropsPct }}%</strong></div>
    </section>
  `,
})
export class StatsPanelComponent {
  @Input() ingestRate = 0;
  @Input() latencyP95 = 0;
  @Input() dropsPct = 0;
}
