# Project Context — Event-Driven Order Processing Platform

Shared context for every agent in this repository. Read this file at the start of every task. It summarizes stable facts; `PLAN.md` remains the full source of truth and `docs/current-state.md` is the live progress tracker.

## What this project is

A production-oriented, portfolio-grade order processing platform demonstrating backend engineering, distributed systems, event-driven architecture, messaging reliability, security, observability, testing, and Azure deployment. A customer submits an order; the platform reserves stock, authorizes a simulated payment, and reports a durable order outcome — correctly, even when services, databases, or the broker fail or deliver messages more than once.

This is deliberately NOT a broad commerce product: no real payments/card data, no shipping, no marketplace, no AI-centric feature, no UI polish beyond a thin Angular demo client.

## Architecture snapshot (fixed by PLAN.md)

- **Three backend deployables:**
  - **Commerce API** — identity (ASP.NET Core Identity + JWT + rotating refresh tokens), sellable product snapshots, orders aggregate, HTTP idempotency, and the **persisted order saga (orchestrator)**. Owns **CommerceDb**.
  - **Inventory Worker** — stock and reservations; atomic all-or-none reserve; no oversell. Owns **InventoryDb**.
  - **Payment Worker** — simulated, deterministic, idempotent payment authorization; no card data ever. Owns **PaymentDb**.
- **Database-per-service:** three SQL Server databases (locally one SQL container is acceptable; Azure may use one logical server — documented cost compromise). No cross-database access, joins, or foreign keys. EF Core owns persistence and per-service migrations.
- **Communication:** Angular → Commerce REST/HTTPS only. All inter-service workflow is **asynchronous** via broker commands/events (`ReserveInventory`, `InventoryReserved`/`InventoryRejected`, `AuthorizePayment`, `PaymentAuthorized`/`PaymentDeclined`, `ReleaseInventory`, `InventoryReservationReleased`, `OrderConfirmed`, `OrderFailed`). No synchronous service-to-service calls in the order workflow.
- **Broker:** **RabbitMQ locally, Azure Service Bus in Azure**, behind a narrow transport adapter. Broker duplicate-detection features are optimizations only — application inbox/idempotency is authoritative.
- **Reliability model (non-negotiable):**
  - **Transactional outbox** in every publishing service: state change + outgoing message in ONE local SQL transaction; background dispatcher publishes with broker confirms; duplicates on crash are expected and safe.
  - **Transactional inbox** in every consumer: `(ConsumerName, MessageId)` unique; claim + business change + outbox in ONE transaction; broker ack strictly AFTER commit.
  - **At-least-once delivery** + idempotent consumers + business operation keys (order/attempt). No exactly-once claims, ever.
  - Bounded retries with backoff/jitter; business rejection is a durable result event, not an exception loop; poison messages → DLQ with alerts.
  - Compensation is a new idempotent command (e.g., `ReleaseInventory`), never rollback of committed work; payment timeout ≠ decline — reconcile before compensating.
- **Order lifecycle (MVP):** `PendingInventory → PendingPayment → Confirmed`, with `Failed` from either stage (and compensation). `Cancelled`, `Shipped`, `Completed` are future scope. Order submission returns `202 Accepted`; acceptance ≠ completion.
- **Azure:** Container Apps (never Kubernetes), Azure SQL, Service Bus, ACR, Key Vault (only where managed identity can't avoid a secret), Monitor/Application Insights via OpenTelemetry. Single region. Azure Storage and Azure Functions are not MVP dependencies.
- **Frontend:** thin Angular + TypeScript demo client (login, order submit, order list/detail/status). No business authority in the browser; access token in memory; refresh token in HttpOnly secure cookie with CSRF protection.

## What this project is intentionally NOT

Kubernetes-based · event-sourced · CQRS-infrastructure-heavy · a generic workflow engine · multi-region · service-per-domain-object · a CRUD demo · a frontend showcase · an AI project.

## Agent roster and orchestration

| Agent | Mode | Model | Role |
|---|---|---|---|
| `architect` | primary | opencode-go/gpt-5.6-luna | Only orchestrator. Determines phase, writes task briefs with acceptance criteria, delegates, gates phase completion, owns `docs/current-state.md`. |
| `implementer` | subagent | opencode-go/qwen3.8-flash | Default coding worker: C#/.NET/ASP.NET Core, domain, EF Core, messaging, outbox/inbox, saga, tests. |
| `reviewer` | subagent | opencode-go/qwen3.8-flash | Independent READ-ONLY reviewer. Severity-classified findings (CRITICAL/HIGH/MEDIUM/LOW) and PASS/FAIL verdict. |
| `infra` | subagent | opencode-go/qwen3.8-flash | Docker/Compose, SQL/RabbitMQ local infra, GitHub Actions, Bicep/Azure, config, health checks, telemetry plumbing. No application code. |
| `escalation` | subagent | opencode-go/qwen3.8-max | Hard distributed-systems/concurrency/security problems only. Root-cause-first; smallest plan-consistent fix. |

Flow (controlled exclusively by the architect; subagent depth is 1 — workers cannot spawn workers):

```text
ARCHITECT → IMPLEMENTER → REVIEWER ─PASS→ ARCHITECT
                             │FAIL (max 3 loops, then architect intervenes)
                             └────→ IMPLEMENTER → REVIEWER

Hard problem flagged → ESCALATION (root cause + minimal fix) → ARCHITECT → IMPLEMENTER

ARCHITECT → INFRA → REVIEWER → ARCHITECT
```

Only one worker modifies a given feature at a time. Every implementation and infrastructure change is reviewed.

## Sources of truth and precedence

1. **`PLAN.md`** — architecture, boundaries, data ownership, messaging, reliability, security, testing, infra, CI/CD, Azure, phases, acceptance criteria. Agents may not contradict it; changes require the architect + an ADR.
2. **`docs/current-state.md`** — live tracker: current phase, current task, completed work, active decisions, constraints, blockers, next task. Updated by the architect after every completed task/phase.
3. **The repository itself** — when docs and code disagree, report the discrepancy; never silently trust either.
4. **ADRs** (`docs/adr/`, once created) — recorded decisions and their rationale.

## Phase model (from PLAN.md)

Phase 0 Repository/Architecture Foundation → 1 Domain & Contracts → 2 SQL Ownership & Persistence → 3 Identity/AuthN/AuthZ → 4 Order Acceptance API → 5 Messaging Adapters & Local Broker → 6 Outbox → 7 Inbox & Consumer Foundation → 8 Inventory Reservation → 9 Payment Simulator → 10 Order Saga & Compensation → 11 Observability & Operational Recovery → 12 Test Completion & Contract Hardening → 13 Docker & Local DX → 14 CI Quality & Security Gates → 15 Azure Infrastructure & Deployment → 16 Angular Demo Client → 17 Documentation, Portfolio & Hardening.

Milestones: **M0** Foundation · **M1** Secure Order Acceptance · **M2** Reliable Workflow · **M3** Observable Local Release Candidate · **M4** Azure Portfolio Release.

The authoritative current position is always `docs/current-state.md` — check it before assuming any phase.

## Standing constraints for all agents

- Never cross service or database ownership boundaries; never add synchronous workflow calls between services.
- Never weaken the outbox/inbox/idempotency/at-least-once model; never claim exactly-once semantics.
- No secrets in code, config, images, workflows, logs, or docs; local secrets via env/user-secrets, Azure via Key Vault/managed identity, CI via GitHub OIDC.
- No future-phase functionality ahead of its phase; no scope creep; prefer the simple plan-consistent solution.
- Money = integer minor units + currency; UTC timestamps; async I/O with cancellation tokens.
- Tests are part of every task; deterministic (no sleep-based timing); no EF InMemory as relational proof; failure windows are tested, not assumed.
- A task is done only per PLAN.md's Definition of Done: implementation + tests + security + observability + docs + CI green + reviewer PASS.
