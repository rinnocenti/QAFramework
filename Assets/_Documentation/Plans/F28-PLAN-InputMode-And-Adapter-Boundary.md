# F28 Plan — InputMode and Adapter Boundary Reorganization

## Status

Planned / audit-first

## Purpose

F28 reorganizes the future input and adapter track before any new runtime implementation.

The goal is to prevent Pause, Gate and Input from being solved reactively. The framework needs a typed InputMode boundary and an adapter ownership plan before deciding how Pause drives gameplay/UI input.

## Trigger

F27A-F27D validated Pause surface, PauseToggle input and Gate diagnostic reframe. The attempted next step, direct Gate checks from input consumers, was rejected because it spread Pause behavior into every consumer and skipped the missing InputMode decision.

## Boundary

F28 owns planning and minimal contracts for InputMode and adapter ownership.

F28 does not own:

```text
full Player system
actor/player lifecycle
camera/audio/gameplay adapters
Time.timeScale freeze policy
combat/interact/movement implementation
product pause menu
```

## Canonical Separation

```text
PauseRuntime
  owns pause state and pause request results

InputModeRuntime
  owns typed active input mode

Unity Input adapters
  apply typed input mode to concrete Unity Input System targets

Player/Actor adapters
  provide concrete PlayerInput targets later, after player ownership is resolved

Gate
  remains passive admission/hard-lock language, not the normal action-map switch mechanism
```

## Cut matrix

| Cut | Name | Status | Scope |
|---|---|---|---|
| F28A | Input/Adapter Audit Matrix | Planned | Read current framework, QA assets and NewScripts. Produce the authoritative matrix for InputMode, PlayerInput ownership, Pause, Gate and adapter module boundaries. No runtime. |
| F28B | InputMode Contract Boundary | Planned | Define typed InputMode language and diagnostics without binding to Unity `PlayerInput`. No action-map switching yet. |
| F28C | Unity Input Adapter Ownership Plan | Planned | Decide where Unity Input System adapters live and how targets are registered/provided. No broad scene scan as canonical behavior. |
| F28D | QA InputMode Surface / Manual Target Proof | Planned | Optional QA-only proof with explicit authoring target, if F28A-C authorize it. |
| F28E | Pause Drives InputMode | Planned | Pause requests `PauseOverlay` / `Gameplay` through the typed InputMode boundary. Still no TimeScale policy. |
| F28F | InputMode Closeout Guide | Planned | Document setup, accepted ownership and remaining adapter deferrals. |

## F28A audit questions

F28A must answer these before any code cut:

```text
1. What owns the concrete PlayerInput target today?
2. Is there one PlayerInput, multiple player slots, or no player object yet?
3. Should framework core know about PlayerInput directly?
4. Which action maps are canonical by typed mode?
5. Does PauseOverlay switch maps, enable/disable maps, or only mark state?
6. How does UI input remain active while gameplay input is unavailable?
7. Where do adapter modules live: framework package, project Assets, or separate packages?
8. What remains Gate responsibility after InputMode exists?
```

## Preliminary InputMode set

The initial typed set should stay small:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

Names can change during F28A, but action-map strings must not become the canonical contract.

## NewScripts reference

NewScripts is reference material, not source code to copy.

Useful concepts:

```text
SessionActivityPauseToggleInputAdapter
  - narrow PauseToggle adapter
  - Player/UI duplicate callback dedupe

InputModeService
  - typed mode requests: Gameplay / PauseOverlay / FrontendMenu / InputLocked
  - applies Unity action maps technically
  - does not own lifecycle

ADR-0007 Gates, InputModes and Simulation Executors
  - Gate and InputMode are executors/effect applicators
  - they do not decide lifecycle
```

Rejected imports:

```text
DependencyManager pattern
scene-wide PlayerInput discovery as canonical behavior
hard-coupling InputMode service into Pause core
copying Base 1.x pipeline assumptions before player ownership is resolved
```

## Gate role after F28

Gate should remain:

```text
admission diagnostics
lifecycle request guard language
hard lock / exceptional block language
safety layer for in-flight/stale/foreign operations
```

Gate should not become:

```text
a replacement for InputMode
a component pause system
a mandatory check inside every ordinary input consumer
the owner of Unity action maps
```

## Required freeze

Until F28A closes, do not implement:

```text
F27E input-consumer Gate checks
Time.timeScale pause policy
PlayerInput ownership assumptions
broad InputMode runtime service
product pause menu behavior
```
