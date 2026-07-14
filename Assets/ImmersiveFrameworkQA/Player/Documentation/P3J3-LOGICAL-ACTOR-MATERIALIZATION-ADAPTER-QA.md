# P3J.3 — Logical Actor Materialization Adapter QA

## Purpose

Prove the P3J.3 technical contracts and attached Unity adapter without adding
Session preparation/replacement authority.

## Run

Outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3J.3 Run Logical Actor Materialization Adapter Smoke
```

## Covered evidence

```text
typed contracts exist
operation identity is framework-generated
adapter is a scoped plain C# runtime object
valid attached materialization succeeds
runtime ActorId differs from ActorProfileId
Logical Actor remains inactive below explicit ActorMount
PlayerInput evidence is injected from the stable host
RuntimeContent handle is registered
snapshot preserves Slot/Profile/Actor/content identities
explicit rollback destroys only the Logical Actor
RuntimeContent handle is unregistered
joined host is required
host and Slot identities must match
missing prefab is rejected
PlayerInput inside Logical Actor is rejected
missing/multiple PlayerActorDeclaration is rejected
additional ActorDeclaration authority is rejected
RuntimeContent owner root is required
negative cases leave no RuntimeContent leaks
```

## Expected result

```text
[P3J3_LOGICAL_ACTOR_MATERIALIZATION_ADAPTER_SMOKE]
status='Passed'
```

The smoke creates transient objects and removes them after execution. It does not
modify project scenes, prefabs or product assets.
