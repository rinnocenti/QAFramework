# F31B — Session PlayerInputManager Boundary

Status: Closed / implementation + QA.

## Purpose

Correct the ownership of Unity `PlayerInputManager`: it is Session-scoped integration evidence, not Activity-scoped content.

A Route such as character creation can require player joining or player presence before a gameplay Activity exists. Therefore, the `PlayerInputManager` integration point must be available at Session level.

## Decision

| Concern | Decision |
|---|---|
| Unity `PlayerInputManager` execution | Official Unity component. |
| Framework ownership | Session-level evidence and validation only. |
| Activity ownership | Rejected for canonical manager. Activities may consume players, not own the manager. |
| Route ownership | Rejected for canonical manager. Routes may use session input state, not own the manager. |
| Join/player prefab policy | Deferred to later Unity adapter cuts. |

## Runtime changes

Added:

```text
Runtime/UnityInput/UnityInputPlayerInputManagerScope.cs
Runtime/UnityInput/SessionPlayerInputManagerDeclaration.cs
Runtime/Diagnostics/SessionPlayerInputManagerBoundaryQaSmokeRunner.cs
```

Updated:

```text
UnityInputPlayerInputManagerEvidence
UnityInputTargetValidator
UnityInputTargetSetIssueKind
FrameworkQaCanvas
```

## What this proves

The framework can now state and validate:

```text
there must be one Session-scoped PlayerInputManager evidence point
zero is blocking when the session manager is required
more than one is blocking
this evidence is available before Route/Activity ownership
```

## What this does not do

F31B does not:

```text
call JoinPlayer
instantiate player prefabs
activate/deactivate PlayerInput
switch action maps
create a custom input manager
move a player
spawn actors
connect Pause to Unity input behavior
```

## QA

Run:

```text
Run Session PlayerInputManager Boundary Smoke
```

Expected steps:

```text
contracts
session-playerinputmanager-required-valid
session-playerinputmanager-missing-blocking
session-playerinputmanager-duplicate-blocking
session-scope-before-route-activity
no-unity-input-behavior
```

All steps must keep:

```text
actionMapSwitching='False'
inputBehavior='False'
playerJoin='none'
playerPrefabSpawn='none'
```

## Next

F31C should decide the authored QA fixture for a Session PlayerInputManager object, or defer fixture authoring until a session content surface is explicitly available.
