# realtime-sensor-analytics

Real-time sensor analytics demo with .NET 8 (API + SignalR + Redis), Angular 17 frontend, and optional Prometheus/Grafana.

This page is a step-by-step guide for new developers to run the project locally on Windows using PowerShell. It covers an all-in-one Docker setup and a pure local setup.

## Prerequisites

- Docker Desktop (recommended path to run everything)
- .NET SDK 8.0+ (for HTTPS dev certs and/or local API runs)
- Node.js 20 + npm (for local frontend runs)

Repo layout: `backend/` (.NET), `frontend/` (Angular), `deploy/` (compose), `specs/` (design notes).

## Quick start (Docker — recommended)

1) Copy env and set secrets

- Copy `.env.example` to `.env` at the repo root.
  (API keys are not required for local development.)

2) Create and trust a local HTTPS dev certificate

```powershell
# Trust dev cert on the host (prompts on Windows)
dotnet dev-certs https --trust

# Export a PFX the container can read (password must match your .env)
# Writes to deploy/certs/aspnetapp.pfx with password 'pass123'
New-Item -ItemType Directory -Force -Path ./deploy/certs | Out-Null
dotnet dev-certs https -ep ./deploy/certs/aspnetapp.pfx -p pass123
```

3) Start the stack

```powershell
# From the repo root
docker compose -f deploy/docker-compose.yml up --build
# Optional: run detached with monitoring
# docker compose -f deploy/docker-compose.yml --profile monitoring up -d --build
```

4) Open the apps

- Frontend: http://localhost:4200
- API Swagger: https://localhost:5001/swagger
- Health: http://localhost:5000/healthz
- Prometheus (optional): http://localhost:9090
- Grafana (optional): http://localhost:3000 (default admin/admin)

5) Connect the frontend UI

- In Live View, enter:
  - API Base URL: http://localhost:5000 or https://localhost:5001
- Click Connect, then Join (sensor-1)

6) Stop the stack

```powershell
docker compose -f deploy/docker-compose.yml down
# Add -v to also remove the Redis volume
# docker compose -f deploy/docker-compose.yml down -v
```

Notes
- CORS allows `http://localhost:4200` by default.
- API exposes `/metrics` (Prometheus) and `/healthz` (compose liveness/readiness).
- The simulator runs automatically; tune via `.env` (`SIM__SENSORS`, `SIM__RATE`).

## Quick start (local without Docker)

Run Redis, the .NET API, and the Angular dev server on your machine.

1) Start Redis (via Docker is easiest)

```powershell
docker run --name rsa-redis -p 6379:6379 --rm redis:7-alpine `
  redis-server --save "" --appendonly no
```

2) Trust the dev HTTPS certificate (once)

```powershell
dotnet dev-certs https --trust
```

3) Run the API

```powershell
# From backend/src/Api
$env:REDIS__CONNECTION = "localhost:6379"
$env:API_KEY = "dev-key"
# Optional: override CORS if needed (default is http://localhost:4200)
# $env:FRONTEND_ORIGIN = "http://localhost:4200"

dotnet run
# The console will print the actual HTTP/HTTPS URLs from launchSettings, e.g.:
# http://localhost:5028 and https://localhost:7215
```

4) Run the frontend

```powershell
# From frontend
npm ci
npm start
# Opens http://localhost:4200
```

5) Connect the frontend UI

- API Base URL: use the port printed by `dotnet run` (e.g., http://localhost:5028 or https://localhost:7215)
- API Key: not required
- Click Connect, then Join (sensor-1)

### One-command local runner (PowerShell)

You can use the helper script to run everything locally (starts Redis via Docker if available, then backend and frontend):

```powershell
pwsh -ExecutionPolicy Bypass -File ./scripts/run-local.ps1 -ApiKey dev-key
```

Flags:
- `-CheckOnly` to validate prerequisites and exit
- `-NoDockerRedis` to skip starting Redis via Docker
- `-SkipCert` to skip trusting the dev HTTPS cert

## Verify the setup

- API Swagger loads at the printed HTTPS URL (e.g., https://localhost:7215/swagger).
- Frontend at http://localhost:4200 renders and begins receiving live data.
- Health returns 200 at the API HTTP URL: `/healthz`.

## Auth

Local development environment does not require an API key. Endpoints like `POST /api/ack/{alertId}` and the SignalR hub `/api/stream` are open for easier setup.

## Configuration reference

- `API_KEY`: not required in local development.
- `REDIS__CONNECTION`: Redis connection string (compose default `redis:6379`, local `localhost:6379`).
- `FRONTEND_ORIGIN`: CORS allowlist origin (default `http://localhost:4200`).
- `SIM__SENSORS`, `SIM__RATE`: simulator sensor count and total events/sec.
- Dev cert in compose:
  - `.env`: `ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx`
  - `.env`: `ASPNETCORE_Kestrel__Certificates__Default__Password=pass123`
  - `deploy/docker-compose.yml` mounts `./deploy/certs` to `/https`.

## Troubleshooting

- HTTPS warning or 403 on Swagger: trust the dev cert and ensure `deploy/certs/aspnetapp.pfx` exists with the password from `.env`.
- CORS errors: confirm `FRONTEND_ORIGIN` matches the frontend URL and restart the API.
- Port conflicts (5000/5001/4200/6379): stop the conflicting process or adjust port mappings in `deploy/docker-compose.yml`.
- Redis not reachable: verify the container is running (`docker ps`) and `REDIS__CONNECTION` points to the correct host:port.
- SignalR connect fails over HTTPS: try the HTTP port first; if HTTPS, ensure the cert is trusted.

## Optional monitoring

Enable Prometheus and Grafana via the compose profile:

```powershell
docker compose -f deploy/docker-compose.yml --profile monitoring up -d --build
```

Prometheus: http://localhost:9090, Grafana: http://localhost:3000 (default admin/admin).

## Dev tips (Copilot Chat)

This repo includes Copilot Chat slash commands under `.github/prompts` to help go from idea → spec → plan → tasks → implementation. See `specs/001-title-real-time/manual-checklist.md` for a manual validation flow and `specs/**` for design notes.
