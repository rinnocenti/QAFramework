# F32H — InputMode Unity PlayerInput Application Closeout

## Status

Closed.

F32 closes the Unity `PlayerInput` application lane for `InputMode`.

## Closed scope

F32 now provides the complete explicit chain:

```text
InputModeState + InputModeRequest
  -> InputModeRequestEvaluator
  -> InputModeUnityApplicationPreviewEvaluator
  -> InputModeUnityActionMapPreviewEvaluator
  -> InputModeUnityApplicationPlanEvaluator
  -> InputModeUnityPlayerInputApplication
```

It also provides the explicit Pause bridge:

```text
PauseResult
  -> PauseInputModeRequestMapper
  -> InputModeUnityPlayerInputRequestApplication
  -> PlayerInput
```

## Final behavior accepted in F32

| Source | InputMode | Unity PlayerInput application |
| --- | --- | --- |
| Gameplay request | `Gameplay` | `ActivateInput()` + `SwitchCurrentActionMap("Player")` |
| Pause request/result | `PauseOverlay` | `ActivateInput()` + `SwitchCurrentActionMap("UI")` |
| Frontend request | `FrontendMenu` | `ActivateInput()` + `SwitchCurrentActionMap("UI")` |
| Lock request | `InputLocked` | `DeactivateInput()` |
| Already in requested mode | ignored | no side effect |
| Missing evidence/action map/invalid plan | failed | no side effect |

## Guardrails still active

F32 does not:

- create a framework input manager;
- own `PlayerInputManager`;
- call `PlayerInputManager.JoinPlayer`;
- spawn player prefabs;
- move `PlayerActor`;
- read gameplay commands;
- wire itself automatically into `PauseRuntime`;
- wire itself automatically into `FrameworkRuntimeHost`.

## Closed smokes

```text
InputMode Unity Application Preview Smoke
InputMode Unity Action Map Preview Smoke
InputMode Unity Application Plan Smoke
InputMode Unity PlayerInput Adapter Smoke
InputMode Unity PlayerInput Application Smoke
InputMode Unity PlayerInput Request Application Smoke
Pause InputMode Unity PlayerInput Application Smoke
```

## Next phase

The next phase should be a separate runtime wiring phase. It may consume the explicit F32 application path, but it must decide ownership before wiring anything automatically.

Recommended next phase:

```text
F33 — Pause Runtime PlayerInput Wiring
```

Entry criteria for F33:

- F32H closed;
- explicit `PlayerInput` instance/reference available;
- no implicit `PlayerInputManager` ownership;
- runtime wiring opt-in or authoring declaration defined before `FrameworkRuntimeHost` integration.
