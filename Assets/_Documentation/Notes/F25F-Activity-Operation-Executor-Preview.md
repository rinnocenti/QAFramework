# IF-FW-F25F - Activity Operation Executor Preview

## Status
Implemented as planning/preview baseline.

## Purpose

F25F introduces the first executable shape around `ActivityOperationPlan` without replacing the legacy Activity runtime wiring yet.

The cut adds:

- `ActivityOperationPlanner`;
- `ActivityOperationExecutor` preview facade;
- side-effect-free operation plan creation from current Activity scene composition/release evidence;
- ActivityFlow preview entry point for later wiring.

## Boundary

This cut still does not move the canonical runtime sequence.

No Activity scene is loaded or unloaded by the new planner/executor. No transition, loading, Activity state or lifecycle callback is executed by the new path.

The current F25C-D4 runtime remains unchanged and experimental.

## Planning behavior

The planner now produces one `ActivityOperationPlan` from:

```text
operation kind
previous Activity
target Activity
visual mode
Activity scene loads required by target Activity
Activity scene releases required by previous Activity or Route cleanup
current Unity scene loaded state
tracked Activity-owned scene evidence
```

The plan applies the F25R rules:

```text
Seamless + scene side-effect = blocking issue
Fade + scene side-effect = blocking issue
FadeWithLoading + scene side-effect = valid and requires LoadingSurface
AlreadyLoaded = diagnostics only, not a side-effect
```

Route exit cleanup can be planned as `ActivityOperationKind.RouteExitCleanup` and includes all currently tracked Activity-owned scenes that are still loaded in Unity.

## Why this is not the final executor

The final executor still needs to replace the current split between:

```text
FrameworkRuntimeHost loading probes
GameFlowRuntime transition decisions
ActivityFlowRuntime scene composition/release execution
RouteLifecycleRuntime startup Activity path
```

F25F only establishes the canonical operation preview object that those systems will consume in later cuts.

## Acceptance

- Activity operation planning can be created without side-effects.
- Scene load/release side-effects are represented in one plan.
- Invalid visual combinations are represented as blocking plan issues.
- Stale tracked scenes are represented as warnings and are not counted as release side-effects.
- Runtime execution behavior remains unchanged.
