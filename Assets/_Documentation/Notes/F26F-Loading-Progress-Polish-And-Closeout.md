# IF-FW-F26F - Loading Progress Polish / Documentation Closeout

## Status
Closed / Documentation and QA polish

## Context

F26C, F26D and F26E validated the loading progress path end-to-end:

- F26C added progress receiver support to the Unity loading surface.
- F26D connected determinate progress from real Unity scene operations.
- F26E mapped local scene-operation progress into Route/Activity aggregate loading phases.

The remaining work before leaving the loading-progress thread was polish and documentation alignment.

## Decision

F26F closes the loading progress thread as a validated framework baseline.

Accepted semantics:

```text
technical progress = deterministic runtime signal emitted by lifecycle operations
visual progress    = player-facing presentation that may smooth/lag the signal for readability
```

The framework core owns technical progress and diagnostics. Visual adapters own presentation smoothing. The loading surface must not invent core progress; it may interpolate visual fill after receiving determinate updates.

## Applied changes

- Renamed the QA Activity content scene from `AtivityAdditionalConent` to `ActivityAdditionalContent`.
- Updated `ActivityContentProfile.asset` to reference the corrected scene path/name.
- Added an explicit delete manifest for the old typo scene files because zip-based patches do not remove renamed files automatically.
- Consolidated F26C-F26E documentation into a single closeout note and plan entry.
- Updated documentation indexes to mark loading progress as closed/validated.

## Canonical loading progress baseline

| Cut | Result |
|---|---|
| F26C | Loading surface can receive and present progress. |
| F26D | Real `LoadSceneAsync` / `UnloadSceneAsync` progress reaches the loading surface. |
| F26E | Route and Activity transitions report aggregate phases instead of raw scene phases. |
| F26F | Documentation and QA naming are aligned; visual smoothing policy is recorded. |

## QA evidence accepted

Activity loading path:

```text
loading='SucceededWithUnitySurface'
loadingProgressMode='Determinate'
loadingProgressPhase='ActivityTransition'
loadingProgressPercent='100'
```

Route loading path:

```text
loading='SucceededWithUnitySurface'
loadingProgressMode='Determinate'
loadingProgressPhase='RouteTransition'
loadingProgressPercent='100'
```

Skipped policy path:

```text
loading='SkippedByActivityPolicy'
loadingProgressSupported='False'
loadingProgressMode='Indeterminate'
```

## Non-goals

- No new loading UI layout.
- No change to transition/loading ordering.
- No DOTween dependency.
- No Addressables integration.
- No broad `Task` to `Awaitable` lifecycle migration.
- No fake framework progress.

## Manual cleanup required when applying as zip patch

Delete the old typo scene files after applying this patch:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/AtivityAdditionalConent.unity
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/AtivityAdditionalConent.unity.meta
```

The corrected files preserve the same Unity `.meta` GUID to keep references stable.
