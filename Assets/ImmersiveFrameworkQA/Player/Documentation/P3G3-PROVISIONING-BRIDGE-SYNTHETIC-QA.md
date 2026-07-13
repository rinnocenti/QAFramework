# P3G.3 — Provisioning Bridge Synthetic QA

## Menu

```text
Immersive Framework/QA/Player/P3G.3 Run Provisioning Bridge Synthetic Smoke
```

Run in Edit Mode. The smoke uses an in-memory provisioning backend and inactive temporary GameObjects. It does not invoke a real `PlayerInputManager`, enter Play Mode, create persistent assets or depend on frame timing.

## Expected result

```text
[P3G3_PROVISIONING_BRIDGE_SYNTHETIC_SMOKE] status='Passed' cases='17'
```

## Coverage

```text
ordered Slot reservation and two joins
reservation exists before provisioning
PlayerInput.playerIndex remains diagnostic only
callback before JoinPlayer return
admission from direct non-null result without frame delay
late callback confirmation
unexpected callback rejection
null, destroyed PlayerInput and missing PlayerActorDeclaration rollback
callback divergence rollback
joining/capacity/manager policy rejection
reentrant operation rejection
reservation and commit evidence preservation
```

The existing P3G.2 smoke is updated only so its nominal Player Prefab contains the newly required `PlayerActorDeclaration` evidence.
