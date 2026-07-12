# C9L — Player Camera Arbitration QA

Status: Closed — functional smoke PASS; fixture hygiene revalidation pending
Executed: 2026-07-11

## Objective

Prove the canonical single-output succession:

```text
Route
-> Local Player
-> Activity
-> Local Player
-> Route
```

## Runtime path under test

```text
RouteCameraRequestBinding
LocalPlayerCameraRequestBinding
ActivityCameraRequestBinding
CameraOutputSession
CameraOutputContext
CameraOutputRigApplicator
```

## Recorded result

```text
[QA][C9L Player Camera Arbitration] PASS.
status='Passed'
cases='10'
```

Completed cases:

```text
route-player-activity-enter
invalid-player-binding-blocked
activity-exit-restores-player
player-publish-idempotent
player-release-restores-route
player-release-idempotent
player-reeligible-overrides-route
activity-overrides-player
activity-release-restores-player
final-player-release-restores-route
```

Route entry and return to QA Hub succeeded with `blockingIssues='0'`. The
invalid Player binding was explicitly blocked because its eligibility scope id
was missing. Activity release restored Player, Player release restored Route,
and re-eligibility restored Player as winner.

## Fixture hygiene correction

The functional PASS ran while the C9L scene also contained the historical
`QaC9MActivityRouteTeardownProbe`, which emitted unrelated C9O/C9M teardown
diagnostics. That probe was not a dependency of the C9L fixture and was removed
from `QA_PlayerCameraArbitration.unity` only. The C9L installer does not add it;
C9O retains the probe in its own scene and installer.

The functional result remains PASS because all ten C9L cases completed. Repeat
the smoke after cleanup to confirm `cases='10'`, Hub return with
`blockingIssues='0'`, and absence of `[QA][C9M Activity Route Teardown]` logs.

## Install

```text
Immersive Framework QA/Camera/C9L Install Player Camera Arbitration QA
```

Then run from the Hub:

```text
Camera / C9L Player Arbitration
```
