# Architecture Decision Records (ADRs)

ADRs record non-obvious or irreversible technical decisions with their context, alternatives, and
consequences (PLAN.md §25, §34). They are written **before or while implementing** the decision.

## Rules

1. One decision per file: `NNNN-short-slug.md` (zero-padded number, kebab-case slug).
2. Copy `template.md` as the starting point; number the next ADR sequentially.
3. Statuses: `Proposed` → `Accepted` (by the architect, with human owner approval where required)
   → possibly `Superseded by ADR-NNNN`. `Rejected` is allowed for evaluated alternatives.
4. An ADR must not contradict `PLAN.md`; changing a plan-level decision requires explicit
   architect approval plus the ADR. ADR content that simply records a decision PLAN.md has already
   fixed inherits PLAN.md's authority.
5. Decisions are superseded, never silently rewritten: keep the old file, link the new one.
6. Documentation examples use synthetic data only; no secrets, ever.

## Planned inventory

The minimum ADR list (ADR-001 … ADR-019) is defined in PLAN.md §34. The index below is updated as
ADRs are added with their phases.

| ADR | Title | Status | Phase |
|---|---|---|---|
| [0001](0001-architecture-style.md) | Architecture Style | Proposed — pending approval | 0 |
| [0002](0002-service-boundaries-and-ownership.md) | Service Boundaries and Ownership | Proposed — pending approval | 0 |
| 0003 | SQL Server and Database-per-Service | Not started | 2 |
| 0004 | RabbitMQ vs Azure Service Bus | Not started | 5 |
| 0005 | Integration Contracts and Versioning | Not started | 1 |
| 0006 | Transactional Outbox | Not started | 6 |
| 0007 | Inbox and Consumer Transactions | Not started | 7 |
| 0008 | Idempotency Strategy | Not started | 4 |
| 0009 | Order Saga and Compensation | Not started | 10 |
| 0010 | Authentication | Not started | 3 |
| 0011 | Authorization | Not started | 3 |
| 0012 | Inventory Concurrency | Not started | 8 |
| 0013 | Payment Simulation Boundary | Not started | 9 |
| 0014 | Observability | Not started | 11 |
| 0015 | Containerization and Local Stack | Not started | 13 |
| 0016 | Azure Deployment | Not started | 15 |
| 0017 | API Versioning and Error Contract | Not started | 4 |
| 0018 | Stored Procedure Use | Not started | 8 |
| 0019 | Data Retention and Replay | Not started | 11 |
