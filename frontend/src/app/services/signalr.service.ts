import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { ReplaySubject, Observable } from 'rxjs';

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

  connect(baseUrl: string, apiKey: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/api/stream?x-api-key=${encodeURIComponent(apiKey)}`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('frame', (payload: Reading[]) => {
      this.frames$.next(payload);
    });

    return this.connection.start();
  }

  disconnect(): Promise<void> {
    if (!this.connection) return Promise.resolve();
    return this.connection.stop();
  }

  joinSensor(sensorId: string): Promise<void> {
    if (!this.connection) return Promise.reject('Not connected');
    return this.connection.invoke('JoinSensor', sensorId);
  }

  leaveSensor(sensorId: string): Promise<void> {
    if (!this.connection) return Promise.reject('Not connected');
    return this.connection.invoke('LeaveSensor', sensorId);
  }

  frames(): Observable<Reading[]> {
    return this.frames$.asObservable();
  }
}
