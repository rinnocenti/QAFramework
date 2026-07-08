# F49I — PlayerControl Passive QA

## Objective

Validate passive PlayerControl contracts and Unity adapter behavior.

## Smoke

Run:

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

Open the QA Hub and select:

```text
PlayerControl Passive QA
```

Expected log:

```text
[F49I_PLAYER_CONTROL_QA] status='Succeeded'
```

## Coverage

- Component references exist.
- Declared snapshot is valid.
- `IPlayerControl` exposure works.
- Bound control requires PlayerEntry evidence in `Active` state.
- Bound control accepts Active PlayerEntry with view readiness.
- Active control requires Actor readiness for control.
- Active control accepts Active PlayerEntry with control readiness.
- Suspended control requires explicit reason.
- Control target and input source are optional diagnostic evidence.
- Release and rebuild are explicit.

## Out of scope

- PlayerInputManager
- InputAction routing
- movement enable/disable
- ControlBinding runtime lifecycle
- FIRSTGAME integration
