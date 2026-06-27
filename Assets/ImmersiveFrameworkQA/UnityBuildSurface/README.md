# Unity Build Surface QA Workspace

Workspace isolado para fixtures de QA da etapa F24.

## Current fixtures

```text
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
5. The transition surface prefab is instantiated under the persistent runtime host.

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
`QA_TransitionGameApplication.asset` loads this scene before the Startup Route and resolves adapters from it; legacy surface prefabs remain fallback-only.
