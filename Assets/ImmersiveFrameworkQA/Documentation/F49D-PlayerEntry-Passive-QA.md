# F49D — PlayerEntry Passive QA

Local QA note for the synthetic QAFramework smoke.

## Purpose

Validate the passive PlayerEntry model from the package without creating PlayerEntryCoordinator, PlayerView, ControlBinding, PlayerInputManager integration or FIRSTGAME gameplay.

## Builder

```text
Immersive Framework QA > Player > Create or Refresh F49D PlayerEntry Passive QA Scene
```

The Hub builder also calls this builder through:

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

## Hub button

```text
PlayerEntry Passive QA
```

## Expected final log

```text
[F49D_PLAYER_ENTRY_QA] status='Succeeded'
```

## Covered checks

```text
Configured PlayerEntry snapshot
ActorReady requires Actor readiness for view
Active can carry ReadyForControl evidence without owning ControlBinding
Suspended requires explicit suspension reason
Released snapshot is terminal-state evidence
IPlayerEntry exposes the same snapshot data
```

## Out of scope

```text
PlayerEntryCoordinator
transition ordering policy
PlayerView
ControlBinding
movement
PlayerInputManager bridge
FIRSTGAME integration
```
