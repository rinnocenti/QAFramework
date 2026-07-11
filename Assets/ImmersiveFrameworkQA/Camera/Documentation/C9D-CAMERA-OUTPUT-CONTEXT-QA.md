# C9D — CameraOutputContext QA

## Objective

Prove the single-output runtime authority through the canonical QA flow:

```text
QA Hub
-> Camera / C9D Output Context
-> dedicated Route
-> dedicated synthetic scene
-> runtime fixture
```

## Installer

Run:

```text
Immersive Framework QA/Camera/C9D Install Camera Output Context QA
```

The installer creates or repairs:

```text
Assets/ImmersiveFrameworkQA/Camera/Activities/QA_CameraOutputContextActivity.asset
Assets/ImmersiveFrameworkQA/Camera/Routes/QA_CameraOutputContextRoute.asset
Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_CameraOutputContext.unity
```

It also registers the Route in `QA_Hub.unity`, adds Back to Hub navigation and adds the scene to Build Settings.

## Runtime cases

```text
winner lifecycle and restoration
lower precedence preserves winner
deterministic equal-precedence tie-breaker
missing tie-breaker blocked
duplicate tie-breaker blocked
duplicate request id blocked
foreign output blocked
unknown release explicit
snapshot ordering
camera state unchanged
```

## Expected PASS

```text
[QA][C9D Camera Output Context] PASS. status='Passed' cases='10'
```

## Boundaries

This QA does not prove:

```text
Route/Activity/Player publishers
automatic lifetime observation
Cinemachine priority application
blending
multi-output registry
```

## Suggested commit

```text
QA: prove C9D camera output context runtime
```
