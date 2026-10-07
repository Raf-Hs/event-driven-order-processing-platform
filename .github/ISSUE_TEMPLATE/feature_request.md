---
name: Feature request
about: "Propose behavior or work with testable acceptance criteria (PLAN.md §30 label: type:feature)"
title: "feat: "
labels: ["type:feature"]
---

## User / business outcome

<!-- As a [role], I want [capability], so that [value]. Required — issues are
written as outcomes, not vague tasks (PLAN.md §30). -->

## Scope

- **Includes:** <!-- what this issue delivers, split by vertical deliverable,
  not multi-week components -->
- **Excludes:** <!-- explicitly out of scope; future-phase items stay deferred -->

## Acceptance criteria

<!-- Testable statements. Definition of Done: PLAN.md §37. Each criterion must
be checkable by a reviewer from evidence, not opinion. -->

- [ ] <!-- ... -->
- [ ] <!-- ... -->

## Dependencies

<!-- Issues/phases that must complete first (PLAN.md §31 phase order). -->

## Area and milestone

- Area: <!-- exactly one plan-defined label: `area:commerce` / `area:inventory`
  / `area:payment` / `area:messaging` / `area:infra` / `area:frontend`
  (PLAN.md §30) → add it at triage. There is no `area:docs`: docs-only work
  carries `type:docs` and names the area it documents. There is no
  `area:buildingblocks`: a Messaging.Abstractions / Messaging.Contracts /
  Observability change (PLAN.md §24) maps to the area it serves — usually
  `area:messaging` or `area:infra`. -->
- Milestone: <!-- M0 Foundation · M1 Secure Order Acceptance · M2 Reliable
  Inventory/Payment Workflow · M3 Observable Release Candidate · M4 Azure
  Portfolio Release -->

## Test expectations

<!-- Which unit / integration / contract / E2E / failure / concurrency tests
will prove this works? Reliability-relevant behavior must state the failure
window covered (at-least-once, duplicate delivery, crash windows). -->

## Docs / migration / security impact

- Docs: <!-- README / ARCHITECTURE / database / messaging / runbooks / ADR -->
- Migrations/schema: <!-- expected impact, or "none expected" -->
- Security/secrets: <!-- auth, PII/payment-data, secret-handling implications,
  or "none expected". No credential material in this issue. -->
