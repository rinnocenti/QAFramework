# IF-FW-F26D - Determinate Loading Progress Source

## Status
Accepted / Runtime Progress Bridge

## Context

F26C validated the visual receiver: `QA_UIGlobal` can present loading progress and the loading surface runtime can detect progress-capable adapters.

The remaining gap was source ownership. The framework still reported `loadingProgressMode='Indeterminate'` because no lifecycle operation was feeding determinate values into `LoadingSurfaceRequest.Update(...)`.

## Decision

F26D connects determinate progress from concrete Unity scene operations to the loading surface:

- `SceneLifecycleRuntime` reports progress during:
  - `SceneManager.LoadSceneAsync(...)`
  - `SceneManager.UnloadSceneAsync(...)`
- Route lifecycle propagates the reporter through:
  - route primary scene composition
  - route-owned additive scene release
  - activity-owned scene release on route change
  - startup Activity scene composition
- Activity lifecycle propagates the reporter through:
  - Activity-owned additive scene composition
  - Activity-owned additive scene release on Activity change or clear
- `FrameworkRuntimeHost` creates a loading-surface progress reporter only when:
  - a loading surface is visible for the operation
  - at least one resolved adapter supports progress presentation
- Request diagnostics now use the last observed determinate progress when a source reported progress.

## Runtime Semantics

The loading sequence remains unchanged:

`transition before -> loading show -> lifecycle load/release -> loading hide -> transition after`

F26D only adds progress updates during the lifecycle window. It does not move transition ownership into the loading surface and does not make visual adapters own lifecycle execution. Progress publication uses Unity `Awaitable` boundaries so loading-surface updates stay aligned with the existing visual adapter contract.

Expected successful operation diagnostics now include determinate evidence when a scene operation actually reports progress:

```text
loadingProgressSupported='True'
loadingProgressMode='Determinate'
loadingProgressValue='1.00'
loadingProgressPercent='100'
loadingProgressPhase='SceneLoad' or 'SceneUnload'
```

If a loading surface is shown but no scene operation emits progress, diagnostics remain supported but indeterminate. That remains valid behavior.

## Non-goals

- No fake progress animation.
- No DOTween fill tween.
- No progress smoothing.
- No weighted multi-step loading plan yet.
- No Addressables integration.
- No editor-only progress source.
- No broad migration of the pre-existing lifecycle `Task` return types; this cut only removes the F26D-added `Task` progress bridge and frame wait from the progress reporting path.

## Closeout note

F26D is closed as the determinate source cut. Visual inspection smoothing was added as QA presentation only, and weighted aggregation was completed in F26E. F26F records the final documentation baseline.


## F26D visual inspection correction

Small QA scenes can complete their `AsyncOperation` in one or two frames. In that case the core can correctly report determinate progress and still look visually static because the request jumps from `0` to `1` before a person can inspect the bar. The QA loading adapter now applies a QA-only minimum progress motion for determinate update requests. This is presentation smoothing only: the reported core value remains the real scene operation progress.

The no-op progress reporter also awaits a Unity `Awaitable` frame to avoid compiler warning CS1998 without introducing `Task` into the F26D progress path.
