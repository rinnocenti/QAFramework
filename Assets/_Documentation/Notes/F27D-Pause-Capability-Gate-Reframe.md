# F27D — Pause Capability Gate Reframe

Status: Ready for smoke  
Type: Runtime correction / diagnostics correction  
Date: 2026-06-28

## Purpose

F27D applies the F27C audit decision in runtime code. Pause-derived Gate blockers are no longer described as broad gameplay/component blockers. They are now capability/admission blockers consumed by input and command-facing adapters.

## Runtime decision

Pause produces these blockers while `PauseState.Paused`:

```text
Input / InputAcceptance
Interaction / InteractionAcceptance
```

Pause explicitly does not produce blockers for:

```text
Pause / PauseRequest
UI / UiNavigation
Gameplay / GameplayAction
```

`Gameplay / GameplayAction` remains a valid Gate domain for future concrete command admission, but it is not the first Pause blocker. This avoids pulling Gate toward a component-level pause/freezer model.

## Files changed

```text
Packages/com.immersive.framework/Runtime/Pause/PauseGateBlockerPolicy.cs
Packages/com.immersive.framework/Runtime/Pause/PauseRuntime.cs
Packages/com.immersive.framework/Runtime/Gate/GateDomain.cs
Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
Packages/com.immersive.framework/Runtime/Diagnostics/PauseGateBlockerQaSmokeRunner.cs
Packages/com.immersive.framework/Runtime/Diagnostics/PauseRuntimeRequestQaSmokeRunner.cs
```

## Diagnostic vocabulary

Previous diagnostic vocabulary:

```text
blocksGameplay
blocksInteraction
blocksPauseRequest
```

F27D vocabulary:

```text
blocksInputAcceptance
blocksInteractionAcceptance
blocksPauseRequest
```

`blocksPauseRequest` remains as a negative proof: it must stay `False` while paused so PauseToggle/Resume can work.

## Expected smoke

Pause request while running should now report:

```text
gateBlockers='2'
blocksInputAcceptance='True'
blocksInteractionAcceptance='True'
blocksPauseRequest='False'
```

Resume should report:

```text
gateBlockers='0'
blocksInputAcceptance='False'
blocksInteractionAcceptance='False'
blocksPauseRequest='False'
```

The F27B input smoke should still pass because the `UnityPauseInputActionAdapter` emits `PauseRequest`; Pause requests are not blocked by the Pause capability blockers.

## Non-goals

F27D does not:

```text
- implement InputModeService;
- switch action maps;
- make gameplay input adapters consult Gate yet;
- change Time.timeScale;
- find or pause gameplay components.
```

The next behavior cut should be `F27E — Input Consumers Respect Gate`, where gameplay-facing input/command adapters begin evaluating `Input / InputAcceptance` before emitting commands.
