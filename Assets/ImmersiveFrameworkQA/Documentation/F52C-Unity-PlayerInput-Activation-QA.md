# F52C — Unity PlayerInput Activation QA

## Purpose

Validate that F52C switches a configured Unity `PlayerInput` action map from existing F52B bridge evidence and restores the previous action map on clear.

## Expected smoke

```text
[F52C_UNITY_PLAYERINPUT_ACTIVATION_QA] status='Succeeded'
```

## Covered cases

- component references
- successful action-map activation
- missing bridge target
- missing bridge evidence
- missing activation target
- missing PlayerInput
- missing action map name
- missing configured action map
- PlayerSlot mismatch
- clear no-op
- clear after activation restores previous action map
- boundary: no movement, no actor spawning, no gameplay command execution

## Out of scope

F52C does not route `InputAction` callbacks, read action values, enable movement, execute gameplay, spawn actors, own lifecycle or integrate FIRSTGAME.
