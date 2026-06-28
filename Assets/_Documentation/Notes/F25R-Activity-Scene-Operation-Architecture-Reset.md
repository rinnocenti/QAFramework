# IF-FW-F25R - Activity Scene Operation Architecture Reset

## Status
Accepted / Documentation reset

## Context

F25C through F25D4 added Activity scene execution, release and local guards after the F25A/F25B contract baseline.
Those cuts are now treated as experimental/partial execution evidence, not as the final Activity operation architecture.

Those cuts exposed a structural issue:

```text
ActivityVisualTransitionMode.Seamless
+
Activity scene load/release side-effect
=
LoadingSurface appears without fade
```

This is not a scene-side-effect bug by itself. The real bug is when `LoadingSurface` opens while the authored visual mode is not `FadeWithLoading`.

## Decision

The canonical reset is recorded in:

```text
Packages/com.immersive.framework/Documentation~/ADRs/F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md
```

The visual-policy clarification is recorded in:

```text
Assets/_Documentation/Notes/F25R1-Activity-Visual-Policy-Awaitable-Clarification.md
```

The next implementation work must introduce `ActivityOperationPlan` as the owner of:

- Activity visual policy;
- Activity scene composition;
- Activity scene release;
- LoadingSurface requirement;
- TransitionSurface visual envelope requirement;
- Activity scene side-effect validity;
- Route startup Activity operation;
- Route exit cleanup operation;
- Activity scene ledger ownership.

## Preserved Rules

- `AlreadyLoaded` is diagnostics only, not a side-effect.
- Route change force-releases Activity-owned content regardless of Activity release policy.
- Activity release policy applies only to Activity change/clear.
- Tracked Activity scene evidence must be verified against actual Unity scene load state.

## Valid Rules

- `Seamless` may execute Activity scene load/release side-effects without `LoadingSurface`.
- `Fade` may execute Activity scene load/release side-effects without `LoadingSurface`.
- `FadeWithLoading` is required when the operation requests `LoadingSurface`.
- Route startup Activity must use the same `ActivityOperationPlan` and executor as normal Activity requests.
- Activity scene tracking must become an explicit ledger with `RouteInstanceId`.

## Async Execution Model

- `ActivityOperationPlan` is synchronous and side-effect-free.
- `ActivityOperationExecutor` is the async execution boundary and may use `UnityEngine.Awaitable` in a later executor cut.
- `LoadingSurface` remains presentation only.
- Do not use `Task.Delay`.
- Do not create coroutine-based canonical flow.

## Follow-Up Cuts

| Cut | Name |
|---|---|
| F25R1 | Activity Visual Policy / Awaitable Clarification |
| F25E | Activity Operation Plan Baseline |
| F25F | Activity Operation Executor |
| F25G | Startup Activity Path Unification |
| F25H | Activity Scene Ledger |
| F25I | Activity Operation Validator Guards |

## Replaced Direction

Future cuts must replace host-side loading probes and scattered execution decisions with one Activity operation plan/executor path.

No runtime code was changed in this reset.
