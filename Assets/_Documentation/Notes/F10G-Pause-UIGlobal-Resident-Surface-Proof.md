# F10G — Pause UIGlobal Resident Surface Proof

Status: Closed / PASS.

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

Validated smoke button:

```text
Run Pause UIGlobal Resident Surface Smoke
```

Validated successful fields:

```text
step='pause-uiglobal-resident-surface'
passed='True'
surfaceRuntime='Succeeded'
adapterCount='1'
supportedAdapters='1'
appliedAdapters='1'
initialHidden='True'
pausedVisible='True'
resumedHidden='True'
rootActiveWhenPaused='True'
rootInactiveWhenRunning='True'
canvasAlphaPaused='1,00'
blocksRaycastsWhenPaused='True'
interactableWhenPaused='True'
canonicalResidentUIGlobalSurface='True'
materialization='False'
contentAnchorBinding='False'
physicalRelease='False'
logicalRuntimeContentRelease='False'
contentAnchorBindingCleanup='False'
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


## Closeout evidence

F10G smoke was executed twice and both runs passed. The resident `UIGlobal` surface was resolved from the QA scene with one adapter. The smoke validated hidden -> paused visible -> resumed hidden transitions, CanvasGroup interactive state while paused, and preserved all expected non-goals: no materialization, no ContentAnchor binding, no InputMode mutation, no `Time.timeScale` policy, no automatic lifecycle wiring, no Route/Activity auto-materialization and no Route/Activity auto-release.

## Closure

F10G closes the production-facing Pause presentation surface proof. The next Pause cut should not return to the materialized path unless a future optional/advanced requirement explicitly selects it. The next production-facing step is Pause logical toggle/integration against the resident surface, while keeping InputMode and timeScale as separate future decisions.
