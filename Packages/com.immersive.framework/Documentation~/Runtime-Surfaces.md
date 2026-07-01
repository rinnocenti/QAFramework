# Runtime Surfaces

This page summarizes the current runtime-facing surfaces exposed by `com.immersive.framework`.

## Surface model

Loading, Transition and Pause are current runtime-facing surfaces. A Surface is a stable runtime capability boundary with readiness, diagnostics and consumer-facing result language; it is not a synonym for any visual component.

Adapters execute local side effects and return evidence. They do not decide lifecycle, route/activity ownership or cross-domain policy. Multi-step orchestration belongs in explicit runtime services.

`UIGlobal` is a runtime surface host/consumer for app/session scoped presentation. It is not a universal manager.

## Game Application

`GameApplicationAsset` is the project-authored application root. It selects the startup route and controls whether a canonical app/session scoped `UIGlobal` scene is loaded.

`FrameworkRuntimeHost` is the runtime owner that starts the application, loads `UIGlobal` when configured, requests route/activity changes and coordinates visual surfaces. It is internal runtime infrastructure, not a user-authored manager.

## Route and Activity

Route baseline:

- `RouteAsset`
- route lifecycle runtime
- route content bindings and lifecycle receivers
- route content scene composition

Activity baseline:

- `ActivityAsset`
- activity flow runtime
- activity content lifecycle receivers
- activity content scene composition/release
- activity operation planning and execution

Route owns top-level navigation. Activity owns the active unit inside the route. Activity content does not replace route lifecycle.

## UIGlobal

`UIGlobal` is an optional shared scene for app/session scoped presentation. It can provide:

- Transition adapters.
- Loading adapters.
- Pause surface adapters.

When configured as required, missing surfaces are reported by diagnostics. When not configured, runtime uses explicit no-op behavior.

## Loading

Loading runtime surfaces include:

- `UnityLoadingSurfaceAdapter`
- `ILoadingSurfaceAdapter`
- `ILoadingSurfaceProgressPresentationAdapter`
- loading result/readiness/progress contracts

Loading presentation is separate from transition effects.

## Transition

Transition runtime surfaces include:

- `ITransitionOrchestrator`
- `NoOpTransitionOrchestrator`
- transition requests/plans/results
- transition effect adapters
- `UnityFadeCurtainEffectAdapter`

Transition controls the visual envelope around route/activity operations; it is not a loading screen by itself.

## Pause

Pause runtime surfaces include:

- `PauseRuntime`
- `PauseRequestTrigger`
- `PauseSurfaceRuntime`
- `IPauseSurfaceAdapter`
- `UnityPauseResidentSurfaceAdapter`

The production-facing default is a resident Pause panel in `UIGlobal` that is shown/hidden from logical Pause state.

## Pause input and InputMode

Current Pause input path:

- `PauseInputActionRuntimeBridgeTrigger`
- `PauseInputModeUnityPlayerInputRuntimeBridge`
- `PauseInputModeRequestMapper`
- `InputModeUnityPlayerInputRequestApplication`
- Unity `PlayerInput`

The bridge path keeps logical Pause, `InputMode` and Unity `PlayerInput` synchronized. It does not create a framework input manager, call `PlayerInputManager.JoinPlayer`, spawn players or read gameplay commands.

Pause visual surface application and Pause/InputMode `PlayerInput` apply are separate boundaries. The visual Pause surface shows or hides resident UI; the InputMode apply path synchronizes logical Pause intent with Unity `PlayerInput` through an explicit apply boundary.

## RuntimeContent

RuntimeContent owns logical content identity and state:

- `RuntimeContentHandle`
- `RuntimeMaterializationRequest`
- `RuntimeMaterializationResult`
- `RuntimeReleaseRequest`
- `RuntimeReleaseResult`
- lifecycle materialization registry and release plan/execution contracts

RuntimeContent handles are logical evidence. Physical Unity objects stay in adapter evidence and materialization services.

## ContentAnchor

ContentAnchor owns anchor declarations, logical binding and Unity adapter orchestration:

- `ContentAnchorDeclaration`
- `RouteContentAnchor`
- `ActivityContentAnchor`
- `ContentAnchorSet`
- `RuntimeContentAnchorBinding`
- `ContentAnchorBindingCleanup`
- `ContentAnchorReleaseExecution`
- `ContentAnchorMaterializationService`
- `UnityContentAnchorMaterializationBridge`
- `UnityContentAnchorMaterializationBridgeSet`
- `UnityContentAnchorPlacementAdapter`
- composite lifecycle release executor

The bridge components are authored Unity wrappers. The reusable materialization orchestration belongs to the non-MonoBehaviour service path.
