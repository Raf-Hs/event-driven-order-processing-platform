<!--
Guidance for the Event-Driven Order Processing Platform (PLAN.md §24 Git and
Pull Requests, §30 GitHub Project Management, §37 Definition of Done). This
HTML comment does not render in the PR description. Delete instruction lines
inside sections as you fill them in.
-->

## Linked issue

Closes #<!-- issue number — required -->

<!-- Every PR links a GitHub issue — no exceptions, including docs and chore
PRs (PLAN.md §24/§30: "Every PR links a GitHub Issue"). If no suitable issue
exists yet, first create a small, scoped issue (outcome, acceptance criteria,
test expectations per PLAN.md §30) and link it here with a closing keyword
(Closes/Fixes/Resolves #N). Solo portfolio work still uses PRs with a
self-review checklist. -->

## Summary

<!-- What does this PR do, and why? 2–4 sentences. -->

## Type of change

<!-- Check all that apply. -->

- [ ] Feature (new behavior)
- [ ] Bug fix
- [ ] Tests only
- [ ] Docs only
- [ ] Refactor (no behavior change)
- [ ] Chore (tooling, dependencies, CI)

## Scope and boundaries

<!-- Name the owning service/area and confirm boundaries hold (PLAN.md §10/§11,
CONTRIBUTING.md): database-per-service, no cross-service data access, no
synchronous service-to-service calls in the order workflow, outbox/inbox and
at-least-once semantics unchanged. -->

- Owning area: <!-- exactly one plan-defined label (PLAN.md §30): `area:commerce`
  / `area:inventory` / `area:payment` / `area:messaging` / `area:infra` /
  `area:frontend`. There is no `area:docs`: a docs-only PR carries `type:docs`
  and names the area it documents. There is no `area:buildingblocks`: a
  Messaging.Abstractions / Messaging.Contracts / Observability change
  (PLAN.md §24) maps to the area it serves — usually `area:messaging` or `area:infra`. -->
- Boundaries preserved: <!-- yes — how, or "n/a — no service code touched" -->

## Risk and behavior

<!-- What could break, what deploy/config/migration impact exists, and how a
reviewer should think about the blast radius. -->

## Test evidence

<!-- Required: actual commands run and observed results (pass counts, relevant
output). "It works on my machine" is not evidence. Include new/changed test
names and, for reliability work, which failure windows are covered. -->

```
Commands:

Results:
```

- [ ] Unit tests pass
- [ ] Integration/contract tests pass (or explain n/a — e.g. scaffold-only PR
      before Phase 2 infrastructure exists)
- [ ] CI checks pass

## Docs and contract implications

- [ ] No doc/contract change needed
- [ ] Docs updated: <!-- README / ARCHITECTURE / API.md / database / messaging / runbook / ADR -->
- [ ] New ADR or ADR update: <!-- link -->
- [ ] Message contract / OpenAPI impact: <!-- describe + compatibility handling -->

## Secrets and configuration

- [ ] No secrets, credentials, connection strings, or real customer/payment
      data anywhere in the diff (code, config, tests, logs, docs); local
      configuration uses environment variables / user-secrets only

## Definition of Done checklist

- [ ] Correct owning service; domain invariants and data ownership preserved
- [ ] Tests added/updated for rules and behaviors changed (unit + integration
      where applicable; concurrency/failure tests where relevant)
- [ ] Formatting, analyzers, build, and CI pass
- [ ] Docs updated for material design or operational changes
- [ ] PR links an issue and scope is reviewable in one sitting
- [ ] No future-phase scope creep

<!-- Full authority: PLAN.md §37. This checklist is a reminder, not a
replacement. -->
