# IF-FW-F25I — Activity Operation Validator Guards

## Status

Implemented.

## Purpose

F25I adds editor-only/QA authoring guards for the Activity operation rules that are already enforced by runtime planning.

The validator must surface invalid authoring before Play Mode whenever an Activity can produce Activity scene load/release side-effects without an explicit loading-capable visual policy.

## Rules

Activity content scene side-effects require explicit `FadeWithLoading` authoring.

Invalid:

```text
ActivityVisualTransitionMode.Seamless
+
Activity Content Profile with scene declarations
```

Invalid:

```text
ActivityVisualTransitionMode.Fade
+
Activity Content Profile with scene declarations
```

Valid:

```text
ActivityVisualTransitionMode.FadeWithLoading
+
Activity Content Profile with scene declarations
```

Rationale:

- `Seamless` has no visual envelope and cannot hide load/release side-effects.
- `Fade` has transition occlusion but does not authorize LoadingSurface usage for Activity scene load/release.
- `FadeWithLoading` explicitly authorizes the TransitionSurface + LoadingSurface operation required by Activity-owned scene side-effects.

## Scope

Changed:

- `FrameworkAuthoringValidator.ValidateActivity(...)` now checks Activity operation visual guards.
- Activity content profile validation messages were updated from early declaration-only wording to current Activity scene composition wording.
- Authoring tooltip/XML comments for `ActivityVisualTransitionMode`, `ActivityAsset`, `ActivityContentProfileAsset` and `ActivityContentSceneEntry` were updated to match the current F25 runtime.

Preserved:

- Existing runtime execution.
- Activity operation planner/gate behavior.
- Activity scene ledger behavior.
- Route startup operation behavior.

## Non-goals

F25I does not add:

- Inspector custom UI.
- validator window redesign.
- runtime load/release changes.
- Addressables.
- loading progress.
- final executor migration.
