# Quickstart: Real-Time Sensor Analytics PoC

## Prereqs
- Docker Desktop
- Dev cert for HTTPS (dotnet dev-certs)

## One-command up
- docker compose -f deploy/docker-compose.yml up --build

## Verify
- Open frontend at https://localhost:4200 (self-signed)
- Verify live chart animates and FPS ≥ 30
- Confirm stats panel shows rolling aggregates
- Temporarily disconnect network (or stop backend) and reconnect; gaps heal (5s backfill)
- Trigger anomaly in simulator; alert appears < 500 ms; acknowledge it
- Check TTL purge (keys expire after 24h) — simulated in demo by reducing TTL temporarily
- Metrics at https://localhost:5001/api/metrics (Prometheus OK)
- Container health checks should report healthy for api, frontend, redis

See also: Manual Test Checklist in specs/001-title-real-time/manual-checklist.md

## Notes
- API key is provided via environment variable in compose; do not commit secrets.
- Grafana/Prometheus are optional services; disable if not needed.
