# F30C — Unity PlayerInput Component Evidence Validation

Status: Closed / implementation + QA smoke pending Unity validation.

## Intent

F30C adds diagnostic evidence validation for official Unity Input System components without creating a framework input manager.

The accepted boundary remains:

```text
Unity PlayerInput / PlayerInputManager = official input execution components
Immersive Framework = lifecycle, validation, diagnostics and integration evidence
Project = InputActionAsset, player prefabs, action maps and concrete behavior
```

## What changed

- `UnityInputTargetDeclaration` can mark a target as requiring PlayerInput evidence.
- `UnityInputTargetDescriptor` records whether PlayerInput evidence is present and whether it is required.
- `UnityInputTargetSet` reports missing required PlayerInput evidence as a blocking issue.
- `UnityInputPlayerInputManagerEvidence` validates PlayerInputManager presence/count without invoking join logic.
- `UnityInputTargetValidator` exposes PlayerInputManager evidence validation.
- QA gains `Unity Input Official Component Evidence Smoke`.

## What this is

This is a component-evidence proof. It answers:

```text
Does a target that requires PlayerInput evidence actually have PlayerInput evidence?
Is PlayerInputManager absent/unique in the validation scope?
Did the framework stay passive, without switching action maps or joining players?
```

## What this is not

F30C does not create:

- custom input manager;
- PlayerInput owner runtime;
- PlayerInputManager wrapper;
- action-map switching;
- Pause bridge;
- player movement;
- player/actor spawning;
- local multiplayer join policy.

## QA smoke

Run from the QA Canvas:

```text
Run Unity Input Official Component Evidence Smoke
```

Expected steps:

```text
playerinput-required-evidence-valid
playerinput-required-evidence-missing
playerinputmanager-optional-none
playerinputmanager-single-valid
playerinputmanager-duplicate-blocking
no-action-map-switching
```

All steps must report `actionMapSwitching='False'` and `inputBehavior='False'`.

## Next

F30D should close the InputMode planning phase and select the next implementation slice. Candidate next slice: authored QA fixture using official Unity Input components, or InputMode request-to-adapter plan.
