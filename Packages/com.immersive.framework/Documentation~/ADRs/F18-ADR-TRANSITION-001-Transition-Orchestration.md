# F18-ADR-TRANSITION-001 - Transition Orchestration

Status: Planned  
Phase: F18 - Transition Orchestration Foundation  
Type: Framework Core / Flow Orchestration / Boundary  
Last updated: 2026-06-26

---

## 1. Context

After Gate is defined, the framework can plan Transition as flow orchestration. Transition must not be reduced to fade, loading screen or curtain visuals.

Transition coordinates the state changes around route/activity/scene/content flow and uses Gate to block or release admission while the flow is not ready for gameplay or interaction.

---

## 2. Decision

Transition belongs to Framework Core as orchestration.

Transition coordinates:

```text
Gate
Scene Lifecycle
content release
runtime readiness
transition callbacks
diagnostic facts
```

Transition is not:

```text
fade effect
loading screen
curtain
UI widget
scene loader replacement
parallel lifecycle
global manager
```

Fade/loading/curtain effects are adapters planned after the orchestration contract.

---

## 3. Orchestration Boundary

Transition should describe the lógical flow around a change:

```text
request admitted by Gate
flow enters transitioning state
previous interaction/gameplay blocked by Gate
release/unload/load steps coordinated by existing lifecycle contracts
readiness observed as facts
callbacks emitted explicitly
Gate released for allowed scopes
```

This ADR does not require a final runtime API. It fixes the ownership boundary before implementation.

---

## 4. Relationship With Gate

Transition consumes Gate. It does not replace Gate.

Gate remains the admission authority. Transition may request blocking/release decisions, but it must not invent its own independent blocker model.

---

## 5. Relationship With Scene Lifecycle

Transition may coordinate Scene Lifecycle, but it must not create a second Scene Lifecycle runtime.

Scene Lifecycle remains owner of scene loading/unloading semantics. Transition owns flow orchestration around those operations.

---

## 6. Excluded Now

F18 does not implement or require:

```text
fade visual
loading screen
DOTween
Asset Store dependency
Pause menu
input implementation
Player/Actor lifecycle
gameplay reset
service locator
singleton transition manager
```

---

## 7. Guardrails

- Transition is orchestration of flow, not a visual effect.
- Transition consumes Gate for blocking/release.
- Transition must not create lifecycle parallel to Session/Route/Activity/Scene Lifecycle.
- Missing required callbacks/adapters in future cuts must fail explicitly.
- Visual effects remain adapters and cannot be the core contract.
