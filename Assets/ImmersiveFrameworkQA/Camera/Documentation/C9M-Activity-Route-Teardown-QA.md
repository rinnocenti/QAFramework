# C9M — Activity Route Teardown QA

## Objective

Prove the exact regression path:

```text
Route with active startup Activity
-> request QA Hub Route
-> Activity receives OnActivityContentExited
-> Activity camera request releases
-> Route content releases
-> C9M scene unloads
```

The Activity is not cleared through `ActivityRequestTrigger`.
The only exit operation is the Route change.

## Source pattern

This cut follows the same materialization pattern used by:

```text
QaC9LPlayerCameraArbitrationInstaller
```

Specifically:

```text
LoadOrCreate<T> for ActivityAsset and RouteAsset
EditorSceneManager.NewScene for isolated scene creation
direct RouteRequestTrigger.TargetRoute assignment
SetString after direct assignment
direct QaHubPanel.entries materialization
EditorBuildSettingsScene registration
```

## Install

Remove the obsolete C9M files listed in `REMOVED-FILES.txt`.

Copy the `Assets` directory into `QAFramework`.

Run:

```text
Immersive Framework QA
└── Camera
    └── C9M Install Activity Route Teardown QA
```

The installer creates or repairs:

```text
Assets/ImmersiveFrameworkQA/Camera/Activities/
└── QA_C9M_ActivityRouteTeardownActivity.asset

Assets/ImmersiveFrameworkQA/Camera/Routes/
└── QA_C9M_ActivityRouteTeardownRoute.asset

Assets/ImmersiveFrameworkQA/Camera/Scenes/
└── QA_C9M_ActivityRouteTeardown.unity
```

It also adds one entry to the existing QA Hub:

```text
Camera / C9M Activity Route Teardown
```

No second panel is created.

## Expected logs

```text
[QA][C9M Activity Route Teardown] Activity teardown probe entered.
[QA][C9M Activity Route Teardown] Requesting Back to Hub while the startup Activity is still active.
[QA][C9M Activity Route Teardown] Activity teardown probe observed lifecycle exit before disable.
[QA][C9M Activity Route Teardown] PASS. Route change dispatched Activity exit before scene-authored content was disabled.
```

Camera evidence:

```text
Activity Camera Request Binding status='Released'
Route Camera Request Binding status='Released'
```

Route result:

```text
previousRoute='QA C9M Activity Route Teardown'
targetRoute='QA Hub Route'
kind='Succeeded'
blockingIssues='0'
```

## Forbidden

```text
Target Route is missing
Activity teardown ordering failed
Camera Request Binding status='Blocked'
invalid winner
rollback did not fully restore consistency
```

## Acceptance

```text
compiles
installer completes once
installer remains idempotent
single QA Hub panel
C9M Hub entry has non-null target Route
startup Activity remains active until Route request
Activity exit occurs before scene-authored content disable
Route request succeeds with blockingIssues='0'
```

## Commit message

```text
QA: prove active Activity teardown during Route change
```


## Compatibility correction

The current `CameraRigComposer` no longer owns or creates a Unity Camera.

The C9M installer therefore configures only:

```text
cinemachineCamera
createCinemachineCameraIfMissing
logApplyRebuildDiagnostics
```

The obsolete `createUnityCameraIfMissing` assignment was removed.


## Route completion synchronization

The C9M smoke no longer starts from `Start()` inside the target scene.

The Hub now contains:

```text
QA_C9M_RouteCompletionCoordinator
```

This root object:

```text
subscribes to the C9M Hub RouteRequestTrigger
waits for RouteRequestTriggerEvent Completed/Succeeded
survives the scene transition through DontDestroyOnLoad
finds QaC9MRouteChangeCoordinator in the loaded C9M scene
calls Begin only after the entry transition is complete
```

This prevents the Back to Hub request from being rejected by the active Transition Gate blocker.


## Synchronization hardening

`QaC9MRouteChangeCoordinator` no longer contains `Start()` or `runOnStart`.

Therefore:

```text
the target scene cannot start the smoke autonomously
serialized legacy values cannot bypass synchronization
Begin() is invoked only by QaC9MRouteCompletionCoordinator
one additional frame is allowed after Completed/Succeeded so the gate blocker can unwind
```


## Persistent trigger correction

The C9M Hub `RouteRequestTrigger` is now a child of:

```text
QA_C9M_RouteCompletionCoordinator
```

The coordinator root uses `DontDestroyOnLoad`, so both the subscriber and the trigger survive the Hub scene unload. This allows the trigger to publish its final `Completed/Succeeded` event after the C9M transition finishes.
