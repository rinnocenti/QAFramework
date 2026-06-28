# IF-FW-F26C - Loading Surface Progress Bar Receiver

## Status
Accepted / Unity Surface Receiver

## Context

F26B introduced the internal loading progress diagnostic contract and clarified that the framework must not invent determinate progress before a real source exists.

The project already had `LoadingSurfaceRequest.Progress` and `LoadingSurfaceRequest.ProgressSupported`, but the Unity-facing loading surface did not consume those fields visually. The QA `LoadingPanel` also had no progress bar receiver wired in the canonical `QA_UIGlobal` scene.

## Decision

F26C prepares the Unity loading surface to receive progress without creating a fake progress source.

Changes:

- `LoadingSurfaceRequest` now has `Show`, `Update` and `Hide` factory overloads that accept:
  - `LoadingProgress progress`
  - `bool progressSupported`
- `ILoadingSurfaceProgressPresentationAdapter` marks adapters that can consume request progress visually.
- `LoadingSurfaceRuntime.ProgressSupported` now reflects whether at least one resolved surface adapter has progress presentation configured.
- `UnityLoadingSurfaceAdapter` can present optional progress references:
  - `Progress Root`
  - `Progress Fill Image`
  - `Progress Slider`
- `QaLoadingSurfaceVisibilityHoldAdapter` can present optional progress references and records QA-facing progress evidence:
  - supported / determinate state
  - normalized value
  - rounded percent
  - progress message
- `QA_UIGlobal.unity` now contains a simple `LoadingProgressRoot` under `LoadingPanel`.

## Runtime Semantics

When a `LoadingSurfaceRequest` carries `ProgressSupported=true`, the adapter applies the request progress to:

- `Image.fillAmount`, when a fill image is assigned.
- `Slider.normalizedValue`, when a slider is assigned.

When `ProgressSupported=false`, the adapter keeps the progress presentation in an indeterminate/reset state. This is intentional: the visual receiver is ready, but the loading lifecycle still needs a real source before determinate movement is valid.

Request-level diagnostics can now report `loadingProgressSupported='True'` when a resolved surface adapter has a progress receiver configured. Until a real source is connected, the mode should remain `Indeterminate`, not `Determinate`.

## Non-goals

- No fake progress animation.
- No DOTween progress fill.
- No Addressables integration.
- No direct `SceneManager.LoadSceneAsync.progress` bridge yet.
- No change to transition/loading ordering.
- No loading lifecycle ownership inside the visual adapter.

## Next Cut

The next logical cut is to connect a real source, most likely `SceneLifecycleRuntime` load/unload progress, into `LoadingSurfaceRequest.Update(...)` while preserving the current sequence:

`transition fade-in -> loading show/update/hide -> transition fade-out`
