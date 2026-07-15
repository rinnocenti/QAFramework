# P3K.6 — Activity GameplayReady Admission Gate QA

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.6 Run Activity GameplayReady Admission Gate Smoke
```

Expected:

```text
[P3K6_ACTIVITY_GAMEPLAYREADY_ADMISSION_GATE_SMOKE]
status='Passed'
cases='30'
```

The smoke validates:

```text
mandatory Activity Profiles
NoSlots / AllJoinedSlots / ExplicitSlots
zero-participant policy
deterministic projection order
None / Joined / Selected / Prepared / GameplayReady
PendingResolution versus Blocked versus Failed
Gate-blocked readiness
stale preparation/admission evidence
multi-Slot aggregation
public result contracts without Unity object references
```
