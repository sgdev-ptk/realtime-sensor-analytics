# Manual Test Checklist

- [ ] Bring up stack: docker compose -f deploy/docker-compose.yml up --build
- [ ] Open frontend http://localhost:4200
- [ ] Connect with API key; join sensor-1
- [ ] LiveChart animates; FPS feels smooth
- [ ] Stats show ingest rate and p95 latency updating
- [ ] Alerts appear when anomaly simulated; Ack works
- [ ] Disconnect/reconnect; backfill visible where applicable
- [ ] Prometheus metrics exposed at https://localhost:5001/api/metrics
- [ ] Redis keys TTL approx 24h; sample keys expire
- [ ] Compose healthchecks show healthy for api, frontend, redis

Screenshots: add to specs/001-title-real-time/screenshots/
