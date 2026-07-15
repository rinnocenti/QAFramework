# P3K.7I-A — Public Default Actor Selection QA

Run in a fresh Play Mode session:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7I Run Public Default Actor Selection Smoke
```

Expected:

```text
[P3K7I_PUBLIC_DEFAULT_ACTOR_SELECTION_SMOKE]
status='Passed'
cases='16'
```

The smoke uses only public product APIs for join and selection:

```text
LocalPlayerProvisioningAuthoring.OpenJoining
LocalPlayerProvisioningAuthoring.RequestJoin
LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection
LocalPlayerProvisioningAuthoring.CloseJoining
```

It does not use reflection or access P3H/P3J runtime modules.

Coverage:

```text
runtime readiness
fresh PlayerInputManager
explicit endpoint configuration
real manual local join
stable host and PlayerInput evidence
PlayerSlotProfile default Actor intent
public default selection
authoritative selected Slot
selection revision
RuntimeSnapshot consistency
idempotent repeated request
endpoint diagnostics
joining close
public API shape
```
