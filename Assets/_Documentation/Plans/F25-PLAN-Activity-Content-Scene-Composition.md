# F25 Plan — Activity Content Scene Composition

## Status
Current planning track

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
| F25F | Activity Operation Executor | Move Activity visual/loading/release/load/state sequencing to one executor. |
| F25G | Startup Activity Path Unification | Route startup Activity uses the same Activity operation path. |
| F25H | Activity Scene Ledger | Replace loose tracking with route-scoped Activity-owned ledger entries. |
| F25I | Validator Guards | Block invalid visual/scene side-effect combinations. |

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

Acceptance:

- Activity operation planning can represent Start, Switch, Clear, RouteStartup and RouteExitCleanup.
- The plan records scenes to load/release without executing them.
- The plan reports scene side-effects separately from `AlreadyLoaded` diagnostics.
- `Seamless + scene side-effect` is represented as a blocking issue.
- `Fade + scene side-effect` is represented as a blocking issue requiring explicit `FadeWithLoading`.
- `FadeWithLoading + no scene side-effect` is valid with a warning and no loading requirement.
- Result diagnostics expose validity, blocking/warning counts, visual occlusion requirement and LoadingSurface requirement.

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
- Activity scene loading runs inside the canonical LoadingSurface window when a LoadingSurface is available.
- Activity local discovery/callbacks run after Activity scene composition execution.
- Activity request diagnostics include loaded/already-loaded/failed/skipped/side-effect counts.
- Loading progress remains indeterminate until a future progress aggregation cut.
- Activity content release/unload remains deferred to F25D.

## F25D acceptance

- Activity-owned scenes loaded by Activity scene composition are tracked by their owning Activity.
- `ReleaseOnActivityChange` scenes unload when the Activity is replaced or cleared.
- Release runs inside the LoadingSurface window when a LoadingSurface exists.
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
ActivityVisualTransitionMode.Seamless
+
Activity scene load/release side-effect
=
LoadingSurface appears without fade
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

- `Seamless` is valid only when there is no Activity scene load/release side-effect.
- `Fade` is valid for a visual Activity operation without mandatory LoadingSurface.
- `FadeWithLoading` is required when Activity scene load/release requires LoadingSurface.
- Invalid combinations must block explicitly; no silent fallback is allowed.

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
