# IF-FW-F25J — Activity Operation Final Documentation / Matrix Alignment

## Status

Documentation consolidation only.

## Purpose

F25J closes the Activity Content Scene Composition track after the F25I1/F25I2 visual-mode correction.

This cut records the final semantics accepted by smoke and prepares the project for a separate cleanup audit. It does not alter runtime code, authoring contracts, validators, loading, transition, scene composition, release or ledger behavior.

## Final Activity visual semantics

Visual mode selects presentation. It does not grant or deny permission to load/release Activity-owned scenes.

```text
Seamless
  Activity scene composition/release may execute.
  TransitionSurface is skipped.
  LoadingSurface is skipped.

Fade
  Activity scene composition/release may execute.
  TransitionSurface is used.
  LoadingSurface is skipped.

FadeWithLoading
  Activity scene composition/release may execute.
  TransitionSurface is used.
  LoadingSurface is used when the Activity operation requests it.
```

The framework must not silently upgrade `Seamless` or `Fade` into `FadeWithLoading`.

## Canonical operation model

Activity scene operations are represented through `ActivityOperationPlan` and `ActivityOperationResult`.

The operation model owns:

- operation kind: `Start`, `Switch`, `Clear`, `RouteStartup`, `RouteExitCleanup`;
- previous Activity;
- target Activity;
- authored visual mode;
- Activity scenes to load;
- Activity scenes to release;
- scene side-effect evidence;
- visual occlusion requirement;
- LoadingSurface requirement;
- blocking/warning diagnostics.

## Canonical ledger model

Activity-owned scenes are tracked by `ActivitySceneLedger`.

A ledger entry records:

```text
RouteInstanceId
Route
Activity
ActivityId
ContentIdentity
ContentId
SceneName / ScenePath
ReleasePolicy
Ownership = Activity
Status = Loaded | Released | Stale
```

Route change force-releases loaded Activity-owned scenes regardless of Activity release policy. Content that must survive Route changes belongs to Session content, not Activity or Route content.

## Accepted smoke evidence

Accepted F25 smoke coverage:

```text
FadeWithLoading + ActivitySceneRelease
  -> transition SucceededWithUnitySurface
  -> loading SucceededWithUnitySurface
  -> activitySceneReleaseReleased=1

Seamless + ActivitySceneComposition
  -> transition SkippedByActivityPolicy
  -> loading SkippedByActivityPolicy
  -> activitySceneCompositionLoaded=1

RouteStartup + ActivitySceneComposition
  -> routeStartupActivityOperationKind=RouteStartup
  -> routeStartupActivityOperationLoad=1
  -> activitySceneCompositionLoaded=1

Route change with loaded Activity-owned scene
  -> routeActivitySceneReleaseReleased=1
  -> activitySceneLedgerReleased=1
```

## Historical supersessions

These cuts remain as history/evidence, but their early wording is not the final rule:

| Cut | Final reading |
|---|---|
| F24F1 | `FadeWithLoading` is no longer merely reserved; it is the explicit Activity visual mode that uses LoadingSurface. |
| F25C/F25D | Early execution/release path; behavior became canonical only after F25R/F25E-F25I2 reconciliation. |
| F25F1 | Runtime gate remains useful, but `Seamless/Fade + scene side-effect` is no longer invalid. |
| F25I | Original validator guard was too strict; F25I1 supersedes it. |

## Cleanup audit targets

After F25 is closed, request a Codex cleanup audit to remove false trails, unused branches and obsolete wording.

Audit targets:

- code branches or enum issues that still imply `Seamless/Fade + scene side-effect` is invalid;
- obsolete comments saying Activity scene side-effects require `FadeWithLoading`;
- host-side loading probes that bypass `ActivityOperationPlan`;
- duplicate planner/executor preview methods that are no longer called;
- fields that always remain `Unknown` or `NotRequested` without diagnostic value;
- F25C-D4 experimental wording that is now misleading;
- validator text that blocks valid visual-mode choices;
- docs saying Activity loading is reserved or unavailable after F25I2;
- redundant temporary notes that can be merged into the canonical F25 plan.

## Non-goals

F25J does not implement:

- final Activity operation executor migration;
- Addressables;
- loading progress aggregation;
- gameplay/player/camera/audio/pause/save adapters;
- cleanup deletion pass.
