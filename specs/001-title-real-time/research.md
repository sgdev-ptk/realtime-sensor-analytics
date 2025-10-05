# Research: Real-Time Sensor Analytics Dashboard — Decisions & Rationale

**Date**: 2025-10-05  
**Context**: This PoC follows Constitution v1.0.0 gates (security, resilience, observability, simplicity, determinism) and targets 1k/s ingest, <200 ms median ingest→paint, UI ≥30 FPS, and 24h TTL.

## Resolved Unknowns (best‑guess per product guidance)

1) Reconnect backfill duration  
Decision: Backfill last 5 seconds on reconnect.  
Rationale: Smoothly heals brief gaps without overfetching; 5s is typical for live dashboards.  
Alternatives: 2s (risk residual gaps), 10s (more bandwidth/UI churn).

2) Expected max sensor count N  
Decision: Size UI and downsampling for up to ~2,000 sensors.  
Rationale: Provides headroom for demo scenarios while keeping UI performant.  
Alternatives: 500 (too conservative), 5,000 (likely requires heavier decimation/WebGL).

3) Late data acceptance window  
Decision: Accept late/out‑of‑order data up to 2 seconds; beyond that, treat as late and exclude from real‑time aggregates.  
Rationale: Keeps aggregates stable; small window tolerates jitter.  
Alternatives: 0s (too strict), 5s (more complex buffering, higher latency variance).

4) Default anomaly method and k  
Decision: Default to Z‑score with |z| ≥ 3; expose EWMA±kσ as an option (k default = 3).  
Rationale: Z‑score is simple, transparent, and aligns with PoC simplicity.  
Alternatives: EWMA only (harder tuning), hybrid ensemble (overkill for PoC).

5) Alert severity scale and acknowledgements  
Decision: Severity levels = info, warn, critical. Acknowledgements are persisted for the TTL window.  
Rationale: Three‑level scale is sufficient; persisted acks aid demos and observability of operator actions.  
Alternatives: More levels (adds noise), non‑persisted acks (lose auditability).

6) Client memory budget and performance thresholds  
Decision: Budget ~150 MB for chart data; p95 ingest→paint target < 250 ms; drops ≤ 5% during 2× burst.  
Rationale: 100k points comfortably within budget on modern machines; p95 target complements median gate.  
Alternatives: Tighter p95 (<200 ms) may be achievable; adjust after measurements.

## Best Practices Notes
- Coalescing: Send frames at 10–20 FPS; include only deltas to minimize DOM updates.
- Downsampling: Use LTTB to ~500–2,000 points per frame; cap ring buffer at 100k with O(1) evict.
- Bounded queues: Channel capacity sized to ~250–500 items; batch window ≤50 ms.
- Metrics: instrument ingest rate, processing latency, fan‑out latency, queue depth, drop rate; expose /metrics for Prometheus.
- Security: Local HTTPS, API key via env var; no secrets in repo.
- Determinism: Pin base images and lockfiles; one‑command `docker compose up` bring‑up.

## Open Questions (non‑blocking)
- Exact Redis structures (lists vs streams) for raw readings may change after spike; acceptance unaffected.
- Optional Prometheus/Grafana: include in compose by default with ability to disable.

---
**Status**: All critical NEEDS CLARIFICATION resolved for planning. Proceed to Phase 1 design.
