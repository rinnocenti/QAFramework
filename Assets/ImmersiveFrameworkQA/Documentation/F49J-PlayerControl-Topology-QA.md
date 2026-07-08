# F49J — PlayerControl Topology QA

## Purpose

Validates passive `PlayerControlTopologyValidator` behavior against synthetic QA scenarios.

## Expected smoke

Run from the QA Hub:

```text
PlayerControl Topology QA
```

Expected final log:

```text
[F49J_PLAYER_CONTROL_TOPOLOGY_QA] status='Succeeded'
```

## Scope

The QA checks valid topology, duplicate controls, missing slot declaration, missing PlayerEntry, stale PlayerEntry evidence, bound/active entry requirements, released controls and PlayerTopology issue propagation.

## Out of scope

No PlayerInputManager, InputAction routing, movement enablement, ControlBinding runtime lifecycle, camera activation or FIRSTGAME integration.
