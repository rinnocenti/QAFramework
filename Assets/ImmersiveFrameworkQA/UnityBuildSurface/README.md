# Unity Build Surface QA Workspace

Workspace isolado para fixtures de QA da etapa F24.

## Current fixtures

```text
Scenes/QA_UIGlobal.unity
Scenes/TransitionRouteA.unity
Scenes/TransitionRouteB.unity
Routes/QA_TransitionRouteA.asset
Routes/QA_TransitionRouteB.asset
Activities/QA_TransitionActivityA.asset
Activities/QA_TransitionActivityA_Alt.asset
Activities/QA_TransitionActivityB.asset
Activities/QA_TransitionActivityB_Alt.asset
GameApplications/QA_TransitionGameApplication.asset
Prefabs/QA_TransitionCurtainSurface.prefab
Prefabs/QA_LoadingSurface.prefab
Scripts/Runtime/TransitionQaRouteSwitchPanel.cs
Scripts/Runtime/TransitionQaActivitySwitchPanel.cs
```

## Temporary tools

The editor installers used to create these fixtures are temporary and can be deleted after validation.

See:

```text
Assets/_Documentation/Notes/F24B1-DELETE-MANIFEST.txt
```

## Validation target

With `QA Transition Game Application` active:

1. Boot enters `TransitionRouteA`.
2. Route switch A -> B logs transition diagnostics.
3. Route switch B -> A logs transition diagnostics.
4. Activity switch and clear in each route log transition diagnostics.
5. `QA_UIGlobal` provides the Transition and Loading adapters that are persisted under the runtime host.

Expected transition diagnostics:

```text
transition='SucceededWithUnitySurface'
transitionVisual='UnitySurface'
transitionEffect='Fade'
transitionBefore='SucceededWithUnitySurface'
transitionAfter='SucceededWithUnitySurface'
transitionBlockingIssues='0'
transitionEffectBefore='Succeeded'
transitionEffectAfter='Succeeded'
transitionEffectBlockingIssues='0'
transitionEffectAdapterCount='1'
```


## F24D1B — Transition Curtain QA warm state

`QA_TransitionCurtainSurface.prefab` keeps `CurtainPanel` active and hidden via `CanvasGroup.alpha = 0` instead of disabling the surface root. This avoids the first-use UI cold start where the transition diagnostics succeeded but the curtain could miss the first visible render.

This is a QA fixture correction only. It does not alter Route, Activity, SceneLifecycle, Loading or Transition ownership.


## F24E Canonical UIGlobal Scene

`Scenes/QA_UIGlobal.unity` is the canonical QA scene for app/session-scoped UI surfaces.
It contains the Transition Curtain Surface and Loading Surface.
`QA_TransitionGameApplication.asset` loads this scene before the Startup Route and resolves adapters from it; legacy surface prefabs, if kept, are templates only and no longer runtime paths.

## F24F Activity Transition Policy QA

The QA activities intentionally cover both Activity transition policy branches:

- `QA_TransitionActivityA` and `QA_TransitionActivityB`: `Seamless`.
- `QA_TransitionActivityA_Alt` and `QA_TransitionActivityB_Alt`: `Fade`.

Expected Activity diagnostics after F24F:

```text
activityTransitionMode='Seamless'
transition='SkippedByActivityPolicy'
transitionVisual='None'
transitionEffectAdapterCount='0'
loading='SkippedNoSceneLoad'
```

or, for the alternate Fade activities:

```text
activityTransitionMode='Fade'
transition='SucceededWithUnitySurface'
transitionVisual='UnitySurface'
transitionEffectAdapterCount='1'
loading='SkippedNoSceneLoad'
```
