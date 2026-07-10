# C7 — Camera Product Surface Regression FindObjects Hotfix Manifest

Status: hotfix
Scope: QAFramework only

## Objective

Remove obsolete Unity `FindObjectsByType` overload usage from the C5/C7 Camera Product Surface regression smoke.

## Files Changed

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC5CameraComposerSinglePlayerSmoke.cs
```

## Change

Replaced deprecated overloads:

```text
FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
```

with the current overload:

```text
FindObjectsByType<T>(FindObjectsInactive.Include)
```

## Out Of Scope

```text
package changes
FIRSTGAME changes
new smoke coverage
new camera contracts
scene edits
```

## Acceptance Criteria

```text
QAFramework compiles without CS0618 warnings from QaC5CameraComposerSinglePlayerSmoke.cs
C7 Camera Product Surface Regression Smoke still passes
C5 remains authoritative for the actual regression result
```

## Commit Message

```text
QA: remove obsolete CameraComposer smoke object queries
```
