# C9M — Route-Change Activity Teardown QA

## Correction in v3

The QA Hub already owns the route-selection UI through:

```text
QaHubPanel.entries
```

C9M no longer creates a separate IMGUI panel.

The installer now:

```text
creates a child RouteRequestTrigger under QA_HubCanvas
assigns the C9M RouteAsset explicitly
validates TargetRoute after serialization
adds or repairs the C9M entry in QaHubPanel.entries
removes the obsolete QA_C9M_ActivityRouteTeardownLauncher object
```

## Install

Replace the prior C9M files and delete:

```text
Assets/ImmersiveFrameworkQA/Camera/Runtime/QaC9MHubLauncher.cs
Assets/ImmersiveFrameworkQA/Camera/Runtime/QaC9MHubLauncher.cs.meta
```

Run:

```text
Immersive Framework QA
└── Camera
    └── C9M Install Route-Change Activity Teardown QA
```

## Hub result

The existing QA Hub panel receives one new button:

```text
Camera / C9M Activity Route Teardown
```

No second panel is created.

## Runtime flow

```text
QA Hub
-> C9M Route
-> startup Activity remains active
-> coordinator requests Back to Hub
-> Activity exit is dispatched
-> Activity camera request releases
-> C9M scene unloads
```

## PASS evidence

```text
[QA][C9M Activity Route Teardown] Requesting Back to Hub while the startup Activity is still active.
[QA][C9M Activity Route Teardown] Activity teardown probe observed lifecycle exit before disable.
[QA][C9M Activity Route Teardown] PASS. Route change dispatched Activity exit before scene-authored content was disabled.
```

## Forbidden

```text
Target Route is missing
Activity teardown ordering failed
invalid winner
rollback did not fully restore consistency
```

## Commit message

```text
QA: integrate C9M teardown smoke into existing Hub
```


## Correction in v4

`RouteRequestTrigger.TargetRoute` is now assigned through the component's public authoring API:

```csharp
trigger.TargetRoute = targetRoute;
```

`SerializedObject` remains used only for the private serialized `reason` field.

This avoids the failed immediate object-reference round-trip that left `TargetRoute` null in v3.
