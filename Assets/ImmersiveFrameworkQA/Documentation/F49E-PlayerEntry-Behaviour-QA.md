# F49E — PlayerEntry Behaviour QA

## Objective

Validate the Unity-facing `PlayerEntryBehaviour` adapter from the QA Hub without touching FIRSTGAME.

## Scope

- Build a synthetic scene with `PlayerSlotDeclaration`, `ActorDeclaration`, `ActorReadinessBehaviour` and `PlayerEntryBehaviour`.
- Prove the adapter exposes `IPlayerEntry`.
- Prove the adapter builds a configured `PlayerEntry` snapshot from Unity declarations.
- Prove `ActorReadinessBehaviour` can be refreshed into the passive PlayerEntry evidence.
- Prove invalid `ActorReady` without view readiness fails explicitly.
- Prove suspension requires an explicit reason.
- Prove release and explicit rebuild are deterministic.

## Expected smoke

```text
[F49E_PLAYER_ENTRY_BEHAVIOUR_QA] status='Succeeded'
```

## Out of scope

- PlayerEntry coordinator.
- PlayerView.
- ControlBinding.
- PlayerInputManager join policy.
- Movement/gameplay controller.
- FIRSTGAME integration.
