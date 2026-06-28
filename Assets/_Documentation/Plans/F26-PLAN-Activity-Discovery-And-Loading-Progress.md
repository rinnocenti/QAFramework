# F26 Plan — Activity Scene Discovery and Loading Progress

## Status
Open phase / loading progress thread closed through IF-FW-F26F

## Purpose

F26 connects Activity-owned additive scenes to the runtime content discovery path and closes the loading-progress path that became necessary after Activity scene composition/release started executing real scene side effects.

## Boundary

F26 is framework lifecycle/content work. It is not a gameplay adapter phase.

The phase may touch Unity-facing QA fixtures when a framework lifecycle contract needs a minimal Unity surface to validate behavior, but ownership remains:

```text
Framework core/runtime: technical loading progress, diagnostics, route/activity aggregation.
Unity surface/QA: visual loading bar receiver and human-readable smoothing.
```

## Cut matrix

| Cut | Name | Status | Evidence |
|---|---|---|---|
| F26A | Activity Scene Discovery Integration | Closed / PASS | Activity discovery scans route primary scene plus loaded Activity-owned scenes for current route/activity. |
| F26A1 | Activity Content Execution Diagnostics Clarification | Closed / PASS | Participant execution diagnostics are separated from local Activity content diagnostics. |
| F26B | Loading Progress Contract | Closed / PASS | `loadingProgress*` diagnostic fields exist without visual or fake progress. |
| F26C | Loading Surface Progress Bar Receiver | Closed / PASS | QA/UIGlobal loading surface exposes a progress receiver and reports `loadingProgressSupported='True'`. |
| F26D | Determinate Loading Progress Source | Closed / PASS | Scene load/unload operations report determinate progress to the loading surface. |
| F26E | Aggregated Loading Progress | Closed / PASS | Final Activity/Route diagnostics report `ActivityTransition` / `RouteTransition` aggregate phases. |
| F26F | Loading Progress Polish / Documentation Closeout | Closed / PASS | Typo cleanup, documentation consolidation and smoothing policy recorded. |

## Canonical loading progress semantics

Technical progress is a runtime signal:

- owned by scene/lifecycle execution;
- reported through `FrameworkLoadingProgressReporter`;
- represented in diagnostics by `loadingProgressMode`, `loadingProgressValue`, `loadingProgressPercent`, `loadingProgressPhase` and `loadingProgressMessage`;
- never fabricated by the visual adapter.

Visual progress is presentation:

- owned by the loading surface adapter;
- may smooth or slightly lag the technical signal;
- must not change the diagnostic value reported by framework core;
- may reset or stay indeterminate when no determinate source exists.

## Final accepted loading evidence

Route transition with loading surface:

```text
loading='SucceededWithUnitySurface'
loadingProgressMode='Determinate'
loadingProgressPhase='RouteTransition'
loadingProgressPercent='100'
```

Activity transition with `FadeWithLoading` and scene release/load side effect:

```text
loading='SucceededWithUnitySurface'
loadingProgressMode='Determinate'
loadingProgressPhase='ActivityTransition'
loadingProgressPercent='100'
```

Activity transition where authored policy skips loading presentation:

```text
loading='SkippedByActivityPolicy'
loadingProgressSupported='False'
loadingProgressMode='Indeterminate'
```

## Remaining phase work

After F26F, loading progress does not require another immediate cut unless a later feature adds a new loading source such as Addressables, asset bundles, remote content or long-running non-scene operations.

Future work should avoid reopening F26C-F26E unless one of those new sources needs a real progress adapter.
