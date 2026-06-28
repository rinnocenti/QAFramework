# F27C — Gate / Input Capability Audit

Status: Closed / Audit PASS  
Type: Architecture audit / no runtime side effects  
Date: 2026-06-28

## Purpose

F27C audits the current Pause/Gate/Input state after F27A-F27B to remove the wrong direction where Gate could evolve into a component-level pause/blocker system.

The accepted correction is:

```text
Gate is not a component blocker.
Gate is a capability/admission boundary.
Input adapters and command producers consult Gate before emitting gameplay-facing commands.
Pause is one producer of blockers, not the owner of Gate.
```

## Current project findings

### 1. Gate primitives are already in the right family

Current `Runtime/Gate` primitives are passive:

```text
GateBlocker
GateDecision
GateEvaluationResult
GateSnapshot
GateRequestAdmission
GateScope
GateDomain
```

They do not discover scene objects, own lifecycle, mutate components, pause GameObjects or bind the Unity Input System. This is correct and should be preserved.

### 2. The problematic pressure is Pause vocabulary, not Gate primitives

Current Pause derives a `GateSnapshot` through `PauseGateBlockerPolicy` and diagnostics expose broad fields such as:

```text
gateBlockers
blocksGameplay
blocksInteraction
blocksPauseRequest
```

That does not currently pause components, but the naming encourages the wrong next step: treating Gate as a gameplay/component freezer.

### 3. NewScripts reference confirms separation

The old project has two separate ideas:

```text
SessionActivityPauseToggleInputAdapter
  - maps Player/PauseToggle and UI/PauseToggle to pause toggle
  - dedupes same-frame callbacks

InputModeService
  - changes or records active input mode
  - Gameplay and FrontendMenu switch PlayerInput action maps
  - PauseOverlay and InputLocked are state-only in the old scope
```

F27B correctly imported only the first behavior. F27 must not copy `InputModeService` yet.

### 4. Gate should be consumed by input/command adapters, not by raw input itself

Correct flow:

```text
Unity Input System action
  -> Unity input adapter
  -> evaluate framework capability/admission gate
  -> emit framework/gameplay command only when allowed
```

Raw input may still be received while paused. This is required so `PauseToggle`, UI navigation and resume remain available.

## Accepted boundary

### Gate is

```text
capability admission
request admission
input/command acceptance language
diagnostic blocker snapshot
```

### Gate is not

```text
component pause
GameObject freezer
Animator controller
Time.timeScale owner
Unity Input System wrapper
InputModeService
global runtime registry
```

## Required conceptual correction

Pause should not be described primarily as blocking components or all gameplay execution.

Pause should produce blockers for player-facing capabilities such as:

```text
Input / InputAcceptance
Interaction / InteractionAcceptance
```

Pause should leave these paths available:

```text
Pause / PauseRequest
UI / UiNavigation
System-safe diagnostics where explicitly allowed
```

`Gameplay / GameplayAction` should not be the first Pause blocker for F27 because it is too broad and points toward component/gameplay freezing. It can remain available for future non-input gameplay command admission if a concrete consumer needs it.

## Recommended next runtime cut

Use F27D for the actual code correction:

```text
F27D — Pause Capability Gate Reframe
```

Expected changes:

```text
- keep Gate primitives passive;
- update PauseGateBlockerPolicy to produce capability/input-facing blockers;
- rename diagnostics away from blocksGameplay toward blocksInputAcceptance / blocksInteractionAcceptance;
- add or update QA smoke to prove Pause blocks gameplay input admission but allows PauseRequest;
- keep UnityPauseInputActionAdapter able to unpause without being blocked by generic input gates;
- do not implement InputModeService yet;
- do not apply Time.timeScale yet.
```

## Deferred cuts

```text
F27E — Input adapters consult Gate
F27F — Pause freeze policy: GateOnly / TimeScaleOnly / Hybrid
F27G — InputMode audit/rebuild from NewScripts, if still needed
```

## Non-goals

F27C does not change runtime code. It is an audit/decision cut so the next code cut is smaller and does not accidentally turn Gate into a component blocker.
