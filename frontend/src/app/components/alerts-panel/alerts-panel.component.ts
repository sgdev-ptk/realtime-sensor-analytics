import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface AlertVm {
  id: string;
  sensorId: string;
  ts: string;
  type: string;
  message: string;
  severity: 'Info' | 'Warn' | 'Critical';
  ack: boolean;
}

@Component({
  selector: 'app-alerts-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section style="padding:.5rem; border:1px solid #ddd; border-radius:8px;">
      <h3 style="margin:0 0 .5rem 0; font-size: 1rem;">Alerts</h3>
      <div *ngIf="!alerts.length">No alerts</div>
      <ul style="margin:0; padding-left:1rem; max-height:200px; overflow:auto;">
        <li *ngFor="let a of alerts">
          <strong>[{{ a.severity }}]</strong> {{ a.sensorId }} &#64; {{ a.ts }} — {{ a.message }}
          <button (click)="ack.emit(a.id)" [disabled]="a.ack" style="margin-left:.5rem;">{{ a.ack ? 'Acked' : 'Ack' }}</button>
        </li>
      </ul>
    </section>
  `,
})
export class AlertsPanelComponent {
  @Input() alerts: AlertVm[] = [];
  @Output() ack = new EventEmitter<string>();
}
