# F30A — InputMode Identity / State / Request Result Contracts

## Status

Closed / runtime contracts + QA smoke.

## Purpose

F30A opens the InputMode track after F29 proved that Unity Input targets can be declared and diagnosed explicitly.

This cut creates the framework language for logical input posture:

```text
current input posture
  -> request target posture
  -> deterministic result
  -> no Unity Input behavior yet
```

F30A is intentionally passive. It does not own Unity Input System state, does not switch action maps, does not read controls and does not bind player/actor gameplay.

## Why this exists

F29 answered:

```text
which authored objects are the explicit input targets?
```

F30A answers:

```text
which logical input mode is current?
what does a request to change it look like?
what result does that request produce?
```

It does not answer:

```text
which PlayerInput receives action-map changes?
how Pause applies the mode?
how movement is blocked?
how gameplay commands are routed?
```

Those are later cuts.

## Added runtime contracts

Placement:

```text
Packages/com.immersive.framework/Runtime/InputMode/
```

Contracts:

| Contract | Role |
|---|---|
| `InputModeKind` | Canonical mode vocabulary: `Gameplay`, `PauseOverlay`, `FrontendMenu`, `InputLocked`. |
| `InputModeId` | Typed identity for a mode. Domain: `InputMode`. |
| `InputModeDefinition` | Passive definition of a canonical mode. |
| `InputModeDefinitions` | Factory for the four initial canonical mode definitions. |
| `InputModeRules` | Pure validation helpers. |
| `InputModeState` | Passive current-state snapshot. |
| `InputModeRequest` | Passive request to change mode. |
| `InputModeRequestStatus` | Result status vocabulary. |
| `InputModeRequestIssueKind` | Diagnostic issue vocabulary. |
| `InputModeRequestIssue` | Diagnostic issue entry. |
| `InputModeRequestResult` | Deterministic request result. |
| `InputModeRequestEvaluator` | Pure preview/evaluator. It does not own runtime state. |

Identity domain added:

```text
FrameworkIdentityDomain.InputMode
```

## QA smoke

New runner:

```text
Packages/com.immersive.framework/Runtime/Diagnostics/InputModeContractQaSmokeRunner.cs
```

QA Canvas path:

```text
Unity Input Diagnostics
  -> Run InputMode Contract Smoke
```

Expected steps:

```text
contracts
initial-state
valid-gameplay-request
valid-pause-overlay-request
ignored-same-mode-request
invalid-mode-request-no-side-effects
no-action-map-switching
```

## Accepted behavior

A request from `Gameplay` to `PauseOverlay` returns:

```text
Succeeded
previousMode = Gameplay
currentMode = PauseOverlay
revision increments
```

A request to the same mode returns:

```text
IgnoredAlreadyInMode
previousMode unchanged
currentMode unchanged
revision unchanged
```

A request to `Unknown` returns:

```text
FailedInvalidTargetMode
previousMode unchanged
currentMode unchanged
revision unchanged
blocking issue = 1
```

All result/state objects expose:

```text
actionMapSwitching = False
inputBehavior = False
```

## Explicit exclusions

F30A does not create:

```text
InputMode owner runtime
Pause bridge
Unity action-map switching
Unity PlayerInput ownership
player movement
player/actor spawning
camera/audio/save/gameplay adapters
per-consumer Gate query policy
```

## Next cut correction

The original next idea, an `InputModeOwnerPreview`, is rejected before application.

F30B must instead document the Unity PlayerInput integration boundary:

```text
Unity PlayerInput / PlayerInputManager remain the input execution components
InputMode stays passive request/result language
UnityInputTargetDeclaration stays an integration-point declaration
no framework input manager is introduced
```

Reference: `Assets/_Documentation/Notes/F30B-Unity-PlayerInput-Integration-Boundary.md`.
