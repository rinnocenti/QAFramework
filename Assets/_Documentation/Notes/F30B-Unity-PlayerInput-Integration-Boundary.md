# F30B — Unity PlayerInput Integration Boundary

## Status

Closed / corrective documentation + naming clarification.

## Why this correction exists

F30A correctly created passive InputMode request/result language. The next proposed implementation, `InputModeOwnerPreview`, was rejected before application because it could push the framework toward a custom input manager.

That is not the desired direction.

For player input, the canonical execution surface is Unity Input System:

```text
PlayerInput
PlayerInputManager
InputActionAsset / project-wide actions
InputSystemUIInputModule when UI requires it
```

The framework must integrate those components. It must not replace them with a parallel manager.

## Correct boundary

| Concern | Owner |
|---|---|
| Reading devices and actions | Unity Input System |
| Single player `PlayerInput` execution | Unity `PlayerInput` component |
| Local multiplayer join / pairing / split-screen entry point | Unity `PlayerInputManager` component when the project needs it |
| Action assets, action maps, control schemes | Project assets / project configuration |
| Lifecycle admission, diagnostics, route/activity/pause coordination | Immersive Framework |
| InputMode names and request/result language | Immersive Framework contracts |
| Actual action-map switching or activation/deactivation | A future Unity Input adapter that calls Unity components explicitly |

## Redirect of existing F29/F30 work

`UnityInputTargetDeclaration` remains valid, but its meaning is now narrowed:

```text
It declares a framework-visible integration point for official Unity Input components.
It is not the component that processes input.
It is not a framework input manager.
It is not a final player/actor runtime.
```

`InputModeRequest` remains valid, but it is only a command language:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

It does not mutate `PlayerInput`, `PlayerInputManager`, action maps, devices or gameplay callbacks.

## Accepted interpretation of UnityInputTargetDeclaration

A declared target answers:

```text
which authored object is allowed to be considered for input integration?
which role does it represent: GlobalUiPause or GameplayCommands?
does it currently expose evidence of a PlayerInput reference?
is the declaration missing or duplicated?
```

It does not answer:

```text
which action map should be active?
which action asset should be used?
which player prefab should be spawned?
which gameplay command should fire?
```

## Rules after F30B

Future input cuts must follow these rules:

1. Prefer official Unity components over framework replacements.
2. Do not introduce an `InputManager`, `InputModeManager` or static input authority inside the framework.
3. Do not hide action-map switching inside passive contracts.
4. Do not make Pause own `PlayerInput`.
5. Do not make Player/Actor spawning a prerequisite for validating Unity input integration.
6. Add validators/adapters around Unity components only after the exact Unity-owned surface is declared.

## Replacement for the rejected F30B owner preview

Rejected direction:

```text
F30B — InputMode Owner Preview
```

Accepted correction:

```text
F30B — Unity PlayerInput Integration Boundary
```

The old owner-preview package must not be applied.

## New next step

The next implementation should not be a framework mode owner.

Preferred next cut:

```text
F30C — Unity PlayerInput Component Evidence Validation
```

Expected scope:

```text
validate declared targets against official Unity Input components;
accept explicit absence where the proof is still declaration-only;
report whether GlobalUiPause and GameplayCommands targets have PlayerInput evidence;
optionally detect PlayerInputManager presence/duplicates as diagnostics only;
no action-map switching;
no player spawning;
no Pause bridge yet.
```

## Closeout

F30B redirects F30 from “framework owns input mode state” to “framework describes and validates integration with Unity-owned input components.”
