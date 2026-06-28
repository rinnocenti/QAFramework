# F25 Plan — Activity Content Scene Composition

## Status
Closed / consolidated in IF-FW-F25J

## Purpose

F25 adds Activity-owned scene/content declarations and prepares Activity content composition.

The goal is to make Activity more than a state switch:

```text
Activity = identity + readiness + content profile + scene/content composition + release
```

## Boundary

F25 belongs to framework lifecycle/content core, not adapter modules.

Activity content scenes are still Unity Build Surface work, but the contracts must remain framework-owned and explicit.

## Canonical ownership

```text
Session/App
  -> UIGlobal persistent scene
      -> TransitionSurface
      -> LoadingSurface

Route
  -> Primary Scene
  -> Route Content Profile

Activity
  -> Activity Content Profile
      -> Activity-owned content scenes
```

## Planned cuts

| Cut | Name | Scope |
|---|---|---|
| F25A | Activity Content Profile Contract | Authoring contract only. No scene loading. |
| F25B | Activity Scene Composition Plan/Result | Plan/result language for Activity scene composition. No execution. |
| F25C | Activity Scene Composition Execution | Experimental/partial. Load Activity content scenes and connect LoadingSurface when real loading exists. Superseded architecturally by F25R. |
| F25D | Activity Content Release | Experimental/partial. Release Activity-owned content according to policy. Superseded architecturally by F25R. |
| F25R | Activity Scene Operation Architecture Reset | Documentation reset. Defines `ActivityOperationPlan`, Visual Envelope, Activity scene ledger direction and next cuts. |
| F25E | Activity Operation Plan Baseline | Add side-effect-free operation plan/result model. |
| F25F | Activity Operation Executor Preview | Add side-effect-free planner and validation-only executor preview over `ActivityOperationPlan`; runtime execution unchanged. |
| F25F1 | Activity Operation Runtime Gate | Activity request/clear uses operation preview to block invalid visual/scene side-effect plans before loading/transition/lifecycle execution. |
| F25F2 | Activity Operation Blocked Diagnostics Fix | Preserve operation visual mode in blocked/failed Activity request diagnostics. |
| F25G | Startup Activity Path Unification | Route startup Activity uses the same Activity operation path. |
| F25H | Activity Scene Ledger | Replace loose tracking with route-scoped Activity-owned ledger entries. |
| F25I | Activity Operation Validator Guards | Superseded by F25I1 for visual-mode scope. |
| F25I1 | Activity Operation Visual Mode Scope Correction | Seamless/Fade/FadeWithLoading are valid presentation choices for scene side-effects; visual mode selects presentation, not permission. |
| F25I2 | Loading Skip Diagnostics Refinement | Distinguish `SkippedNoSceneLoad` from `SkippedByActivityPolicy` in Activity loading diagnostics. |
| F25J | Activity Operation Final Documentation / Matrix Alignment | Close F25 documentation, record final semantics, smoke evidence and cleanup-audit targets. |

## IF-FW-F25E - Activity operation plan baseline

F25E introduces the side-effect-free Activity operation planning language required by F25R.

Added runtime planning types:

- `ActivityOperationKind`
- `ActivityOperationSceneAction`
- `ActivityOperationIssueSeverity`
- `ActivityOperationIssueKind`
- `ActivityOperationPlanStatus`
- `ActivityOperationResultStatus`
- `ActivityOperationIssue`
- `ActivityOperationPlanSceneEntry`
- `ActivityOperationPlan`
- `ActivityOperationResult`

F25E does not alter runtime execution. Host-side loading probes, Activity scene execution/release and Route startup Activity wiring remain unchanged until the executor cut.

## IF-FW-F25F - Activity operation executor preview

F25F introduces `ActivityOperationPlanner` and a validation-only `ActivityOperationExecutor` facade.

The planner produces one side-effect-free `ActivityOperationPlan` from target Activity scene composition declarations, previous Activity release evidence, current Unity scene loaded state and Activity visual mode.

This is the first bridge from the F25R architecture reset into code, but it intentionally does not replace current runtime execution yet.

F25F acceptance:

- Activity operation preview can be created without load/unload/transition/loading side-effects.
- Planned scenes include target Activity loads and previous Activity/Route cleanup releases.
- `Seamless` with scene side-effects produces a valid plan that executes without TransitionSurface and without LoadingSurface.
- `Fade` with scene side-effects produces a valid plan that executes inside TransitionSurface and without LoadingSurface.
- `FadeWithLoading` with scene side-effects produces a valid plan that requires LoadingSurface.
- `AlreadyLoaded` and stale tracked scenes are diagnostics, not scene side-effects.
- Existing F25C-D4 execution remains unchanged and experimental.

Acceptance:

- Activity operation planning can represent Start, Switch, Clear, RouteStartup and RouteExitCleanup.
- The plan records scenes to load/release without executing them.
- The plan reports scene side-effects separately from `AlreadyLoaded` diagnostics.
- `Seamless + scene side-effect` is valid and means the author accepts content load/release without visual envelope.
- `Fade + scene side-effect` is valid and means the author accepts content load/release inside the TransitionSurface without LoadingSurface.
- `FadeWithLoading + no scene side-effect` is valid with a warning and no loading requirement.
- Result diagnostics expose validity, blocking/warning counts, visual occlusion requirement and LoadingSurface requirement.

## IF-FW-F25F1 - Activity operation runtime gate

F25F1 starts consuming `ActivityOperationPlan` in the real Activity request and Activity clear path without moving scene execution into the final executor yet.

Runtime rules:

- Activity request/clear previews the operation before transition, loading hooks, scene composition or release.
- Blocked plans fail explicitly and perform no Activity lifecycle side-effects.
- `FrameworkRuntimeHost` opens Activity `LoadingSurface` only when the preview plan is valid and `RequiresLoadingSurface = true`.
- Legacy host load/release probes are no longer used to decide Activity loading visibility.

This guards the original F25R invalid behavior differently after F25I1: `Seamless + Activity scene side-effect` must not open LoadingSurface implicitly. It remains a valid authoring choice, but executes without TransitionSurface and without LoadingSurface.

F25F1 does not unify Route startup Activity yet; that remains F25G.

## IF-FW-F25F2 - Activity operation blocked diagnostics fix

F25F2 keeps F25F1 behavior unchanged and only fixes misleading diagnostics in blocked/failed Activity requests.

When `ActivityOperationPlan` blocks a request with `visualMode=Fade`, the final `FrameworkActivityRequestResult` must also report `activityTransitionMode=Fade`, not the default `Seamless`.

F25F2 does not execute transition, loading, scene load/release or lifecycle side-effects.


## IF-FW-F25G - Startup Activity path unification

F25G starts applying the same Activity operation path to Route startup Activity.

Runtime rules:

- Route startup Activity is previewed as `ActivityOperationKind.RouteStartup` before Route lifecycle side-effects execute.
- Blocked startup Activity plans fail the Route start explicitly instead of falling through to an incomplete Activity path.
- `ActivityFlowRuntime.StartStartupActivityAsync` also previews and carries the operation result into `ActivityFlowStartResult`.
- Route request diagnostics now report `routeStartupActivityOperation*` and Activity scene composition/release fields for the startup Activity result.
- F25G does not create a separate Activity transition/loading envelope for startup Activity; Route transition/loading remains the outer visual envelope for Route startup.
- F25G does not implement the final Activity scene ledger.


## IF-FW-F25J — Activity Operation Final Documentation / Matrix Alignment

F25J closes the Activity Content Scene Composition track as a documented baseline.

Canonical final semantics:

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

The planner must not silently upgrade `Seamless` or `Fade` to `FadeWithLoading`. Activity scene side-effects alone do not make an operation invalid.

Closure evidence accepted before F25J:

- `FadeWithLoading + ActivitySceneRelease` succeeds with `loading=SucceededWithUnitySurface`.
- `Seamless + ActivitySceneComposition` succeeds with `transition=SkippedByActivityPolicy` and `loading=SkippedByActivityPolicy`.
- Route startup Activity is represented as `ActivityOperationKind.RouteStartup`.
- Route changes force-release Activity-owned scenes regardless of Activity release policy.
- Activity Scene Ledger reports Loaded/Released/Stale snapshots.

F25J does not change runtime behavior. Cleanup of false/legacy tracks is intentionally deferred to the next Codex audit pass.

## F25A acceptance

- Activity can reference an Activity Content Profile.
- Activity Content Profile can declare Activity scenes.
- Each scene entry has explicit content id, scene path/name, requiredness, load mode and release policy.
- Validator covers declaration issues.
- Runtime behavior is unchanged.
- Activity loading remains `SkippedNoSceneLoad` until F25C.

## Non-goals

F25 does not introduce:

- gameplay actor materialization;
- player/input adapters;
- camera/audio adapters;
- save backend;
- Addressables;
- pause overlays;
- pooling/runtime spawned objects.


## F25B acceptance

- Activity scene composition has side-effect-free plan/result language.
- Activity requests produce scene composition diagnostics when an Activity has an Activity Content Profile.
- Required/optional counts are reported.
- Execution-ready declaration count is reported.
- Blocking declaration issues are reported.
- No Activity scene loading occurs yet.
- LoadingSurface remains skipped for Activity until F25C.

## F25C acceptance

- Activity scene composition executes execution-ready Activity content scenes additively.
- Activity scene loading executes according to the authored Activity visual mode. `FadeWithLoading` may use the canonical LoadingSurface; `Seamless` and `Fade` do not.
- Activity local discovery/callbacks run after Activity scene composition execution.
- Activity request diagnostics include loaded/already-loaded/failed/skipped/side-effect counts.
- Loading progress remains indeterminate until a future progress aggregation cut.
- Activity content release/unload remains deferred to F25D.

## F25D acceptance

- Activity-owned scenes loaded by Activity scene composition are tracked by their owning Activity.
- `ReleaseOnActivityChange` scenes unload when the Activity is replaced or cleared.
- Release executes according to the authored Activity visual mode. `FadeWithLoading` may use the canonical LoadingSurface; `Seamless` and `Fade` do not.
- Activity Request diagnostics report release status, released/skipped/failed counts and side effects.
- `KeepOnActivityChange` remains loaded on Activity change and is not expanded in this cut.
- Loading progress remains indeterminate until a future progress aggregation cut.

## IF-FW-F25D1 — Activity release policy semantics

`ActivityContentReleasePolicy` controls Activity changes only:

- `ReleaseOnActivityChange`: unload on Activity replace/clear.
- `KeepOnActivityChange`: keep loaded on Activity replace/clear.

Route changes always force-release all Activity-owned scenes regardless of that policy. Route content has no release policy; Route-owned content is always released on Route change. Content that must survive Route changes belongs to Session content.

## IF-FW-F25R - Activity scene operation architecture reset

F25R documents that F25C-D4 are experimental/partial execution cuts. They remain in the project as evidence, but they are not the final architecture.

Invalid behavior recorded by F25R:

```text
Activity scene load/release side-effect
opens LoadingSurface directly
without being requested by the Activity operation visual mode
```

Activity scene operations must be decided by one `ActivityOperationPlan`.

The plan owns:

- previous and target Activity;
- operation kind: Start, Switch, Clear, RouteStartup, RouteExitCleanup;
- visual mode: Seamless, Fade, FadeWithLoading;
- scenes to load;
- scenes to release;
- scene side-effect detection;
- visual occlusion requirement;
- LoadingSurface requirement;
- validity and blocking issues.

Visual rules:

- `Seamless` skips TransitionSurface and LoadingSurface, even when the operation performs Activity scene load/release.
- `Fade` uses TransitionSurface and skips LoadingSurface, even when the operation performs Activity scene load/release.
- `FadeWithLoading` uses TransitionSurface and LoadingSurface when the operation performs Activity scene load/release.
- The planner must not silently upgrade one visual mode into another.

Route startup Activity must use the same `ActivityOperationPlan` and executor as normal Activity requests. Route exit cleanup must remove all Activity-owned content for the previous Route.

Activity scene tracking must become an explicit ledger:

```text
ActivitySceneLedgerEntry
  RouteInstanceId
  ActivityId
  ContentId
  ScenePath
  ReleasePolicy
  Ownership = Activity
  UnitySceneLoaded
```

Canonical ADR:

```text
Packages/com.immersive.framework/Documentation~/ADRs/F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md
```

## IF-FW-F25H — Activity Scene Ledger

F25H replaces the implicit loaded-scene record list with an explicit `ActivitySceneLedger`.

Ledger entries carry:

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

Operational rules remain unchanged:

- Activity switch/clear respects `ReleaseOnActivityChange` and skips `KeepOnActivityChange`.
- Route change force-releases every loaded Activity-owned scene regardless of Activity release policy.
- If Unity no longer reports a ledger-loaded scene as loaded, the entry becomes `Stale` and is not planned as a release side-effect.
- Activity/Route diagnostics now include `activitySceneLedger*` snapshot fields.

F25H is internal infrastructure only. Validators and authoring guards remain for F25I.


## IF-FW-F25I / F25I1 — Activity Operation Validator Guards Scope Correction

F25I originally made the Activity Content Profile validator too strict. F25I1 corrects the scope: visual mode selects presentation, not permission to load/release Activity content.

Corrected validator/runtime rules:

- Activity content scene declarations are valid with `Seamless`, `Fade` or `FadeWithLoading`.
- `Seamless` means the operation may load/release Activity scenes without TransitionSurface and without LoadingSurface.
- `Fade` means the operation may load/release Activity scenes inside the TransitionSurface and without LoadingSurface.
- `FadeWithLoading` means the operation may load/release Activity scenes inside the TransitionSurface with the canonical LoadingSurface.
- Required Activity content scene entries without a scene remain errors.
- Cached scene names without scene paths remain errors.
- Duplicate content ids inside one Activity Content Profile remain errors.

F25I1 removes the runtime blocking issues for `Seamless/Fade + Activity scene side-effect`. The planner must not silently upgrade `Seamless` or `Fade` to `FadeWithLoading`; it must execute with the authored visual mode.


## IF-FW-F25I2 — Loading Skip Diagnostics Refinement

F25I2 refines request-level loading diagnostics after the F25I1 visual-mode correction.

Corrected diagnostic rule:

- `loading=SkippedNoSceneLoad`: no Activity scene load/release side-effect happened.
- `loading=SkippedByActivityPolicy`: an Activity scene load/release side-effect happened, but the authored Activity visual mode did not request LoadingSurface.

This keeps `Seamless` and `Fade` valid for Activity content scene operations while making smoke logs truthful. No runtime transition/loading/ledger behavior changes in this cut.
