# RESET-035-B — Integration QA

## Coverage added

- QA-NEW-002 now authors one Activity-owned Resettable scene per Activity definition. Its certification checks the Activity owner identity/scope, a valid runtime subject id, and one registered capability for A and B.
- QA-NEW-002 holds B at the existing `WaitVisible` readiness gate after commit, then checks that A has been released and B remains registered. It first submits B with a deliberately invalid required Activity contribution; the Activity transaction registers B's materialized Resettable before validating the invalid binding, then fails before commit. The scenario captures B's Resettable during scene load and checks rollback evidence after the failed request, alongside A's unchanged active state and owner registration.
- QA-NEW-003 reuses the existing Route A/B primary scenes and replacement cycle. It checks the matching Route owner, valid subject id and one capability, and verifies that replacing a Route clears the previous Resettable registration while retaining the new Route registration.

## Evidence boundary

The scenarios use the public evidence exposed by `Resettable`. They prove registration and cleared registration evidence on the authored content units, but do not enumerate the internal registry's complete owner buckets. Owner release idempotency remains a focused helper-level test; the Route replacement scenario covers owner isolation through the public lifecycle.

## Execution status

**PASS — Play Mode 2026-10-06.** Both extended scenarios were executed in Unity against the current integration boundary.

`QA-NEW-002` completed the controlled Activity readiness/rollback cycle, proved the pending gate, issued release, observed Activity B release, restored baseline A, and ended with `cleanup='BaselineRestored'`, `firstDivergence=''`, `cleanupIssue=''`.

```text
[QA-NEW-002] status='Passed' verdict='PASS' submittedB='1' completedB='1' submittedA='1' completedA='1' pendingProved='True' releaseIssued='True' activityBReleased='True' participantState='Completed' baselineANormalized='True' baselineRestored='True' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''.
```

`QA-NEW-003` completed Route A → B → new A with successful public requests, exact exit/releasing/unload/load/available/enter observations in both directions, surviving execution owner, and baseline restoration.

```text
[QA-NEW-003] status='Passed' verdict='PASS' submittedB='1' completedB='1' succeededB='1' exitA='1' releasingA='1' unloadA='1' loadB='1' availableB='1' enterB='1' submittedA='1' completedA='1' succeededA='1' exitB='1' releasingB='1' unloadB='1' loadA='1' availableA='1' enterA='1' ownerSurvived='True' baselineRestored='True' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''.
```

This closes the RESET-035-B integration execution gate represented by these two scenarios. Focused helper-level owner-release idempotency remains separate evidence and is not relabeled by this run.
