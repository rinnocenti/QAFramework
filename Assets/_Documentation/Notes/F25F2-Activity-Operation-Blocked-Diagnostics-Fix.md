# IF-FW-F25F2 - Activity Operation Blocked Diagnostics Fix

## Status
Patch / diagnostics fix

## Context

F25F1 correctly blocks invalid Activity operation plans before transition, loading, scene release/load or Activity lifecycle side-effects execute.

The first smoke after F25F1 showed a valid block for:

```text
Fade + Activity scene release side-effect
```

However, the final `FrameworkActivityRequestResult` diagnostics still reported:

```text
activityTransitionMode='Seamless'
```

while the blocked `ActivityOperationPlan` correctly reported:

```text
visualMode='Fade'
```

This was misleading diagnostic output caused by failed-config Activity request results using the default transition mode.

## Change

`FrameworkActivityRequestResult.FailedInvalidConfig` now accepts an explicit `ActivityVisualTransitionMode` and preserves it in the result.

`GameFlowRuntime` now passes the resolved Activity transition mode when returning blocked/failed Activity request or Activity clear results after Activity operation planning.

## Non-goals

This cut does not change Activity operation execution, transition behavior, loading behavior, scene composition, scene release, Route startup Activity flow, validators or QA assets.

## Expected diagnostics

For a blocked plan such as:

```text
Activity Operation Plan visualMode='Fade'
```

The final request fields should also report:

```text
activityTransitionMode='Fade'
```

The operation should still remain blocked, with:

```text
transition='<none>'
loading='SkippedNoSceneLoad'
activitySceneComposition='Unknown'
activitySceneRelease='Unknown'
```
