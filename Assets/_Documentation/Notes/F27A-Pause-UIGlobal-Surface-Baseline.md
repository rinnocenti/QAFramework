# IF-FW-F27A — Pause UIGlobal Surface Baseline

## Status

Ready for smoke.

## Purpose

F27A implements the deferred Unity-facing Pause surface objective after the F26 loading progress closeout.

The cut proves that the existing logical `PauseRuntime` can drive a visual surface collected from the canonical `UIGlobal` scene without moving Pause ownership into UI, input or gameplay.

## Boundary

Pause ownership remains:

```text
PauseRuntime
  owns logical state: Running / Paused
  owns PauseSnapshot and Pause Gate snapshot

PauseSurfaceRuntime
  applies the latest PauseSnapshot to visual adapters
  does not decide state
  does not evaluate Gate
  does not touch Route/Activity lifecycle
  does not change Time.timeScale

UIGlobal Pause adapter
  presents the snapshot
  may expose QA buttons for Pause/Resume/Toggle
  does not own PauseRuntime
```

## Runtime additions

Added framework runtime files:

```text
Packages/com.immersive.framework/Runtime/Pause/IPauseSurfaceAdapter.cs
Packages/com.immersive.framework/Runtime/Pause/PauseSurfaceRuntime.cs
Packages/com.immersive.framework/Runtime/Pause/PauseRequestTrigger.cs
```

`GlobalUiSceneRuntime` now collects `IPauseSurfaceAdapter` from persisted UIGlobal roots.

`FrameworkRuntimeHost` now creates `PauseSurfaceRuntime` after UIGlobal load and applies the initial Running snapshot. Each `RequestPause(...)` applies the updated snapshot to the Pause surface before logging the final request result.

## QA surface

Added QA adapter:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scripts/Runtime/QaPauseSurfaceAdapter.cs
```

`QA_UIGlobal.unity` now has:

```text
PauseRequestTrigger
QaPauseSurfaceAdapter
```

attached to the persisted UIGlobal root.

The QA adapter uses IMGUI for manual validation. It draws Pause/Resume/Toggle buttons and a compact state panel. It is intentionally QA-only and not a product pause menu prefab.

## Expected boot evidence

```text
UIGlobal scene 'QA_UIGlobal' loaded and persisted ... pauseAdapterCount='1'
Pause surface resolved from UIGlobal scene 'QA_UIGlobal' with adapterCount='1'
```

## Expected Pause request evidence

Pause or Toggle to Paused:

```text
Pause Request completed.
currentState='Paused'
pauseSurface='Succeeded'
pauseSurfaceVisual='UnitySurface'
pauseSurfaceAdapterCount='1'
pauseSurfaceState='Paused'
pauseSurfacePaused='True'
```

Resume or Toggle to Running:

```text
Pause Request completed.
currentState='Running'
pauseSurface='Succeeded'
pauseSurfaceVisual='UnitySurface'
pauseSurfaceAdapterCount='1'
pauseSurfaceState='Running'
pauseSurfacePaused='False'
```

## Non-goals

- No Input System binding.
- No Escape key / controller mapping.
- No Time.timeScale policy.
- No gameplay freeze adapter.
- No route/activity lifecycle ownership.
- No production pause menu layout.

These remain later adapter/policy cuts.
