import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LiveViewComponent } from './components/live-view/live-view.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LiveViewComponent],
  template: `
    <app-live-view />
    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';
}
