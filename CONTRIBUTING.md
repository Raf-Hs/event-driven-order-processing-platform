# Contributing

Conventions for working on the Event-Driven Order Processing Platform. The authoritative source of
truth for scope, architecture, and acceptance criteria is [`PLAN.md`](PLAN.md); this file summarizes
the day-to-day engineering rules from PLAN.md §24 and the Definition of Done (§37).

## Setup

1. Install the .NET SDK pinned in [`global.json`](global.json) (currently `10.0.401`,
   `rollForward: latestFeature` within the 10.0 release line). Running `dotnet --version` in the
   repository root must resolve to a 10.0 SDK.
2. From the repository root:

   ```bash
   dotnet restore OrderProcessingPlatform.sln
   dotnet build OrderProcessingPlatform.sln
   dotnet test OrderProcessingPlatform.sln
   ```

   No databases, broker, or containers are needed yet; the local stack is introduced in Phase 13.

## Solution and project boundaries

- Three backend deployables with strict ownership (PLAN.md §1–§11):
  **Commerce** (`src/Commerce`: API + Application/Domain/Infrastructure),
  **Inventory** (`src/Inventory`: Worker + Application/Domain/Infrastructure),
  **Payments** (`src/Payments`: Worker + Application/Domain/Infrastructure).
- Each service owns **its own database exclusively**. Never reference another service's projects,
  entities, or DbContext from a different service. `src/BuildingBlocks` may contain only
  cross-cutting transport/telemetry primitives and versioned integration contracts — never domain
  entities or shared business logic.
- Inter-service workflow communication is asynchronous via broker commands/events; no synchronous
  service-to-service calls in the order workflow.
- Test boundaries mirror the service boundaries (`tests/` layout per PLAN.md §24): unit tests for
  domain/application rules, integration tests for persistence/transport/security behavior per
  service, plus `Contracts.Tests` and `EndToEnd.Tests`.

## Language and code rules

Enforced at build time (warnings are errors; style is enforced via `EnforceCodeStyleInBuild`):

- `global.json` pins the SDK; `Directory.Build.props` enables nullable reference types, implicit
  usings, deterministic builds, `latest-recommended` analyzers, and `TreatWarningsAsErrors`.
- `Directory.Packages.props` uses **Central Package Management**: every `PackageReference` version is
  declared once there; project files must not carry inline `Version=` attributes.
- [`.editorconfig`](.editorconfig) defines formatting, code style, and naming rules (interfaces
  `I`-prefixed; no generic `Manager`/`Helper` type names). Test projects use the xUnit
  `Method_Condition_Expectation` naming convention (CA1707 lifted only under `tests/`).
- C#: PascalCase types/namespaces, camelCase locals/parameters, `async` methods end in `Async`,
  `CancellationToken` propagates through all I/O.
- Money is **integer minor units plus currency**; timestamps are UTC (`DateTimeOffset` instant
  semantics); IDs are opaque stable identifiers.
- Domain names are business language; no infrastructure leakage into domain projects; DTOs and
  integration contracts are explicit and versioned.

## Local configuration and secrets

- Never commit secrets or environment-specific configuration. Local dev configuration uses
  environment variables / `dotnet user-secrets` (per-project IDs are added when a project actually
  requires configuration — none do yet) and `.env` files that are Git-ignored; committed
  `.env.example` files contain names and fake values only.
- Anything that looks like a credential blocks review, even for throwaway local use.

## Git, issues, and pull requests

- `main` is protected and releasable; short-lived feature branches
  (`feature/order-idempotency`, `fix/inventory-race`, `docs/adr-004-broker`); no long-lived
  `develop` branch.
- Conventional Commits prefixes are recommended (`feat:` `fix:` `test:` `docs:` `refactor:`
  `chore:`) with meaningful descriptions.
- Every PR links a GitHub Issue, states behavior/risk, includes tests/docs/migration or contract
  implications, and passes required CI. Solo work still uses PRs with a self-review checklist.
- GitHub epics, labels, milestones, and board columns follow PLAN.md §30; do not introduce other
  trackers.

## Architecture Decision Records

Non-obvious or irreversible decisions are recorded in `docs/adr/` before or while implementing them
(template and process: [`docs/adr/README.md`](docs/adr/README.md)). An ADR may not contradict
`PLAN.md`; changing a plan-level decision requires the architect's approval and an ADR. The planned
ADR inventory is PLAN.md §34.

## Definition of Done (summary — PLAN.md §37 is authoritative)

A feature is done only when all applicable items pass: correct owning service and preserved
boundaries; unit + integration tests (concurrency/failure tests where relevant); validation,
authorization, idempotency, and threat implications reviewed; outbox/inbox/transactional correctness;
structured logs/metrics/traces without sensitive data; contract/OpenAPI and docs (README /
ARCHITECTURE / ADR / runbook / DB) updated; formatting, analyzers, build, tests, security/dependency
checks, and CI green; configuration/secrets external; PR reviewed and merged through protected
`main`. A happy path alone is never "done".

## Current pending human decisions

- **License**: no `LICENSE` file exists because no licensing decision has been recorded for this
  repository. This is reported to the human owner; do not guess or add a license until decided.
- ADR-001 and ADR-002 are drafted from decisions already fixed in `PLAN.md` and are **pending
  architect/human approval** per the Phase 0 acceptance criteria.
