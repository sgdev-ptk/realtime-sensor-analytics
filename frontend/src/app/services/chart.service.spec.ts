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

  it('downsamples large series to a reasonable cap', (done) => {
    const many = Array.from({ length: 5000 }, (_, i) => ({ sensorId: 's', ts: new Date(Date.now() - (5000 - i)).toISOString(), value: i }));
    const mockSignalR = { frames: () => of(many) } as unknown as SignalRService;
    const svc = new ChartService(mockSignalR);
    const sub = svc.data$().subscribe((d) => {
      const points = d[0]?.series?.length ?? 0;
      expect(points).toBeGreaterThan(0);
      expect(points).toBeLessThanOrEqual(1201); // cap + last point
      sub.unsubscribe();
      done();
    });
    svc.setWindow('1m');
  });
});
