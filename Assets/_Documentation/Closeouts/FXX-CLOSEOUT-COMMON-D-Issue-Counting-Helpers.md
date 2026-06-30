# FXX-CLOSEOUT - COMMON-D Issue Counting / Blocking Issue Counting Helpers

Status: Draft / closeout / docs-only summary
Cut: ARCH-COMMON-D
Date: 2026-06-30

## Helper created

- `Packages/com.immersive.framework/Runtime/Common/FrameworkIssueCounting.cs`
- Namespace: `Immersive.Framework.Common`
- Type: `internal static class FrameworkIssueCounting`
- Mechanical methods:
  - `Count<T>(IReadOnlyList<T> items)`
  - `CountWhere<T>(IReadOnlyList<T> items, Predicate<T> predicate)`
  - `HasAny<T>(IReadOnlyList<T> items)`
  - `HasAnyWhere<T>(IReadOnlyList<T> items, Predicate<T> predicate)`
  - `Sum<T>(IReadOnlyList<T> items, Func<T, int> selector)`

`Sum<T>` was added to preserve exact aggregation behavior in the request/application pilot while keeping the helper internal and domain-agnostic.

## Call sites migrated

1. `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionAggregateResult.cs`
2. `Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityPlayerInputRequestApplicationResult.cs`
3. `Packages/com.immersive.framework/Runtime/Pause/PauseResult.cs`
4. `Packages/com.immersive.framework/Runtime/Gate/GateEvaluationResult.cs`

## Behavior preserved

- `null` and empty collections still yield zero counts.
- Blocking detection stayed in the owning domain objects.
- No severity enum, status enum, or domain rule moved into `Common`.
- No diagnostic message text changed.
- No public API changed.
- No serialized field changed.
- The `InputMode` aggregate still sums the same nested result counts as before.
- `PauseResult` still counts only issues with `BlocksRequest`.
- `GateEvaluationResult` still counts blockers and facts without adding domain semantics to `Common`.

## Smokes affected

No Unity smoke was run in this cut.

Manual validation should recheck the nearest affected smokes later:

- Activity Content Execution smoke corresponding to `ActivityContentExecutionAggregateResult`, if present
- `InputModeUnityPlayerInputRequestApplicationQaSmokeRunner`
- `PauseRuntimeRequestQaSmokeRunner`
- `GateAdmissionQaSmokeRunner`

## Validation performed

- Reviewed the four pilot files before editing.
- Added one internal helper only in `Runtime/Common`.
- Kept the cut limited to the new helper file, the four pilot files, and this closeout document.

## Validation not performed

- Unity compile.
- Unity import.
- Unity smoke execution.
- Playmode or batchmode.

## Manual validation needed

- Confirm `ActivityContentExecutionAggregateResult` still reports the same blocking and non-blocking totals.
- Confirm `InputModeUnityPlayerInputRequestApplicationResult` still reports the same aggregate issue counts across nested results.
- Confirm `PauseResult` still reports the same total issues and blocking issues.
- Confirm `GateEvaluationResult` still reports the same blocker and fact counts.

## Next cuts suggested

1. Revisit additional issue-count shells only after a second concrete call site appears.
2. Keep domain-specific blocking semantics outside `Common`.
3. Defer any result/status-shell consolidation until a separate ADR-backed cut.

## Files altered

- `Packages/com.immersive.framework/Runtime/Common/FrameworkIssueCounting.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityContentExecutionAggregateResult.cs`
- `Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityPlayerInputRequestApplicationResult.cs`
- `Packages/com.immersive.framework/Runtime/Pause/PauseResult.cs`
- `Packages/com.immersive.framework/Runtime/Gate/GateEvaluationResult.cs`
- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-COMMON-D-Issue-Counting-Helpers.md`
