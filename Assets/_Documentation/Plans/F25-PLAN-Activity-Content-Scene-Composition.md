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
| F25C | Activity Scene Composition Execution | Load Activity content scenes and connect LoadingSurface when real loading exists. |
| F25D | Activity Content Release | Release Activity-owned content according to policy. |

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
