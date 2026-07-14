# P3K.2 — Effective Runtime Occupancy QA

Run outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.2 Run Effective Runtime Occupancy Smoke
```

Expected:

```text
[P3K2_EFFECTIVE_RUNTIME_OCCUPANCY_SMOKE]
status='Passed'
```

The synthetic smoke proves:

```text
runtime context initializes from the ordered P3J preparation roster
all configured Slots start Vacant
Active prepared Actor becomes the effective occupant
same confirm is idempotent
conflicting preparation cannot replace an occupied Slot
one runtime Actor identity cannot occupy two Slots
foreign Session preparation is rejected
inactive staged preparation is rejected
two Slots occupy independently
foreign occupancy token is rejected
release requires the exact current occupancy token
repeated release without a token is idempotent
stale token after release is rejected
released Slot can accept a new preparation with a new token
final release leaves no effective occupancy
```
