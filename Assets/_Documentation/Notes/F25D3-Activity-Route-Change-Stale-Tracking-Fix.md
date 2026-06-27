# IF-FW-F25D3 — Activity Route Change Stale Tracking Fix

## Status
Applied / Patch

## Context
F25D2 correctly stopped opening Loading for Activity scene composition when the scene was already tracked. A new issue appeared: Activity-owned scenes retained with `KeepOnActivityChange` could remain tracked after a Route change when there was no active Activity at the moment of the Route switch.

This created stale runtime evidence:

- the Route switch could unload the scene through route scene composition / Single load;
- ActivitySceneCompositionRuntime still believed the Activity scene was tracked;
- returning to the Route and Activity could report `AlreadyLoaded='1'` and skip the additive load.

## Decision
Route change force-releases all tracked Activity-owned scenes, not only scenes owned by the currently active Activity.

Activity release policy remains scoped only to Activity change / clear:

- `ReleaseOnActivityChange` unloads on Activity switch/clear.
- `KeepOnActivityChange` may retain within the same Route.
- Route switch force-releases every Activity-owned scene regardless of Activity release policy.

## Runtime Boundary
This fix does not add Session persistence. Content that must survive Route changes belongs to Session content, not Activity or Route content.

## Expected Smoke
When leaving a Route that has retained Activity-owned scenes, Route Request should report:

```text
routeActivitySceneRelease='Succeeded'
routeActivitySceneReleaseReleased='1'
routeActivitySceneReleaseSideEffects='True'
```

After returning to the Route, Activity composition should load the additive scene again:

```text
activitySceneCompositionLoaded='1'
activitySceneCompositionAlreadyLoaded='0'
activitySceneCompositionSideEffects='True'
```
