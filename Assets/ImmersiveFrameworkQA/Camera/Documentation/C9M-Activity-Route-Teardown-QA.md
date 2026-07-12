# C9M — Activity Route Teardown QA

## Objective

Prove that a scene-authored Activity lifecycle receiver receives:

```text
OnActivityContentExited
```

before its GameObject is disabled or destroyed during a Route change.

This regression specifically protects the ordering required by:

```text
ActivityCameraRequestBinding
RouteCameraRequestBinding
LocalPlayerCameraRequestBinding
CameraOutputSession
```

## Base reused

```text
Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity
```

The existing C9L scene already contains:

```text
QA_C9L_ActivityContent
├── ActivityLocalVisibilityAdapter
└── ActivityCameraRequestBinding
```

C9M adds only:

```text
QaC9MActivityRouteTeardownProbe
```

to the same GameObject.

## Install

Run:

```text
Immersive Framework QA
└── Camera
    └── C9M Install Activity Route Teardown QA
```

The installer is idempotent and saves the scene only when the component is missing.

## Smoke

1. Start the C9L Player Camera Arbitration route.
2. Confirm the Activity camera request is published.
3. Use the existing Back to Hub route request.
4. Inspect the Console.

Expected:

```text
[QA][C9M Activity Route Teardown] Activity teardown probe observed lifecycle exit before disable.
[QA][C9M Activity Route Teardown] PASS. Activity exit was observed before the scene-authored object was disabled.
```

Camera evidence must also remain:

```text
Activity Camera Request Binding status='Released'
Route Camera Request Binding status='Released'
Local Player Camera Request Binding status='Released'
```

Forbidden:

```text
Activity teardown ordering failed
Camera Request Binding status='Blocked'
invalid winner
rollback did not fully restore consistency
```

## Acceptance

```text
compiles
installer is idempotent
probe is attached to QA_C9L_ActivityContent
Activity exit occurs before OnDisable
Route change succeeds
blockingIssues='0'
no inconsistent CameraOutputSession rollback
```

## Files created

```text
Assets/ImmersiveFrameworkQA/Camera/Runtime/QaC9MActivityRouteTeardownProbe.cs
Assets/ImmersiveFrameworkQA/Camera/Editor/QaC9MActivityRouteTeardownInstaller.cs
Assets/ImmersiveFrameworkQA/Camera/Documentation/C9M-Activity-Route-Teardown-QA.md
```

## Commit message

```text
QA: prove Activity teardown before Route scene unload
```
