# FXX-CLOSEOUT - LIFECYCLE-E - Activity Scope Tail Pilot

## Status

LIFECYCLE-E is closed at the implementation-cut level.

Only the mechanical tail of `ActivityFlowRuntime` was migrated to the internal Common lifecycle shell. `RouteLifecycleRuntime` was not changed in this cut.

## Cuts covered

- `LIFECYCLE-C` - internal scope tail operation model shell
- `LIFECYCLE-C1` - scope tail cleanup / exit ordering alignment
- `LIFECYCLE-D` - Route scope tail pilot
- `LIFECYCLE-E` - Activity scope tail pilot

## Tail migrated

The Activity tail now flows through `FrameworkScopeTailOperationExecutor` for the mechanical sequence:

1. cleanup previous Activity Content Anchor bindings;
2. remove previous Activity scope root;
3. merge the final `RuntimeScopeLifecycleResult`.

The Activity domain still owns:

- Activity scope creation;
- Activity owner creation;
- Activity content apply / lifecycle;
- Activity scene composition;
- Activity scene release;
- Activity readiness;
- Activity scene ledger.

## Helper used

- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationRequest.cs`
- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationExecutor.cs`
- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationResult.cs`

The helper remains:

- internal
- mechanical
- additive
- without new public API
- without Route semantics
- without Activity semantics
- without `MonoBehaviour`
- without Unity serialization

## Common/Lifecycle adjustment for Activity clear

A small internal adjustment was made in the shell request / executor to support the Activity clear branch without a current owner.

That adjustment stays mechanical:

- it does not move Activity semantics into Common;
- it does not change public enums;
- it does not add a fallback;
- it does not add a public API surface.

## Files altered

- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationRequest.cs`
- `Packages/com.immersive.framework/Runtime/Common/Lifecycle/FrameworkScopeTailOperationExecutor.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-LIFECYCLE-E-Activity-Scope-Tail-Pilot.md`

## Behavior preserved

The Activity tail keeps the previous behavior contract:

- no Route semantic migration into Common;
- no Activity semantic migration into Common;
- cleanup skipped/default behavior remains for the no-owner path;
- cleanup invoked when the previous owner differs from the current owner;
- remove previous scope root invoked only when applicable;
- `RuntimeScopeLifecycleResult` remains the merged result surface;
- `ContentAnchorBindingLifecycleResult` remains the cleanup surface;
- Activity clear still reports the same logical tail outcome;
- Activity restore still creates the current scope root in the Activity domain.

## Diagnostics that must remain equivalent

The following Activity diagnostics should remain unchanged in meaning and shape:

- `runtimeActivityScope`
- `runtimeActivityRootEnter`
- `runtimeActivityRootExit`
- `runtimeActivityContext`
- `runtimeRootCount`
- `activityContentAnchorBindingCleanup`
- `activityContentAnchorBindingCleanupRemoved`
- `activityContentExecution*`
- `activityContentParticipant*`
- `activitySceneComposition*`
- `activitySceneRelease*`
- `activitySceneLedger*`

Any textual or count drift here is a blocking regression and should not be papered over in smoke assertions.

## Smokes affected

Manual validation should cover:

- `Run Scope Tail Operation Synthetic Smoke`
- `Standard Smoke`
- `Activity Baseline Smoke`
- `Activity Content Execution Participant Source Smoke`
- `Activity Content Anchor Diagnostics Smoke`
- `Composite Lifecycle Release Smoke`
- `Route Scene Composition Smoke` as an indirect regression check
- `Route Release Smoke` as an indirect regression check

## Validation pending

Pending validation:

- Unity compile / import
- manual execution of `Run Scope Tail Operation Synthetic Smoke`
- the smoke list above

## What was not migrated

- `RouteLifecycleRuntime` was not altered in this cut.
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
- scene composition was not moved into the shell.
- readiness was not moved into the shell.
- content apply was not moved into the shell.
- activity scene ledger was not moved into the shell.

## Next cut suggested

`LIFECYCLE-F - Lifecycle Scope Tail Closeout / Decision`

That next cut should proceed only after Unity compile/import and the affected smokes are confirmed.
