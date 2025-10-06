import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { ReplaySubject, Observable, BehaviorSubject } from 'rxjs';

export interface Reading {
  sensorId: string;
  ts: string; // ISO string
  value: number;
  status?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection?: signalR.HubConnection;
  private frames$ = new ReplaySubject<Reading[]>(100); // ~5s at 20 FPS if needed
  private state$ = new BehaviorSubject<'disconnected' | 'connecting' | 'connected' | 'reconnecting'>('disconnected');
  private currentSensor?: string;

  connect(baseUrl: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    const streamUrl = `${baseUrl}/api/stream`;
    this.state$.next('connecting');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(streamUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.on('frame', (payload: Reading[]) => {
      this.frames$.next(payload);
      // lightweight client-side trace to help debug joins
      // eslint-disable-next-line no-console
      if (payload?.length) console.debug('[signalr] frame', { count: payload.length, sensor: payload[payload.length - 1]?.sensorId });
    });

    this.connection.onreconnecting(() => this.state$.next('reconnecting'));
    this.connection.onreconnected(() => this.state$.next('connected'));
    this.connection.onclose(() => this.state$.next('disconnected'));

    return this.connection.start().then(async () => {
      this.state$.next('connected');
      // Auto-join a sensible default if no join has been requested yet
      if (!this.currentSensor) {
        this.currentSensor = 'sensor-1';
      }
      try { await this.joinSensor(this.currentSensor); } catch (e) { /* eslint-disable no-console */ console.warn('[signalr] auto-join failed', e); }
    });
  }

  disconnect(): Promise<void> {
    if (!this.connection) return Promise.resolve();
    return this.connection.stop();
  }

  joinSensor(sensorId: string): Promise<void> {
    if (!this.connection) return Promise.reject('Not connected');
    this.currentSensor = sensorId;
    return this.connection.invoke('JoinSensor', sensorId);
  }

  leaveSensor(sensorId: string): Promise<void> {
    if (!this.connection) return Promise.reject('Not connected');
    return this.connection.invoke('LeaveSensor', sensorId);
  }

  frames(): Observable<Reading[]> {
    return this.frames$.asObservable();
  }

  connectionState(): Observable<'disconnected' | 'connecting' | 'connected' | 'reconnecting'> {
    return this.state$.asObservable();
  }
}
