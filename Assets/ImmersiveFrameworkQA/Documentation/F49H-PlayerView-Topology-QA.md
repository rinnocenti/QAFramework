# F49H — PlayerView Topology QA

## Purpose

Validate passive PlayerView topology coherence against an already validated PlayerTopology result.

## Scene

`Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerViewTopology.unity`

## Route

`Assets/ImmersiveFrameworkQA/Player/Routes/QA_PlayerViewTopologyRoute.asset`

## Expected smoke log

```text
[F49H_PLAYER_VIEW_TOPOLOGY_QA] status='Succeeded'
```

## Coverage

The smoke validates:

- Unity-authored valid PlayerView topology;
- duplicate PlayerView for one PlayerSlot;
- PlayerView pointing to an undeclared PlayerSlot;
- PlayerView without matching PlayerEntry;
- stale PlayerEntry state evidence in PlayerView;
- Bound PlayerView requiring topology PlayerEntry state ViewBound or Active;
- Released PlayerView not participating as an active view candidate;
- propagation of PlayerTopology issues.

## Out of scope

This QA does not activate cameras, drive Cinemachine, bind input, bind control or test FIRSTGAME.
