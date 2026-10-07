# Event-Driven Order Processing Platform — Project Plan

> **Status:** Planning baseline; no implementation has started.  
> **Purpose:** This document is the source of truth for scope, architecture, delivery, and acceptance. When implementation details change, update this plan or record the decision in an ADR before treating the change as intentional.

## 1. Project Overview

The Event-Driven Order Processing Platform is a production-oriented demonstration of how a small commerce workflow can remain correct when independent services, databases, and message brokers fail or deliver messages more than once. A customer submits an order; the platform reserves stock, authorizes a simulated payment, and reports a durable order outcome. The workflow spans independently owned data stores and uses asynchronous commands/events, transactional outboxes, consumer inboxes, idempotency, retries, and a persisted process manager (saga).

This is not a general commerce product. It intentionally models one bounded workflow deeply rather than implementing a broad catalog, real payment integration, shipping network, or polished storefront. The portfolio emphasis is backend engineering, distributed-systems reasoning, security, operational behavior, and documented trade-offs.

## 2. Problem Statement

A single HTTP request cannot safely coordinate an order database, inventory database, payment provider, and broker as one ACID transaction. A process can fail after a database commit but before a message is published; a broker can redeliver; two customers can race for the last unit; a payment response can be delayed; and a retry can repeat a side effect. The platform must make these failure modes explicit and recoverable while presenting clients with understandable order states.

## 3. Goals

- Demonstrate a complete, observable order workflow across service and database boundaries.
- Preserve local transactional consistency with SQL Server and EF Core; use asynchronous messaging for cross-boundary work.
- Demonstrate at-least-once delivery with idempotent producers/consumers, durable outbox/inbox records, and a persisted saga.
- Prevent inventory overselling and duplicate order/payment effects under concurrent requests and message redelivery.
- Provide secure, documented REST APIs and a deliberately thin Angular demonstration client.
- Support local execution using Docker Compose, SQL Server, and RabbitMQ, and a production-like Azure deployment path.
- Make reliability behavior testable and explainable through automated tests, telemetry, failure exercises, and ADRs.
- Keep the MVP feasible for one developer by excluding real-world breadth that does not strengthen the core learning objectives.

## 4. Non-Goals

- A marketplace, multi-merchant system, pricing/promotion engine, tax engine, or full product-catalog platform.
- Real card processing or storage of cardholder data (PCI scope); payment is a deterministic simulator behind an adapter.
- Shipment carrier integrations, warehouse optimization, refunds/chargebacks, or returns in MVP.
- Exactly-once message delivery, distributed transactions/2PC, or a claim of global ACID consistency.
- Kubernetes, service mesh, event sourcing, CQRS infrastructure, multi-region active-active, or a generic workflow engine.
- A microservice per entity, shared database tables across services, or synchronous service-to-service calls for the core workflow.
- AI as a central capability, a sophisticated storefront, or UI polish beyond operational demonstration.
- Production certification/compliance claims. The project demonstrates security practices but is not certified for regulated payment data.

## 5. Scope and Release Boundaries

### In Scope

- Customer registration/login and JWT-based API access, role/policy authorization, refresh-token rotation, and secure credential storage.
- Product/SKU and available inventory seeded or managed through a restricted operator endpoint; order-line price is snapshotted at submission.
- Order creation with validation, request idempotency, durable order state, status/history, cancellation only where the workflow permits it, and pagination.
- Inventory reservation with atomic stock protection, reservation expiry/release behavior, and idempotent command handling.
- Simulated payment authorization with explicit success/failure outcomes, a unique payment operation per order attempt, and idempotent handling.
- Orchestrated asynchronous order workflow, versioned integration messages, outbox/inbox, retries, dead-letter handling, and operational recovery guidance.
- Structured logs, metrics, distributed traces, health checks, OpenAPI, automated tests, Docker, CI, and a documented Azure deployment.
- Minimal Angular client for login, order submission, order list/details, and status.

### Out of Scope for MVP

- Real email/SMS, real payment provider, shipping, refunds, customer profile management beyond authentication, product search, and advanced inventory replenishment.
- Notification Service as a separately deployed service; MVP exposes order status through API and emits events suitable for later notification consumers.
- Full operator dashboard, tenant isolation, user self-service account recovery, social login, MFA, and delegated administration.
- Multi-region failover, autoscaling policy tuning, private networking as a hard MVP prerequisite, and formal SLO error-budget automation.

### MVP

The MVP consists of **three independently deployable backend applications**: Commerce API (including identity and order orchestration), Inventory Worker, and Payment Worker. They each own a separate SQL Server database and communicate asynchronously. Local development uses RabbitMQ; Azure deployment uses Azure Service Bus. A local demo payment adapter is deterministic and never accepts or persists card data. The MVP includes end-to-end order submission through inventory reservation and payment authorization, secure customer/operator API access, observability, tests, Docker Compose, CI, and one documented Azure environment. It does not include shipment, a notification worker, real payment, or a full admin UI.

### Phase 2

- Add a Notification Worker consuming `OrderConfirmed` / `OrderFailed`, using a local email sink or provider adapter, with delivery idempotency.
- Add explicit order cancellation and compensation flows after confirmation, payment void simulation, and reservation expiry reconciliation.
- Add richer operator tools for replay/redrive with authorization, audit records, and safe DLQ inspection.
- Add production-oriented dashboards/alerts, load test baseline, and environment promotion approvals.

### Phase 3

- Add shipment aggregate/service only if a concrete workflow and learning objective justify the boundary.
- Add real payment sandbox integration (never card storage), out-of-process secrets, and webhook signature/replay protection.
- Add blue/green or revision-based deployment, private endpoints, disaster-recovery exercises, and measured autoscaling.
- Consider multi-tenant support, catalog/pricing ownership, and event schema registry only after requirements exist.

### Optional Advanced Features

Contract compatibility checks, chaos/fault-injection test suite, SQL query-plan regression artifacts, transactional partitioning strategy, Azure Service Bus sessions for per-order ordering where justified, and a small read-optimized projection. These are not MVP gates.

## 6. Functional Requirements

1. A customer can authenticate, submit a non-empty order for active SKUs, and retrieve only their own orders.
2. An accepted submission returns `202 Accepted` and an order resource/status URL; it does not claim stock/payment completion before the asynchronous workflow finishes.
3. Repeating the same submission with the same customer and idempotency key returns the original result; reusing the key with a different canonical request returns `409 Conflict`.
4. The platform snapshots SKU, description, unit price, and quantity into immutable order lines at submission. Clients cannot set authoritative totals or order status.
5. The workflow attempts an inventory reservation, then payment authorization. Success confirms the order; stock rejection or payment decline leads to a durable failure outcome and applicable compensation.
6. A customer can list and inspect their own orders; an operator can inspect all orders and perform explicitly permitted operational actions.
7. Consumers safely handle duplicate messages and retry transient faults. Permanent/poison messages reach a dead-letter destination with diagnostics.
8. Operators can identify a workflow by order ID, correlation ID, and message ID using logs, metrics, and traces.
9. OpenAPI describes authentication, request/response schemas, expected asynchronous behavior, status codes, and Problem Details responses.

## 7. Non-Functional Requirements (NFRs)

Targets apply to the documented demo environment and are measurement goals, not guarantees for every hosting tier.

| Attribute | MVP target / definition |
|---|---|
| API responsiveness | Order submission p95 under 500 ms and p99 under 1.5 s at 25 requests/sec in the defined single-region test profile, excluding broker/database outage periods. |
| Workflow latency | Healthy-environment order outcome p95 under 10 seconds at 10 orders/sec; eventual completion is not promised during dependency outages. |
| Correctness | No oversell in concurrency tests; same idempotency key produces one order; duplicate message delivery produces no duplicate business effect. |
| Availability | Design target 99.5% monthly for API in one Azure region; broker/database outages may delay asynchronous completion while accepted durable work remains recoverable. |
| Durability | Accepted order and its initial outbox command commit in one database transaction. No success response until that transaction commits. |
| Security | HTTPS outside local development; least-privilege service identities; no plaintext passwords/tokens/secrets in logs or source; automated dependency/security checks in CI. |
| Scalability | API and worker instances can scale horizontally without relying on in-memory workflow/deduplication state. Inventory correctness remains database-enforced. |
| Observability | Trace/correlation continuity across HTTP, outbox publishing, broker, and consumer; actionable metrics for latency, backlog, retries, and DLQ. |
| Maintainability | Clear ownership, migration discipline, documented contracts/ADRs, automated tests, and code review for changes to domain or event contracts. |
| Deployability | Reproducible local Compose startup and scripted Azure deployment; configuration/secrets external to images. |

## 8. Domain Model

### Domain Boundary and Concepts

- **Customer/User:** authenticated principal in Commerce. Identity data is not copied to Inventory or Payment; messages carry stable customer/order identifiers only when required.
- **Product/SKU:** MVP uses a small Commerce-owned sellable product snapshot and Inventory-owned stock record keyed by SKU. Commerce owns sellable name/price; Inventory owns physical/available quantity. No cross-database foreign keys.
- **Order (aggregate root):** Commerce-owned, identified by `OrderId`; contains customer ID, immutable line snapshots, total, state, workflow version, timestamps, and optimistic concurrency token.
- **OrderLine (entity/value-like child):** SKU, display name snapshot, quantity, unit amount/currency. A line cannot change after submission in MVP.
- **OrderAttempt / Workflow state:** Commerce-owned persisted orchestration state: current step, attempt number, outstanding operation, deadlines, and last failure classification. Prefer fields/records related to order rather than a generic workflow engine.
- **InventoryItem:** Inventory-owned SKU, on-hand quantity, reserved quantity, row version. Available quantity is derived as on-hand minus active reservations, not independently mutable.
- **InventoryReservation:** Inventory-owned reservation ID, order ID, lines, status, expiration, and unique operation key. One active reservation per order attempt.
- **PaymentAuthorization:** Payment-owned record for a simulated authorization, amount/currency, result, operation key, and provider reference (simulated). No PAN/CVV or payment credentials.
- **OutboxMessage / InboxMessage:** infrastructure records owned by each database/service, not shared domain entities.
- **Shipment and Notification:** intentionally not first-version domain aggregates. Later consumers may act on order integration events without changing the core order boundary.

### Aggregates and Invariants

- `Order` is the Commerce aggregate root. A submission creates the order and initial `ReserveInventory` command in the outbox atomically. The aggregate owns allowed state transitions and immutable lines.
- Inventory uses an `InventoryItem`/reservation transaction boundary. Reservations are recorded per order and stock counters update atomically; an order-level reservation operation is idempotent.
- Payment authorization is a Payment-owned idempotent operation, unique by `OrderId + Attempt` (and provider operation key).
- No aggregate spans databases. Cross-service invariants are eventually coordinated by the order saga and compensating commands.
- Order total equals the sum of snapshotted line quantity × unit amount, calculated using decimal minor-unit semantics and one currency per order. Quantity is positive and bounded; SKU must be active and purchasable at submission.
- A customer may read only their orders; only the service may change workflow state. An order may not become confirmed until both reservation and payment authorization have succeeded.
- Stock on hand and active reservations never permit available stock below zero. Releasing a reservation is idempotent.
- One payment authorization result is recorded per operation key. A retry must not create a second authorization effect.

### Value Objects

`Money` (amount in integer minor units plus ISO currency), `OrderLineSnapshot`, `OrderId`, `CustomerId`, `Sku`, and `IdempotencyKey`. Value objects validate at construction/application boundaries. Use decimal or integer minor units consistently; never use binary floating point for money.

### Domain Events vs Integration Messages

Domain events are internal facts used to express a local state change. Integration events are stable, versioned contracts placed in the outbox and published to other services. A domain event may result in an integration event, but internal CLR types are not automatically exposed as broker contracts. MVP integration messages include `ReserveInventory`, `InventoryReserved`, `InventoryRejected`, `AuthorizePayment`, `PaymentAuthorized`, `PaymentDeclined`, `ReleaseInventory`, `InventoryReservationReleased`, `OrderConfirmed`, and `OrderFailed`.

## 9. Order Lifecycle and State Machine

The externally visible `OrderStatus` is intentionally concise:

```text
PendingInventory → PendingPayment → Confirmed
        │                  │
        └──────→ Failed ←──┘
                 ↑
                 └── compensation/timeout failure

PendingInventory or PendingPayment → Cancelled (Phase 2 only, subject to compensation)
```

`PendingInventory` and `PendingPayment` are observable in MVP. `Confirmed`, `Failed`, and later `Cancelled` are terminal for the initial workflow. `Processing`, `Shipped`, and `Completed` are excluded until fulfillment/shipping exists.

| From → To | Trigger / actor | Sync or async | Event/effect | Invalid behavior |
|---|---|---|---|---|
| None → PendingInventory | Customer `POST /v1/orders` | HTTP + local SQL transaction | Persist order, idempotency record, saga state, and initial `ReserveInventory` outbox command | Validation/auth failure: no order; repeated key follows idempotency rules |
| PendingInventory → PendingPayment | `InventoryReserved` consumer | Async, inbox + Commerce transaction | Store reservation ID; append `AuthorizePayment` command to outbox | Ignore/reject stale or mismatched attempt; do not regress state |
| PendingInventory → Failed | `InventoryRejected`, permanent error, or configured timeout | Async | Persist reason code; emit `OrderFailed`; no payment request | Duplicate result is a no-op; unknown result is quarantined/alerted |
| PendingPayment → Confirmed | `PaymentAuthorized` consumer | Async | Persist payment reference; emit `OrderConfirmed` | Must match order, attempt, amount, currency, and current state |
| PendingPayment → Failed | `PaymentDeclined` or terminal processing failure | Async | Persist safe reason; emit `OrderFailed`; issue `ReleaseInventory` compensation | Compensation remains retryable; don't claim released until result |
| PendingPayment → Failed (compensation pending) | Payment result timeout/permanent failure policy | Async | Saga records ambiguity and requests payment status/reconciliation or release policy | Never blindly repeat a non-idempotent external operation |
| Pending* → Cancelled | Not MVP | Later authenticated customer/operator command | Requires explicit compensation and state validation | Not available in MVP; endpoint returns 404/405 as API design dictates |

Messages include `MessageId`, `MessageType`, `SchemaVersion`, `OccurredUtc`, `CorrelationId`, `CausationId`, `Producer`, and payload. Order/attempt/version checks prevent stale messages changing current state. A timeout is not proof that a payment did not occur; reconcile by idempotent operation lookup before compensation where ambiguity exists.

### Saga Behavior

Commerce is the **orchestrator/process manager** because it owns the customer-visible order lifecycle. It persists every step and emits commands through its outbox. Inventory and Payment execute local transactions and publish result events. No long-running database transaction remains open across messaging. Compensation is a new command, not rollback of already-committed work. The saga state machine and timeout/reconciliation policy must have unit and failure-integration tests.

## 10. Architecture

### Architectural Style

Use a **small service-oriented architecture with a modular Commerce API and two narrowly scoped workers**, not a service per domain noun. Commerce, Inventory, and Payment are separate deployables and database owners because they demonstrate independent ownership, asynchronous consistency, failure isolation, and scaling. Commerce keeps identity, order API, order aggregate, and saga together because splitting them would add synchronous hops and operational overhead without a strong MVP boundary. Payment is a simulated worker because it gives the workflow a genuinely independent failure boundary while keeping PCI and provider scope out of the project.

Each deployable is internally organized as a pragmatic layered/vertical-slice application: API/host, application use cases, domain, infrastructure/adapters. Avoid a generic framework, shared domain model, or premature CQRS. A small shared contracts package may contain versioned message schemas and primitives only; no shared EF entities, database migrations, or business logic.

### Components, Responsibility, Ownership

| Component | Owns | Responsibilities | Communicates with |
|---|---|---|---|
| Commerce API / Order Service | Commerce DB: users, refresh sessions, product sellable data, orders, order attempts/saga, idempotency records, inbox/outbox | REST/OpenAPI, authN/authZ, validation, pricing snapshots, order state machine/orchestration, customer/operator reads | Clients over HTTPS; broker asynchronously |
| Inventory Worker | Inventory DB: SKU stock, reservations, its inbox/outbox | Reserve/release/query stock; atomic no-oversell enforcement; emit reservation result | Broker only for business workflow; operator stock command endpoint is not needed in MVP |
| Payment Worker | Payment DB: authorization operations, inbox/outbox | Simulated authorization/decline; idempotency and safe result publication; no sensitive card data | Broker only |
| RabbitMQ (local) / Azure Service Bus (Azure) | Durable transport, not business truth | Queue/topic routing, competing consumers, redelivery, DLQ | Service transport adapters |
| Angular client | No authoritative data | Demonstration flows only; access API through HTTPS | Commerce API |
| Optional Notification Worker (Phase 2) | Notification delivery records | Consume terminal order events and send through adapter | Broker; not required for MVP |

### Communication and Integration Boundaries

- **Synchronous:** Angular → Commerce HTTPS REST only. Health/readiness probes are local to each deployable. No synchronous Commerce→Inventory/Payment calls in the order workflow.
- **Asynchronous:** Commerce issues `ReserveInventory`, `AuthorizePayment`, and `ReleaseInventory` commands. Workers emit outcome events. Commerce consumes outcomes, persists saga progression, and issues the next command.
- **Data ownership:** each service has its own database/schema credentials. No cross-service joins, foreign keys, or direct table access. APIs/messages are the integration contracts.
- **Failure boundaries:** API request acceptance is independent of worker completion after durable commit; each database, broker, and worker can fail separately. Outbox preserves work while broker is unavailable. Inbox and idempotent operations tolerate redelivery.
- **Security boundaries:** public client/API boundary; authenticated customer/operator policies; private worker-to-broker identity; service-specific database principals; secret store boundary; no untrusted message is treated as validated business input.
- **Commands vs events:** commands express intent and have one logical destination; events state an outcome/fact and may have multiple subscribers. Names and schemas communicate this distinction.

### Dependencies

Commerce depends on its database and broker transport adapter. Inventory and Payment depend on their own DB and broker. All depend on versioned contract compatibility, telemetry, and configuration. No worker depends on Commerce HTTP availability to process a committed command. Broker unavailability delays publishing/consumption; database unavailability prevents safe local processing and messages are not acknowledged as completed.

## 11. Data Architecture

### Database Strategy

Use **database per service**: CommerceDb, InventoryDb, PaymentDb. In local development these may be three databases on one SQL Server container; in Azure they may initially be separate databases on one Azure SQL logical server for cost and operations, with separate contained users/credentials. This is logical isolation, not independent compute failure isolation. Document that limitation. No cross-database transaction or shared tables. EF Core owns normal persistence and schema migrations per service.

### Commerce Tables (MVP)

| Table | Key fields / relationships | Important constraints and indexes |
|---|---|---|
| `Users` / Identity tables | `UserId` PK, normalized email, password hash, role/claims | Unique normalized email. Use ASP.NET Core Identity password hasher; do not invent hashing. Avoid logging credential fields. |
| `RefreshTokens` | `RefreshTokenId` PK, `UserId` FK, token hash, expiry, revoked/replaced metadata | Unique token hash; index `(UserId, ExpiresUtc)` for sessions/revocation; store hash, not raw bearer token. |
| `Products` | `Sku` PK or stable ID + unique SKU, name, price minor units, currency, active | Unique SKU; check price nonnegative; indexes only for actual list/filter access. Commerce owns sellable price. |
| `Orders` | `OrderId` PK, `CustomerId`, status, total/currency, created/updated UTC, `RowVersion`, saga fields | FK to Commerce user if identity relationship is local; index `(CustomerId, CreatedUtc DESC, OrderId)` for customer history; index `(Status, UpdatedUtc)` for operator/work recovery. |
| `OrderItems` | `OrderItemId` PK, `OrderId` FK, SKU snapshot, name, quantity, unit minor amount, currency | FK and check quantity > 0/amount >= 0; index `(OrderId)`; cascade only for order-owned children. |
| `OrderIdempotency` | scope/customer, key hash, request hash, order ID, response/status, expiry | Unique `(CustomerId, IdempotencyKeyHash)`; reject same key/different request; retention policy. |
| `OrderAttempts` (or saga state table) | `(OrderId, Attempt)` unique, step/status, correlation ID, reservation/payment refs, deadlines, error code | Unique order + attempt; index on pending step/deadline for reconciliation worker. |
| `OutboxMessages` | message ID PK, type/version, payload, occurred, available, attempts, lease, published timestamp | Index `(PublishedUtc, AvailableUtc)`/pending selection; use filtered/partial strategy supported by SQL Server for unpublished rows; retain until published plus safety window. |
| `InboxMessages` | consumer name + message ID unique, received/processed, state, attempts, error | Unique `(ConsumerName, MessageId)`; pending/recovery index by state/updated. |
| `OrderStatusHistory` | ID PK, order ID FK, prior/new state, reason code, actor/system, occurred UTC, correlation | Index `(OrderId, OccurredUtc)`; avoid storing secrets or arbitrary untrusted payload. |

### Inventory Tables

`InventoryItems(Sku PK, OnHand, RowVersion, UpdatedUtc)` with checks `OnHand >= 0`; `InventoryReservations(ReservationId PK, OrderId, Attempt, Status, ExpiresUtc, CreatedUtc)` unique `(OrderId, Attempt)`; `InventoryReservationLines(ReservationId FK, Sku, Quantity)` with positive-quantity check and SKU lookup index; service-local `InboxMessages` and `OutboxMessages`. Reservation operation and stock deduction/reserved accounting commit in one Inventory DB transaction. Choose either decrement available stock at reserve and restore on release, or maintain reserved count; this plan selects explicit `OnHand` and `Reserved` counters with check `0 <= Reserved <= OnHand`, and available=`OnHand-Reserved`. Reservation lines and all affected SKU rows are locked/updated atomically in stable SKU order to reduce deadlocks.

### Payment Tables

`PaymentAuthorizations(PaymentAuthorizationId PK, OrderId, Attempt, OperationKey unique, AmountMinor, Currency, Status, SafeFailureCode, ProviderReference, CreatedUtc, UpdatedUtc, RowVersion)` unique `(OrderId, Attempt)`. No PAN, CVV, raw payment token, or secret in payload/database. Include local inbox/outbox. The simulator’s deterministic result is configurable for tests/dev but cannot be configured through an unauthenticated public endpoint.

### Transactions and Isolation

- HTTP order acceptance: one Commerce transaction persists order + items + saga/attempt + idempotency result + initial `ReserveInventory` command in the outbox. Commit before returning `202`.
- Consumer handling: one local DB transaction persists inbox claim, business state changes, and outgoing outbox message; acknowledge broker only after commit.
- Inventory reservation: one Inventory transaction validates all requested SKUs and reserves all or none. No partial reservation is visible.
- Payment: one Payment transaction claims operation key, records result, and outboxes outcome. External real payment integration later requires provider idempotency and reconciliation; SQL transaction cannot include provider transaction.
- Use default `READ COMMITTED` for normal reads/writes. Use `READ_COMMITTED_SNAPSHOT` at database level only after testing expected behavior and documenting snapshot semantics. Use explicit row-level locking/atomic conditional updates for inventory correctness rather than relying on a broad serializable transaction for every request. Avoid `NOLOCK`.
- Keep transactions short; never hold one open while calling a broker, HTTP service, or waiting on user input.

### EF Core, Query Plans, and Stored Procedures

Use EF Core for aggregate persistence, transactions, migrations, and ordinary bounded reads. Project list queries to DTOs, use `AsNoTracking` for read-only requests, avoid lazy loading, and prevent N+1 queries with explicit projections/includes appropriate to page size. Paginate using stable ordering (`CreatedUtc`, `OrderId`) and cursor/keyset pagination if offset cost grows; cap page size. Select only required columns.

Inspect actual SQL and actual execution plans for customer order list, operator pending-work scans, outbox claim, inbox recovery, and inventory reserve. Verify intended seeks and row estimates, check key lookups/sorts, and add covering/composite indexes only for measured workloads. Do not assert a particular plan in all environments; preserve representative query-plan notes and benchmark conditions.

Stored procedures are justified only where measured SQL-specific behavior is clearer or materially safer—for example, one atomic inventory reservation operation using conditional updates/locking across multiple SKU rows, or a carefully tuned outbox batch-claim query. Any procedure needs parameterization, least-privilege `EXECUTE`, migration/version ownership, integration tests, and a documented EF boundary. Do not use stored procedures for ordinary CRUD merely to demonstrate the feature. Prefer an EF transaction plus conditional updates if it is clear, testable, and performs adequately.

## 12. API Design

Base path `/api/v1`; REST resource names are plural and status transitions are not arbitrary client-settable fields. OpenAPI is published by Commerce. Versioning begins with a stable URL prefix and versioned integration contracts; add a versioning library only if multiple concurrently supported HTTP versions become necessary.

| Method and route | Access | Result |
|---|---|---|
| `POST /api/v1/auth/register` | Anonymous, rate-limited | `201` minimal user representation; duplicate email `409`; validation `400` |
| `POST /api/v1/auth/login` | Anonymous, rate-limited | `200` access token + refresh-token cookie/session policy; invalid credentials generic `401` |
| `POST /api/v1/auth/refresh` | Refresh credential | `200` rotated tokens; revoked/reused token `401` and revoke session family |
| `POST /api/v1/auth/logout` | Authenticated | `204`, revoke refresh session |
| `GET /api/v1/products` | Authenticated customer/operator | Paginated active sellable products |
| `POST /api/v1/orders` | Customer | Require `Idempotency-Key`; `202` with order resource and status link |
| `GET /api/v1/orders` | Customer / operator | Customer-scoped list; operator may filter; paginated |
| `GET /api/v1/orders/{orderId}` | Owner / operator | Order state, line snapshots, timestamps, safe status history |
| `GET /api/v1/health/live` | Probe | Process liveness only |
| `GET /api/v1/health/ready` | Probe | Dependency readiness; do not reveal credentials/connection strings |

Operator product/stock setup for MVP is through seed data/scripts, not an unaudited broad admin API. If an operator write API is added, it requires a distinct policy, audit record, validation, and explicit acceptance criteria.

Use `202 Accepted` for asynchronous workflow creation, `200` for reads/login, `201` only for resources created synchronously, `204` for successful no-content logout, `400` validation, `401` unauthenticated, `403` unauthorized, `404` absent or not-owned resource (avoid revealing existence), `409` idempotency/state/concurrency conflict, `429` rate limit, and `500/503` for controlled server/dependency errors. Never return stack traces, SQL messages, broker internals, or sensitive provider details.

### Validation

Use DataAnnotations for basic transport metadata where it improves schema generation and FluentValidation for cross-field/business request validation. Validation is not a substitute for domain invariants or database constraints. Normalize and bound strings/collections; reject unknown/malformed fields when appropriate; validate message contracts independently at consumer boundaries.

## 13. Authentication and Authorization

### Authentication

- Use ASP.NET Core Identity for user management and a supported password hasher (PBKDF2 via Identity by default; use framework-upgrade guidance). Never implement custom password cryptography. Enforce password length/compromised-password policy proportionate to a portfolio app and generic login failure responses.
- Commerce issues short-lived signed JWT access tokens (target 10 minutes) with issuer, audience, expiry, subject, role/permission claims, and unique token ID. Validate signature, issuer, audience, lifetime, and signing-key rollover. Do not put secrets or sensitive profile data in claims.
- Use rotating refresh tokens with high entropy, stored only as hashes, bound to a user/session, expiring (e.g. 7 days configurable), revocable, and one-time use. Detect reuse and revoke the token family. For Angular, prefer `HttpOnly`, `Secure`, appropriately `SameSite` refresh cookie plus CSRF protection for cookie-authenticated refresh/logout; keep short-lived access token in memory rather than localStorage. Document local HTTP exception as development-only.
- Signing keys are not hardcoded. Local keys come from user secrets/environment configuration excluded from Git; Azure uses Key Vault and managed identity. Rotate keys and support a controlled overlap for token validation.
- Rate-limit register/login/refresh endpoints; use generic errors and avoid account enumeration. No access or refresh token is logged.

### Authorization

Use policy-based authorization with role claims as coarse group membership and explicit policies/requirements for actions. Enforce resource ownership in application query/handler so a customer cannot read another customer’s order; do not rely on obscurity of GUIDs.

| Principal | Permissions |
|---|---|
| Customer | Read public sellable products; submit orders; list/read only own orders; manage own session/logout. Cannot set price/status or inspect another customer. |
| Operator | Customer capabilities where explicitly needed; inspect all orders and safe operational status. Any manual replay/stock adjustment is excluded MVP or requires a separately defined audited policy. |
| Administrator | User/role configuration and narrowly scoped operational administration; not an implicit bypass of audit/security rules. No broad admin UI required in MVP. |
| Service workload identity | Access only its own DB and required broker entities; no interactive user JWT privileges. |

Claims are derived from trusted identity records, not request fields. Authorization failures are logged as security events with principal/resource metadata only where safe; avoid exposing whether a resource exists.

## 14. Security Architecture and Threat Model

### API Security Controls

- Require HTTPS in hosted environments; configure HSTS, appropriate content/security headers, TLS termination trust, and forwarded-header handling only for known proxies.
- Configure exact CORS origins per environment; never wildcard origins with credentials. Angular and API origins are explicit.
- Apply global and stricter per-endpoint rate limits, bounded request body sizes, timeouts, pagination limits, and JSON depth/collection limits. Use antiforgery/CSRF protection for cookie-based refresh operations.
- Validate DTOs and broker contracts; use parameterized EF queries; avoid dynamic SQL unless identifiers are allow-listed. Do not bind persistence entities from client payloads (mass-assignment prevention).
- Standardized errors omit internals. Apply least privilege to SQL identities, broker identities, GitHub environments, and Azure managed identities.
- Security logging records authentication failures, authorization denials, key operational actions, and suspicious idempotency/replay patterns, without passwords, bearer tokens, refresh tokens, payment secrets, or complete sensitive bodies.
- Pin/regularly update dependencies, enable Dependabot or equivalent, secret scanning, code scanning, and container scanning where available. Define retention and access for logs containing identifiers.

### Threat Model (MVP)

| Threat | Mitigation / evidence |
|---|---|
| Broken authentication / brute force / account enumeration | Identity password hasher, generic login response, endpoint rate limits, lockout/backoff policy, short JWT lifetime, security event metrics, tests. |
| JWT forgery, algorithm confusion, wrong audience, expired token | Allow-listed signing algorithm, issuer/audience/lifetime/signature checks, secure key storage/rotation, negative auth tests. |
| Token theft / replay | HTTPS, short access lifetime, in-memory access token, HttpOnly secure refresh cookie, hashed rotating refresh tokens, reuse detection, revoke/logout, no token logs. |
| Broken object-level authorization | Customer ownership query/policy and tests proving cross-customer reads return non-disclosing `404/403` behavior. |
| SQL injection | EF parameterization; no interpolated untrusted SQL; reviews and security tests for any raw SQL/procedure parameters. |
| Mass assignment / price or state tampering | Request DTO allow-list, server-side prices/totals/status, immutable line snapshots, domain rules and tampering tests. |
| Sensitive data exposure | Minimize retained data; no card data; safe errors/log redaction; encrypted transport and Azure SQL at-rest encryption; restricted telemetry access. |
| Oversized or abusive requests / resource exhaustion | Request/body and collection caps, pagination caps, rate limits, timeouts, queue prefetch/concurrency limits, bounded retry. |
| Duplicate/replay HTTP or broker requests | Scoped idempotency keys with request hash; inbox unique key; operation keys; expiry policy; stale attempt/version checks. |
| Malicious or malformed message | Broker access control, schema/version validation, payload size bounds, allow-listed message types, poison-to-DLQ, no direct arbitrary type deserialization. |
| Secret leakage in repository, image, logs, or CI | Secret scanning, `.gitignore`, local user-secrets/env, Azure Key Vault/managed identity, GitHub OIDC, log filtering, rotation/revocation runbook. |
| Dependency/supply-chain vulnerability | Lock/centralize versions, automated dependency alerts, CI dependency audit and image scan; review update before release. |
| CORS/CSRF abuse | Exact origins; no credentialed wildcard; CSRF token/origin checks for cookie-authenticated endpoints; SameSite policy. |
| Payment ambiguity / duplicate charge | Simulator only in MVP; unique provider operation key and idempotent adapter; future provider webhooks require signature verification and replay window. |
| Insider/operational misuse | Least privilege, audit events, protected Azure/GitHub environments, no unaudited replay/stock modification in MVP. |

## 15. Messaging Architecture

### Broker Choice and Role

Use RabbitMQ for a convenient local broker and Azure Service Bus in Azure, behind a small application-owned transport adapter. Both are not run as active production brokers and there is no cross-broker replication requirement. The abstraction covers only required operations (publish command/event, consume, acknowledge, dead-letter, metadata), not every vendor feature. Keep broker-specific topology/configuration visible and tested; do not pretend semantics are identical.

RabbitMQ provides local queues, exchanges, routing keys, publisher confirms, durable messages, manual acknowledgements, and dead-letter exchanges. Azure Service Bus provides managed queues/topics/subscriptions, dead-lettering, locks, duplicate-detection options, and managed identity. Application inbox/idempotency remains authoritative even if broker duplicate detection is enabled. Avoid depending on a broker duplicate-detection window for correctness.

### Topology and Message Flow

- Commands are routed to durable service command queues: `inventory.commands`, `payment.commands`; each command has one logical owning consumer group.
- Integration events are published to a durable event exchange/topic. Service subscriptions/queues receive only relevant event types. Commerce consumes Inventory and Payment outcomes; optional Notification (Phase 2) consumes terminal order events.
- Local broker topology should preserve the same logical routing contract as Azure, but not claim identical lock, ordering, DLQ, or retry mechanics.
- Consumers use manual acknowledgment / complete only after inbox + business update + outbox transaction commits. Requeue transient failures with bounded delayed retry; do not hot-loop immediate requeues.
- Configure prefetch/lock duration, concurrent handlers, max delivery count, message TTL, max payload, and DLQ policy per environment. Values are tuned from metrics.
- Message envelope includes immutable ID, type, schema version, created time, correlation/causation IDs, trace context, and producer. Payloads are minimal, contain no secrets, and use UTC timestamps and stable identifiers.
- Contract evolution is additive where possible. Consumers tolerate unknown optional fields; breaking changes use a new schema version/type and a compatibility/deprecation window. Integration contracts are tested and documented.

### Commands, Events, Producers, Consumers

| Kind | Message | Producer → consumer | Purpose |
|---|---|---|---|
| Command | `ReserveInventory` v1 | Commerce → Inventory | Reserve all order lines for a specific order attempt with expiry and idempotent operation ID. |
| Event | `InventoryReserved` v1 | Inventory → Commerce | Reservation succeeded; includes reservation ID, order/attempt, SKU quantities, expiry. |
| Event | `InventoryRejected` v1 | Inventory → Commerce | Reservation not possible; safe reason code and retryability classification. |
| Command | `AuthorizePayment` v1 | Commerce → Payment | Authorize amount/currency for order/attempt; includes operation key, never card data. |
| Event | `PaymentAuthorized` v1 | Payment → Commerce | Authorization recorded; payment reference, amount/currency, operation key. |
| Event | `PaymentDeclined` v1 | Payment → Commerce | Safe decline category, retryability, operation key; no sensitive provider response. |
| Command | `ReleaseInventory` v1 | Commerce → Inventory | Compensate an existing reservation; idempotent reservation/order-attempt key. |
| Event | `InventoryReservationReleased` v1 | Inventory → Commerce | Confirms release or reports already released/not found with safe outcome. |
| Event | `OrderConfirmed` v1 | Commerce → event topic | Durable order fact for external consumers; published from Commerce outbox. |
| Event | `OrderFailed` v1 | Commerce → event topic | Durable terminal failure fact with stable safe reason code. |

Payloads include only required data, stable IDs, quantities, amount/currency as integer minor units, and attempt. Contracts do not contain access tokens, user passwords, payment credentials, or arbitrary database entities.

### Event Catalog and Failure Policy

| Event/command | Producer | Consumers | Version/payload essentials | Failure, retry, idempotency |
|---|---|---|---|---|
| `ReserveInventory` command | Commerce | Inventory | v1: message ID, order ID, attempt, operation ID, lines, expiry | Retry bounded; Inventory inbox + unique order/attempt reservation; all-or-none response. Permanent invalid SKU/stock result is an event, not repeated exception. |
| `InventoryReserved` event | Inventory | Commerce | v1: order/attempt, reservation ID, lines, expiry | Commerce inbox; stale/duplicate result no-op; transient DB failure leaves broker delivery retryable. |
| `InventoryRejected` event | Inventory | Commerce | v1: order/attempt, reason code, retryable flag | Same dedupe; business rejection is not transport retry. |
| `AuthorizePayment` command | Commerce | Payment | v1: order/attempt, operation key, amount/currency | Payment inbox and unique operation key; same operation returns recorded result; transient failures retry; simulator decline is a durable business result. |
| `PaymentAuthorized` / `PaymentDeclined` | Payment | Commerce | v1: order/attempt, operation key, safe result/reference | Inbox and state/version validation; duplicate is no-op; ambiguous provider status requires reconciliation in future provider adapter. |
| `ReleaseInventory` command | Commerce | Inventory | v1: reservation/order/attempt and reason | Idempotently release; retry until confirmed or operationally escalated; never decrement twice. |
| `InventoryReservationReleased` | Inventory | Commerce | v1: order/attempt, reservation ID, outcome | Inbox; record compensation completion. |
| `OrderConfirmed` / `OrderFailed` | Commerce | Phase 2 Notification, future analytics | v1: order ID, customer ID, status, occurred time, safe reason code | Outbox retry; consumers dedupe; notification provider failures do not roll back order. |

## 16. Outbox Pattern

Every service that needs to publish has an `OutboxMessages` table in its owned database. The business state update and serialized, versioned outgoing message are written in the **same local SQL transaction**. A hosted background dispatcher claims unpublished records in bounded batches using short leases/claim state, publishes with broker confirmation, then marks them published. SQL Server claim logic must prevent two dispatchers from permanently owning the same record; use lease expiry and concurrency/locking appropriate to the chosen query. Do not hold a DB transaction open while waiting for broker confirmation.

If the database commit succeeds but broker publishing fails, the outbox row remains pending and is retried with exponential backoff plus jitter and a cap. If the broker accepted a message but the publisher crashed before marking the row published, it may publish again; MessageId remains stable and consumer inbox deduplicates it. This is expected at-least-once behavior, not a defect. Persist attempt count, last error classification, next attempt, and timestamps; avoid storing secret-bearing exception text.

Alert on age/count of pending records. After published plus a safety retention window, archive/delete in bounded batches. Never delete pending/poisoned rows silently. A poison outbox record is surfaced to operator investigation and safe repair/replay; message schema serialization failures must not block unrelated rows indefinitely. Operational repair is audited.

## 17. Inbox Pattern, Idempotency, and Deduplication

### Inbox

Each consumer stores an inbox record in its own business database with `(ConsumerName, MessageId)` unique constraint, receive time, state, processed time, attempt/error classification, and correlation. In one local transaction the consumer claims/deduplicates the message, applies local business changes, appends outgoing outbox messages, and marks processed. A unique-key collision means already received/processed; acknowledge without repeating side effects. If processing fails before commit, transaction rollback leaves no completed inbox record and broker may redeliver. For long processing, do not commit a permanent “processed” marker before the business update; if a claim/lease model is required, define stale-claim recovery explicitly.

Inbox retention exceeds broker redelivery and replay horizons (initial policy: 30 days, configurable and revisited before enabling longer replay). Retain compact IDs/status longer where audit requirements warrant; clean only finalized records in batches. A manual replay with a new MessageId must still be protected by business operation keys/state checks.

### HTTP Idempotency

`POST /orders` requires a high-entropy or client-generated `Idempotency-Key` (bounded length/format). Scope by authenticated customer and operation. Store a hash of the key and a canonical request hash with the created order/result in the same Commerce transaction. On same key + same request, return the original order/status response (include `Idempotency-Replayed: true` if useful). Same key + different request returns `409`. Define retention (initially 24 hours for full response mapping, while order remains durable); do not use the raw key as an unbounded metric/log label.

### Business Idempotency

- Order submission: unique customer/key; never create a second order for replay.
- Inventory reserve/release: unique order/attempt operation and persisted reservation state; repeated command returns same outcome.
- Payment authorization: unique operation key and `(OrderId, Attempt)`; return stored result on retry.
- Consumer effects: inbox unique message key plus domain uniqueness/state checks.
- Outbox: stable MessageId across retries; broker-level duplicate detection is an optimization only.

Use message ID for transport dedupe, operation key for business effect dedupe, and HTTP idempotency key for client retry safety; these identifiers solve different problems and must not be conflated.

## 18. Delivery Semantics

- **At-most-once:** a message/effect is delivered zero or one times; loss is possible. Not sufficient for important workflow commands.
- **At-least-once:** message delivery may occur one or more times; retries/redelivery avoid silent loss but require idempotent consumers. This is the platform’s chosen delivery model.
- **Exactly-once:** a broker may offer a constrained deduplication/transaction feature, but there is no general exactly-once effect across SQL, a broker, and external payment. A crash between two systems creates an ambiguity window. Do not claim end-to-end exactly-once.

The design uses durable local commits/outbox, broker at-least-once delivery, inbox deduplication, unique business operation keys, conditional state transitions, and compensation/reconciliation. These provide effectively-once business outcomes for defined operations under the retained deduplication horizon, not a magical exactly-once transport guarantee. Retention and manual replay behavior are part of correctness.

## 19. Error Handling

Use ASP.NET Core exception-handling middleware and `ProblemDetails` (RFC 7807-compatible; use current RFC 9457 conventions where the framework emits them) as the single HTTP error shape. Add stable `type`, `title`, `status`, safe `detail`, and `traceId`; validation may include an `errors` dictionary with field messages. Never serialize exception, SQL, broker, stack trace, secrets, or internal topology to clients.

Map domain outcomes deliberately: validation→400, unauthenticated→401, forbidden→403, absent/non-owned resource→404, duplicate key/state/concurrency conflict→409, rate limit→429 with `Retry-After`, dependency unavailable→503 where the request cannot be safely accepted, unexpected failure→500. If order transaction/outbox commit succeeded, a later broker outage must not turn accepted work into a false failure; return 202 and expose pending status.

Validation errors are stable and field-oriented. Global middleware assigns/propagates trace/correlation IDs, records sanitized structured error telemetry, and avoids duplicate exception logging at every layer. Background consumers classify failures as transient (bounded retry/backoff), permanent/business rejection (publish outcome), or poison/contract fault (DLQ/quarantine and alert). Cancellation tokens and shutdown behavior must not acknowledge work whose transaction did not commit.

## 20. Concurrency and Consistency

### Oversell Protection

Inventory is authoritative. For each reservation, validate all lines and atomically ensure available quantity is sufficient before changing reservation state. For each SKU update use a conditional update/row lock with `Available >= requested`; process multiple SKUs in stable order and roll back all lines if any fails. Enforce nonnegative/consistent database checks and unique reservation key. A concurrency token/rowversion detects competing updates; retry a bounded number of serialization/concurrency conflicts by re-reading state. A successful reservation is committed before `InventoryReserved` is outboxed. Never rely on Commerce’s product cache or an earlier availability read for correctness.

### Other Concurrent Operations

- Order aggregate uses SQL Server `rowversion` and EF Core concurrency handling. Stale client writes are not allowed to alter workflow state; duplicate/stale worker events are accepted as no-op or conflict after state validation.
- Idempotency unique constraints arbitrate simultaneous duplicate HTTP requests. On unique-key race, load and return the committed result after verifying request hash.
- Payment unique operation key prevents repeated authorization side effects.
- Multiple worker replicas may consume the same logical work only once at the business level through broker lock/ack plus inbox/unique constraints. Leases and inbox transactions address crashes; no in-memory lock is correctness-critical.
- Use SQL transactions at `READ COMMITTED` for local atomic work; choose row locks/conditional updates for stock. Use `SERIALIZABLE` only if an operation’s correctness demands it and measurements show acceptable contention. Do not use distributed locks as a substitute for database constraints.
- Broker ordering is not assumed globally. Include order attempt and state/version; reject stale outcomes. If later business rules require per-order ordering, use broker sessions/partition keys and still validate state.

## 21. Observability

### Logs

Use structured JSON logs with consistent fields: `TraceId`, `SpanId`, `CorrelationId`, `CausationId`, `RequestId`, `OrderId`, `MessageId`, service, environment, operation, outcome, duration, retry attempt, and safe error category. Do not use high-cardinality IDs as metric labels. Include authentication/security events without tokens or secrets. Define retention/access per environment.

### Metrics

Instrument low-cardinality counters/histograms/gauges:

- HTTP request count, status class, latency, rate-limited requests.
- Orders accepted, confirmed, failed, time-in-state and terminal outcome.
- Inventory reservation success/rejection and concurrency conflict count.
- Payment authorization success/decline/technical failure (no sensitive dimensions).
- Outbox pending count/oldest age/publish attempts/failures.
- Inbox duplicate count, processing duration, failures.
- Queue depth, oldest message age, delivery count, retry count, DLQ count.
- Consumer processing duration/concurrency and DB pool/connection saturation where available.

Define alerts for sustained outbox age, oldest queue age, DLQ growth, repeated payment/inventory technical failures, elevated API 5xx/latency, database connectivity, and auth abuse. Alert thresholds are documented and tuned, not asserted universally.

### Tracing

Use OpenTelemetry .NET instrumentation for ASP.NET Core, `HttpClient` if later used, EF/database where supported, and custom spans for outbox dispatch and message handling. Propagate W3C trace context through message metadata. Trace the flow:

```text
HTTP POST /orders → Commerce SQL commit/outbox → broker publish
→ Inventory consume/SQL/outbox → broker → Commerce consume/saga update
→ Payment consume/SQL/outbox → broker → Commerce confirm
```

Local development exports to console/OTLP collector when configured. Azure exports to Application Insights / Azure Monitor through supported OpenTelemetry integration. Sampling must preserve errors and representative long workflows. Never attach secrets or full request/message bodies to spans.

Health endpoints distinguish liveness from readiness. Readiness reports degraded dependency state safely; liveness must not restart a healthy process solely because a remote broker has a transient outage.

## 22. Testing Strategy

### Unit Tests (xUnit)

- Domain state transitions, totals, invariant enforcement, stale/invalid transitions, state version/attempt handling.
- FluentValidation and request normalization, idempotency canonicalization, authorization policy/resource checks.
- Inventory all-or-none reservation decisions and payment simulator outcomes using deterministic fakes.
- Retry/backoff classification, outbox scheduling and saga timeout/compensation decisions with injected clock/randomness.
- Avoid mocking every internal method; test observable outcomes and pure domain/application logic.

### Integration Tests

- ASP.NET Core API with `WebApplicationFactory`: routing, Problem Details, auth policies, ownership, rate limits (where stable), OpenAPI, serialization, idempotency.
- SQL Server integration (prefer containerized SQL Server in CI/local test profile): real EF migrations, constraints, transactions, concurrency token behavior, query projections. Do not use EF InMemory for relational semantics.
- RabbitMQ adapter topology/ack/redelivery/publisher confirms and Azure adapter contract/serialization configuration. Use adapter-level tests; do not make every unit test require a broker.
- Outbox commit/publish failure window; inbox duplicate and crash/retry behavior; poison event handling; migrations from empty database.
- Inventory parallel requests competing for the final unit; assert at most one reservation and no negative availability.
- Contract compatibility/serialization tests using committed representative JSON fixtures and consumer schema validation.

### End-to-End Tests

Run Commerce + Inventory + Payment + SQL + RabbitMQ locally in Compose/test harness; register/login, submit order, poll status to confirmed or expected failed state, verify inventory/payment/order persistence and trace identifiers. Include one path for inventory rejection and simulated payment decline. Use bounded polling and deterministic simulator configuration.

### Failure and Resilience Tests

- Broker unavailable after order DB commit: API still returns durable acceptance; outbox later publishes.
- Publisher crash after broker confirm but before outbox mark: duplicate publish is harmless.
- Consumer crash before DB commit: message redelivered and operation occurs once.
- Consumer crash after commit before ack: inbox suppresses duplicate effect.
- Duplicate and out-of-order/stale attempt event; malformed/unsupported schema goes to DLQ with alert.
- Database unavailable, deadlocks/concurrency conflicts, payment timeout/decline, inventory unavailable, retry exhaustion, DLQ redrive, and graceful shutdown.
- Concurrent duplicate HTTP submissions and concurrent inventory purchases.

### Contract Tests

Useful at message producer/consumer boundaries and API schema. Validate required fields/version compatibility and that producers emit documented payloads. Do not introduce a separate contract-testing platform unless simple JSON schema/fixtures stop being sufficient.

### Test Exclusions / Discipline

Do not assert private method calls or exact broker delivery timing. Do not treat an in-memory database as proof of SQL behavior. Avoid flaky sleep-based tests; use test clocks, completion signals, bounded polling, and isolated data. Load/chaos tests are phase 2/optional and must state the environment and workload.

## 23. Swagger / OpenAPI

Enable OpenAPI generation and Swagger UI in Development and an explicitly configured non-production environment. Include JWT bearer security scheme, endpoint summaries/descriptions, async `202` semantics, required idempotency header, pagination, request/response examples, status codes, validation rules, auth requirements, and Problem Details schema/examples. Document that order acceptance does not mean confirmation. Use XML documentation where useful and explicit response metadata. Do not expose debug/admin endpoints or unrestricted Swagger UI in production; if enabled, protect access and keep the schema available through the approved documentation pipeline.

## 24. Repository Structure and Engineering Conventions

Planned repository layout (names may be refined in Phase 0 but boundaries remain):

```text
.
├── README.md
├── PLAN.md
├── ARCHITECTURE.md
├── SECURITY.md
├── API.md
├── CONTRIBUTING.md
├── LICENSE
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── src/
│   ├── BuildingBlocks/
│   │   ├── Messaging.Abstractions/
│   │   ├── Messaging.Contracts/
│   │   └── Observability/
│   ├── Commerce/
│   │   ├── Commerce.Api/                 # ASP.NET Core host, minimal API endpoints/controllers
│   │   ├── Commerce.Application/
│   │   ├── Commerce.Domain/
│   │   └── Commerce.Infrastructure/      # EF Core, SQL, broker adapters, Identity
│   ├── Inventory/
│   │   ├── Inventory.Worker/
│   │   ├── Inventory.Application/
│   │   ├── Inventory.Domain/
│   │   └── Inventory.Infrastructure/
│   ├── Payments/
│   │   ├── Payments.Worker/
│   │   ├── Payments.Application/
│   │   ├── Payments.Domain/
│   │   └── Payments.Infrastructure/
│   └── web/                               # Angular + TypeScript, backend-supporting UI only
├── tests/
│   ├── Commerce.UnitTests/
│   ├── Commerce.IntegrationTests/
│   ├── Inventory.UnitTests/
│   ├── Inventory.IntegrationTests/
│   ├── Payments.UnitTests/
│   ├── Payments.IntegrationTests/
│   ├── Contracts.Tests/
│   └── EndToEnd.Tests/
├── docs/
│   ├── adr/
│   ├── architecture/
│   ├── database/
│   ├── messaging/
│   ├── operations/
│   └── runbooks/
├── infrastructure/
│   ├── compose/
│   └── azure/                             # Bicep, introduced with Azure phase
├── scripts/
└── .github/
    ├── workflows/
    ├── ISSUE_TEMPLATE/
    ├── PULL_REQUEST_TEMPLATE.md
    ├── dependabot.yml
    └── CODEOWNERS
```

Do not force an abstraction project to exist if it has no stable shared responsibility. Building blocks contain only cross-cutting transport/telemetry primitives and versioned contracts, not domain entities. Prefer minimal API route groups for simple endpoints and controllers where MVC conventions/filters/model binding materially improve clarity; choose and document the primary style in Commerce rather than building duplicate APIs. The project must demonstrate both concepts only if there is a clear example and no duplicate business route; otherwise MVC concepts are explained through ASP.NET Core endpoint/filter/DI/middleware architecture, not artificial parallel endpoints.

### Naming and Code Standards

- C# namespaces/types use PascalCase; locals/parameters camelCase; async methods end in `Async`; cancellation tokens propagate through I/O.
- Use nullable reference types, analyzers, `.editorconfig`, deterministic builds, central package versions, and a pinned supported .NET SDK via `global.json`.
- Domain names are business language; DTOs/contracts are explicit and versioned. Avoid generic `Manager`/`Helper` names and infrastructure leakage into domain.
- Use UTC `DateTimeOffset`/instant semantics; money is integer minor units plus currency; IDs are opaque stable identifiers.
- Angular uses TypeScript strict mode, feature-based structure, typed API clients, and no business authority on the client.
- No secrets/configuration in source or checked-in local `.env`; provide `.env.example` with placeholders only.

### Git and Pull Requests

Use `main` as protected, releasable branch and short-lived feature branches; avoid long-lived develop branch. Branch examples: `feature/order-idempotency`, `fix/inventory-race`, `docs/adr-004-broker`. Conventional Commits are recommended (`feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`), not a substitute for meaningful descriptions.

Every PR links a GitHub Issue, states behavior/risk, includes tests/docs/migration or contract implications, and passes required CI. Require at least one review when collaborators exist; solo portfolio work still uses PRs for meaningful features and self-review checklist. Protect `main`, require status checks, prevent secret-bearing artifacts, and do not merge failing migration/contract/security checks.

## 25. Documentation Strategy

Documentation is a deliverable, updated with code:

- `README.md`: problem, architecture diagram, features, prerequisites, quickstart, demo, links, limitations, status, and portfolio highlights.
- `PLAN.md`: this baseline, scope and roadmap.
- `ARCHITECTURE.md`: context/container diagrams, boundaries, data ownership, flow, consistency, failure semantics, and key trade-offs.
- `SECURITY.md`: threat model, auth design, secret handling, reporting vulnerabilities, local-vs-hosted differences, and security checklist.
- `API.md`: API conventions and generated OpenAPI usage/examples; do not duplicate generated schema manually.
- `CONTRIBUTING.md`: setup, coding/test conventions, migrations, PR process, issue labels, and Definition of Done.
- ADRs: context, decision, alternatives, consequences, status, and supersession links.
- Database docs: ownership/ERD, constraints/index rationale, migration/restore procedure, query-plan notes.
- Messaging docs: topology, contracts/catalog/versioning, outbox/inbox, retries/DLQ, replay policy.
- Deployment/local development docs: Compose, configuration, Azure infrastructure, CI/CD, smoke tests, rollback.
- Runbooks: broker/database outage, growing outbox/queue, DLQ inspection/redrive, secret rotation, failed migration, payment ambiguity, and incident triage.
- Failure scenario documentation explains expected user-visible behavior and evidence/tests for each failure.

Prefer diagrams as Mermaid checked into docs. Every major decision should explain **why**, alternatives rejected, and cost/trade-off. Documentation examples must use synthetic data and no secrets.

## 26. Docker and Local Development

- Multi-stage Dockerfiles for Commerce API, Inventory Worker, and Payment Worker; restore/build/publish in SDK stage and run as non-root in minimal runtime stage, with health checks and no build tools/secrets in final image.
- Pin base image tags/digests according to maintenance policy, use `.dockerignore`, expose only required ports, configure graceful shutdown, and log to stdout/stderr.
- Docker Compose provides SQL Server, RabbitMQ management for local-only use, all three .NET applications, and optionally Angular. SQL Server uses a developer-only environment secret supplied outside committed files. Add health checks and dependency readiness; services still handle runtime outages rather than assuming startup ordering guarantees health.
- Compose provisions separate service databases and Rabbit topology; migrations/seed data run through explicit development scripts or controlled startup migration policy. Production must not grant every app schema-owner rights or run uncontrolled destructive seed logic.
- Local configuration uses user-secrets/environment variables and `.env` ignored by Git; `.env.example` contains names and fake values only. Document supported OS requirements, ports, reset of disposable local data, and troubleshooting.
- Containers are stateless; no workflow state or dedupe state in memory. Do not mount developer secrets into the published image.

## 27. GitHub Actions and CI/CD

### Pull Request Pipeline

```text
Checkout → setup pinned .NET/Node → restore → format/analyzers → build
→ unit tests → SQL/broker integration tests → contract tests
→ Angular lint/build/tests → dependency/security/secret checks → publish test reports
```

Use least-privilege workflow permissions, pinned/reviewed actions, caching that does not leak secrets, test containers/services as appropriate, and clear artifacts. Required PR checks: build, unit tests, integration/contract suite, formatting/analyzer, frontend validation, dependency/secret checks. Security tooling can begin with built-in GitHub features and .NET dependency audit; do not make an expensive enterprise scanner a prerequisite.

### Deployment Pipeline

```text
main/tag → repeat CI → build immutable versioned images → scan/sign if available
→ push to Azure Container Registry → deploy to Development → smoke/health checks
→ manual approval → Test/Production promotion of same image digest
```

Use GitHub OIDC federated identity for Azure rather than long-lived client secrets. Environment approvals/protection for production; use deployment slots/revisions and documented rollback. Database migrations are backward-compatible expand/contract where possible, run as a controlled one-shot job before app rollout, and have backup/rollback notes. Never auto-apply destructive schema changes in production.

Environments: **Development** (rapid, synthetic data, optional verbose diagnostics with redaction), **Test** (release candidate, production-like config, automated smoke), **Production** (least privilege, restricted telemetry, approvals, backups, alerts). MVP may deploy one development/test environment plus document production configuration; the pipeline must still define promotion strategy and avoid different binaries per environment.

## 28. Azure Architecture

| Azure service | Decision and reason |
|---|---|
| Azure Container Apps | Host Commerce API and the two workers as separate container apps/jobs with managed identity, revision/scale controls, and lower operational burden than AKS. Scale workers based on queue/backlog metrics after measurement. |
| Azure SQL Database | Host Commerce, Inventory, and Payment databases with separate service principals/contained users and migration paths. One logical server can reduce MVP cost but is a shared infrastructure failure/capacity boundary; separate databases preserve logical ownership. |
| Azure Service Bus | Managed queues/topics/subscriptions, retries/DLQ, lock/settlement behavior, and managed identity. Configure entities for the chosen message flow and explicitly map adapter semantics. |
| Azure Key Vault | Store signing keys/connection secrets only when a managed identity cannot avoid a secret. Prefer managed identity for Azure SQL and Service Bus; secret values are never in app settings checked into Git. |
| Azure Monitor + Application Insights | Metrics, logs, traces, dashboards, alerts, and container/service diagnostics through OpenTelemetry. Apply sampling, retention, RBAC, and redaction. |
| Azure Container Registry | Store immutable versioned service images for deployment. |
| Azure Storage | Not required for MVP. Add only for a concrete durable artifact/large payload/export need; message payloads remain small. |
| Azure Functions | Not selected for core workers because Container Apps already hosts continuously running .NET workers and keeps deployment/runtime model consistent. Consider a timer-trigger function only for a clearly isolated scheduled reconciliation/cleanup task if Container Apps jobs are not a better fit. |

Use Bicep for reproducible infrastructure (resource groups, identities/roles, Container Apps environment, SQL, Service Bus, Key Vault, ACR, monitoring). Start with single-region deployment. Configure TLS, managed identities, private networking where budget/complexity permit, SQL firewall restrictions, backups/retention, and Service Bus least privilege. Document any public endpoint exception for a portfolio demo. Separate dev/test/prod subscriptions/resource groups as practical; apply tags/cost budgets and teardown guidance. Do not put infrastructure secrets in Bicep parameters or repository.

## 29. Angular Frontend

Angular + TypeScript is a thin client, not a second domain implementation. MVP screens: login/register, product selection/order submission, order list, order detail/status with polling at a restrained interval/backoff, and logout. Typed API service consumes OpenAPI-generated or documented DTOs; do not duplicate business rules/prices as authority. Show asynchronous states (accepted/pending/confirmed/failed), safe error messages, and retry with the same idempotency key for a user retry of the same intent.

Use route guards for UX only; API remains authorization authority. Keep access token in memory and refresh token in secure HttpOnly cookie under the documented CSRF model. Avoid tokens in localStorage, sensitive browser logs, and excessive polling. Basic accessibility and responsive layout are sufficient; no state-management framework unless complexity warrants it.

## 30. GitHub Project Management

Use GitHub Issues, GitHub Projects, and PRs only; do not introduce Jira.

- **Epics:** `Foundation & Architecture`, `Order Domain/API`, `Identity & Security`, `Messaging Reliability`, `Inventory`, `Payment Simulation`, `Observability & Testing`, `Docker & Local Dev`, `Azure & CI/CD`, `Angular Demo`, `Hardening & Portfolio Docs`.
- **Labels:** `type:feature`, `type:bug`, `type:docs`, `type:security`, `type:chore`; `area:commerce`, `area:inventory`, `area:payment`, `area:messaging`, `area:infra`, `area:frontend`; `priority:p0/p1/p2`; `status:blocked`; `good-first-issue` only if real.
- **Milestones:** M0 Foundation, M1 Secure Order Acceptance, M2 Reliable Inventory/Payment Workflow, M3 Observable Release Candidate, M4 Azure Portfolio Release. Phase 2/3 are future milestones, not MVP blockers.
- **Board columns:** Backlog → Ready → In Progress → In Review → Verify → Done; include Blocked field/label and owner. Limit work-in-progress (one primary issue at a time for solo work).
- Each issue has user/business outcome, scope/non-scope, acceptance criteria, dependencies, test expectations, documentation/migration/security impact, and definition of done. Split by vertical deliverable, not vague multi-week component.
- PR links issue, updates acceptance evidence, and is the unit of review. Release milestone only when all exit criteria pass.

## 31. Implementation Roadmap / Phases

Phases are ordered by dependency; phases may be split into small GitHub issues. Each phase must meet its acceptance criteria before downstream complexity is added.

### Phase 0 — Repository and Architecture Foundation

- **Objective:** Establish a clean, reproducible, reviewed project skeleton and standards.
- **Tasks:** create repo/solution structure, SDK/package pinning, `.editorconfig`, analyzers, nullable settings, license, contribution/security templates, initial CI restore/build, docs skeleton, ADR process, issue board/milestones, Compose design (do not yet add every runtime dependency unless needed).
- **Deliverables:** initial repository, buildable empty .NET solution, GitHub PR/issue templates, architecture context/container diagrams, ADR-001 and ADR-002 draft/accepted.
- **Acceptance:** clean clone builds with documented command; CI runs on PR; no secrets; paths and project references match boundaries; no business implementation yet.
- **Dependencies:** none.
- **Risks:** overdesigning abstractions. Mitigate by freezing MVP boundaries and deferring speculative infrastructure.

### Phase 1 — Domain and Contracts

- **Objective:** Encode coherent order and workflow language before persistence.
- **Tasks:** define entities/value objects, money/quantity rules, transition model, integration contract envelope/versioning, failure/reason codes; xUnit unit tests.
- **Deliverables:** domain/contracts tests and state machine documentation.
- **Acceptance:** valid/invalid transitions, totals, and stale attempts covered; contracts contain no internal entities/secrets; scope aligns with plan.
- **Dependencies:** Phase 0.
- **Risks:** modeling shipping/payment breadth prematurely; keep only order/reservation/authorization concepts.

### Phase 2 — SQL Ownership and Persistence

- **Objective:** Create per-service relational persistence and local transaction patterns.
- **Tasks:** EF Core DbContexts, migrations for three owned databases, constraints/indexes, rowversion, local SQL Compose, test database setup, data seeding for synthetic SKUs/stock.
- **Deliverables:** schema diagrams, migrations, transaction integration tests, database docs.
- **Acceptance:** fresh DB migration works; service credentials/contexts cannot access other DB; constraints reject invalid data; concurrent stock test design established.
- **Dependencies:** Phase 1.
- **Risks:** migration drift and overly shared schema; separate migrations and ownership.

### Phase 3 — Identity, Authentication, and Security Baseline

- **Objective:** Secure API access before exposing business endpoints.
- **Tasks:** ASP.NET Core Identity, login/register, JWT validation/issuance, hashed rotating refresh tokens, policies, CORS/HTTPS/rate limits, Problem Details, security logging, secret configuration, security tests.
- **Deliverables:** `SECURITY.md` baseline and auth integration tests.
- **Acceptance:** authenticated and anonymous behavior documented; cross-user resource authorization tests pass; no raw token/password in storage/logs; signing key external.
- **Dependencies:** Phases 0–2.
- **Risks:** browser refresh-token CSRF/token storage complexity; document cookie/CSRF behavior and keep Angular access token in memory.

### Phase 4 — Order Acceptance and Read API

- **Objective:** Durably accept an order request and expose useful status without pretending it is complete.
- **Tasks:** product read/price snapshot, order validation, idempotency table, order/line/history persistence, `POST/GET` endpoints, pagination, concurrency token, OpenAPI, API tests.
- **Deliverables:** secure asynchronous acceptance API with durable order record.
- **Acceptance:** same key/request returns same order, conflicting reuse returns 409; transaction commit precedes 202; ownership and validation tests pass; totals server-computed.
- **Dependencies:** Phase 3.
- **Risks:** ambiguous retry response; define canonical request hash and response semantics.

### Phase 5 — Messaging Adapters and Local Broker

- **Objective:** Establish reliable local message transport before business consumers.
- **Tasks:** RabbitMQ topology, versioned envelope, publish confirms/manual ack, retry/DLQ conventions, adapter contract tests, Compose health configuration, tracing propagation.
- **Deliverables:** local command/event round trip and messaging runbook draft.
- **Acceptance:** durable routing, controlled redelivery, malformed message quarantine, graceful shutdown, contract compatibility tested.
- **Dependencies:** Phase 2 and contract work.
- **Risks:** hiding broker differences behind an oversized abstraction; keep adapter narrowly scoped.

### Phase 6 — Transactional Outbox

- **Objective:** Make each local commit and outgoing intent atomic.
- **Tasks:** service outbox schema/index, transaction integration, bounded dispatcher/leases/backoff, publisher confirms, retry metrics, retention policy.
- **Deliverables:** reusable but minimal outbox implementation per service boundary.
- **Acceptance:** tests prove commit+broker failure preserves message; publish-then-crash can duplicate safely; concurrent dispatchers recover expired lease; poison serialization is observable.
- **Dependencies:** Phases 2 and 5.
- **Risks:** stuck leases/unbounded table; alerting and cleanup policies required.

### Phase 7 — Inbox and Consumer Foundation

- **Objective:** Safely process broker deliveries under at-least-once semantics.
- **Tasks:** inbox schema/unique key, transactional consumer pipeline, validation, ack-after-commit, retry classification, DLQ, duplicate and crash-window tests.
- **Deliverables:** documented inbox/retry/DLQ behavior.
- **Acceptance:** duplicate delivery causes one business effect; commit-before-ack crash is harmless; failed transaction is redeliverable; retention/replay policy is explicit.
- **Dependencies:** Phases 2, 5, 6.
- **Risks:** incorrectly marking messages complete before business commit; prove with integration tests.

### Phase 8 — Inventory Reservation

- **Objective:** Implement stock ownership and no-oversell reservation operation.
- **Tasks:** inventory schema/worker, atomic multi-line reserve, idempotent reserve/release commands, outcome events, SQL concurrency handling, concurrency tests.
- **Deliverables:** worker deployable and inventory runbook.
- **Acceptance:** simultaneous requests for final unit yield at most one success; multi-line reserve all-or-none; duplicate reserve/release stable; inventory unavailable/rejection classified correctly.
- **Dependencies:** Phases 1, 2, 5–7.
- **Risks:** deadlocks under multi-SKU contention; stable lock order, bounded retries, metrics.

### Phase 9 — Payment Simulator

- **Objective:** Add independent idempotent authorization without card-data scope.
- **Tasks:** payment DB/worker, provider adapter interface and deterministic simulator, operation keys, success/decline outcomes, safe diagnostics and tests.
- **Deliverables:** Payment Worker and documented limitations.
- **Acceptance:** same operation returns same result; decline is a business outcome; no payment credentials stored/logged; technical failure retries without duplicate effect.
- **Dependencies:** Phases 1, 2, 5–7.
- **Risks:** confusing decline with infrastructure failure; stable result/error taxonomy.

### Phase 10 — Persisted Order Saga and Compensation

- **Objective:** Complete the end-to-end order process across boundaries.
- **Tasks:** Commerce saga transitions, reserve→authorize→confirm, failure paths, release compensation, timeout handling/reconciliation, stale-event handling, status history.
- **Deliverables:** working end-to-end order workflow and state/failure diagrams.
- **Acceptance:** success, stock rejection, payment decline, duplicate/out-of-order outcomes, transient outages, and recovery tests pass; all accepted orders are explainable/reconcilable; no cross-service synchronous call.
- **Dependencies:** Phases 4, 6–9.
- **Risks:** timeout interpreted as definitive payment failure; operation status/idempotent reconciliation before compensation.

### Phase 11 — Observability and Operational Recovery

- **Objective:** Make normal and failed workflows diagnosable.
- **Tasks:** structured logs, OpenTelemetry context propagation, metrics, dashboards/alerts, health probes, safe DLQ/outbox inspection procedure, correlation guide.
- **Deliverables:** `ARCHITECTURE.md` observability section and runbooks.
- **Acceptance:** trace crosses HTTP and each consumer; critical backlog/retry/DLQ metrics visible; health probes do not cause restart loops; secrets absent from telemetry.
- **Dependencies:** Phases 5–10.
- **Risks:** cardinality/cost; use low-cardinality metric labels and sampling.

### Phase 12 — Test Completion and Contract Hardening

- **Objective:** Meet reliability evidence for MVP.
- **Tasks:** unit/integration/E2E/failure suite, migrations-from-empty, test fixtures, contract compatibility tests, deterministic fault injection, race tests.
- **Deliverables:** test strategy/results and repeatable local E2E command.
- **Acceptance:** critical acceptance/failure scenarios run in CI or documented gated integration job; no flaky timing dependencies; no use of EF InMemory as relational proof.
- **Dependencies:** Phases 3–11.
- **Risks:** slow integration CI; split fast required suite and explicit nightly/manual extended suite.

### Phase 13 — Docker and Local Developer Experience

- **Objective:** One documented command starts the MVP locally.
- **Tasks:** multi-stage non-root Dockerfiles, Compose for SQL/Rabbit/apps, config examples, health/readiness, seed scripts, troubleshooting guide.
- **Deliverables:** reproducible local stack.
- **Acceptance:** clean machine setup follows docs; services start and process an order; no secret in repo/image; graceful shutdown works.
- **Dependencies:** Phases 8–12.
- **Risks:** Compose masks cloud differences; adapter contract tests and Azure validation remain necessary.

### Phase 14 — CI Quality and Security Gates

- **Objective:** Prevent regressions before merge.
- **Tasks:** GitHub Actions restore/build/format/test/contract/integration/frontend, dependency/secret scans, artifact/report handling, protected main and PR templates.
- **Deliverables:** required checks and contribution documentation.
- **Acceptance:** PR cannot merge with failed required checks; permissions minimal; workflow does not need long-lived cloud secrets for CI.
- **Dependencies:** Phases 0, 12, 13.
- **Risks:** flaky external services; isolate integration services and define retries only for infrastructure startup, not test assertions.

### Phase 15 — Azure Infrastructure and Deployment

- **Objective:** Deploy the same immutable application artifacts to a documented Azure environment.
- **Tasks:** Bicep, Container Apps, Azure SQL databases, Service Bus entities, ACR, identities/RBAC, Key Vault, Monitor/App Insights, GitHub OIDC, migration and smoke jobs, rollback.
- **Deliverables:** deployment pipeline and operations/deployment docs.
- **Acceptance:** no committed credentials; workloads use least privilege; message round trip and smoke order succeed; alerts/telemetry visible; cost/teardown documented.
- **Dependencies:** Phases 13–14.
- **Risks:** cost and cloud-specific semantics; enforce budgets, use a disposable environment, verify lock/DLQ behavior differs from RabbitMQ.

### Phase 16 — Angular Demonstration Client

- **Objective:** Demonstrate the API workflow without changing backend priorities.
- **Tasks:** authentication flow, product/order form, order list/detail/status, typed client, retry with same idempotency key, accessibility basics.
- **Deliverables:** minimal frontend deployable or local dev server.
- **Acceptance:** customer can complete and observe a workflow; authorization remains server-side; no token localStorage; UI communicates asynchronous state.
- **Dependencies:** Phases 3–4 and stable API.
- **Risks:** frontend scope creep; cap screens and avoid elaborate design system.

### Phase 17 — Documentation, Portfolio, and Production Hardening

- **Objective:** Make the result defensible and maintainable for another engineer.
- **Tasks:** complete README/docs/ADRs/runbooks, demo seed scenario, architecture diagrams, performance baseline, threat review, failure demo, release/tag, known limitations.
- **Deliverables:** MVP release and portfolio walkthrough.
- **Acceptance:** another developer can run, test, deploy, and explain failure behavior; all MVP acceptance criteria met; no claims exceed evidence.
- **Dependencies:** all MVP phases.
- **Risks:** polishing docs too late; documentation updates are part of every phase DoD.

## 32. Milestones and Exit Criteria

| Milestone | Includes | Exit criteria |
|---|---|---|
| M0 — Foundation | Phase 0 | Buildable scaffold, CI baseline, boundaries and ADRs accepted. |
| M1 — Secure Order Acceptance | Phases 1–4 | Authenticated customer can submit idempotent order, receive 202, and read own pending order; SQL constraints/tests pass. |
| M2 — Reliable Workflow | Phases 5–10 | Rabbit-based E2E success and failure paths work; outbox/inbox, compensation, concurrency, and redelivery tests pass. |
| M3 — Observable Local Release Candidate | Phases 11–14 | Repeatable Compose, end-to-end tracing, required CI/security checks, runbooks, failure tests. |
| M4 — Azure Portfolio Release | Phases 15–17 | Azure deployment, managed identity, monitoring, smoke workflow, published docs/demo and known limitations. |

## 33. GitHub Issues Breakdown

Create one Epic issue per roadmap area and child issues with testable acceptance criteria. Initial suggested issue set:

1. `Epic: Foundation & Architecture`; issues: initialize .NET solution and conventions; define context/container diagrams; add ADR template and ADR-001/002; establish GitHub project/labels/templates; add baseline PR workflow.
2. `Epic: Domain & Data`; issues: model order invariants/state tests; define message envelope/contracts; create per-service DbContexts/migrations; add relational integration test harness; document keys/indexes.
3. `Epic: Identity & Security`; issues: Identity registration/login; JWT validation/issuance; refresh rotation/revocation; order ownership policy; Problem Details/security middleware; rate limit/CORS/secret setup; threat model review.
4. `Epic: Order API`; issues: product snapshot query; idempotent order submission; customer order list/detail; pagination/OpenAPI; API auth/validation tests.
5. `Epic: Messaging Reliability`; issues: Rabbit topology/adapter; outbox schema/dispatcher; inbox consumer pipeline; retry/DLQ policy; contract tests; broker outage recovery test.
6. `Epic: Inventory`; issues: reservation aggregate/schema; atomic reserve command; idempotent release; oversell concurrency test; rejection/result events.
7. `Epic: Payment`; issues: payment operation schema; simulator adapter; idempotent authorization command; decline/technical failure tests.
8. `Epic: Order Saga`; issues: persist workflow attempt; inventory result handling; payment result handling; compensation; timeout/reconciliation; duplicate/stale event tests.
9. `Epic: Observability & Operations`; issues: trace propagation; metrics; dashboards/alerts; health probes; outbox/DLQ runbooks.
10. `Epic: Delivery`; issues: Dockerfiles/Compose; CI integration tests/security checks; Bicep/Azure resources; OIDC deployment; smoke/rollback.
11. `Epic: Demo & Portfolio`; issues: Angular thin client; architecture/security/API/database/messaging docs; performance test baseline; final demo and release.

Issue sequencing uses milestone dependencies. Keep tasks independently reviewable (ideally hours to a few days), include acceptance tests, and move deferred features to Phase 2/3 rather than expanding MVP issues.

## 34. ADR List

Create short ADRs in `docs/adr/` before or when implementing the decision. Minimum planned ADRs:

1. **ADR-001 Architecture Style:** bounded service-oriented architecture with modular Commerce plus Inventory/Payment workers; alternatives monolith and broader microservices.
2. **ADR-002 Service Boundaries and Ownership:** Commerce/Inventory/Payment responsibilities and no shared database tables.
3. **ADR-003 SQL Server and Database-per-Service:** three logical databases, EF Core ownership, local/Azure placement and limitation.
4. **ADR-004 RabbitMQ vs Azure Service Bus:** Rabbit local, ASB hosted, supported abstraction and semantic differences.
5. **ADR-005 Integration Contracts and Versioning:** envelope, command/event distinction, compatibility policy.
6. **ADR-006 Transactional Outbox:** transaction, dispatcher/lease, duplicate publish and retention.
7. **ADR-007 Inbox and Consumer Transactions:** dedupe key, ack ordering, retention/replay.
8. **ADR-008 Idempotency Strategy:** HTTP, operation, and message identity roles and retention.
9. **ADR-009 Order Saga and Compensation:** Commerce orchestration, states, timeouts, ambiguity/reconciliation.
10. **ADR-010 Authentication:** Identity, JWT lifetime, refresh rotation/cookie and CSRF model.
11. **ADR-011 Authorization:** policy/claims/resource ownership.
12. **ADR-012 Inventory Concurrency:** atomic all-or-none reserve, rowversion/locking/isolation, stored procedure decision.
13. **ADR-013 Payment Simulation Boundary:** no PCI data, adapter and deterministic failure controls.
14. **ADR-014 Observability:** OpenTelemetry context/metrics/retention and Azure sink.
15. **ADR-015 Containerization and Local Stack:** Compose, non-root images, local SQL/Rabbit.
16. **ADR-016 Azure Deployment:** Container Apps, Azure SQL, ASB, identities, single region, cost/security trade-offs.
17. **ADR-017 API Versioning and Error Contract:** `/api/v1`, Problem Details, async 202 behavior.
18. **ADR-018 Stored Procedure Use:** measured inventory/outbox SQL justification or explicit EF-only decision.
19. **ADR-019 Data Retention and Replay:** inbox/outbox/idempotency/audit retention and safe operator replay.

An ADR may conclude a proposed technology is not needed. Record consequences and supersession rather than silently rewriting history.

## 35. Failure Scenarios and Recovery Matrix

| Failure | Expected behavior | Recovery / evidence |
|---|---|---|
| Commerce DB unavailable before order commit | Controlled 503/500 Problem Details; no accepted order claim; no outbox gap | Client retries with same idempotency key after recovery; DB health alert; integration test. |
| Commerce DB commit succeeds, HTTP response is lost | Client may retry; same key resolves original order | Idempotency record is in same transaction; test response-loss simulation. |
| Broker unavailable when outbox publishes | Order remains accepted/pending; pending outbox grows; no message loss | Backoff and automatic retry after recovery; alert on oldest pending age. |
| Broker accepts publish but dispatcher crashes before marking sent | Duplicate message may appear | Stable MessageId + consumer inbox/operation idempotency; crash-window test. |
| Worker database unavailable | Do not acknowledge as processed; broker redelivery/retry with backoff; readiness degraded | DB recovery; bounded retries; alert; verify no busy loop. |
| Duplicate HTTP order request | Return original order for same key/request; conflict for altered request | Unique scoped key and canonical hash; concurrent request test. |
| Duplicate broker message | No repeated business side effect | Inbox unique key plus domain operation key; duplicate count metric. |
| Out-of-order/stale order attempt event | No regression or invalid transition | Validate attempt/state/version; quarantine/alert if correlation is impossible. |
| Two customers buy final inventory unit | Exactly one reservation succeeds | Atomic conditional update/transaction and DB constraints; high-concurrency test. |
| Multi-line order has one unavailable SKU | Entire reservation rejected; no partial stock hold | Local inventory transaction rollback; integration test. |
| Inventory worker crashes before commit | Transaction rolls back; broker redelivers | Inbox/business changes are atomic; redelivery test. |
| Inventory worker crashes after commit before broker ack | Message redelivered; existing result returned/no duplicate reservation | Inbox and unique reservation; test crash window. |
| Inventory is unavailable/insufficient | Business rejection event, order fails; no payment authorization | Durable result event/outbox; status/history reason safe. |
| Payment decline | Order fails; issue release inventory compensation | Durable decline result, idempotent release, visible failure reason. |
| Payment technical timeout before known result | Do not assume decline/no authorization; retry/query same operation key or reconcile | Persist ambiguity; provider adapter contract; alert if exceeds SLA. Simulator gives deterministic lookup. |
| Payment succeeds but result event is delayed/lost at transport | Payment outbox retries; order remains pending | Outbox backlog alert and eventual result; no new operation key on retry. |
| Compensation command duplicated | Reservation released once | Idempotent release by reservation/order attempt and inbox. |
| Compensation cannot complete | Order records compensation pending/needs attention; no false claim of released inventory | Retry with backoff, alert, operator runbook; reconciliation before manual action. |
| Poison/malformed/unsupported message | No unbounded retries; move to DLQ/quarantine with safe metadata | Alert, inspect schema/version, fix consumer or controlled redrive; audit action. |
| DLQ grows | Normal work continues if isolated; operations alert | Diagnose root cause; replay only after fix with documented idempotency safety. |
| Outbox row cannot serialize | Mark/classify poison and alert; unrelated messages continue | Correct contract/data or audited repair; no silent deletion. |
| SQL deadlock/concurrency exception | Bounded retry only for safe idempotent local transaction | Jitter and metrics; persistent conflict surfaced; no whole-workflow blind replay. |
| JWT signing key unavailable/rotated incorrectly | Authentication fails closed; no insecure bypass | Restore/rotate via Key Vault, overlapping keys; alert and runbook. |
| Rate-limit abuse | 429 with retry information; service remains bounded | Rate-limit telemetry, tune limits, investigate abuse. |
| Azure region/service outage | API may be unavailable or async backlog accumulates; no active-active claim | Azure backup/restore and documented recovery; DR is future hardening. |
| Deployment introduces incompatible schema | Deployment halted/rolled back; previous app remains compatible during expand/contract | Migration gate, backup, migration test, documented rollback. |

## 36. Performance Strategy

Measure before optimizing. Establish a reproducible baseline with hardware/runtime, dataset, concurrency, warmup, and request mix documented. Primary measurements: API p50/p95/p99 and error rate; order acceptance latency; end-to-end workflow time; DB query duration/rows/read count; SQL pool saturation; reserve conflicts/deadlocks; outbox age and publish rate; queue depth/oldest age; consumer throughput/latency; retry/DLQ rates; memory/CPU per replica.

Database: inspect actual execution plans and logical reads; ensure customer history and worker scans use intentional indexes; use bounded batch sizes and page limits; avoid N+1; use async I/O and connection pooling; avoid over-fetching. Outbox dispatcher uses bounded batches and a claim index. Inventory uses stable lock order and only required rows. Do not add a cache for authoritative stock/order state. A short-lived cache for public product reads may be considered only after measured load, with explicit invalidation/staleness; it is not MVP.

Messaging: cap consumer concurrency and prefetch to DB capacity; scale workers based on oldest message age/queue depth, not CPU alone. Retries add load, so exponential backoff/jitter and retry budgets are required. Ensure poison/slow messages do not block unrelated work; broker ordering is not assumed. Connection pools and thread pool are monitored. Load test at defined target before claiming NFR compliance; tune Container Apps replicas/SQL tier from measurements.

## 37. Definition of Done

A feature is done only when all applicable criteria pass:

- Acceptance criteria implemented in the correct owning service; domain invariants and data ownership preserved.
- Unit tests for rules and integration tests for persistence/transport/security behavior; concurrency/failure tests added where relevant.
- Validation, authorization, idempotency, error behavior, and security threat implications reviewed; no client-controlled authoritative state.
- Transactions, constraints, migrations, indexes, retry behavior, and message contracts are correct and reviewed; no unsafe cross-service DB access.
- Structured logs, relevant metrics/traces, correlation propagation, and operational failure visibility exist without sensitive data leakage.
- API/OpenAPI or event contract docs/examples updated; HTTP status/error contract remains consistent.
- README/architecture/ADR/runbook/DB docs updated for material design or operational changes; explain why and trade-offs.
- Formatting, analyzers, build, all required tests, dependency/security checks, and CI pass.
- Configuration/secrets are external; local and hosted behavior is understood; Docker/deployment impact and rollback/migration safety addressed.
- PR links Issue, contains reviewable scope and evidence, passes code review, and is merged through protected `main`.
- Feature can be operated/recovered and is not considered complete merely because the happy-path code runs.

## 38. Portfolio and Interview Value

### Demonstrated Topics

C#, .NET, ASP.NET Core, Minimal APIs/controllers/MVC concepts, REST/HTTP, JSON, DataAnnotations, FluentValidation, EF Core, LINQ, DI, middleware, JWT, Identity, authorization policies, SQL Server relational modeling, constraints/indexes/query plans, stored procedure trade-offs, transactions/isolation/concurrency, RabbitMQ, Azure Service Bus, commands/events, event-driven architecture, outbox/inbox, idempotency/deduplication, at-least-once semantics, saga/compensation, retries/DLQ, OpenTelemetry, Azure Monitor/Application Insights, xUnit, integration/E2E/failure testing, Docker, Git/GitHub, Actions, CI/CD, Azure Container Apps/SQL/Key Vault, Angular/TypeScript, threat modeling, operational runbooks, and system design.

### Interview Questions the Project Must Enable

1. Why did you choose these service boundaries, and why not a monolith or more microservices?
2. What happens if Commerce commits an order but cannot publish its command?
3. How does the outbox dispatcher avoid loss, and why can it still publish twice?
4. How do inbox and business idempotency differ? What are their retention limits?
5. Why can’t you promise exactly-once processing end-to-end?
6. Walk through order submission, reservation, payment authorization, and compensation.
7. How does the system prevent overselling under concurrent purchases? Which SQL constraints/locks/isolation choices are involved?
8. What if payment succeeds but the response times out? Why is blindly releasing inventory unsafe?
9. What data belongs to Commerce versus Inventory/Payment, and how do services query across boundaries?
10. Why use an orchestrated saga here? What are the trade-offs versus choreography?
11. How do you evolve event schemas without breaking older consumers?
12. What happens when a consumer crashes before commit versus after commit but before acknowledgment?
13. How do retry, backoff, poison-message handling, and DLQ redrive work without creating a retry storm?
14. Why use RabbitMQ locally and Azure Service Bus in cloud? Which semantics differ?
15. How are customer-owned resources protected against IDOR/BOLA?
16. Where are JWT keys and refresh tokens stored? How do refresh rotation, theft detection, and CSRF work?
17. How do you prevent secrets or sensitive payment/auth data from entering logs and traces?
18. Which SQL indexes support order history and outbox claims? How would you verify with an actual execution plan?
19. When would a stored procedure be better than EF Core, and why is it not used everywhere?
20. What does `202 Accepted` mean in this API? How does a client safely retry?
21. How do you trace one order across HTTP, the broker, and independent workers?
22. Which metrics would reveal a stuck workflow before users report it?
23. How would you scale workers without violating per-order/stock correctness?
24. How does the architecture handle schema migration and rollback during deployment?
25. What does the system guarantee during broker, database, and region outages, and what does it not guarantee?
26. What would you change for a real payment provider, multiple regions, shipment, or high throughput?
27. How do you test concurrency and crash windows without relying on flaky timing?
28. Which trade-offs did you accept to keep a one-developer MVP deliverable?

## 39. Risks

| Risk | Likelihood/impact | Mitigation |
|---|---|---|
| Scope expands into full commerce/shipping platform | High/high | Enforce MVP boundary, GitHub milestone gates, defer Phase 2/3. |
| Three services create more operational work than learning value | Medium/high | Keep only three; simple contracts; local Compose; avoid extra notification/catalog services. Reassess if workflow cannot be completed on schedule. |
| RabbitMQ/ASB behavior differs | Medium/high | Narrow adapter, separate topology tests, document non-portable lock/DLQ semantics, run Azure smoke tests. |
| Payment ambiguity mishandled | Medium/high | Simulator first; stable operation keys; explicit unknown status/reconciliation; no blind compensation. |
| Outbox/inbox implementation is subtly incorrect | Medium/high | Failure-window integration tests, unique constraints, transaction review, metrics/runbooks. |
| SQL oversell race/deadlocks | Medium/high | Atomic conditional update, consistent lock order, constraints, high-contention tests, bounded retry. |
| Authentication implementation becomes custom/unsafe | Medium/high | Use ASP.NET Identity/framework primitives; threat review; token tests; documented refresh/CSRF design. |
| Azure cost exceeds portfolio budget | Medium/medium | Single region, small tiers, budget alerts, tear-down scripts, deploy only for demos. |
| CI integration tests are slow/flaky | Medium/medium | Separate fast and extended suites, container health checks, deterministic tests, avoid sleep-based assertions. |
| Shared “building blocks” become hidden coupling | Medium/medium | Restrict to transport contracts/telemetry; no shared domain/persistence; ADR review. |
| Documentation becomes stale | Medium/high | Docs included in DoD and PR template; release checklist. |
| NFR targets claimed without measurement | Medium/medium | Define workload and environment; publish results/limitations; no unsupported claims. |

## 40. Technical Trade-offs

- **Three deployables vs modular monolith:** modest operational cost buys real ownership/failure-boundary practice. Commerce remains cohesive to constrain complexity. If solo delivery stalls, the first simplification is co-hosting worker processes locally, not merging database ownership or removing reliability guarantees.
- **Orchestrated saga vs choreography:** central persisted order workflow is easier to reason about and expose to users; it concentrates workflow logic in Commerce and must remain small. Events remain facts, commands remain intent.
- **Database per service vs shared DB:** separate ownership prevents accidental cross-service transactions and enables independent evolution; local/cloud cost and migration work rise. Same Azure SQL logical server is a cost compromise, not full infrastructure isolation.
- **RabbitMQ locally and ASB in Azure:** fits requested stack and managed cloud operations; it adds adapter/semantic differences. Correctness lives in database constraints/inbox/outbox, not broker feature assumptions.
- **At-least-once instead of exactly-once:** retries improve recoverability but require dedupe and operations. This is honest and portable.
- **JWT + refresh vs opaque server session:** JWT demonstrates common API auth and distributed validation but revocation is harder; short access lifetime plus rotating refresh-session persistence limits exposure. If client/security complexity grows, reconsider BFF/cookie session model.
- **EF Core first, selective SQL:** productive and maintainable for normal operations; explicit SQL/procedure only for measured atomic reservation/outbox claims. This avoids both ORM dogma and unnecessary stored-procedure sprawl.
- **No cache for inventory:** strongest correctness and simplest invalidation; scale through DB/worker tuning before introducing stale stock reads.
- **No real payment/provider in MVP:** avoids PCI and real side effects while preserving idempotent adapter boundaries; it does not prove provider-specific webhook/reconciliation correctness.
- **No shipment/notification in MVP:** keeps one complete workflow deliverable. Terminal events provide a clean extension point without implementing speculative consumers.
- **Azure Container Apps over AKS:** lower operational overhead for this scale; less infrastructure control. Kubernetes is not justified by the project’s requirements.

## 41. Final Acceptance Criteria

The MVP is portfolio-ready only when all are true:

1. A clean clone can follow documentation to start SQL Server, RabbitMQ, Commerce API, Inventory Worker, and Payment Worker locally.
2. Authentication, refresh rotation, role/policy checks, resource ownership, rate limits, CORS, secret handling, and safe errors have automated evidence and documentation.
3. Customer can submit a valid order with an idempotency key and receive `202`; product price and state are server-controlled; duplicate/reused-key behavior is correct.
4. Order acceptance and its initial `ReserveInventory` command are atomic. Inventory and Payment own independent databases and never depend on cross-service SQL access.
5. Successful reservation + authorization produces a confirmed order; inventory rejection/payment decline produce a durable failure and appropriate compensation.
6. Concurrent attempts for final stock never oversell; simultaneous duplicate HTTP requests and duplicated messages do not duplicate effects.
7. Outbox/inbox recovery is tested at commit/publish/ack failure windows; retry is bounded; poison messages reach DLQ; replay/retention policy is documented.
8. API errors use safe Problem Details; OpenAPI explains async workflow, JWT, headers, requests, responses, and errors.
9. Logs/metrics/traces correlate a workflow across API, broker, Inventory, Payment, and Commerce; backlog/DLQ alerts and runbooks exist.
10. Unit, relational integration, contract, end-to-end, and targeted failure/concurrency tests pass in CI or documented gated validation.
11. Docker images are multi-stage/non-root and secrets are not baked in. GitHub Actions validates code/security and produces immutable images.
12. A documented Azure path deploys the same artifact using Container Apps, Azure SQL, Service Bus, managed identity/Key Vault as appropriate, and Azure Monitor/Application Insights; a smoke flow is demonstrated.
13. Angular supports only the defined demo journey and communicates asynchronous status without holding authoritative business logic.
14. Architecture, security, database, messaging, deployment, ADR, and troubleshooting docs match implementation; all MVP trade-offs and known limitations are candid.
15. NFR measurements are reported with workload/environment; no unsupported exactly-once, high-availability, or production-compliance claims are made.

## Architecture Summary

Three independently deployable backend applications form the MVP: Commerce API owns identity, product sellable snapshots, orders, and a persisted order saga; Inventory Worker owns stock/reservations; Payment Worker owns simulated authorization. Each owns a separate SQL Server database. Commerce accepts orders synchronously over secured REST and coordinates inventory/payment asynchronously using commands and outcome events. Each local state change and outgoing message is committed through an outbox; each consumer uses a transactional inbox and business idempotency. Delivery is at-least-once, and compensation/reconciliation handles cross-service failures. RabbitMQ is the local broker; Azure Service Bus is the hosted broker. Angular is a thin demo client. Azure Container Apps, Azure SQL, managed identity/Key Vault, and Azure Monitor/Application Insights provide the deployment path.

## Technology Summary

- **Backend:** C#, supported .NET SDK, ASP.NET Core, Minimal APIs (with MVC/controller concepts only where justified), HTTP/REST, JSON, DataAnnotations, FluentValidation, EF Core, LINQ, DI, middleware, ASP.NET Core Identity, JWT, policy/resource authorization, Problem Details, Swagger/OpenAPI, xUnit.
- **Data:** SQL Server locally; Azure SQL in Azure; relational constraints/indexes, transactions, rowversion, query-plan analysis; selective stored procedures only when measured/justified.
- **Messaging:** RabbitMQ local; Azure Service Bus hosted; queues/topics/subscriptions as appropriate; commands/events; transactional outbox/inbox; idempotency/deduplication; bounded retries/DLQ; at-least-once delivery.
- **Frontend:** Angular and strict TypeScript, thin API client.
- **Observability:** OpenTelemetry, structured logs, metrics/traces, Azure Monitor and Application Insights.
- **Cloud/runtime:** Azure Container Apps, Azure Container Registry, Azure Key Vault where secrets are required, managed identities, Bicep; Azure Storage and Azure Functions are not MVP dependencies.
- **Delivery:** Docker/multi-stage images, Docker Compose, Git/GitHub, GitHub Issues/Projects/PRs, GitHub Actions, OIDC Azure deployment, automated tests and security/dependency checks.

## MVP Boundary

**Included:** Commerce API + Inventory Worker + Payment Worker; three owned SQL databases; secure customer/operator authentication and authorization; product snapshots; idempotent asynchronous order submission; inventory reserve/reject; simulated payment authorize/decline; persisted order saga and release compensation; RabbitMQ local and Azure Service Bus deployment adapter; transactional outbox/inbox; retries/DLQ; OpenAPI/Problem Details; observability; unit/integration/contract/E2E/failure/concurrency tests; Docker Compose; CI/CD; documented Azure deployment; minimal Angular login/order/status flow; core architecture/security/database/messaging/runbook documentation.

**Excluded:** real payment/card data, shipping, refunds/returns, notification delivery service, broad catalog/pricing/tax/promotion, multi-tenant marketplace, event sourcing, Kubernetes, multi-region active-active, AI core feature, elaborate admin UI, and exactly-once claims. These remain deferred unless separately approved through scope and ADR updates.

## First Implementation Task

**After this plan is approved, create GitHub Issue `M0-01: Establish repository skeleton and engineering baseline`.** In one focused task, initialize the Git repository with the planned top-level documentation/source/test/infrastructure directories; pin the supported .NET SDK and central package/build settings; add `.editorconfig`, nullable/analyzer defaults, `.gitignore`, license, `CONTRIBUTING.md`, PR/Issue templates, ADR template, and a minimal GitHub Actions workflow that restores, builds, and runs the currently empty test solution. Add only empty project scaffolds matching Commerce, Inventory, Payment, and test boundaries, plus a Mermaid context/container diagram in `ARCHITECTURE.md`. Acceptance: a clean clone builds locally and in CI, no application/business behavior is implemented, no secrets are present, and ADR-001/002 are recorded as accepted or explicitly pending review. Do not begin database schemas, API endpoints, or messaging implementation in this first task.
