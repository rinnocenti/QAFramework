# F26E — Aggregated Loading Progress

## Status

Closed / PASS. Implemented as framework/runtime aggregation over the F26D determinate scene-operation progress source.

## Goal

F26D made each concrete `LoadSceneAsync` / `UnloadSceneAsync` operation report its own local `0..1` progress. That was correct technically, but multi-step route/activity operations could visually restart progress per scene operation.

F26E maps local scene progress into a single weighted operation progress window.

## Scope

F26E adds internal weighted progress reporters for:

- Route transition loading progress.
- Route-owned content release.
- Route scene composition.
- Route startup Activity scene operations.
- Activity transition loading progress.
- Activity scene composition.
- Activity scene release.

The `SceneLifecycleRuntime` remains the owner of concrete Unity scene load/unload observation. It still reports local scene progress. Higher lifecycle owners decide how that local progress maps into the current aggregate operation.

## Expected diagnostics

A multi-step Route operation should now keep a single aggregate phase at the request boundary:

```text
loadingProgressMode='Determinate'
loadingProgressPhase='RouteTransition'
loadingProgressPercent='100'
```

Activity operations using `FadeWithLoading` should similarly report:

```text
loadingProgressMode='Determinate'
loadingProgressPhase='ActivityTransition'
loadingProgressPercent='100'
```

Lower internal phases may still be visible while nested reporters are executing, but the final request diagnostic should represent the whole operation, not only the last scene load/unload.

## Non-goals

- No new visual adapter.
- No fake progress source.
- No scheduler/executor replacement.
- No conversion of existing framework `Task` signatures to `Awaitable`; F26E only keeps the loading progress reporter bridge on `Awaitable`.
- No asset/scene authoring changes.

## Validation

Use the existing QA transition/loading smoke. The important evidence is that operations with scene side-effects still complete and the final loading diagnostics use an aggregate phase with `Determinate` progress.


## Closeout note

F26E is closed after Activity and Route smoke evidence showed final request diagnostics using `ActivityTransition` and `RouteTransition` aggregate phases. F26F records this loading progress thread as closed.
