# P3H.3 — Actor Selection Runtime QA

## Run

Outside Play Mode:

```text
Immersive Framework/QA/Player/P3H.3 Run Actor Selection Runtime Smoke
```

## Expected

```text
[P3H3_ACTOR_SELECTION_RUNTIME_SMOKE] status='Passed' cases='20'
```

The smoke uses transient ScriptableObjects and GameObjects only. It creates no persistent assets or scenes.

It proves:

```text
explicit policy composition
Joined but Unselected validity
Joined-only selection
snapshot evidence and revisions
idempotent select/clear
explicit replacement
stale request rejection
atomic unique ActorProfileId policy
AllowDuplicates behavior
explicit default selection
missing default/profile/policy/Slot rejection
Profile immutability
```
