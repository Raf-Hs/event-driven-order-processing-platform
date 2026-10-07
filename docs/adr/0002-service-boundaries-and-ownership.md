# ADR-0002: Service Boundaries and Ownership

- **Status:** Proposed — records boundaries fixed by PLAN.md §10–§11; pending architect/human acceptance
- **Date:** 2026-10-07
- **Deciders:** Architect (approves), human owner (approves PLAN-level decisions)
- **Phase / Issue:** Phase 0 / M0-01
- **Relates to:** PLAN.md §10 (Components/Responsibilities/Ownership), §11 (Data Architecture),
  §15 (Messaging), §34-2; ADR-0001

## Context

ADR-0001 chose three backend deployables. The exact responsibilities, data ownership, and
integration contracts of each boundary decide whether the system can keep its reliability
guarantees (at-least-once, outbox/inbox, compensation) as features are added in later phases.
Ambiguous boundaries make cheap, wrong coupling possible later.

## Decision

We will fix these boundaries for the MVP:

- **Commerce API** owns **CommerceDb**: ASP.NET Core Identity users and refresh sessions, sellable
  product/price snapshots, the orders aggregate (`Orders`, `OrderItems`, `OrderStatusHistory`),
  saga/attempts state, HTTP idempotency records, and its own inbox/outbox tables. It exposes the
  only public REST API, runs the order saga **orchestrator**, and is the only service clients may
  call.
- **Inventory Worker** owns **InventoryDb**: SKU stock counters (`OnHand`/`Reserved` with
  `0 <= Reserved <= OnHand`), reservations and lines, and its own inbox/outbox tables. It performs
  atomic all-or-none reservations with no oversell and emits reservation outcome events. It has no
  public HTTP surface in the MVP.
- **Payment Worker** owns **PaymentDb**: simulated authorization operations keyed by a unique
  operation key, plus its inbox/outbox. **No card data exists anywhere in the system.** It has no
  public HTTP surface in the MVP.
- Each service owns its database exclusively: **no shared tables, no cross-database access, joins,
  foreign keys, or transactions**; per-service EF Core contexts and migrations; least-privilege
  credentials per database.
- Integration happens **only** through versioned commands/events on the broker (PLAN.md §15
  topology); the **only synchronous path in the order workflow** is Angular → Commerce HTTPS.
- `src/BuildingBlocks` (Messaging.Abstractions, Messaging.Contracts, Observability) may contain
  only cross-cutting transport/telemetry primitives and versioned contracts — **never** domain
  entities, EF mappings, or business logic.

## Alternatives Considered

| Alternative | Summary | Why not |
|---|---|---|
| Shared database ("database for the whole app") | One schema with a service column or schemas-per-service | Removes the genuine consistency boundary; cross-service joins tempt synchronous-style coupling; one bad migration breaks every service. |
| Shared domain/business-logic library | Common domain assembly used by all three | Silent coupling of release trains; domain logic must live in exactly one owner (PLAN.md: no shared business logic). |
| More services (identity, notification, saga engine…) | Finer decomposition | No distinct data-ownership or scaling need in the MVP; violates PLAN.md §4 non-goals; adds cost without demonstrable reliability value. |
| Point-to-point queues per message pair (no topics) | Direct command queues both ways, events duplicated per consumer | Event fan-out (e.g. `OrderConfirmed` to future Phase 2 consumers) would require contract churn; a durable event topic keeps producers unaware of subscribers. |

## Consequences

- (+) Ownership is mechanically checkable: project references in `src/` mirror the boundary, so a
  reviewer (and, from Phase 14, CI dependency checks) can reject cross-service coupling by
  construction.
- (+) Each database can be failed, restored, and migrated independently, matching the failure
  matrix (PLAN.md §35).
- (−) Queries that span services (e.g. order list with live stock) must go through messages or
  locally stored snapshots — never joins; product data is therefore a **snapshot** owned by
  Commerce.
- (−) Three migrations, three deployment pipelines, and three sets of credentials to operate;
  documented in later phases (2, 13–15).
- Local convenience (one SQL container with three DBs) must not blur the rule: it is logical
  isolation only, and the limitation will be documented (PLAN.md §11, ADR-0003).
