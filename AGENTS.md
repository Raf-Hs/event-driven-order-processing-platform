# AGENTS.md — Event-Driven Order Processing Platform

Instructions for all AI agents working in this repository.

## Read first, every task

1. `PLAN.md` — the single source of truth for architecture, service boundaries, database ownership, messaging, reliability (outbox/inbox/idempotency/at-least-once), saga, security, testing, infrastructure, CI/CD, Azure, phases, and acceptance criteria. Never contradict it; changes require an ADR approved through the architect.
2. `docs/current-state.md` — the live tracker of current phase, current task, completed work, decisions, and blockers.
3. `.opencode/prompts/project-context.md` — condensed shared context: architecture snapshot, agent roster, orchestration flow, standing constraints.

## Agent structure

- `architect` is the primary orchestrator and the default agent (`opencode.json`). All delegation flows through it.
- Workers (`implementer`, `reviewer`, `infra`, `escalation`) run as subagents with depth 1 — they cannot spawn further subagents.
- `reviewer` is strictly read-only (edit/write denied by permissions).
- One worker per feature at a time; every implementation and infrastructure change goes through review.

## Non-negotiables

- Database-per-service; no cross-service data access or shared business logic; no synchronous service-to-service calls in the order workflow.
- Transactional outbox + transactional inbox + idempotent consumers + at-least-once delivery. Never claim exactly-once.
- No future-phase scope creep; no secrets in code, config, images, workflows, logs, or docs.
- Phase 0 is in progress: the empty .NET solution/project scaffold and Git baseline are present, but no business/domain functionality has been implemented. Read `docs/current-state.md` for the verified current task. Do not begin any work without an explicit task brief from the architect.
