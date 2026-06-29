# F32A — InputMode Unity Application Preview

Status: Closed by patch / awaiting QA smoke.
Type: runtime preview + QA smoke.

## Purpose

F32A reconnects the input track after the canonical references introduced by F29-F31:

```text
SessionPlayerInputManagerDeclaration
PlayerActorDeclaration : IActor + PlayerInput evidence
UnityInputTargetDeclaration
InputModeRequest / PauseInputModeRequestMapper
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

The evaluator consumes existing evidence snapshots:

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
```

## Next cut

F32B should decide the Unity adapter boundary for action-map/application behavior.
It should still keep action-map names project-owned and Unity-adapter-owned, not framework-core-owned.
