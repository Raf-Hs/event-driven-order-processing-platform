---
name: Bug report
about: "Report incorrect or failing behavior (PLAN.md §30 label: type:bug)"
title: "fix: "
labels: ["type:bug"]
---

<!--
Every issue — including bugs — carries a user/business outcome, scope and
non-scope, testable acceptance criteria, dependencies, test expectations, and
docs/migration/security impact, with the Definition of Done from PLAN.md §37
as its exit gate (PLAN.md §30). Fill in every section before it can move to
Ready; "none expected" is an acceptable answer where applicable.

Never paste secrets, connection strings, tokens, or real customer/payment
data into issue fields, logs, or screenshots — redact them first
(CONTRIBUTING.md "Local configuration and secrets").
-->

## Describe the bug

<!-- A clear, concise description of what the bug is and its user/business
impact: who is affected and which correct outcome is prevented
(PLAN.md §30: every issue states a user/business outcome). -->

## Area

<!-- Exactly one plan-defined area: `area:commerce` · `area:inventory` ·
`area:payment` · `area:messaging` · `area:infra` · `area:frontend`
(PLAN.md §30). Add it during triage (labels cannot be pre-selected reliably
until they exist in repository settings). There is no `area:docs` and no
`area:buildingblocks`: a docs-only bug carries `type:docs` and still names the
area its documentation is about, and a BuildingBlocks change (Messaging.Abstractions /
Messaging.Contracts / Observability, PLAN.md §24) carries the area it serves —
typically `area:messaging` or `area:infra`. -->

## Steps to reproduce

1. <!-- ... -->
2. <!-- ... -->
3. <!-- ... -->

## Expected behavior

<!-- What the code/PLAN/docs say should happen, with a reference (section,
test, or contract) when possible. -->

## Actual behavior

<!-- What happens instead: errors, wrong state, failed test names/output
(redacted). Include the branch/commit and `dotnet --version` where relevant. -->

## Impact

- [ ] Blocking (breaks acceptance criteria or the order workflow)
- [ ] Degraded (works around possible)
- [ ] Cosmetic

## Scope

- **Includes:** <!-- the fix this issue must deliver: root cause to address,
  owning service/area, and the regression coverage that comes with it — one
  reviewable unit, not a vague multi-week cleanup -->
- **Excludes:** <!-- explicitly out of scope; related-but-separate defects and
  future-phase items get their own issues -->

## Acceptance criteria

<!-- Testable statements a reviewer can check from evidence, not opinion
(PLAN.md §30). Typically: the repro no longer occurs, and a named regression
test proves it. -->

- [ ] <!-- e.g. "Reproducing steps 1–N no longer yields <actual behavior>; regression test <name> fails before and passes after the fix" -->
- [ ] <!-- ... -->

## Dependencies

<!-- Issues/PRs/phases that must complete first or that block this fix
(PLAN.md §31 phase order). "None — unblocked" if there are none. -->

## Test expectations

<!-- The failing test that demonstrates the bug (name/output, if any) and the
unit / integration / contract / E2E / failure / concurrency tests that will
prove the fix. Reliability-relevant bugs must state the failure window covered
(at-least-once, duplicate delivery, crash windows). -->

## Docs / migration / security impact

- Docs: <!-- README / ARCHITECTURE / API.md / database / messaging / runbook / ADR, or "none expected" -->
- Migrations/schema: <!-- expected impact, or "none expected" -->
- Security/secrets: <!-- auth, PII/payment-data, secret-handling implications, or "none expected". No credential material in this issue. -->

## Additional context

<!-- Logs, links to related issues/PRs/milestones (M0–M4), failing CI runs. -->

<!--
Done means PLAN.md §37 in full — correct owning service, tests with evidence,
no boundary/semantics drift, docs updated where material, CI green — not just
a happy path that no longer throws. The linked PR repeats the self-review
checklist.
-->
