# F30C1 — PlayerInputManager Smoke Warning Cleanup

Status: Closed / corrective implementation.

## Intent

F30C passed, but the duplicate PlayerInputManager QA step created two real Unity `PlayerInputManager` components. Unity correctly emitted its native warning:

```text
Multiple PlayerInputManagers in the game. There should only be one PlayerInputManager
```

The framework diagnostic was correct, but the smoke itself should not pollute the Console with a native Unity warning when the duplicate case can be validated as passive evidence.

## Correction

- `UnityInputPlayerInputManagerEvidence` now supports count-based evidence through `FromManagerCount`.
- `UnityInputTargetValidator` now exposes `ValidatePlayerInputManagerEvidenceCount`.
- `UnityInputOfficialComponentEvidenceQaSmokeRunner` validates none/single/duplicate PlayerInputManager cases through passive counts instead of creating real `PlayerInputManager` components.

## Boundary Preserved

This correction does not create a framework input manager. It also does not call join logic, instantiate player prefabs, switch action maps, activate/deactivate input, or integrate Pause.

The official Unity components remain the execution authority. The framework only validates integration evidence.

## Expected Result

Run:

```text
Run Unity Input Official Component Evidence Smoke
```

Expected result:

```text
playerinputmanager-duplicate-blocking passed='True'
```

without the native Unity Console warning about multiple `PlayerInputManager` instances.
