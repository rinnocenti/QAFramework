# IF-FW-F25R1 - Activity Visual Policy / Awaitable Clarification

## Status
Documentation correction only

## Context

F25R introduced the Activity operation reset, but its first draft wording conflicted with the consolidated F25/F25J reading of visual policy.

The contradiction was not `Seamless + scene side-effect`. The real bug is:

```text
LoadingSurface opens when the authored visual mode is not FadeWithLoading.
```

That means `Seamless` and `Fade` may still execute Activity scene load/release side-effects. They simply do not open `LoadingSurface`.

## Decision

Final visual policy:

```text
Seamless
  Activity scene load/release may execute.
  TransitionSurface is skipped.
  LoadingSurface is skipped.

Fade
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is skipped.

FadeWithLoading
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is used when the Activity operation requests it.
```

Invalid combinations are now limited to `LoadingSurface` outside `FadeWithLoading`:

```text
Seamless + LoadingSurface = invalid
Fade + LoadingSurface = invalid
```

The planner must not silently upgrade the authored mode:

```text
Seamless -> Fade
Seamless -> FadeWithLoading
Fade -> FadeWithLoading
```

## Diagnostic Mapping

- `loading=SkippedNoSceneLoad` means no Activity scene load/release side-effect happened.
- `loading=SkippedByActivityPolicy` means a scene load/release side-effect happened, but the authored visual mode did not request `LoadingSurface`.
- `loading=SucceededWithUnitySurface` is only expected for Activity when `ActivityVisualTransitionMode = FadeWithLoading` and `LoadingSurface` is available/resolved.

## Async Execution Model

`ActivityOperationPlan` is synchronous and side-effect-free.

- It has no `Awaitable`.
- It performs no scene load/unload.
- It performs no transition/loading calls.

`ActivityOperationExecutor` is the async runtime execution boundary.

- It may use `UnityEngine.Awaitable` for `TransitionSurface`, `LoadingSurface`, scene load/release and future progress operations.
- It must not use `Task.Delay`.
- It must not create coroutine-based canonical flow.
- It must not await the same `Awaitable` instance more than once.
- It must not mix loose Task-based async with framework lifecycle behavior unless an explicit adapter boundary is introduced.

`LoadingSurfaceAdapter` remains presentation only.

- It may expose awaitable show/hide/progress methods in a later executor cut.
- It does not own lifecycle, loading policy, scene lifecycle or progress aggregation.

## Preserved Decisions

- `ActivityContentProfile` remains valid for Activity-owned scene declarations.
- `ActivityOperationPlan` remains the owner of operation decision.
- Route startup Activity must use `ActivityOperationPlan` and the same executor path.
- Route exit cleanup must remove all Activity-owned content from the previous Route.
- Activity scene tracking must become `ActivitySceneLedger` with `RouteInstanceId`.
- `AlreadyLoaded` is diagnostics only and not a scene side-effect.
- F25C-D4 remain experimental/partial evidence, not canonical architecture.

## Affected Cuts

- `F25E` records the synchronous plan model and the final visual semantics.
- `F25F` consumes the plan model and prepares the executor boundary.
- `F25F1` applies the plan to Activity request/clear behavior.
- `F25F2` keeps diagnostics aligned with the resolved visual mode.
- `F25G` reuses the same plan/executor path for Route startup Activity.
- `F25H` records `RouteInstanceId` in the Activity scene ledger.
- `F25I` and later validator cuts must not reintroduce the old Seamless invalidation.
