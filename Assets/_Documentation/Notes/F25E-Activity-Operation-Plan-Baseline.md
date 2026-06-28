# IF-FW-F25E - Activity Operation Plan Baseline

## Status
Implemented / Planning model only

## Context

F25R reset the Activity scene operation architecture and established that Activity visual policy, scene composition, scene release, LoadingSurface requirement and TransitionSurface envelope requirement must be decided by one operation plan.

F25E introduces that planning language without changing the current runtime execution path.

## Scope

F25E adds side-effect-free runtime model types under:

```text
Packages/com.immersive.framework/Runtime/ActivityFlow
```

New planning types:

```text
ActivityOperationKind
ActivityOperationSceneAction
ActivityOperationIssueSeverity
ActivityOperationIssueKind
ActivityOperationPlanStatus
ActivityOperationResultStatus
ActivityOperationIssue
ActivityOperationPlanSceneEntry
ActivityOperationPlan
ActivityOperationResult
```

## Rules Captured

The plan records:

- previous Activity;
- target Activity;
- operation kind: Start, Switch, Clear, RouteStartup, RouteExitCleanup;
- visual mode: Seamless, Fade, FadeWithLoading;
- scenes to load;
- scenes to release;
- scene side-effect count;
- visual occlusion requirement;
- LoadingSurface requirement;
- blocking and warning issues.

Visual invariants are represented as planning issues:

```text
Seamless + scene side-effect = blocking
Fade + scene side-effect = blocking unless authored as FadeWithLoading
FadeWithLoading + no scene side-effect = warning only
AlreadyLoaded = diagnostics only, not a side-effect
```

## Non-Goals

F25E does not:

- move existing host loading probes;
- change Activity request execution;
- change Route startup Activity execution;
- execute scene load or release;
- create the Activity operation executor;
- create the Activity scene ledger;
- add validators or Inspector changes;
- modify QA assets;
- implement progress, Addressables, coroutines or DOTween.

## Next Cut

`IF-FW-F25F - Activity Operation Executor` should consume this planning language and begin replacing scattered host/runtime decisions with one canonical Activity operation path.
