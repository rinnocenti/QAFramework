# C9R — Camera Override Authority QA

The canonical authority contract is:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Route and Activity lifecycle entry only makes their override available. The QA
must explicitly call request/release and assert idempotence, owner-exit cleanup,
invalid configuration blocking and restoration of the next valid request.

For session-owned output validation, configure exactly one
`CameraOutputSessionBinding` and one `SessionCameraOverrideBinding` in the QA
`UIGlobal` scene. The Session request must be active only while the transition
cover is closed.

The QA Hub exposes the canonical route as **C9R Camera Override Authority**.
The arbitration scene deliberately contains no Unity Camera, CinemachineBrain or
CameraOutputSessionBinding: Player, Activity and Route receive the persistent
output through `CameraOutputSessionInjectionRuntime`.

The smoke waits for injected-output readiness, then verifies `player-default`,
`activity-request`, `route-request`, `session-request`, the three restoration
releases, duplicate request/release, and Activity/Route lifecycle cleanup. Its
only final success line is `[QA][C9R Camera Override Authority] PASS.`.

Install or repair the complete C9R surface before the smoke:

```text
Immersive Framework QA/Camera/C9R Install Camera Override Authority QA
```
