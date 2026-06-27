# IF-FW-F25D4 — Activity Scene Tracked-But-Unloaded Fix

## Status
Applied / Patch

## Context
F25D3 force-released tracked Activity scenes on Route change. A follow-up smoke showed a retained Activity scene could still be reported as `AlreadyLoaded` after returning to the Route.

The issue is that Activity scene tracking is runtime evidence, not the source of truth for loaded Unity scenes. If Unity unloads a scene outside of Activity release bookkeeping, a tracked record can become stale.

## Decision
Before treating an Activity scene as already loaded, the Activity scene composition runtime must verify the actual Unity scene load state through `SceneLifecycleRuntime`.

If a tracked record exists but the Unity scene is no longer loaded, the record is removed and the scene is loaded again.

## Expected Smoke
After leaving and returning to a Route, entering an Activity whose retained scene was unloaded by route transition should report:

```text
activitySceneCompositionLoaded='1'
activitySceneCompositionAlreadyLoaded='0'
activitySceneCompositionSideEffects='True'
loading='SucceededWithUnitySurface'
```

If the Activity scene is truly still loaded inside the same Route, the expected result remains:

```text
activitySceneCompositionLoaded='0'
activitySceneCompositionAlreadyLoaded='1'
activitySceneCompositionSideEffects='False'
loading='SkippedNoSceneLoad'
```
