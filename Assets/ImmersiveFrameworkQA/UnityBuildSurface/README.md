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

## Loading surface QA hold

`QA_LoadingSurface.prefab` uses `QaLoadingSurfaceVisibilityHoldAdapter` to keep the loading panel visible briefly after a hide request.

This is QA-only visibility aid:

```text
- It does not delay Route, Activity, SceneLifecycle or GameFlow.
- It does not simulate loading in the framework core.
- It only delays the visual hide state on the QA prefab.
```

Expected loading diagnostics remain:

```text
loading='SucceededWithUnitySurface'
loadingVisual='UnitySurface'
loadingBefore='Succeeded'
loadingAfter='Succeeded'
loadingBlockingIssues='0'
loadingAdapterCount='1'
loadingProgressSupported='False'
loadingProgress='Indeterminate'
```
