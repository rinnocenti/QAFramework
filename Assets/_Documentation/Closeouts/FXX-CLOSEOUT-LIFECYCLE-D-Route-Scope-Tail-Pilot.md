# FXX-CLOSEOUT - LIFECYCLE-D - Route Scope Tail Pilot

## Status

LIFECYCLE-D is closed at the implementation-cut level.

Only the mechanical tail of `RouteLifecycleRuntime` was migrated to the internal Common lifecycle shell. `ActivityFlowRuntime` was not touched.

## Tail migrated

The Route tail now flows through `FrameworkScopeTailOperationExecutor` for the mechanical sequence:

1. cleanup previous Route Content Anchor bindings;
2. remove previous Route scope root;
3. merge the final `RuntimeScopeLifecycleResult`.

Route semantic decisions remain in `RouteLifecycleRuntime`.

## Helper used

- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationRequest.cs`
- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationExecutor.cs`
- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationResult.cs`

The helper remains:

- internal
- mechanical
- without Route or Activity ownership knowledge
- without new public API

## Files altered

- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationRequest.cs`
- `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-LIFECYCLE-D-Route-Scope-Tail-Pilot.md`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-LIFECYCLE-D-Route-Scope-Tail-Pilot.md.meta`

## Behavior preserved

The Route tail keeps the previous behavior contract:

- no Route semantic migration into Common;
- no Activity migration;
- cleanup skipped when there is no previous Route;
- cleanup skipped when previous owner equals current owner;
- cleanup invoked when previous owner differs;
- remove previous scope root invoked only when applicable;
- `RuntimeScopeLifecycleResult` remains the merged result surface;
- `ContentAnchorBindingLifecycleResult` remains the cleanup surface.

## Diagnostics that must remain equivalent

The following Route diagnostics should remain unchanged in meaning and shape:

- `runtimeRouteScope`
- `runtimeRouteRootEnter`
- `runtimeRouteRootExit`
- `runtimeRouteContext`
- `runtimeRootCount`
- `routeContentAnchorBindingCleanup`
- `routeContentAnchorBindingCleanupRemoved`
- `routeRelease*`
- `routeSceneComposition*`
- `routeExit*`

Any textual or count drift here is a blocking regression and should not be papered over in smoke assertions.

## Smokes affected

Manual validation should cover:

- `Run Scope Tail Operation Synthetic Smoke`
- `Standard Smoke`
- `Route Scene Composition Smoke`
- `Route Release Smoke`
- `Content Anchor Diagnostics Smoke`
- `Composite Lifecycle Release Smoke`
- `Activity Content Execution Participant Source Smoke` as an indirect regression check

## Validation pending

Pending validation:

- Unity compile / import
- manual execution of `Run Scope Tail Operation Synthetic Smoke`
- the Route smokes listed above

## What was not changed

- `ActivityFlowRuntime` was not altered.
- `GameFlowRuntime` was not altered.
- `SceneLifecycleRuntime` was not altered.
- `RouteSceneCompositionRuntime` was not altered.
- `ActivitySceneCompositionRuntime` was not altered.
- `ContentReleaseRuntime` was not altered.
- `RouteContentRuntime` was not altered.
- `ActivityContentRuntime` was not altered.
- `ContentAnchorDiscoveryRuntime` was not altered.
- `ContentAnchorBindingRuntime` was not altered.
- `RuntimeContentRuntime` was not altered.
- `Loading` was not altered.
- `Transition` was not altered.

## Next cut suggested

`LIFECYCLE-E - Activity Scope Tail Pilot`

That next cut should proceed only after Route validation is confirmed PASS.

