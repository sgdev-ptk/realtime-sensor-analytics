import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AlertsService {
  private baseUrl = '';
  private apiKey = '';

  constructor(private readonly http: HttpClient) {}

  setAuth(baseUrl: string, apiKey: string) {
    this.baseUrl = baseUrl?.trim();
    this.apiKey = apiKey?.trim();
  }

  ackAlert(alertId: string) {
    if (!this.baseUrl || !this.apiKey) {
      throw new Error('AlertsService not configured. Call setAuth(baseUrl, apiKey) first.');
    }
    const url = `${this.baseUrl}/api/ack/${encodeURIComponent(alertId)}`;
    const headers = new HttpHeaders({ 'x-api-key': this.apiKey });
    return this.http.post<void>(url, null, { headers });
  }
}
