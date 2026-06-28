# IF-FW-F25G - Startup Activity Path Unification

## Status
Implemented as runtime-path consolidation over the F25E/F25F operation plan baseline.

## Context

F25R requires Route startup Activity to use the same Activity operation planning model as normal Activity requests. Before this cut, Route startup Activity entered through `StartStartupActivityAsync` and then `StartActivityCoreAsync`, but it did not expose a first-class Activity operation result and Route request logs did not show startup Activity scene composition/release diagnostics.

## Changes

- `ActivityFlowRuntime.StartStartupActivityAsync` now previews startup Activity as `ActivityOperationKind.RouteStartup`.
- Blocked startup plans return an explicit failed Activity flow result with the `ActivityOperationResult`. After F25I1, `Seamless/Fade + scene side-effect` is not blocked by itself.
- Route lifecycle preflights the startup Activity operation before Route lifecycle side-effects continue.
- `ActivityFlowStartResult` can carry the relevant `ActivityOperationResult`.
- Route request diagnostics now include `routeStartupActivityOperation*` fields.
- Route request diagnostics now include startup Activity `activitySceneComposition*` and `activitySceneRelease*` fields.

## Visual boundary

F25G does not create a separate Activity visual envelope for Route startup Activity. The Route request still owns the outer transition/loading window. Startup Activity planning is now represented by the same Activity operation model, but Route transition/loading remains the presentation boundary for Route startup.

## Non-goals

- No Activity scene ledger.
- No Addressables.
- No loading progress aggregation.
- No validator/Inspector changes.
- No Camera/Input/Audio/Player/Pause/Save changes.
- No coroutine or `Task.Delay`.

## Expected smoke

For a Route whose startup Activity has Activity-owned scenes and `FadeWithLoading` authoring, Route request logs should include:

```text
routeStartupActivityOperation='Planned'
routeStartupActivityOperationKind='RouteStartup'
routeStartupActivityOperationVisualMode='FadeWithLoading'
routeStartupActivityOperationLoad='1'
activitySceneComposition='Succeeded'
activitySceneCompositionLoaded='1'
activitySceneCompositionSideEffects='True'
loading='SucceededWithUnitySurface'
```

For a Route whose startup Activity has Activity-owned scenes and `Seamless` authoring, the Route start should succeed and Activity composition should execute inside the Route operation without opening an Activity LoadingSurface. Structural declaration/configuration failures should still fail explicitly before lifecycle side-effects continue.
