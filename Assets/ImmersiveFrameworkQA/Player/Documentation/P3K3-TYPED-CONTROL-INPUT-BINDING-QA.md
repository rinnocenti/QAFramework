# P3K.3 — Typed Control and Input Binding QA

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.3 Run Typed Control and Input Binding Smoke
```

Expected:

```text
[P3K3_TYPED_CONTROL_INPUT_BINDING_SMOKE]
status='Passed' cases='20'
```

The smoke proves:

```text
context initializes from the live P3K.2 occupancy authority
vacant occupancy cannot bind
released stale occupancy evidence cannot bind
Active preparation and current occupancy bind the exact generated ActorId
stable Local Player Host remains the PlayerInput owner
PlayerActorDeclaration must reference that exact PlayerInput
configured gameplay action map activates
same bind is idempotent
Actor, host, PlayerInput and Gate mismatches are rejected
missing gameplay action map is rejected
one PlayerInput cannot bind two Slots
Gate availability changes without replacing binding identity
foreign/stale binding tokens are rejected
release restores the previous action map
release is idempotent
rebind generates a new token
release-all leaves no input bindings
```
