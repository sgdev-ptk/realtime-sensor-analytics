
# Implementation Plan: Real-Time Sensor Analytics Dashboard — Technical Plan

**Branch**: `001-title-real-time` | **Date**: 2025-10-05 | **Spec**: `specs/001-title-real-time/spec.md`
**Input**: Feature specification from `/specs/001-title-real-time/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code, or `AGENTS.md` for all other agents).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Deliver an end-to-end real-time sensor analytics PoC with: 1k/s ingest, Angular dashboard, rolling aggregates and anomaly alerts, 24h retention with TTL, and strict observability and performance gates. Architecture: .NET 8 Web API with a HostedService simulator and processing pipeline (bounded channels, Welford aggregates, anomaly detection), SignalR for push (coalesced ~10–20 FPS), Redis for 24h retention and aggregate storage, Angular 17 + RxJS frontend with LTTB downsampling and ring buffers. Docker Compose orchestrates API, Redis, frontend, and optional Prometheus/Grafana.

## Technical Context
**Language/Version**: Backend .NET 8; Frontend Angular 17; Node 20 for tooling  
**Primary Dependencies**: SignalR, RxJS, Redis (StackExchange.Redis), Prometheus.Net, Grafana (optional)  
**Storage**: Redis (TTL=24h) for raw readings (lists/streams) and aggregates (hashes)  
**Testing**: xUnit for .NET; Jest (or Karma) for Angular component/service tests  
**Target Platform**: Dockerized services via docker compose on developer workstation  
**Project Type**: web (frontend + backend)  
**Performance Goals**: Ingest 1k/s; UI ≥30 FPS; E2E median ingest→paint <200 ms; P95 ingest→UI <200 ms; drops ≤5% during 2× burst  
**Constraints**: Bounded queues; coalesced frames (~10–20 FPS); ring buffer cap (100k points); HTTPS locally; API-key  
**Scale/Scope**: Sensor count N configurable; best-effort target up to ~2,000 sensors (assumption for sizing UI downsampling)

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Security-by-Default: PASS — API-key auth; no secrets in repo; local HTTPS enforced via dev cert.

Resilient Streaming: PASS — Bounded channels, coalesced frames, backpressure; explicit drop metrics and policies.

Observability First: PASS — Metrics: ingest rate, queue depth, processing latency, fan-out latency, drop rate; logs and minimal tracing; optional Prometheus/Grafana.

Simplicity over Novelty: PASS — Redis TTL for retention, ring buffers for memory caps, single compose file for local.

Deterministic Builds: PASS — Lock dependencies; pinned base images; reproducible Docker builds.

Performance & Quality Standards: PASS (planned) — Median <200 ms; P95 <200 ms; UI ≥30 FPS; 24h TTL purge verified; acceptance tests defined below and in research/quickstart.

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->
```
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
```
backend/
├── src/
│   ├── Simulation/           # SimulatorHostedService; producer logic
│   ├── Processing/           # Processor (batching, aggregates, anomalies)
│   ├── Api/                  # Web API controllers, SignalR Hub
│   └── Infrastructure/       # Redis, metrics, auth, config
└── tests/                    # xUnit tests (contract/integration/unit)

frontend/
├── src/
│   ├── app/
│   │   ├── components/       # LiveChart, StatsPanel, AlertsPanel, StatusBar, TimeWindowPicker
│   │   └── services/         # SignalRService, ChartService
│   └── environments/
└── tests/

deploy/
└── docker-compose.yml
```

**Structure Decision**: Web application split into `backend/`, `frontend/`, and `deploy/` for local orchestration.

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [ ] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved (assumptions documented in research.md)
- [ ] Complexity deviations documented

---
*Based on Constitution v1.0.0 - See `/memory/constitution.md`*
