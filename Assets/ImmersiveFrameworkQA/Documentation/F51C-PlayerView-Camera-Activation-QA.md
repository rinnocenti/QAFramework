# F51C — PlayerView Camera Activation QA

## Objective

Validate that the framework can activate one explicit Unity Camera from PlayerView camera-target binding evidence.

## Scene

```text
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerViewCameraActivation.unity
```

## Hub button

```text
PlayerView Camera Activation QA
```

## Expected result

```text
[F51C_PLAYER_VIEW_CAMERA_ACTIVATION_QA] status='Succeeded'
```

## Coverage

- Component references.
- Successful camera activation.
- Missing camera-target binding.
- Missing camera-target binding target.
- Missing camera activation target.
- Missing explicit camera.
- Clear no-op.
- Clear after activation.
- Boundary: camera activation may be true; control/input/movement/actor spawning remain false.

## Out of scope

- Cinemachine.
- CameraDirector.
- Camera priority.
- Runtime lifecycle.
- Control/input/movement.
- FIRSTGAME integration.
