<!--
Sync Impact Report
Version change: N/A → 1.0.0
Modified principles: (initial adoption)
	- Security-by-Default
	- Resilient Streaming
	- Observability First
	- Simplicity over Novelty
	- Deterministic Builds
Added sections:
	- Performance & Quality Standards
	- Development Workflow & Quality Gates
Removed sections: None
Templates status:
	- .specify/templates/plan-template.md — ⚠ pending (footer references Constitution v2.1.1; consider updating to v1.0.0 or removing hard-coded version)
	- .specify/templates/spec-template.md — ✅ aligned (no changes needed)
	- .specify/templates/tasks-template.md — ✅ aligned (performance gate present)
	- .github/prompts/* — ✅ aligned
Follow-ups:
	- Optional: Document anomaly detection approach (simple threshold vs z-score) in research.md during /plan
-->

# Real-Time Sensor Analytics Dashboard — PoC Constitution

## Core Principles

### I. Security-by-Default (NON-NEGOTIABLE)
- No secrets in the repository. Use environment variables or a local secret store.
- Enforce HTTPS even locally (self-signed/dev cert acceptable for PoC).
- All API access requires a simple API key for this PoC; key rotation documented.
- Apply least-privilege defaults (CORS, headers, process/user perms) and safe defaults.

### II. Resilient Streaming
- Use bounded queues and backpressure; never allow unbounded growth.
- Coalesce frames/updates under pressure and degrade gracefully (drop/decimate visuals before failing).
- Define explicit drop policies and measure drop rate; target 0% drop under normal load.
- Ensure failure containment: one slow consumer must not stall the pipeline.

### III. Observability First
- Instrument ingest rate, drop rate, end-to-end ingest→paint latency, and queue depth from day one.
- Emit structured logs with correlation IDs; add minimal tracing spans around ingest, process, publish, paint.
- Provide a minimal dashboard/panel to visualize key metrics for the demo.
- SLOs are codified and validated as part of acceptance (see Performance & Quality Standards).

### IV. Simplicity over Novelty
- Prefer simple, well-understood building blocks: Redis TTL for retention, ring buffers for in-memory caps.
- Avoid premature distribution and complex patterns unless justified by measured need.
- Minimize surface area and configuration; one-command local up, clear teardown.

### V. Deterministic Builds
- Lock all dependencies; pin container base images and toolchains.
- Reproducible Docker images; build scripts must be hermetic and idempotent.
- Provide a single, documented local run path (Docker Compose) that consistently works.

## Performance & Quality Standards

### Mission
Deliver a production-credible PoC that demonstrates end-to-end real-time analytics for streaming sensor data.

### Must-Haves (measurable)
- Ingest simulation at ≥1,000 readings/second with configurable sensor count.
- Angular dashboard achieves <200 ms median ingest→paint latency under normal load.
- Live chart shows rolling aggregates: min, max, mean, stdev, p95, plus anomaly alerts.
- Frontend safely holds ≥100,000 points in memory without FPS degradation (≥30 FPS sustained).
- 24-hour data retention with automatic purge (e.g., Redis TTL or equivalent TTL mechanism).
- Clean, auditable architecture with a reproducible local Docker run.

### Scope Boundaries (Out of Scope)
- Multi-tenant auth/RBAC, long-term historical analytics, HA/DR, billing.

### Technical Constraints
- Frontend framework: Angular for this PoC dashboard.
- Local environment: Docker Compose based; HTTPS termination locally; API-key auth.

### Acceptance & Validation
- Stable live chart at 1k/s with responsive UI.
- Correct aggregates and timely anomaly alerts demonstrated.
- Automatic TTL purge verified within demo (e.g., via keys expiring and data aging tests).
- Observability dashboard/panel shows ingest rate, drop rate, end-to-end latency, queue depth.

## Development Workflow & Quality Gates

### Workflow
- Use the structured flow: `/specify` → `/clarify` → `/plan` → `/tasks` → `/analyze` → `/implement`.
- Tests-first for contracts/integration where applicable; performance validation tasks are mandatory before acceptance.
- Keep architecture clean and auditable; document decisions in `research.md` during `/plan`.

### Quality Gates (must pass)
1. Security gate: no secrets in repo; API-key required; HTTPS enabled locally.
2. Observability gate: metrics/logs/traces for ingest, drop, latency, queue depth are present and visible.
3. Performance gate: median ingest→paint <200 ms under normal load; demo evidences sustained 1k/s.
4. Determinism gate: dependency lockfiles present; Docker images reproducibly build locally.
5. Retention gate: 24h TTL enforced and verified (automatic purge).

## Governance

- This Constitution supersedes other practices for the PoC; deviations require explicit, documented justification.
- Amendments require: (a) change proposal summary, (b) rationale and impact, (c) version bump per SemVer below.
- Versioning of the Constitution uses Semantic Versioning:
	- MAJOR: Backward-incompatible rule changes/removals.
	- MINOR: New principle/section additions or material expansions.
	- PATCH: Clarifications and non-semantic edits.
- Compliance: All PRs/reviews must verify gates and principles. CI should surface violations early (lint, build, minimal checks).

**Version**: 1.0.0 | **Ratified**: 2025-10-05 | **Last Amended**: 2025-10-05