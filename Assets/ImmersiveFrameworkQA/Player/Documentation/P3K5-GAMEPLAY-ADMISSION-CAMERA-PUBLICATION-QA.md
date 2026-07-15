# P3K.5 — Gameplay Admission and Camera Publication QA

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.5 Run Gameplay Admission and Camera Publication Smoke
```

Expected:

```text
[P3K5_GAMEPLAY_ADMISSION_CAMERA_PUBLICATION_SMOKE]
status='Passed'
cases='28'
```

The smoke builds the real technical chain:

```text
P3J preparation evidence
-> P3K.2 occupancy
-> P3K.3 input binding
-> P3K.4 camera eligibility
-> P3K.5 admission/publication
```

It verifies:

```text
SkippedOptional readiness without output
Gate-derived GameplayReady refresh
required camera output failure rollback
CameraRequest publication and Cinemachine rig application
camera persistence while input is Gate-blocked
exact-token release
reverse release order
re-admission token replacement
stale evidence rejection
clean final snapshots
```
