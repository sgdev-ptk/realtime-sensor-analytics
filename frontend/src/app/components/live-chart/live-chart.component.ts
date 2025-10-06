import { CommonModule } from '@angular/common';
import { Component, Input, OnDestroy, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { NgxChartsModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { curveMonotoneX } from 'd3-shape';
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
    <div #container style="width: 100%; height: 420px;">
      <ngx-charts-line-chart
        [results]="data"
        [xScaleMin]="xMin"
        [xScaleMax]="xMax"
        [scheme]="scheme"
        [autoScale]="true"
        [timeline]="false"
        [animations]="true"
        [curve]="curve"
        [yAxis]="true"
        [xAxis]="true"
        [showGridLines]="true"
        [view]="view"
      ></ngx-charts-line-chart>
    </div>
  `,
})
export class LiveChartComponent implements OnInit, OnDestroy {
  @Input() window: TimeWindow = '1m';
  data: Series[] = [];
  xMin: number = Date.now() - 60_000;
  xMax: number = Date.now();
  scheme: Color = { name: 'cool', selectable: true, group: ScaleType.Ordinal, domain: ['#42a5f5'] };
  curve = curveMonotoneX;
  private sub?: Subscription;
  @ViewChild('container', { static: true }) containerRef!: ElementRef<HTMLDivElement>;
  // [width, height] in px; width adapts to container, height fixed to container height
  view: [number, number] = [700, 420];

  constructor(private readonly chart: ChartService) {}

  ngOnInit(): void {
    this.chart.setWindow(this.window);
    // Initialize view based on container size
    this.resizeView();
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

  @HostListener('window:resize')
  private resizeView() {
    // Safely compute width from container; keep height fixed from container style
    const el = this.containerRef?.nativeElement;
    if (!el) return;
    const width = Math.max(300, el.clientWidth);
    const height = el.clientHeight || 420;
    this.view = [width, height];
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
