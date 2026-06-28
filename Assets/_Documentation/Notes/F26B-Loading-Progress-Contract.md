# IF-FW-F26B - Loading Progress Contract

## Status
Accepted / Diagnostics + Documentation

## Context

F26A and F26A1 are closed. Activity-owned additive scenes are already part of Activity discovery, and participant diagnostics are already split from local Activity content diagnostics.

The remaining gap is that loading logs still expose only a legacy `loadingProgress='Indeterminate'` field and a `loadingProgressSupported` boolean. That is enough to report a surface-level state, but it is not a clear internal contract for loading progress itself.

## Decision

F26B introduces an internal loading progress contract for framework runtime diagnostics:

- `FrameworkLoadingProgressMode`
  - `Unknown`
  - `Indeterminate`
  - `Determinate`
- `FrameworkLoadingProgress`
  - `Supported`
  - `Mode`
  - `Value01`
  - `Percent`
  - `Phase`
  - `Message`

Invariants:

- `Value01` is clamped to `0..1`.
- `Percent` is derived from `Value01` and stays in `0..100`.
- `NaN` is rejected.
- if progress is not supported, the mode stays `Indeterminate` or `Unknown`.
- the contract does not invent real progress when no real source exists.

Logging now has explicit fields for:

- `loadingProgressSupported`
- `loadingProgressMode`
- `loadingProgressValue`
- `loadingProgressPercent`
- `loadingProgressPhase`
- `loadingProgressMessage`

The legacy `loadingProgress` field is kept temporarily as a compatibility alias for the mode text.

## Non-goals

- No loading bar UI.
- No direct binding to `SceneManager.LoadSceneAsync` progress yet.
- No change to F25 visual/loading semantics.
- No change to F26A discovery semantics.
- No Pause, Save, Player, Camera, Audio, Addressables or gameplay adapter changes.

## Reading Logs

Current interpretation:

- `loadingProgressSupported='False'` means the runtime still does not expose a real progress source for that operation.
- `loadingProgressMode='Indeterminate'` means the framework can describe loading state, but it is not reporting a determinate 0..1 source yet.
- `loadingProgressValue='0.00..1.00'` is the normalized progress snapshot when progress becomes determinate.
- `loadingProgressPercent='0..100'` is derived from `loadingProgressValue`.
- `loadingProgressPhase='...'` names the coarse phase or source of the loading observation.
- `loadingProgressMessage='...'` explains the current progress state.

F26C or the next loading cut can connect `SceneLifecycleRuntime` to a real progress source.
F26D or the next loading cut can let `LoadingSurface` consume percent/progress display data.
