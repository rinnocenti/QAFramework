# P1 Pause Product Binding QA

> Historical filename retained. The current QA source of truth is the product contract below, not the original P1 composition.

## Current contract

The canonical single-player Pause/Input/Gate composition is:

```text
UnityPlayerInputGateAdapter
  owns the explicit PlayerInput target
  owns the Gameplay Action Map reference
  owns the physical Framework Gate writes

PlayerPauseInput
  owns the Pause InputActionReference
  derives PlayerInput from the co-located Gate Adapter
  derives the Gameplay Action Map from the co-located Gate Adapter
  derives the Global Action Map from the Pause Action
  participates in Pause lifecycle registration

PauseRequestTrigger
  exposes Pause / Resume / Toggle independently from physical Player Pause input
```

`PlayerPauseInput` must not serialize its own `PlayerInput`, Gameplay Action Map or
legacy gameplay-map-name authority.

Exactly one `UnityPlayerInputGateAdapter` is required on the same GameObject when
`PlayerPauseInput` is authored. The Gate Adapter remains valid without
`PlayerPauseInput`.

Runtime resolution uses action/map identity by GUID. Name fallback is not part of
the current contract.

## Edit Mode composition regression

The focused static proof lives at:

```text
Assets/ImmersiveFrameworkQA/Player/Editor/
QaPauseInputGateCompositionRegression.cs
```

Run in Edit Mode:

```text
Immersive Framework
  > QA
    > Player
      > Pause
        > Run Pause Input Gate Composition
```

Certified result:

```text
[P0_PAUSE_INPUT_GATE_COMPOSITION]
status='Passed'
verdict='StaticContractComplete'
cases='8/8'
```

Certified cases:

```text
pause-authors-only-pause-action
gate-owns-playerinput-and-gameplay-map
gate-valid-without-pause
pause-derives-gate-authority
pause-rejects-missing-gate
gate-authoring-prevents-duplicate-adapters
gameplay-map-resolution-does-not-fallback-by-name
gate-restore-remains-idempotent
```

This regression proves the authoring/composition boundary. It does not replace the
Play Mode Pause lifecycle regression.

## Play Mode Pause lifecycle regression

The canonical runtime proof remains:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
QaPauseRuntimeBindingSmoke.cs
```

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Regressions
      > Pause
        > Run Pause Contract Regression
```

That regression proves the runtime/lifecycle side of the product contract,
including:

```text
canonical Pause runtime binding
Pause / Resume state transitions
Framework Gate blocking and restoration
Gameplay/Global input posture
explicit no-change behavior for repeated Pause
Activity Restart while Pause remains authoritative
scene release and destroy teardown
preservation of a pre-disabled Gameplay baseline
absence of opportunistic runtime fallback
```

The Edit Mode composition regression and the Play Mode lifecycle regression are
complementary:

```text
Edit Mode composition
  proves who authors PlayerInput, Gameplay map and Pause action

Play Mode lifecycle
  proves how the valid composition participates in Pause/Gate runtime
```

Neither surface creates a parallel Pause runtime.

## Current authoring expectation

A Player Host that supports physical Pause input uses:

```text
PlayerInput
LocalPlayerHostAuthoring
UnityPlayerInputGateAdapter
PlayerPauseInput
```

Authoring order:

1. Add exactly one `UnityPlayerInputGateAdapter`.
2. Assign its exact `PlayerInput`.
3. Assign its Gameplay Action Map.
4. Add `PlayerPauseInput` on the same GameObject.
5. Assign the Pause `InputActionReference`.

`PlayerPauseInput` must not create, repair or overwrite the Gate Adapter.

## Negative contract

The composition must fail explicitly when:

```text
Gate Adapter is missing
Gate Adapter composition is duplicated
Gate Adapter PlayerInput/actions are missing
Gate Adapter Gameplay Action Map is missing or invalid
Pause Action is missing
Pause Action GUID is absent from PlayerInput.actions
Global and Gameplay resolve to the same map
```

No hierarchy search, singleton, service locator, global opportunistic lookup or
action-map-name fallback is accepted to make the composition succeed.

## Scope

This QA covers the single-player Pause/Input/Gate product surface.

Out of scope:

```text
multiplayer Pause ownership policy
device/control-scheme allocation
Player provisioning policy
Actor selection
Camera
FIRSTGAME-specific input migration
```
