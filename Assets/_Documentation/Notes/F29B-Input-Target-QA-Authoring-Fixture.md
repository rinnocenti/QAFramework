# F29B — Input Target QA Authoring Fixture

## Status

Closed / pending Unity compile-smoke by user.

## Purpose

F29A proved Unity Input target ownership with synthetic descriptors and temporary diagnostic GameObjects.

F29B adds authored QA evidence so the smoke no longer proves only in-memory contracts. The canonical QA StartupScene now contains explicit `UnityInputTargetDeclaration` objects for the two accepted input target roles.

## What Changed

The QA StartupScene now declares two authored targets under the existing `QA` object:

| GameObject | Role | Target Id | Meaning |
|---|---|---|---|
| `QA_UnityInput_GlobalUiPause_Target` | `GlobalUiPause` | `qa.input.target.global-ui-pause` | Global UI / Pause intent target. |
| `QA_UnityInput_GameplayCommands_Target` | `GameplayCommands` | `qa.input.target.gameplay-commands` | Gameplay command target for future InputMode work. |

The existing `Unity Input Target Ownership Smoke` now includes a loaded-scene fixture step:

```text
loaded-scene-fixture
```

This step validates real declarations found in loaded scenes by `UnityInputTargetValidator.ValidateLoadedSceneDeclarations`.

## What This Cut Proves

F29B proves that:

```text
QA target declarations can be authored in a Unity scene;
Global UI / Pause and Gameplay targets are distinct authored objects;
loaded-scene discovery returns exactly one declaration per required role;
invalid/missing/duplicate diagnostics remain owned by the validator;
no input behavior is applied while proving ownership.
```

## What This Cut Does Not Implement

F29B does not implement:

```text
InputMode runtime;
action-map switching;
PlayerInput ownership;
player movement;
player or actor spawning;
Pause presentation/input behavior;
camera/audio/save/gameplay adapters.
```

The fixture is QA evidence only. It is not a production player prefab or project input configuration.

## Manual Smoke

Run from the canonical QA StartupScene:

```text
Run Unity Input Target Ownership Smoke
```

Expected additional step:

```text
QA Unity Input Target Ownership Smoke step completed. step='loaded-scene-fixture' passed='True' targets='2' issues='0' blockingIssues='0' globalUiPauseTargets='1' gameplayCommandTargets='1'
```

## Closeout

F29B is closed when:

```text
the package compiles;
the canonical QA scene opens without missing script references;
the Unity Input Target Ownership Smoke passes including loaded-scene-fixture;
no InputMode/action-map/player behavior was introduced.
```

Next cut:

```text
F29C — Input Target Closeout
```
