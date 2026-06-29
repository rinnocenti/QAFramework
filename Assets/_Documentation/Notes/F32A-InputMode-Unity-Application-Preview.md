# F32A — InputMode Unity Application Preview

Status: Closed by patch / awaiting QA smoke.
Type: runtime preview + QA smoke.

## Sequence correction

F32A is the real continuation after the documented closeouts:

```text
F30E — InputMode / Unity Input Boundary Closeout
F31C — PlayerActor / Session Unity Input Reference Closeout
F32A — InputMode Unity Application Preview
```

The previously proposed `F31D — PlayerInput Reference Set` is cancelled and must not be applied or counted.

## Purpose

F32A reconnects the input track after the canonical references closed by F30/F31:

```text
F30 InputModeRequest / InputModeRequestResult
F30 PauseInputModeRequestMapper
F31 PlayerActorDeclaration : IActor + PlayerInput evidence
F31 SessionPlayerInputManagerDeclaration + PlayerInputManager evidence
F29/F30 UnityInputTargetDeclaration / UnityInputTargetSet
```

The cut answers a narrow question:

```text
Given a successful InputMode request, is there enough official Unity Input evidence to apply it later?
```

It does not apply the mode yet.

## Accepted mapping

| InputMode | Required Unity Input target | Requires PlayerActor | Requires Session PlayerInputManager |
|---|---:|---:|---:|
| Gameplay | GameplayCommands | Yes | Yes |
| PauseOverlay | GlobalUiPause | No | No |
| FrontendMenu | GlobalUiPause | No | No |
| InputLocked | None in F32A preview | No | No |

## Runtime added

```text
Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityApplicationPreviewStatus.cs
Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityApplicationPreviewIssueKind.cs
Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityApplicationPreviewIssue.cs
Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityApplicationPreviewResult.cs
Packages/com.immersive.framework/Runtime/InputMode/InputModeUnityApplicationPreviewEvaluator.cs
```

The evaluator consumes existing evidence snapshots directly. No aggregate reference set is introduced in F32A.

```text
InputModeRequestResult
UnityInputTargetSet
PlayerActorSet
UnityInputPlayerInputManagerEvidence
```

## QA smoke

```text
InputMode Unity Application Preview Smoke
```

Expected steps:

```text
contracts
gameplay-requires-playeractor-session-manager
pause-overlay-targets-global-ui
missing-playeractor-blocking
missing-session-playerinputmanager-blocking
inputlocked-no-target-required
no-unity-input-behavior
```

## Non-goals

F32A does not:

```text
switch action maps
call PlayerInput.SwitchCurrentActionMap
activate or deactivate PlayerInput
call PlayerInputManager.JoinPlayer
spawn a player prefab
move a PlayerActor
own Unity input
create a custom input manager
integrate Pause dispatch with Unity input behavior
create a new F31 reference aggregation layer
```

## Next cut

F32B should define the Unity adapter boundary for action-map/application behavior.
It must keep action-map names project-owned/adapter-owned, not framework-core-owned.
