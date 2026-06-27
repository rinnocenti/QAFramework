# IF-FW-F25D — Activity Content Release

## Status
Implemented / pending Unity smoke

## Context

F25C loaded Activity-owned scenes additively, but it intentionally deferred release.
That left scenes declared with `ReleaseOnActivityChange` alive until a later Route switch removed them implicitly through route scene composition.

This contradicted the authoring policy:

```text
ActivityContentSceneEntry.ReleasePolicy = ReleaseOnActivityChange
```

## Decision

Activity-owned scenes loaded by Activity scene composition are tracked by the Activity scene composition runtime.
When the owning Activity changes, any tracked scene with `ReleaseOnActivityChange` is unloaded by `SceneLifecycleRuntime.UnloadSceneAsync`.

Scenes with `KeepOnActivityChange` are not unloaded by Activity change. F25D1 clarifies that Route changes still force-release all Activity-owned scenes regardless of this policy.

## Runtime order

Activity request now keeps release inside the same visual loading window used for Activity content loading:

```text
transition before, if Activity policy asks for transition
loading show, if target composition or previous Activity release exists
Activity scene composition execution
Activity local lifecycle / content execution
Activity scene release for previous Activity ReleaseOnActivityChange scenes
loading hide
transition after, if Activity policy asks for transition
```

Activity clear follows the same rule for the active Activity release path.

## Diagnostics

Activity Request logs include:

```text
activitySceneRelease
activitySceneReleaseScenes
activitySceneReleaseReleased
activitySceneReleaseFailed
activitySceneReleaseSkipped
activitySceneReleaseSideEffects
activitySceneReleaseBlockingIssues
```

Expected release path:

```text
activitySceneRelease='Succeeded'
activitySceneReleaseReleased='1'
activitySceneReleaseSideEffects='True'
```

## Non-goals

This cut does not add:

- loading progress aggregation;
- Addressables;
- Activity scene reload policy beyond current Additive support;
- KeepOnActivityChange unload on Activity change is intentionally not performed; Route switch force-release is clarified in F25D1;
- Activity-owned runtime spawned object release.
