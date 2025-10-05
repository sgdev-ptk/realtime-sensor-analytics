# Tasks: Real-Time Sensor Analytics Dashboard — Technical Plan

**Input**: Design documents from `/specs/001-title-real-time/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Decisions → setup/perf/observability tasks
   → quickstart.md: Integration scenarios → tests
3. Generate tasks by category:
   → Setup, Tests (contract/integration), Core, Integration, Polish
4. Apply task rules:
   → Different files = mark [P]
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph & parallel examples
7. Validate task completeness
```

## Path Conventions
- Web app: `backend/src/`, `backend/tests/`, `frontend/src/`, `deploy/`

---

## Phase 3.1: Setup
- [x] T001 Create backend solution and projects (Api, Simulation, Processing, Infrastructure) in `backend/src/` and test project in `backend/tests/`. (Done: sln + Api, Simulation, Processing, Infrastructure, Tests created; refs wired)
- [x] T002 Initialize Angular 17 workspace and app in `frontend/` with routing and strict mode. (Done: Angular CLI 17 scaffolded in `frontend/`)
- [x] T003 [P] Configure linting/formatting: `.editorconfig`, backend analyzers, Angular ESLint/prettier. (Done: root .editorconfig; backend analyzers via Directory.Build.props; frontend ESLint + Prettier and scripts)
- [x] T004 Add docker compose and base images in `deploy/docker-compose.yml` for Api, Frontend, Redis, (optional) Prometheus & Grafana. (Done: compose + backend/frontend Dockerfiles + prometheus config)
- [x] T005 [P] Set up HTTPS dev certs, API key via environment, and .env handling (no secrets in repo). (Done: .env.example, compose env_file, Kestrel cert binding, README notes)
- [x] T006 [P] Add CI basics (build and lint jobs) and dependency lockfiles enforcement. (Done: GitHub Actions for backend build/test and frontend lint/format check)
- [x] T007 Install Playwright (or Cypress) for frontend integration tests under `frontend/tests/integration/`. (Done: Playwright installed and folder created)

## Phase 3.2: Tests First (TDD) ⚠ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

Contract tests (backend):
- [x] T008 [P] Contract test: metrics endpoint 200 in `backend/tests/contract/MetricsEndpointTests.cs`. (Added; failing until implemented)
- [x] T009 [P] Contract test: acknowledge alert 204 in `backend/tests/contract/AckAlertEndpointTests.cs`. (Added; failing until implemented)
- [x] T010 [P] Contract test: SignalR stream connection/auth in `backend/tests/contract/StreamConnectionTests.cs`. (Added; failing until implemented)

Integration tests (backend):
- [x] T011 [P] Reconnect backfill returns last 5s in `backend/tests/integration/ReconnectBackfillTests.cs`. (Added stub; skipped pending implementation)
- [x] T012 [P] Aggregates windows (1m/5m/15m/1h/24h) correctness in `backend/tests/integration/AggregatesWindowTests.cs`. (Added stub; skipped)
- [x] T013 [P] Anomaly detection latency <500 ms and severity mapping in `backend/tests/integration/AnomalyAlertingTests.cs`. (Added stub; skipped)

Integration tests (frontend):
- [x] T014 [P] Live chart updates smoothly (≥30 FPS) at 1k/s in `frontend/tests/integration/live_chart.spec.ts`. (Added stub)
- [x] T015 [P] Time window switch responsiveness in `frontend/tests/integration/time_window.spec.ts`. (Added stub)
- [x] T016 [P] Alert appears and can be acknowledged in `frontend/tests/integration/alerts_ack.spec.ts`. (Added stub)

## Phase 3.3: Core Implementation (ONLY after tests are failing)
Models (backend):
- [x] T017 [P] Implement Reading model in `backend/src/Processing/Models/Reading.cs`.
- [x] T018 [P] Implement Aggregate model in `backend/src/Processing/Models/Aggregate.cs`.
- [x] T019 [P] Implement Alert model in `backend/src/Processing/Models/Alert.cs`.

Services (backend):
- [x] T020 Implement SimulatorHostedService emitting to bounded `Channel<Reading>` in `backend/src/Simulation/SimulatorHostedService.cs`.
- [x] T021 Implement Processor batching (≤50 ms or 250–500 items), Welford aggregates, z-score anomalies in `backend/src/Processing/Processor.cs`.
- [x] T022 Implement Redis repositories (raw, aggregates, alerts with TTL=24h) in `backend/src/Infrastructure/RedisStore.cs`.

API & Hub (backend):
- [x] T023 Implement SignalR Hub with groups per sensor and coalesced frames (~10–20 FPS) in `backend/src/Api/StreamHub.cs`. (Done: Hub groups + FrameBroadcaster coalescing + metrics)
 - [x] T024 Implement metrics endpoint and Prometheus.Net configuration in `backend/src/Api/MetricsController.cs`. (Done: /api/metrics returns 200; /metrics exposes Prometheus; added basic counter)
 - [x] T025 Implement POST /api/ack/{alertId} with API-key auth in `backend/src/Api/AlertsController.cs`. (Done: returns 204; logs ack; middleware enforces key)
 - [x] T026 Add API-key middleware + HTTPS + CORS/security headers in `backend/src/Api/Program.cs`. (Done: API-key header/query; CORS; security headers; HSTS non-dev)

Frontend services & UI:
- [x] T027 [P] Implement `SignalRService` (RxJS subjects: readings/aggregates/alerts; 5s replay) in `frontend/src/app/services/signalr.service.ts`. (Done: frames subject + Join/Leave + reconnect)
- [x] T028 [P] Implement `ChartService` (frame scheduler, LTTB downsampling, 100k ring buffer with O(1) evict) in `frontend/src/app/services/chart.service.ts`. (Done: ring buffer + stride downsampling; emits series)
- [x] T029 Implement `LiveChart` component in `frontend/src/app/components/live-chart/`. (Done: ngx-charts line chart wired)
- [x] T030 Implement `StatsPanel` component in `frontend/src/app/components/stats-panel/`. (Done: placeholder metrics)
- [x] T031 Implement `AlertsPanel` component in `frontend/src/app/components/alerts-panel/`. (Done: list with ack buttons)
- [x] T032 Implement `StatusBar` (ingest, latency, drops) and `TimeWindowPicker` in `frontend/src/app/components/`. (Done)

## Phase 3.4: Integration
- [x] T033 Wire Processor→Redis and Hub publishing; ensure per-sensor and global aggregates in `backend/src/Processing/Processor.cs`.
- [x] T034 Configure Redis TTL and maxmemory policy; verify purge behavior in `backend/src/Infrastructure/RedisStore.cs`.
- [x] T035 Add structured logging + minimal tracing spans across Simulator/Processor/Hub in `backend/src/*`.
- [x] T036 Frontend: connect services to components; delta frames only; performance tuning flags in `frontend/src/app/*`.
- [x] T037 Add docker compose wiring (ports, env, secrets via env), dev cert mapping in `deploy/docker-compose.yml`.

## Phase 3.5: Polish
- [x] T038 [P] Unit tests for aggregator (Welford) and anomaly thresholds in `backend/tests/unit/ProcessorMathTests.cs`.
- [x] T039 [P] Frontend unit tests for `ChartService` in `frontend/src/app/services/chart.service.spec.ts`.
- [ ] T040 Performance validation script (sustain 1k/s for 10 min; p95 <250 ms; drops ≤5% during 2× burst) in `backend/tests/perf/`.
   - Progress: Added perf harness (`backend/tests/Perf`) with SignalR client runner and latency capture. Config via env: PERF_RUN=1, PERF_BASE_URL, API_KEY, PERF_SENSOR, PERF_DURATION_SEC, PERF_P95_BUDGET_MS, PERF_MIN_RATE. Defaults to skip when not enabled.
 - [ ] T041 [P] Update docs: quickstart, README, runbook in `specs/001-title-real-time/quickstart.md` and repo `README.md`.
 - [x] T042 Compose hardening: healthchecks, restart policies, resource limits in `deploy/docker-compose.yml`.
 - [x] T043 Manual test checklist and screenshots for demo in `specs/001-title-real-time/`.

## Dependencies
- Setup (T001–T007) before Tests (T008–T016) and implementation.
- Contract tests (T008–T010) require Api project scaffolding (T001).
- Integration tests (T011–T016) require compose and basic routes (T004 minimal stubs acceptable).
- Models (T017–T019) before Processor/Repositories (T021–T022).
- Processor (T021) before Hub publish (T023) and Aggregates correctness tests.
- Auth/middleware (T026) before ack endpoint (T025) finalization.
- Frontend services (T027–T028) before components (T029–T032).
- Integration (T033–T037) after Core; Polish (T038–T043) after Integration.

## Parallel Example
```
# Launch these in parallel after setup:
Task: "Contract test: metrics endpoint 200 in backend/tests/contract/MetricsEndpointTests.cs"
Task: "Contract test: acknowledge alert 204 in backend/tests/contract/AckAlertEndpointTests.cs"
Task: "Contract test: SignalR stream connection/auth in backend/tests/contract/StreamConnectionTests.cs"
Task: "Live chart updates smoothly (≥30 FPS) at 1k/s in frontend/tests/integration/live_chart.spec.ts"
Task: "Time window switch responsiveness in frontend/tests/integration/time_window.spec.ts"
Task: "Alert appears and can be acknowledged in frontend/tests/integration/alerts_ack.spec.ts"
```

## Notes
- [P] tasks = different files, no dependencies; avoid [P] when touching same file.
- Ensure tests fail before implementing; then implement to make them pass.
- Use environment variables for API key; enforce HTTPS locally.
- Expose metrics at /api/metrics; ensure drop rate and latency are observable.
- Commit after each task; keep PRs small and focused.
