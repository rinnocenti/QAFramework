# F49F — PlayerTopology Passive QA

## Objective

Validate the passive PlayerTopology validator in the QA harness before any FIRSTGAME integration.

## Scope

The QA scene validates:

- coherent Unity-authored topology;
- duplicate PlayerEntry slot;
- PlayerEntry/occupancy Actor mismatch;
- missing PlayerSlot occupancy;
- occupancy without PlayerEntry;
- duplicate occupied Actor;
- PlayerSlotSet issue propagation.

## Out of scope

This QA does not validate:

- PlayerInputManager;
- join flow;
- spawn/materialization;
- PlayerView;
- ControlBinding;
- movement;
- FIRSTGAME usability.

## How to run

1. Run `Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes`.
2. Open `Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity`.
3. Enter Play Mode.
4. Click `PlayerTopology Passive QA`.

## Expected log

```text
[F49F_PLAYER_TOPOLOGY_QA] status='Succeeded'
```

## Suggested commit message

```text
F49F-QA: add PlayerTopology passive smoke
```
