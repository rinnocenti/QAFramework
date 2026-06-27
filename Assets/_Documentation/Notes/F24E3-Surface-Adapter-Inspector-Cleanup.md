# IF-FW-F24E3 — Surface Adapter Inspector Cleanup

## Status
Accepted / Documentation Only

## Context

F24E, F24E1 and F24E2 consolidated the canonical visual shape:

```text
Session / FrameworkRuntimeHost
  -> UIGlobal persistent scene
      -> TransitionSurface Adapter
      -> LoadingSurface Adapter
```

The validated Route visual cascade is:

```text
transition fade-in
loading show
route scene/content composition
loading hide
transition fade-out
```

F24E1 removed the legacy prefab path from `GameApplicationAsset`.

This cut cleans the public Inspector exposure of the surface adapters so authoring shows only what is required.

## Decision

- `UIGlobal` remains the canonical session-scoped source.
- This cut does not change runtime behavior.
- This cut only cleans authoring/Inspector exposure for the adapters.
- `TransitionEffectKind` may still have historical values, but the canonical transition adapter does not expose a public kind choice.
- QA loading hold is QA-only.

## UnityFadeCurtainEffectAdapter

Public Inspector should stay focused on:

```text
Surface
  Canvas Group
  Surface Root

Timing
  Fade In Seconds
  Fade Out Seconds
  Fade In Curve
  Fade Out Curve
```

Technical fields such as `adapterName`, `effectKind`, `setSurfaceRootActive`, `hiddenAlpha`, `visibleAlpha`, `blockRaycastsWhenVisible`, `interactableWhenVisible`, `applyHiddenStateOnAwake`, `lastStatus`, `lastMessage`, `lastVisibleState` stay hidden from authoring.

## UnityLoadingSurfaceAdapter

Public Inspector should stay focused on:

```text
Surface
  Canvas Group
  Surface Root

Presentation
  Optional progress visual, if present
```

Technical fields and runtime diagnostics stay hidden from authoring.

## QaLoadingSurfaceVisibilityHoldAdapter

QA-only adapter used to make fast loading transitions visually inspectable.
Not a canonical production loading surface adapter.

Public Inspector should stay focused on:

```text
Surface
  Canvas Group
  Surface Root

QA Visibility Hold
  Hold Seconds
```

## Non-goals

- No runtime implementation changes.
- No loading progress feature changes.
- No new lifecycle.
- No new policy.
- No change to the validated visual order.
- No fallback to prefab surfaces.

## Future Work

- `F24E4` or later may simplify obsolete transition kinds if that can be done safely.
- Future loading progress should remain conditional on a real progress source.
