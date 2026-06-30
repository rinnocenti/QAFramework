# FXX-CLOSEOUT - COMMON-C Defensive Copy / Empty Collection Helpers

Status: Draft / closeout / docs-only summary
Cut: ARCH-COMMON-C
Date: 2026-06-30

## Helper created

- `Packages/com.immersive.framework/Runtime/Common/FrameworkCollectionCopy.cs`
- Namespace: `Immersive.Framework.Common`
- Type: `internal static class FrameworkCollectionCopy`

## Call sites migrated

1. `Packages/com.immersive.framework/Runtime/Gate/GateSnapshot.cs`
2. `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionPhasePlan.cs`
3. `Packages/com.immersive.framework/Runtime/GlobalUi/GlobalUiSceneRuntime.cs`
4. `Packages/com.immersive.framework/Runtime/Loading/LoadingProgressAggregationResult.cs`

## Behavior preserved

- `null` and empty inputs still resolve to `Array.Empty<T>()` where that was already the behavior.
- Defensive copies still create a new array when a copy is required.
- Collection order is unchanged.
- No external mutable collection reference was introduced where the code previously copied.
- `CopyIssues` remained local because it carries domain-specific behavior.
- No public API was added or changed.

## Smokes affected

No Unity smoke was run in this cut.

Manual validation should recheck the nearest affected smokes later:

- `GateAdmissionQaSmokeRunner`
- Activity Content Execution smoke corresponding to the phase plan, if present
- Global UI smoke corresponding to `GlobalUiSceneRuntime`, if present
- `LoadingProgressQaSmokeRunner`
- `LoadingObservationQaSmokeRunner`

## Validation performed

- Reviewed the four pilot files before editing.
- Kept `CopyIssues` local because it validates its own domain shape.
- Kept the cut limited to one new helper file, the four pilot files, and this closeout document.

## Validation not performed

- Unity compile.
- Unity import.
- Unity smoke execution.
- Playmode or batchmode.

## Manual validation needed

- Confirm `GateSnapshot` still returns an empty array for null/empty input and still copies non-empty input.
- Confirm `ActivityContentExecutionPhasePlan` still preserves order and still validates copied entries and requests.
- Confirm `GlobalUiSceneRuntime` still exposes read-only collections and no mutable collection leaks.
- Confirm `LoadingProgressAggregationResult` still copies step input defensively and still rejects invalid steps.

## Next cuts suggested

1. `COMMON-D` - Issue counting helper.
2. `COMMON-E` - Result/status container ADR only after a second concrete use is confirmed.
3. Revisit any additional collection copies only after another audit finds a second concrete shared shape.

## Files altered

- `Packages/com.immersive.framework/Runtime/Common/FrameworkCollectionCopy.cs`
- `Packages/com.immersive.framework/Runtime/Gate/GateSnapshot.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionPhasePlan.cs`
- `Packages/com.immersive.framework/Runtime/GlobalUi/GlobalUiSceneRuntime.cs`
- `Packages/com.immersive.framework/Runtime/Loading/LoadingProgressAggregationResult.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-COMMON-C-Defensive-Copy-Empty-Collection-Helpers.md`
