# F24E — Canonical UIGlobal Scene

## Goal

Replace the temporary app-scoped Transition/Loading prefab shape with a canonical app/session-scoped `UIGlobal` scene.

## Boundary

`UIGlobal` owns only authored Unity UI surfaces:

- Transition Curtain Surface;
- Loading Surface.

It does not own:

- `RouteLifecycleRuntime`;
- `SceneLifecycleRuntime`;
- `ActivityFlowRuntime`;
- `GameFlowRuntime`.

## Runtime shape

`FrameworkRuntimeHost` loads the configured `UIGlobal` scene before Startup Route, moves its authored roots under the persistent host, unloads the empty template scene, and discovers visual adapters from the persisted roots.

Route loading remains owned by the existing flow:

```text
FrameworkRuntimeHost
  -> GameFlowRuntime
    -> RouteLifecycleRuntime
      -> SceneLifecycleRuntime
```

## Surface resolution

`UIGlobal` is now the only runtime source for Transition and Loading adapters.
Legacy prefab fields were removed from `GameApplicationAsset` and are no longer runtime paths.
If any prefab template remains in the repository, it is manual-only and not part of boot.

## QA fixture

Created:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity
```

The QA Game Application now uses:

```text
Global UI Scene Policy: Required
UIGlobal Scene: QA_UIGlobal
Transition/Loading prefab fields: removed
```

## Expected visual cascade

```text
fade-in / curtain closed
loading visible
route scene/content loading
loading hidden
fade-out / curtain open
```

## Future

This scene-authored UIGlobal model should become the canonical place for later global UI surfaces such as Pause, save feedback, route-level overlays and other app/session UI.
