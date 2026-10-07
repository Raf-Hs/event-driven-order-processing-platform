# Current State — Event-Driven Order Processing Platform

> **Live progress tracker.** Owned and updated exclusively by the `architect` agent after every completed task and every phase transition. All other agents must read this file at the start of a task and must never write to it. Record only verified facts — never invent progress. When this file and the repository disagree, the repository wins and the discrepancy must be reported.

_Last updated: 2026-10-07 — by architect (M0-01A review completion)_

## Project stage

**PHASE 0 IN PROGRESS — scaffold is verified; no business functionality has been implemented.**

The repository contains the planning and agent-configuration baseline plus the empty .NET solution/project scaffolds. The Git baseline is pushed to `origin/main`. The scaffold contains host boilerplate and placeholder tests only; no domain behavior, endpoints, database schemas, messaging implementation, infrastructure, or CI pipeline exists. A clean clone of `origin/main` restored, built, and passed the eight scaffold tests.

## Current phase

**Phase 0 — Repository and Architecture Foundation** (Milestone M0) is in progress. The next phase after Phase 0 exit criteria are verified is:

- **Phase 1 — Domain and Contracts**

## Current task

**M0-01A scaffold review is complete (PASS).** The scaffold was present in the pushed baseline and has been independently reviewed and verified from a clean clone.

- **Next task: M0-01B — Phase 0 GitHub and CI foundation.** Add the planned minimal PR validation workflow and GitHub contribution templates. No GitHub Actions have been created yet. The license decision remains with the repository owner.

## Completed phases

| Phase | Milestone | Status | Completed (verified) | Notes |
|---|---|---|---|---|
| Phase 0 | M0 — Foundation | In progress | — | Git baseline complete; scaffold present; Phase 0 exit criteria not yet met. |

## Completed tasks

| Task ID | Description | Review verdict | Date | Evidence (tests/artifacts/docs) |
|---|---|---|---|---|
| M0-GIT-01 | Initialize Git, add .NET `.gitignore`, create baseline commit, configure `origin` | PASS | 2026-10-07 | Initial commit `9cb8ec7a808027cc66a5d086ea28f98461766d34`; reviewer PASS; branch `main`; remote configured; baseline subsequently pushed. |
| M0-01A-REVIEW | Independently review existing empty .NET scaffold and verify clean-clone build/test | PASS | 2026-10-07 | Reviewer PASS; clean clone at `ddf625c606f2b1d787247130018a4479096856da`; `dotnet restore`, `dotnet build --no-restore` (0 warnings/errors), and `dotnet test --no-build` (8 passed). |

## Active decisions (ADRs)

| ADR | Title | Status |
|---|---|---|
| ADR-001 | Architecture Style | Proposed — pending architect/human acceptance |
| ADR-002 | Service Boundaries and Ownership | Proposed — pending architect/human acceptance |

## Known constraints

- `PLAN.md` is the source of truth; no architectural decision may contradict it without an approved ADR.
- MVP boundaries are fixed: three deployables (Commerce API, Inventory Worker, Payment Worker), database-per-service, RabbitMQ local / Azure Service Bus in Azure, orchestrated saga, outbox/inbox, at-least-once + idempotent consumers.
- Explicitly excluded: Kubernetes, event sourcing, CQRS infrastructure, generic workflow engine, multi-region, real payments/card data, shipment, notification service (Phase 2), cancellation flows (Phase 2), Azure Storage/Functions as MVP dependencies.
- OpenCode multi-agent structure is in place: `architect` (primary orchestrator) delegating to `implementer`, `reviewer` (read-only), `infra`, `escalation`; subagent depth 1 (workers cannot spawn workers).
- Git repository is on `main`; `origin` is `https://github.com/Raf-Hs/event-driven-order-processing-platform.git`; baseline commit `ddf625c606f2b1d787247130018a4479096856da` was verified equal to `origin/main` during this task.
- No license choice is recorded; do not add a license until the repository owner decides.
- The six local development settings/launch-profile files are intentionally ignored and absent from a clean clone; the solution builds and tests without them.

## Known blockers

- None recorded.

## Next task

1. **M0-01B — Phase 0 GitHub and CI foundation:** add minimal PR validation workflow and GitHub contribution templates through INFRA, then independent REVIEWER pass.
2. Resolve the human license choice and accept/reject the proposed ADR-001/002 before declaring M0 complete. Do not begin Phase 1 until all Phase 0 exit criteria in `PLAN.md` are evidenced.

## Update protocol (for the architect)

After every completed task: add a row to *Completed tasks* with review verdict and evidence; update *Current task* / *Next task*; record new ADRs, constraints, or blockers.
After every phase transition: add a row to *Completed phases* with milestone and verification evidence; set *Current phase*; confirm all phase exit criteria from `PLAN.md` were met before advancing.
Keep every entry factual and verifiable against the repository. Update the _Last updated_ line on every change.
