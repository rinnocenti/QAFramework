# Unity Build Surface QA Workspace

Workspace isolado para fixtures de QA da etapa F24.

## Current fixtures

```text
Scenes/QA_UIGlobal.unity
Scenes/TransitionRouteA.unity
Scenes/TransitionRouteB.unity
Scenes/ActivityAdditionalContent.unity
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
Scripts/Runtime/QaPauseSurfaceAdapter.cs
```

## Temporary tools

The editor installers used to create these fixtures are temporary and can be deleted after validation.

Historical setup notes were removed from the QA Project legacy documentation root in `POST-RESET-B5`. Keep future operational notes in `Assets/ImmersiveFrameworkQA/Documentation`.

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

## F26C Loading progress bar receiver

`Scenes/QA_UIGlobal.unity` now includes a QA progress bar under `LoadingPanel`:

```text
LoadingProgressRoot
  LoadingProgressTrack
    LoadingProgressFill
```

The bar is wired to `QaLoadingSurfaceVisibilityHoldAdapter` through the optional `Progress Presentation` fields. The adapter now consumes `LoadingSurfaceRequest.Progress` when `ProgressSupported=true`; otherwise it stays in an indeterminate/reset state.

The framework `UnityLoadingSurfaceAdapter` has the same optional progress receiver fields for non-QA surfaces. `LoadingSurfaceRuntime.ProgressSupported` becomes true when a resolved adapter has progress presentation configured, but no real determinate loading source is invented in this cut.


## F26D Determinate loading progress source

F26D keeps the F26C progress bar receiver and connects it to real Unity scene operation progress. During route/activity operations that execute `LoadSceneAsync` or `UnloadSceneAsync`, `FrameworkRuntimeHost` forwards determinate progress updates to the resolved loading surface.

Expected smoke evidence after a scene load/release operation with the QA loading surface configured:

```text
loadingProgressSupported='True'
loadingProgressMode='Determinate'
loadingProgressPercent='100'
```

If no scene operation occurs, the progress mode may remain `Indeterminate`; that is still valid because no real source reported progress.


### F26D loading progress visual inspection

The QA loading adapter smooths determinate progress updates for a short minimum visual duration. This makes tiny QA scene loads/release operations inspectable without changing the determinate progress value reported by the framework diagnostics.


## F26E Aggregated loading progress

F26E keeps the F26D visual smoothing and changes the framework progress source from isolated per-scene `0..1` reporting to weighted operation-level reporting. Route transitions now aggregate activity-scene release, route content release, route scene composition and route startup Activity scene work into one route transition progress window. Activity transitions aggregate Activity scene composition and release into one Activity transition progress window.

Expected final request evidence:

```text
loadingProgressMode='Determinate'
loadingProgressPhase='RouteTransition'
loadingProgressPercent='100'
```

or, for Activity loading presentation:

```text
loadingProgressMode='Determinate'
loadingProgressPhase='ActivityTransition'
loadingProgressPercent='100'
```


## F26F Loading progress polish / closeout

The QA Activity content scene typo was corrected:

```text
AtivityAdditionalConent.unity -> ActivityAdditionalContent.unity
```

`ActivityContentProfile.asset` now points to the corrected scene path and scene name. When applying this change from a zip patch, delete the old typo scene files listed in:

```text
AtivityAdditionalConent.unity
AtivityAdditionalConent.unity.meta
```

The corrected scene keeps the original Unity `.meta` GUID so existing references remain stable after cleanup.

F26F also records the final loading progress rule: framework diagnostics report technical progress, while the QA loading adapter may smooth visual fill movement for readability. Smoothing is presentation only and does not change `loadingProgressValue` / `loadingProgressPercent` diagnostics.

## F27A Pause UIGlobal Surface Baseline

`Scenes/QA_UIGlobal.unity` now also contains a QA Pause surface adapter and a `PauseRequestTrigger` on the persisted UIGlobal root.

The QA adapter presents the current logical Pause snapshot and exposes manual IMGUI buttons for:

```text
Pause
Resume
Toggle
```

Expected boot evidence:

```text
pauseAdapterCount='1'
Pause surface resolved from UIGlobal scene 'QA_UIGlobal' with adapterCount='1'
```

Expected request evidence after Toggle/Pause:

```text
Pause Request completed.
currentState='Paused'
pauseSurface='Succeeded'
pauseSurfaceVisual='UnitySurface'
pauseSurfaceAdapterCount='1'
```

This is still QA surface validation only. It does not bind keyboard/controller input, does not change `Time.timeScale` and does not own Route or Activity lifecycle.



## F27B Pause Input Binding

The canonical authored Pause input path for `QA_UIGlobal` is now:

```text
PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
```

The QA input asset has `PauseToggle` in both `Player` and `UI` maps. Default bindings are Escape and Gamepad Start.

This bridge path is intentionally narrow: it validates the explicit Unity input evidence, forwards the Pause request and keeps Pause, InputMode and PlayerInput synchronized without owning InputMode policy or player join/spawn behavior.
