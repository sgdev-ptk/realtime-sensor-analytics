import { ChartService } from './chart.service';
import { SignalRService } from './signalr.service';
import { of } from 'rxjs';

describe('ChartService', () => {
  it('updates time window and emits data', (done) => {
    const mockSignalR = {
      frames: () => of([{ sensorId: 's', ts: new Date().toISOString(), value: 1 }]),
    } as unknown as SignalRService;
    const svc = new ChartService(mockSignalR);
    const sub = svc.data$().subscribe((d) => {
      expect(Array.isArray(d)).toBeTrue();
      sub.unsubscribe();
      done();
    });
    svc.setWindow('30s');
  });
});
