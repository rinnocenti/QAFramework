# QA-NEW-004 — Session Camera Continuity

**Status: CERTIFIED / PASS — Play Mode 9/9 on 2026-10-06.**

QA-NEW-004 verifies that one targetless, Session-scoped Camera Assignment and
its physical Output remain available while public Route and Activity operations
replace their owned content.

## Current contract

- GameApplication owns one Camera Output and one Session-scoped Assignment.
- The Assignment has no Player membership and requires no Actor Subject.
- The Output owns an explicit Fixed Fallback Camera.
- Player Session remains disabled.
- Route and Activity transitions do not select, replace or release Session
  Camera ownership.

The current scenario covers Route A -> B -> A -> C -> D -> E -> A, Activity
D-A -> D-B, and Activity E-A -> E-C. After each successful public request it
checks that the same initialized Session Output probe, runtime Output token,
Output identity and authored Assignment identity remain present. Route primary
scenes and required Activity content scenes are checked at their transition
boundaries.

This migrates the former continuity intent. Camera A -> Camera C replacement
through Route/Activity Presentation selections was removed by IF-ADR-038 and is
not reproduced through a hidden request or selection surface.

## Run

1. Confirm `GameApplication_QaNew004` is the active QA Game Application.
2. Open `Assets/QA-NEW-004/Scenes/QA_NEW_004_Persistent.unity`.
3. Enter Play Mode.
4. Wait for the terminal log:

```text
[QA-NEW-004] status='Passed' verdict='PASS' cases='9/9'
```

Any missing output, invalid Fallback, Assignment drift, failed lifecycle
request, or output recreation fails/blocks the run with the first causal issue.

## Executed certification — 2026-10-06

```text
[QA-NEW-004] status='Passed' verdict='PASS' cases='9/9' outputId='94040000000000000000000000000001' outputToken='233dacc43f4140f5b3a6164cac0cb278' assignment='94040000000000000000000000000005' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''.
```

Accepted evidence: the same initialized Session Output and authored Session-scoped Assignment survived the complete Route/Activity matrix, zero-Player coverage remained valid, and cleanup restored the baseline with no causal divergence or cleanup issue.
