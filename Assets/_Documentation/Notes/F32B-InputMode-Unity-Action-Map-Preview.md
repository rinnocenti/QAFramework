
# F32B — InputMode Unity Action Map Preview

Status: implemented / pending smoke.

## Intent

F32B adds passive action-map evidence on top of F32A.

F32A answered:

```text
Does this InputMode request have enough canonical Unity Input references to be applied later?
```

F32B answers:

```text
Which Unity action map would be requested later, and is that map present in the available action-map evidence?
```

This cut still does not call `PlayerInput.SwitchCurrentActionMap`, `ActivateInput`, `DeactivateInput`, `PlayerInputManager.JoinPlayer`, prefab spawn or movement.

## Canonical preview bindings

Initial QA/default bindings:

| InputMode | Unity action map | Required |
|---|---|---:|
| Gameplay | Player | yes |
| PauseOverlay | UI | yes |
| FrontendMenu | UI | yes |
| InputLocked | none | no |

These names match the default QA `InputSystem_Actions.inputactions` shape currently present in the consumer project. Later authoring can make the names project-configurable.

## New runtime types

```text
Runtime/UnityInput/UnityInputActionMapName.cs
Runtime/UnityInput/UnityInputActionMapEvidence.cs
Runtime/InputMode/InputModeUnityActionMapBinding.cs
Runtime/InputMode/InputModeUnityActionMapBindings.cs
Runtime/InputMode/InputModeUnityActionMapPreviewEvaluator.cs
Runtime/InputMode/InputModeUnityActionMapPreviewResult.cs
Runtime/InputMode/InputModeUnityActionMapPreviewIssue.cs
Runtime/InputMode/InputModeUnityActionMapPreviewIssueKind.cs
Runtime/InputMode/InputModeUnityActionMapPreviewStatus.cs
```

## QA

Button:

```text
Run InputMode Unity Action Map Preview Smoke
```

Expected steps:

```text
contracts
gameplay-resolves-player-map
pause-overlay-resolves-ui-map
frontend-menu-resolves-ui-map
missing-gameplay-map-blocking
inputlocked-no-action-map-required
no-action-map-switching
```

All steps must keep:

```text
actionMapSwitching='False'
inputBehavior='False'
playerInputActivation='False'
playerJoin='False'
actorSpawning='False'
customInputManager='False'
```

## Non-goals

F32B does not apply InputMode to Unity Input. It only validates action-map evidence.
