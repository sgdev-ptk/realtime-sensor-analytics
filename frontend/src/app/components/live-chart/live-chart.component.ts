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
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-live-chart',
  standalone: true,
  imports: [CommonModule, NgxChartsModule, FormsModule],
  template: `
    <!-- Zoom controls -->
    <div style="display:flex; align-items:center; gap: .5rem; margin-bottom: .5rem;">
      <label>Window:</label>
      <button (click)="zoomIn()">+</button>
      <button (click)="zoomOut()">-</button>
      <span>{{window}}</span>
    </div>

    <!-- Pan controls -->
    <div style="display:flex; gap: .5rem; margin-bottom: .5rem;">
      <button (click)="panLeft()">&#8592; Pan Left</button>
      <button (click)="panRight()">Pan Right &#8594;</button>
    </div>

    <!-- Focus section selection -->
    <div style="margin-bottom: .5rem; display:flex; gap: .5rem; align-items:center;">
      <label>Focus From:</label>
      <input type="time" [(ngModel)]="focusStart" step="1" (change)="applyFocus()" />

      <label>To:</label>
      <input type="time" [(ngModel)]="focusEnd" step="1" (change)="applyFocus()" />

      <button (click)="resetFocus()">Reset</button>
    </div>

    <!-- Chart -->
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
  xMin: number = Date.now() - 60_000;
  xMax: number = Date.now();
  scheme: Color = { name: 'cool', selectable: true, group: ScaleType.Ordinal, domain: ['#42a5f5'] };
  curve = curveMonotoneX;
  private sub?: Subscription;

  @ViewChild('container', { static: true }) containerRef!: ElementRef<HTMLDivElement>;
  view: [number, number] = [700, 420];

  // Zoom windows
  private windows: TimeWindow[] = ['30s', '1m', '5m', '15m'];
  private panStep = 10_000; // 10 seconds pan

  // Focus section
  focusStart: string = '';
  focusEnd: string = '';

  constructor(private readonly chart: ChartService) {}

  ngOnInit(): void {
    this.chart.setWindow(this.window);

    this.sub = this.chart.data$().subscribe((d) => {
      this.data = d;

      // Only update live xMin/xMax if no focus applied
      if (!this.focusStart || !this.focusEnd) {
        const now = Date.now();
        const ms = this.toMs(this.window);
        this.xMax = now;
        this.xMin = now - ms;
      }
    });
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.resizeView(), 100);
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  // Zoom in/out
  zoomIn() {
    const idx = this.windows.indexOf(this.window);
    if (idx > 0) this.onWindow(this.windows[idx - 1]);
  }

  zoomOut() {
    const idx = this.windows.indexOf(this.window);
    if (idx < this.windows.length - 1) this.onWindow(this.windows[idx + 1]);
  }

  onWindow(w: TimeWindow) {
    this.window = w;
    this.chart.setWindow(w);
    this.resetFocus();
  }

  // Focus section
  applyFocus() {
    if (!this.focusStart || !this.focusEnd) return;

    const now = new Date();
    const [h1, m1, s1] = this.focusStart.split(':').map(Number);
    const [h2, m2, s2] = this.focusEnd.split(':').map(Number);

    const start = new Date(now);
    start.setHours(h1, m1, s1, 0);

    const end = new Date(now);
    end.setHours(h2, m2, s2, 0);

    this.xMin = start.getTime();
    this.xMax = end.getTime();
  }

  resetFocus() {
    this.focusStart = '';
    this.focusEnd = '';
    const ms = this.toMs(this.window);
    const now = Date.now();
    this.xMax = now;
    this.xMin = now - ms;
  }

  // Pan left/right
  panLeft() {
    this.xMin -= this.panStep;
    this.xMax -= this.panStep;
  }

  panRight() {
    const now = Date.now();
    if (this.xMax + this.panStep <= now) {
      this.xMin += this.panStep;
      this.xMax += this.panStep;
    } else {
      const delta = now - this.xMax;
      this.xMin += delta;
      this.xMax = now;
    }
  }

  @HostListener('window:resize')
  private resizeView() {
    const el = this.containerRef?.nativeElement;
    if (!el) return;
    const width = Math.max(400, el.clientWidth);
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

  timeTick(val: number): string {
    const d = new Date(val);
    const h = d.getHours().toString().padStart(2, '0');
    const m = d.getMinutes().toString().padStart(2, '0');
    const s = d.getSeconds().toString().padStart(2, '0');
    return `${h}:${m}:${s}`;
  }
}
