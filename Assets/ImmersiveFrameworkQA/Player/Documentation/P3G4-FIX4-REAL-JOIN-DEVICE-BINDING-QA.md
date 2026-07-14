# P3G.4 FIX4 — Real Join Device Binding QA

## Cause

The generated `P3G4_InputActions.asset` contained the `JoinEvidence` action without any binding or control scheme.

For a manual `PlayerInputManager.JoinPlayer(...)` request without an explicit device hint, `PlayerInput` searches the action asset for a binding usable by an available unpaired device. With no binding, no valid `InputUser` is created. The manager requests destruction when device setup is unsuccessful, so the instantiated host is destroyed and `JoinPlayer` returns `null`.

## Correction

The fixture now materializes this binding idempotently:

```text
Gameplay/JoinEvidence
  <Keyboard>/space
```

The smoke request remains device-agnostic. Slot identity and device choice are not supplied by the caller.

## Reapply

Exit Play Mode and execute:

```text
Immersive Framework > QA > Player > P3G.4 Apply Real Join Fixture
```

Then enter Play Mode after the runtime reports `status='Ready'` and run the real join smoke.

## Scope

```text
QA fixture only
no framework runtime change
no contract change
no PlayerSlot policy change
no device fallback in the framework
```
