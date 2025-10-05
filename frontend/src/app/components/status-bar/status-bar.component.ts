import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-status-bar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer style="display:flex; gap:1rem; font-size:.9rem; color:#444; padding:.25rem 0;">
      <div>Ingest: {{ ingestRate }}/s</div>
      <div>Latency: p95 {{ latencyP95 }} ms</div>
      <div>Drops: {{ dropsPct }}%</div>
      <div *ngIf="status">Status: {{ status }}</div>
    </footer>
  `,
})
export class StatusBarComponent {
  @Input() ingestRate = 0;
  @Input() latencyP95 = 0;
  @Input() dropsPct = 0;
  @Input() status = '';
}
