# Setup

Use this page to wire the current package baseline in a Unity project.

## Package dependencies

`com.immersive.framework` depends on:

- `com.immersive.foundation`
- `com.immersive.logging`

The framework package should be installed as a Unity package. Do not copy old project assets, scenes or `ProjectSettings` into the package.

## Game Application

Create a `GameApplicationAsset` and assign:

- Startup route: a `RouteAsset`.
- `UIGlobal` scene policy: `NoneConfigured` for explicit no-op visuals, or `Required` when shared visual surfaces must be loaded before the startup route.
- `UIGlobal` scene path/name when the policy requires it.

The runtime host loads the Game Application, then starts route/activity runtime state from the authored assets.

## UIGlobal scene

Use a dedicated `UIGlobal` scene when the project wants shared presentation surfaces.

Common contents:

- Loading surface GameObject with `UnityLoadingSurfaceAdapter`.
- Transition curtain/effect GameObject with `UnityFadeCurtainEffectAdapter`.
- Resident Pause panel with `UnityPauseResidentSurfaceAdapter`.
- Optional `PauseInputModeUnityPlayerInputRuntimeBridge` and `PauseInputActionRuntimeBridgeTrigger` when Pause input should be authored in the shared scene.

If the policy is `Required`, missing adapters can block startup diagnostics. If the policy is not configured, the framework keeps explicit no-op visual behavior.

## Route and Activity baseline

Create route and activity assets:

- `RouteAsset` for top-level route selection.
- `ActivityAsset` for route-owned activity selection.
- Optional route/activity content profiles when the project uses additive content scenes.

Route changes may use Transition and Loading surfaces. Activity changes may request visual wrapping depending on the authored activity visual transition mode.

## Pause input baseline

Use this path for authored Pause input:

```text
InputAction
  -> PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> PauseRuntime
  -> InputMode request
  -> Unity PlayerInput application
```

Do not add new usage of the retired direct Pause input adapter. The supported path is the runtime bridge trigger plus the runtime bridge.

## RuntimeContent / ContentAnchor setup

For explicit materialization paths:

- Author ContentAnchor declarations with route/activity ownership.
- Use RuntimeContent identities and handles as logical content state.
- Use Unity materialization adapters/bridges for physical prefab materialization.
- Use ContentAnchor placement adapters/services for physical placement under anchor transforms.
- Use explicit release/composite release paths for cleanup; do not assume route/activity auto-materialization.
