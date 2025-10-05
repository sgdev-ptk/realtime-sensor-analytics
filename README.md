# realtime-sensor-analytics

## Using slash commands

This repo is configured with Copilot Chat slash commands under `.github/prompts` to help you go from idea → spec → plan → tasks → implementation.

Quickstart:
- Copy `.env.example` to `.env` and set `API_KEY`.
- Ensure you have a dev HTTPS cert. On Windows/macOS with .NET SDK: run `dotnet dev-certs https -ep <path-to>/aspnetapp.pfx -p <password>` and update `.env` paths.
- Bring up services via Docker: `docker compose -f deploy/docker-compose.yml up --build`

Health checks: the API exposes `/healthz` and Prometheus at `/metrics`; compose uses these for liveness.
See `specs/001-title-real-time/manual-checklist.md` for a step-by-step manual test flow.

Services:
- API: https://localhost:5001
- Frontend: https://localhost:4200
- Redis: localhost:6379

### Simulator
The backend includes a simple simulator that emits synthetic readings into the processing pipeline. Configure with environment variables:
- SIM__SENSORS: number of sensors (default 10)
- SIM__RATE: total events per second across sensors (default 1000)
# realtime-sensor-analytics

Real-time sensor analytics demo with .NET 8 (API + SignalR + Redis), Angular 17 frontend, and optional Prometheus/Grafana.

## Prerequisites

- Docker Desktop (recommended run path)
- .NET SDK 8.0+ (for HTTPS dev certs and local runs)
- Node.js 20 + npm (for local frontend runs)

Quick reference to project structure: `backend/` (.NET), `frontend/` (Angular), `deploy/` (compose), `specs/` (design notes).

## Run with Docker (recommended)

1) Copy env and set secrets

- Copy `.env.example` to `.env` at the repo root and set `API_KEY` (used for protected endpoints).
- The defaults work for local compose (Redis, simulator, HTTPS cert path inside the container).

2) Create and trust a local HTTPS dev certificate

- Windows/macOS/Linux with .NET SDK:

```powershell
# Trust dev cert on the host (prompts on Windows/macOS)
dotnet dev-certs https --trust

# Export a PFX the container can read (password must match .env)
# This writes to deploy/certs/aspnetapp.pfx with password 'pass123'
New-Item -ItemType Directory -Force -Path ./deploy/certs | Out-Null
dotnet dev-certs https -ep ./deploy/certs/aspnetapp.pfx -p pass123
```

3) Start the stack

```powershell
# From the repo root
docker compose -f deploy/docker-compose.yml up --build
# (Optional) start in background and include monitoring stack
# docker compose -f deploy/docker-compose.yml --profile monitoring up -d --build
```

4) Open the apps

- Frontend: http://localhost:4200
- API Swagger UI: https://localhost:5001/swagger
- Health probe: http://localhost:5000/healthz
- Prometheus (optional profile): http://localhost:9090
- Grafana (optional profile): http://localhost:3000 (default admin/admin)

5) Stop the stack

```powershell
# Stop and remove containers
docker compose -f deploy/docker-compose.yml down
# If you also want to clear Redis data volume, add -v
# docker compose -f deploy/docker-compose.yml down -v
```

Notes
- CORS is set to allow `http://localhost:4200` by default.
- The API exposes `/metrics` (Prometheus) and `/healthz` (compose liveness/readiness).
- The built-in simulator runs automatically and publishes synthetic readings; control via `.env` (`SIM__SENSORS`, `SIM__RATE`).

## Run locally (without Docker)

You can run Redis + the .NET API + the Angular dev server on your host.

1) Start Redis

```powershell
# Using Docker for Redis
docker run --name rsa-redis -p 6379:6379 --rm redis:7-alpine redis-server --save "" --appendonly no
```

2) Trust HTTPS dev cert (if you haven’t already)

```powershell
dotnet dev-certs https --trust
```

3) Run the API

```powershell
# From backend/src/Api
$env:REDIS__CONNECTION = "localhost:6379"
$env:API_KEY = "your-local-api-key"
# Optional: frontend origin override (default is http://localhost:4200)
# $env:FRONTEND_ORIGIN = "http://localhost:4200"

# Run API
dotnet run
# The console will print the actual HTTP/HTTPS URLs (from launchSettings). Typical:
# https://localhost:7215 and http://localhost:5028
```

4) Run the frontend

```powershell
# From frontend
npm ci
npm start
# Opens http://localhost:4200
```

5) Verify

- API Swagger loads at the printed HTTPS URL (e.g., https://localhost:7215/swagger) and shows endpoints.
- Frontend at http://localhost:4200 should render and start receiving live data from the simulator.
- Health: GET http://localhost:5028/healthz (or the printed HTTP port) returns 200.

## Auth and protected endpoints

Set `API_KEY` in your environment (compose uses `.env`). The following endpoints require the key via `x-api-key` header or `?x-api-key=` query string:

- `POST /api/ack/*`
- `GET /api/stream` (SignalR WebSocket connection)

Example header: `x-api-key: your-key`.

## Configuration reference

- `API_KEY`: API key for protected endpoints; blank in dev accepts any non-empty value.
- `REDIS__CONNECTION`: Redis connection string (compose default `redis:6379`, local `localhost:6379`).
- `FRONTEND_ORIGIN`: CORS allowlist origin (default `http://localhost:4200`).
- `SIM__SENSORS`, `SIM__RATE`: Simulator sensor count and total events/sec.
- Dev cert mounting in compose:
  - `.env`: `ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx`
  - `.env`: `ASPNETCORE_Kestrel__Certificates__Default__Password=pass123`
  - `deploy/docker-compose.yml` mounts `./deploy/certs` into the container at `/https`.

## Troubleshooting

- HTTPS warning or 403 on Swagger: ensure the dev cert is trusted and exported to `deploy/certs/aspnetapp.pfx` with the password from `.env`.
- CORS errors from the browser: confirm `FRONTEND_ORIGIN` matches the frontend URL (`http://localhost:4200`) and restart the API.
- Port conflicts (5000/5001/4200/6379): stop the other process or adjust the mappings in `deploy/docker-compose.yml`.
- Redis not reachable: verify container is up (`docker ps`) and `REDIS__CONNECTION` points to the correct host:port.

## Copilot Chat helper commands

This repo includes Copilot Chat slash commands under `.github/prompts` to help go from idea → spec → plan → tasks → implementation.

See `specs/001-title-real-time/manual-checklist.md` for a manual validation flow and `specs/**` for design notes.
