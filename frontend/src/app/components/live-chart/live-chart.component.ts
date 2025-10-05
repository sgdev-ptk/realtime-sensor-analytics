import { CommonModule } from '@angular/common';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { NgxChartsModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { ChartService, Series, TimeWindow } from '../../services/chart.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-live-chart',
  standalone: true,
  imports: [CommonModule, NgxChartsModule],
  template: `
    <div style="display:flex; align-items:center; gap: .5rem; margin-bottom: .5rem;">
      <label for="windowSel">Window:</label>
      <select id="windowSel" [value]="window" (change)="onWindow($any($event.target).value)">
        <option value="30s">30s</option>
        <option value="1m">1m</option>
        <option value="5m">5m</option>
        <option value="15m">15m</option>
      </select>
    </div>
    <ngx-charts-line-chart
      [results]="data"
      [xScaleMin]="xMin"
      [xScaleMax]="xMax"
      [scheme]="scheme"
      [autoScale]="true"
      [timeline]="false"
      [animations]="false"
      [curve]="'monotoneX'"
      [yAxis]="true"
      [xAxis]="true"
      [showGridLines]="true"
    ></ngx-charts-line-chart>
  `,
})
export class LiveChartComponent implements OnInit, OnDestroy {
  @Input() window: TimeWindow = '1m';
  data: Series[] = [];
  xMin: number = Date.now() - 60_000;
  xMax: number = Date.now();
  scheme: Color = { name: 'cool', selectable: true, group: ScaleType.Ordinal, domain: ['#42a5f5'] };
  private sub?: Subscription;

  constructor(private readonly chart: ChartService) {}

  ngOnInit(): void {
    this.chart.setWindow(this.window);
    this.sub = this.chart.data$().subscribe((d) => {
      this.data = d;
      const now = Date.now();
      const ms = this.toMs(this.window);
      this.xMax = now;
      this.xMin = now - ms;
    });
  }

  onWindow(w: TimeWindow) {
    this.window = w;
    this.chart.setWindow(w);
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private toMs(w: TimeWindow): number {
    switch (w) {
      case '30s':
        return 30_000;
      case '1m':
        return 60_000;
      case '5m':
        return 300_000;
      case '15m':
        return 900_000;
    }
  }
}
