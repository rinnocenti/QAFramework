# F27 Plan — Pause UIGlobal Surface and Input Wiring

## Status

Open / F27B ready for smoke

## Purpose

F27 turns the already-existing logical Pause foundation into a minimal Unity-facing surface.

This phase starts with visual surface wiring only. Input and gameplay freeze behavior remain separate cuts so Pause does not become a hidden owner of unrelated systems.

## Boundary

```text
Framework Core:
  PauseRuntime, PauseSnapshot, PauseRequest, Pause Gate snapshot

Unity Build Surface:
  UIGlobal Pause adapter and authored request trigger

Later adapters:
  Input binding, Time.timeScale policy, gameplay freeze, product pause menu
```

## Cut matrix

| Cut | Name | Status | Scope |
|---|---|---|---|
| F27A | Pause UIGlobal Surface Baseline | Closed / PASS | Collect `IPauseSurfaceAdapter` from UIGlobal, apply Pause snapshots to QA surface, expose PauseRequestTrigger buttons. |
| F27B | Pause Input Signal Wiring | Ready for smoke | Map authored `Player/PauseToggle` and `UI/PauseToggle` actions to `PauseRequestKind.Toggle` without owning InputMode. |
| F27C | Pause Time Policy Adapter | Planned | Optional adapter for `Time.timeScale` or local gameplay freeze policy. |
| F27D | Pause Closeout / Guide | Planned | Document designer setup and boundary rules. |

## F27A acceptance

- Boot logs `pauseAdapterCount='1'` for `QA_UIGlobal`.
- Boot logs `Pause surface resolved ... adapterCount='1'`.
- QA pause button applies `PauseState.Paused`.
- QA resume button applies `PauseState.Running`.
- Pause request logs include `pauseSurface='Succeeded'` and `pauseSurfaceVisual='UnitySurface'`.
- Route/Activity transitions continue to work while Pause surface exists.

## Non-goals for F27A

- No keyboard/controller input yet.
- No Time.timeScale changes.
- No product-grade menu.
- No changes to Route/Activity request ownership.

## F27B acceptance

- `InputSystem_Actions` has `PauseToggle` in both `Player` and `UI` maps.
- `QA_UIGlobal` has `UnityPauseInputActionAdapter` configured with the authored input asset.
- Boot logs `Pause Input Action Adapter ready`.
- Pressing Escape or Gamepad Start triggers `PauseRequestKind.Toggle`.
- Same-frame duplicate action callbacks are ignored.
- No InputMode or action-map switching is introduced in this cut.
