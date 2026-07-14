# P3K.4 — Prepared Player Camera Eligibility QA

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.4 Run Prepared Player Camera Eligibility Smoke
```

Expected:

```text
[P3K4_PREPARED_PLAYER_CAMERA_ELIGIBILITY_SMOKE]
status='Passed'
cases='25'
```

The smoke proves:

```text
internal context surface
matching occupancy/input Slot rosters
vacant occupancy rejection
unbound input rejection
live stale-input rejection
required camera cannot skip
optional skip and idempotency
skip release token guard and idempotency
required explicit camera authoring eligibility
eligibility idempotency
Actor identity rejection
authoring hierarchy rejection
PlayerComposer-backed rig rejection
rig/authoring target mismatch rejection
required missing rig rejection
eligibility release token guard and idempotency
re-eligibility creates a new token
stale eligibility token rejection
release-all
live stale-occupancy rejection
```

This cut does not require a Camera Output, Unity Camera, CinemachineBrain or
published camera request. P3K.5 owns that integration.
