import { CommonModule } from '@angular/common';
import { Component, OnDestroy, signal } from '@angular/core';
import { SignalRService, Reading } from '../../services/signalr.service';
import { AlertsService } from '../../services/alerts.service';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-live-view',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section style="padding: 1rem; max-width: 900px; margin: 0 auto; font-family: ui-sans-serif, system-ui, -apple-system;">
      <h2 style="margin-bottom: 0.5rem;">Minimal Live View</h2>
      <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 0.5rem; align-items: end;">
        <label style="display: flex; flex-direction: column;">
          <span>API Base URL</span>
          <input [(ngModel)]="baseUrl" placeholder="http://localhost:5028" />
        </label>
        <button (click)="connect()" [disabled]="connecting">{{ connecting ? 'Connecting…' : 'Connect' }}</button>
      </div>

      <div style="display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 0.5rem; margin-top: 0.75rem; align-items: end;">
        <label style="display: flex; flex-direction: column;">
          <span>Sensor Id</span>
          <input [(ngModel)]="sensorId" placeholder="sensor-1" />
        </label>
        <button (click)="join()" [disabled]="!isConnected()">Join</button>
        <button (click)="leave()" [disabled]="!isConnected()">Leave</button>
      </div>

      <div style="margin-top: 1rem; padding: 0.75rem; border: 1px solid #ddd; border-radius: 8px;">
        <div>Connection: <strong>{{ connectionState() }}</strong></div>
        <div>Total frames: <strong>{{ totalFrames }}</strong></div>
        <div>Last frame size: <strong>{{ lastFrameSize }}</strong></div>
        <div>Last reading: <code>{{ lastReadingText }}</code></div>
      </div>

      <div *ngIf="lastFrame.length" style="margin-top: 0.75rem;">
        <h3 style="margin: 0 0 0.25rem 0;">Frame preview (up to 10)</h3>
        <ul style="max-height: 200px; overflow: auto; margin: 0; padding-left: 1rem;">
          <li *ngFor="let r of lastFrame | slice:0:10">{{ r.sensorId }} — {{ r.ts }} — {{ r.value | number: '1.2-2' }}</li>
        </ul>
      </div>
    </section>
  `,
})
export class LiveViewComponent implements OnDestroy {
  baseUrl = 'http://localhost:5028';
  sensorId = 'sensor-1';

  connecting = false;
  totalFrames = 0;
  lastFrameSize = 0;
  lastFrame: Reading[] = [];
  lastReadingText = '';

  private sub?: Subscription;

  constructor(private readonly signalR: SignalRService, private readonly alerts: AlertsService) {
    // keep the status label in sync with the service
    this.signalR.connectionState().subscribe((s) => this.connectionState.set(s));
  }

  connect() {
    if (!this.baseUrl) { alert('Enter base URL'); return; }
    this.connecting = true;
    this.signalR
      .connect(this.baseUrl)
      .then(() => {
        this.connecting = false;
        this.alerts.setAuth(this.baseUrl);
        this.subscribeFrames();
        // Automatically join the current sensor for a fast first run
        if (this.sensorId) {
          this.signalR.joinSensor(this.sensorId).catch((e) => console.error('Auto-join failed', e));
        }
      })
      .catch((err) => {
        this.connecting = false;
        console.error(err);
        alert('Connection failed. See console.');
      });
  }

  join() {
    if (!this.sensorId) return;
    this.signalR.joinSensor(this.sensorId).catch((e) => console.error(e));
  }

  leave() {
    if (!this.sensorId) return;
    this.signalR.leaveSensor(this.sensorId).catch((e) => console.error(e));
  }

  subscribeFrames() {
    this.sub?.unsubscribe();
    this.sub = this.signalR.frames().subscribe((frame) => {
      this.totalFrames++;
      this.lastFrameSize = frame.length;
      this.lastFrame = frame;
      const last = frame.at(-1);
      this.lastReadingText = last ? `${last.sensorId} @ ${last.ts} = ${last.value}` : '';
    });
  }

  isConnected() {
    const s = this.connectionState();
    return s === 'connected' || s === 'reconnecting';
  }

  connectionState = signal<'disconnected' | 'connecting' | 'connected' | 'reconnecting'>('disconnected');

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
