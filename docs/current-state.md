# Current State — Event-Driven Order Processing Platform

> **Live progress tracker.** Owned and updated exclusively by the `architect` agent after every completed task and every phase transition. All other agents must read this file at the start of a task and must never write to it. Record only verified facts — never invent progress. When this file and the repository disagree, the repository wins and the discrepancy must be reported.

_Last updated: 2026-10-07 — by architect (M0-01B completion)_

## Project stage

**PHASE 0 IN PROGRESS — scaffold is verified; no business functionality has been implemented.**

The repository contains the planning and agent-configuration baseline plus the empty .NET solution/project scaffolds. The Git baseline is pushed to `origin/main`. The scaffold contains host boilerplate and placeholder tests only; no domain behavior, endpoints, database schemas, messaging implementation, infrastructure, or CI pipeline exists. A clean clone of `origin/main` restored, built, and passed the eight scaffold tests.

## Current phase

**Phase 0 — Repository and Architecture Foundation** (Milestone M0) is in progress. The next phase after Phase 0 exit criteria are verified is:

- **Phase 1 — Domain and Contracts**

## Current task

**M0-01B — Phase 0 GitHub and CI foundation is complete (Reviewer PASS).** This commit records the workflow/templates and tracker state; no business functionality was implemented.

- **Next task: M0-01C — GitHub project-management foundation.** Establish the PLAN-defined issue labels, milestones, and project-board structure. The first actual GitHub Actions run and the license decision remain outstanding; do not declare Phase 0 complete until the plan's exit criteria are evidenced.

## Completed phases

| Phase | Milestone | Status | Completed (verified) | Notes |
|---|---|---|---|---|
| Phase 0 | M0 — Foundation | In progress | — | Git baseline complete; scaffold present; Phase 0 exit criteria not yet met. |

## Completed tasks

| Task ID | Description | Review verdict | Date | Evidence (tests/artifacts/docs) |
|---|---|---|---|---|
| M0-GIT-01 | Initialize Git, add .NET `.gitignore`, create baseline commit, configure `origin` | PASS | 2026-10-07 | Initial commit `9cb8ec7a808027cc66a5d086ea28f98461766d34`; reviewer PASS; branch `main`; remote configured; baseline subsequently pushed. |
| M0-01A-REVIEW | Independently review existing empty .NET scaffold and verify clean-clone build/test | PASS | 2026-10-07 | Reviewer PASS; clean clone at `ddf625c606f2b1d787247130018a4479096856da`; `dotnet restore`, `dotnet build --no-restore` (0 warnings/errors), and `dotnet test --no-build` (8 passed). |
| M0-01B | Add Phase 0 GitHub PR/issue templates and minimal PR CI | PASS | 2026-10-07 | Infra validation and final Reviewer PASS; actionlint + issue-frontmatter parsing; `dotnet restore`, `dotnet build` (0 warnings/errors), `dotnet test` (8 passed); exact task changes are in this commit. |

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
- Git repository is on `main`; `origin` is `https://github.com/Raf-Hs/event-driven-order-processing-platform.git`; pushed baseline `ddf625c606f2b1d787247130018a4479096856da` was verified equal to `origin/main` at cycle start. The M0-01A tracker commit and this M0-01B commit remain local and unpushed; history is preserved.
- No license choice is recorded; do not add a license until the repository owner decides.
- The six local development settings/launch-profile files are intentionally ignored and absent from a clean clone; the solution builds and tests without them.
- GitHub Actions workflow is configured and locally validated, but no live GitHub Actions run has been observed yet. This remains required evidence for Phase 0 exit.
- GitHub issue labels, milestones, and Project board have not yet been configured in repository settings.

## Known blockers

- Repository owner license choice is still required before adding `LICENSE`.
- GitHub-side project setup and a first live CI run remain outstanding before Phase 0 can be declared complete.

## Next task

1. **M0-01C — GitHub project-management foundation:** configure the PLAN-defined labels, milestones, and board structure; verify GitHub issue forms can use the labels.
2. Run/verify the first GitHub Actions workflow on a PR before Phase 0 exit; obtain the owner's license choice and accept/reject ADR-001/002. Do not begin Phase 1 until all Phase 0 exit criteria in `PLAN.md` are evidenced.

## Update protocol (for the architect)

After every completed task: add a row to *Completed tasks* with review verdict and evidence; update *Current task* / *Next task*; record new ADRs, constraints, or blockers.
After every phase transition: add a row to *Completed phases* with milestone and verification evidence; set *Current phase*; confirm all phase exit criteria from `PLAN.md` were met before advancing.
Keep every entry factual and verifiable against the repository. Update the _Last updated_ line on every change.
