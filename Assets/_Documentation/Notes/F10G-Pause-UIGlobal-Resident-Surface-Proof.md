# F10G — Pause UIGlobal Resident Surface Proof

Status: Ready for smoke.

## Purpose

F10G corrects the Pause presentation track after F10E proved a useful but optional capability. The production-oriented default for Pause presentation is a resident surface in the canonical `UIGlobal` scene, not runtime materialization.

The intended product shape is:

```text
UIGlobal scene
  -> resident Pause visual hierarchy
  -> UnityPauseResidentSurfaceAdapter
  -> PauseSurfaceRuntime applies logical Pause snapshots
```

## Decision carried into code

F10G adds a concrete Unity-facing adapter for the canonical resident path:

```text
UnityPauseResidentSurfaceAdapter : MonoBehaviour, IPauseSurfaceAdapter
```

The adapter shows/hides an already-authored surface from `PauseSnapshot` state. It does not instantiate, bind ContentAnchor, own input, change `Time.timeScale`, control Route/Activity lifecycle, or perform RuntimeContent release.

## What changed

- Added `UnityPauseResidentSurfaceAdapter`.
- Updated the QA Canvas Pause/F10 section to expose the resident UIGlobal path as the visible Pause smoke.
- Kept F10B/F10C/F10D/F10E code available as optional/advanced materialization infrastructure, but removed those buttons from the current Pause QA surface.
- Added usage documentation for authoring resident Pause UI in `UIGlobal`.

## QA smoke

New smoke button:

```text
Run Pause UIGlobal Resident Surface Smoke
```

Expected successful fields:

```text
step='pause-uiglobal-resident-surface'
passed='True'
surfaceRuntime='Succeeded'
adapterCount='1'
initialHidden='True'
pausedVisible='True'
resumedHidden='True'
canonicalResidentUIGlobalSurface='True'
materialization='False'
contentAnchorBinding='False'
inputModeChange='False'
timeScalePolicy='False'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
```

## Boundaries

F10G does not implement:

- Pause input toggle wiring;
- `PlayerInput` or `InputMode` changes;
- `Time.timeScale` policy;
- ContentAnchor binding for Pause;
- Pause visual materialization;
- Route/Activity auto-release;
- Route/Activity auto-materialization;
- camera, audio, save/progression, actor, pooling, PlayerJoin or gameplay/F34 consumers.

## Product interpretation

For a normal game Pause menu, use the resident `UIGlobal` path. Place the pause panel in `UIGlobal`, add `UnityPauseResidentSurfaceAdapter`, and let the framework apply logical Pause snapshots to that resident surface.

Use F10E-style materialization only when the project explicitly needs modular, route-specific, streamed, DLC-like, skin-swapped or QA-only Pause presentation.
