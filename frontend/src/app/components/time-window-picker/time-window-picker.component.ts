import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TimeWindow } from '../../services/chart.service';

@Component({
  selector: 'app-time-window-picker',
  standalone: true,
  imports: [CommonModule],
  template: `
    <label for="tw">Window:</label>
    <select id="tw" [value]="value" (change)="onChange($any($event.target).value)">
      <option value="30s">30s</option>
      <option value="1m">1m</option>
      <option value="5m">5m</option>
      <option value="15m">15m</option>
    </select>
  `,
})
export class TimeWindowPickerComponent {
  @Input() value: TimeWindow = '1m';
  @Output() valueChange = new EventEmitter<TimeWindow>();
  onChange(v: TimeWindow) {
    this.value = v;
    this.valueChange.emit(v);
  }
}
