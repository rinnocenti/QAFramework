# C9L — Player Camera Arbitration QA

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

## Cases

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

## Install

```text
Immersive Framework QA/Camera/C9L Install Player Camera Arbitration QA
```

Then run from the Hub:

```text
Camera / C9L Player Arbitration
```

## Expected result

```text
[QA][C9L Player Camera Arbitration] PASS.
status='Passed'
cases='10'
```
