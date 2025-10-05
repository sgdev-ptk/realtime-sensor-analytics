# Data Model: Real-Time Sensor Analytics Dashboard

## Entities

### Reading
- sensorId: string
- ts: datetime (server timestamp)
- value: number
- status: string (e.g., ok, error)

### Aggregate
- sensorId: string | null (null = global)
- window: enum {1m,5m,15m,1h,24h}
- count: number
- min: number
- max: number
- mean: number
- stdev: number
- p95: number

### Alert
- id: string
- sensorId: string
- ts: datetime
- type: string (e.g., zscore, ewma)
- message: string
- severity: enum {info, warn, critical}
- ack: boolean

## Notes
- Use server timestamps for ordering to avoid clock skew.
- Aggregates stored per window for per-sensor and global (sensorId = null).
- Alert acknowledgements persisted for TTL duration (24h).
