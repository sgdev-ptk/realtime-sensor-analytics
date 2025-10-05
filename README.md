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

## Using slash commands

This repo is configured with Copilot Chat slash commands under `.github/prompts` to help you go from idea → spec → plan → tasks → implementation.

Quickstart inside Copilot Chat:

1. Create a feature spec:
	- `/specify <your feature description>`
2. Clarify the spec (optional but recommended):
	- `/clarify`
3. Generate the implementation plan and design artifacts:
	- `/plan`
4. Generate an actionable task list:
	- `/tasks`
5. Analyze for gaps (read-only):
	- `/analyze`
6. Implement according to tasks:
	- `/implement`

See `.github/prompts/README.md` for details on each command and prerequisites.
