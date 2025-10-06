import { CommonModule } from '@angular/common';
import {
  Component,
  Input,
  OnDestroy,
  OnInit,
  AfterViewInit,
  ElementRef,
  ViewChild,
  HostListener
} from '@angular/core';
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
        [animations]="true"
        [curve]="curve"
        [yAxis]="true"
        [xAxis]="true"
        [showGridLines]="true"
        [timeline]="false"
        [view]="view"
        [legend]="false"
        [showXAxisLabel]="false"
        [showYAxisLabel]="false"
        [xAxisTickFormatting]="timeTick"
      ></ngx-charts-line-chart>
    </div>
  `,
})
export class LiveChartComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() window: TimeWindow = '1m';
  data: Series[] = [];
  xMin: number = Date.now() - 60_000; // in ms
  xMax: number = Date.now();          // in ms
  scheme: Color = { name: 'cool', selectable: true, group: ScaleType.Ordinal, domain: ['#42a5f5'] };
  curve = curveMonotoneX;
  private sub?: Subscription;

  @ViewChild('container', { static: true }) containerRef!: ElementRef<HTMLDivElement>;
  view: [number, number] = [700, 420];

  constructor(private readonly chart: ChartService) {}

  ngOnInit(): void {
    this.chart.setWindow(this.window);

    // Subscribe to live data
    this.sub = this.chart.data$().subscribe((d) => {
      this.data = d;
      const now = Date.now();
      const ms = this.toMs(this.window);
      this.xMax = now;
      this.xMin = now - ms;
    });
  }

  ngAfterViewInit(): void {
    // Resize chart after view init
    setTimeout(() => this.resizeView(), 100);
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
    const el = this.containerRef?.nativeElement;
    if (!el) return;
    const width = Math.max(400, el.clientWidth); // prevent too small width
    const height = el.clientHeight || 420;
    this.view = [width, height];
  }

  private toMs(w: TimeWindow): number {
    switch (w) {
      case '30s': return 30_000;
      case '1m': return 60_000;
      case '5m': return 300_000;
      case '15m': return 900_000;
      default: return 60_000;
    }
  }

  // Format X-axis ticks as HH:MM:SS
  timeTick(val: number): string {
    const d = new Date(val);
    const h = d.getHours().toString().padStart(2, '0');
    const m = d.getMinutes().toString().padStart(2, '0');
    const s = d.getSeconds().toString().padStart(2, '0');
    return `${h}:${m}:${s}`;
  }
}
