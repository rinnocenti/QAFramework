# IF-FW-F25D2 — Activity Loading Leak Guard

## Status
Accepted / Runtime diagnostics fix

## Context
After F25D1, Activity scenes can be retained with `KeepOnActivityChange`. A retained Activity-owned scene may be requested again when returning to the same Activity.

The runtime must not open the LoadingSurface when the target Activity scene is already tracked and loaded, because no real scene load side effect will occur.

## Decision
Activity LoadingSurface visibility is driven by pending side effects, not by the existence of an ActivityContentProfile plan.

- show loading when an Activity scene will actually load;
- show loading when previous Activity scenes will actually unload;
- do not show loading when all target Activity scenes are already tracked/loaded;
- keep `AlreadyLoaded` as diagnostics only, not as a load side effect.

## Current Behavior
`activitySceneCompositionLoaded` counts only newly loaded scenes.

`activitySceneCompositionAlreadyLoaded` counts retained/already-loaded scenes.

`activitySceneCompositionSideEffects` is true only when a real additive load occurred.

## Non-goals
- no loading progress implementation;
- no Addressables;
- no change to Route transition/loading;
- no change to Activity release policy semantics.
