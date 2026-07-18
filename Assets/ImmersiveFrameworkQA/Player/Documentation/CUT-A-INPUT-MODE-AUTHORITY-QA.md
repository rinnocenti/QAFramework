# Cut A4 — Layered Global InputMode QA

## Objective

Prove the completed IC1/IC2/IC3/IC4 architecture:

```text
one resident logical InputMode state
one in-flight posture transaction
one canonical Pause InputAction submitter
one package-owned physical writer
exact enabled-map posture sets
persistent Global/PauseToggle continuity
exact commit and rollback
```

## Canonical policy

```text
Gameplay     -> Global + Player
PauseOverlay -> Global + UI
FrontendMenu -> Global + UI
InputLocked  -> Global
```

## Scope

- resident `InputModeRuntimeContext` and monotonic transactions;
- concurrent and stale transaction rejection;
- canonical Pause trigger delegation;
- inert legacy Pause trigger;
- persistent `Global` map preflight;
- exact enabled-map-set application and rollback;
- `PauseToggle` continuity through Pause and Resume;
- negative preflight for missing Global, UI and PlayerActor evidence;
- preservation of the IC1 physical writer boundary;
- no InputMode-owned `PlayerInput.ActivateInput()` call.

## Out of scope

- Player gameplay binding ownership;
- device/control-scheme policy;
- PlayerInputManager provisioning changes;
- which local Player may Pause;
- FIRSTGAME action-asset migration.

## Edit Mode authority smoke

Use:

```text
Immersive Framework
> QA
> Input Mode
> IC2 Run Runtime Authority Smoke
```

Expected:

```text
[IC2_INPUT_MODE_RUNTIME_AUTHORITY_SMOKE]
status='Passed'
cases='20'
```

Additional A4 source cases:

```text
global-map-is-persistent-policy
layered-map-set-application
```

## Play Mode layered runtime smoke

Enter a fresh Play Mode session and run:

```text
Immersive Framework
> QA
> Input Mode
> IC2 Run Pause Runtime Regression Smoke
```

Expected:

```text
[IC2_PAUSE_INPUT_MODE_RUNTIME_REGRESSION_SMOKE]
status='Passed'
cases='13'
```

Cases:

```text
play-mode-required
runtime-host-available
scoped-authority-fixture-created
toggle-pause-committed-global-ui
duplicate-pause-idempotent
toggle-resume-committed-global-player
global-pause-toggle-remains-enabled
missing-global-map-preflight-preserves-state
missing-ui-map-preflight-preserves-state
restored-ui-map-pause-committed
missing-playeractor-preflight-preserves-state
final-resume-clean-no-join-spawn
cleanup-restores-running-gameplay
```

The final cleanup leaves the application in:

```text
PauseState.Running
InputModeKind.Gameplay
enabled maps: Global + Player
UI disabled
```

The canonical runtime smoke should not print:

```text
Cannot switch to actions 'Player'; input is not enabled
```

## IC1 physical writer regression

In another fresh Play Mode session, run:

```text
Immersive Framework
> QA
> Unity Input
> IC1 Run PlayerInput Single Writer Smoke
```

Expected:

```text
[IC1_PLAYER_INPUT_SINGLE_WRITER_SMOKE]
status='Passed'
cases='18'
```

New layered writer cases:

```text
global-player-set-applied
global-ui-set-applied
layered-set-rollback-exact
layered-set-baseline-restored
```

## Acceptance

```text
IC2 Edit Mode smoke passes 20 cases
IC2 Play Mode regression passes 13 cases
IC1 smoke passes 18 cases
framework and QA compile in Unity 6.5
Global is required before Pause mutation
Global/PauseToggle remains enabled across Gameplay and Pause
Player and UI are mutually exclusive by posture
exact rollback restores primary map and complete enabled-map set
InputMode application contains no ActivateInput call
only UnityPlayerInputStateWriter contains package-owned physical mutation
no singleton, service locator or implicit PlayerInput lookup is introduced
```
