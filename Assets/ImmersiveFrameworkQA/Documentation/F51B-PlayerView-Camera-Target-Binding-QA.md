# F51B — PlayerView Camera Target Binding QA

This QA scene validates the explicit Unity camera-target binding adapter introduced in F51B.

## Scene

```text
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerViewCameraTargetBinding.unity
```

## Hub button

```text
PlayerView Camera Target Binding QA
```

## Expected smoke

```text
[F51B_PLAYER_VIEW_CAMERA_TARGET_BINDING_QA] status='Succeeded'
```

## Covered cases

```text
component references
successful camera-target binding
missing PlayerView binding evidence
missing PlayerViewBehaviour evidence
missing Unity Transform view target
missing camera-target binding target
clear no-op
clear after bind
passive boundary
```

## Boundary

Successful F51B may set:

```text
viewBinding='True'
cameraTargetBinding='True'
```

It must not set:

```text
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```
