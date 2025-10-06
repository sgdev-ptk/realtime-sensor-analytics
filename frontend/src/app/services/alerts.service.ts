import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AlertsService {
  private baseUrl = '';
  private apiKey = '';

  constructor(private readonly http: HttpClient) {}

  setAuth(baseUrl: string, apiKey?: string) {
    this.baseUrl = baseUrl?.trim();
    this.apiKey = apiKey?.trim() ?? '';
  }

  ackAlert(alertId: string) {
    if (!this.baseUrl) throw new Error('AlertsService not configured. Call setAuth(baseUrl) first.');
    const url = `${this.baseUrl}/api/ack/${encodeURIComponent(alertId)}`;
    // API is open; no auth header needed
    return this.http.post<void>(url, null);
  }
}
