# F29C — Input Target Closeout

## Status

Closed / documentation-only.

User smoke evidence for F29B passed, including the authored loaded-scene fixture:

```text
Unity Input Target Ownership Smoke
contracts=True
valid-target-set=True
missing-required-target=True
duplicate-target=True
global-ui-and-gameplay-target-split=True
no-action-map-switching=True
loaded-scene-fixture=True
declaration-component=True
```

## Purpose

F29C closes the Unity Input Target Ownership Proof phase.

F29 proved that the framework can identify explicit Unity Input targets before any InputMode behavior, action-map switching, PlayerInput ownership, player movement or actor spawning exists.

The phase answered:

```text
which target represents global UI / Pause intent;
which target represents gameplay command input;
how missing required targets are diagnosed;
how duplicate required targets are diagnosed;
how authored QA scene declarations are validated;
that no action-map switching or input behavior is hidden inside the proof.
```

## Closed Evidence

| Evidence | Status | Notes |
|---|---|---|
| Passive target vocabulary | Closed in F29A | Role/id/descriptor/set/issue language exists under the framework package. |
| Synthetic validator smoke | PASS in F29A | Valid/missing/duplicate cases are covered without scene dependency. |
| Authored QA fixture | PASS in F29B | `StartupScene` contains one global UI/Pause target and one gameplay commands target. |
| Loaded-scene validation | PASS in F29B smoke | The loaded-scene fixture step found two targets and zero blocking issues. |
| No InputMode behavior | Preserved | Smoke reports `actionMapSwitching=False` and `inputBehavior=False`. |

## Boundary Confirmed

F29 target declarations are not gameplay input.

They are only evidence that future input systems have a safe attachment point.

Allowed after F29:

```text
InputMode identity/state language;
InputMode request/result contracts;
InputMode owner preview with no action-map side effects;
Pause requesting Gameplay/PauseOverlay through typed mode language.
```

Still not allowed directly after F29:

```text
player movement;
player/actor spawn;
full Unity PlayerInput ownership;
action-map switching;
Camera/Audio/Save adapters;
runtime-spawned actors;
per-consumer Gate checks.
```

## Next Phase Selected

F30 is selected as the next implementation phase:

```text
F30 — InputMode Identity and Request Result Model
```

First recommended cut:

```text
F30A — InputMode Identity / State / Request Result Contracts
```

F30A should define the framework language for mode requests before it connects to Unity Input System behavior.

F30A may create:

```text
InputModeKind or equivalent typed mode identity;
InputModeState or equivalent state snapshot;
InputModeRequest;
InputModeRequestResult;
blocking/ignored/succeeded request reason vocabulary;
manual QA smoke for pure request/result behavior.
```

F30A must not create:

```text
Unity action-map switching;
PlayerInput ownership;
player movement;
player/actor spawn;
Pause visual behavior;
Camera/Audio/Save adapters;
RuntimeContentHandle-based actor lifetime.
```

## Closeout Decision

F29 is closed.

The framework now has enough input target ownership evidence to start F30 safely, because future InputMode behavior can point at explicit declared targets instead of guessing scene objects, relying on GameObject names or coupling directly to player/actor runtime.
