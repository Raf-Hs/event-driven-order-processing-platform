# ADR-0001: Architecture Style

- **Status:** Proposed — records a decision fixed by PLAN.md §10; pending architect/human acceptance
- **Date:** 2026-10-07
- **Deciders:** Architect (approves), human owner (approves PLAN-level decisions)
- **Phase / Issue:** Phase 0 / M0-01
- **Relates to:** PLAN.md §10 (Architecture), §34-1; ADR-0002

## Context

The platform must demonstrate production-grade distributed-systems engineering — asynchronous
consistency, failure isolation, messaging reliability — for a deliberately narrow MVP (order →
reservation → simulated payment → durable outcome). The architecture style determines how much of
that complexity is real versus simulated, and constrains every later phase.

## Decision

We will use a **small service-oriented architecture**: a **modular Commerce API** plus **two
narrowly scoped workers** (Inventory, Payment). Each deployable is internally a pragmatic
layered/vertical-slice application (host → application → domain → infrastructure). Each deployable
is an independent build and deployment unit and owns its relational database exclusively
(details in ADR-0002).

Explicitly **not** chosen at this time:

- One monolithic deployable for everything.
- "Service per domain noun" decomposition (separate Identity, Notification, Shipping, Pricing,
  Saga-engine services, etc.).
- A generic workflow engine, event-sourcing/CQRS infrastructure, or framework-building abstraction
  layers (PLAN.md §4, §10).

## Alternatives Considered

| Alternative | Summary | Why not |
|---|---|---|
| Monolith | Single deployable, internal modules only | Removes the genuine cross-service failure/consistency demonstrations (outbox/inbox, compensation, redelivery) that are the point of the project; would simulate rather than exercise distributed reliability. |
| Broad microservices (service per domain noun) | 6–10 services (identity, notification, shipping, pricing…) | Operational sprawl disproportionate to the MVP; PLAN.md explicitly excludes notification/shipping breadth; dilutes the meaning of a service boundary. |
| Splitting identity/saga out of Commerce | Independent Identity or Orchestrator service | Adds synchronous hops and networked-authentication complexity without a distinct ownership need; order saga state is inseparable from order state (PLAN.md §10). |

## Consequences

- (+) Three real failure boundaries demonstrate independent deployment, database-per-service,
  at-least-once messaging, and compensation honestly.
- (+) Commerce keeps identity + orders + saga cohesively: no synchronous inter-service hop inside
  request acceptance; the orchestrator persists progression transactionally.
- (−) Distributed reliability machinery (outbox, inbox, retries, DLQ, idempotency) is mandatory
  work, not optional polish; every workflow feature must account for redelivery and partial failure.
- (−) More moving parts than a monolith: local DX must be protected by Compose and docs
  (Phase 13).
- Follow-on: boundary details in ADR-0002; persistence in ADR-0003; broker in ADR-0004.
