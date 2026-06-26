# F18-ADR-TRANSITION-001 - Transition Orchestration

Status: Accepted / F18B Primitives Applied  
Phase: F18 - Transition Orchestration Foundation  
Type: Framework Core / Flow Orchestration / Boundary  
Last updated: 2026-06-26

---

## 1. Context

F17 closed Gate Foundation. The framework now has a canonical admission language for allowed/blocked decisions, blockers, facts and diagnostics. F18 can therefore define Transition as flow orchestration that consumes Gate instead of inventing a parallel blocking model.

Transition exists because route/activity/content changes need an explicit operation boundary around request admission, previous-scope exit, release, scene/content movement, readiness observation, next-scope enter and diagnostics.

Transition must not be reduced to fade, loading screen or curtain visuals.

---

## 2. Decision

Transition belongs to Framework Core as orchestration.

Transition coordinates:

```text
Gate admission/block/release
Scene Lifecycle operations
content release results
runtime scope enter/exit results
readiness observations
lifecycle callback ordering
transition facts/diagnostics
```

Transition is not:

```text
fade effect
loading screen
curtain
UI widget
scene loader replacement
route lifecycle replacement
activity lifecycle replacement
parallel lifecycle
global manager
service locator
```

Fade/loading/curtain effects are adapters planned after the orchestration contract, in F19.

---

## 3. Orchestration Boundary

Transition describes the logical flow around a change:

```text
request admitted by Gate
transition operation opened
relevant gameplay/interaction admission blocked by Gate
previous scope exit observed
owned content release observed
scene/content load or composition observed
next scope enter observed
readiness observed
transition operation completed or failed
Gate blockers released for allowed scopes
facts emitted for diagnostics and QA
```

Transition owns the operation narrative. It does not own the underlying scene, route, activity or content systems.

---

## 4. Relationship With Gate

Transition consumes Gate. It does not replace Gate.

Gate remains the admission authority. Transition may require Gate blockers while an operation is active, but it must not invent an independent blocker model.

Required transition blockers must be explicit and observable as Gate blockers/facts.

---

## 5. Relationship With Scene Lifecycle

Transition may coordinate Scene Lifecycle, but it must not create a second Scene Lifecycle runtime.

Scene Lifecycle remains owner of scene loading/unloading semantics. Transition owns flow orchestration around those operations.

The same rule applies to Route Lifecycle, Activity Flow, RuntimeContent, ContentAnchor and Object Reset. Transition coordinates their outputs; it does not absorb them.

---

## 6. Minimal F18 Runtime Shape

F18 should introduce a small logical contract before any visual effect:

```text
TransitionOperationId
TransitionKind
TransitionPhase
TransitionStatus
TransitionStep
TransitionPlan
TransitionResult
TransitionSnapshot or diagnostics summary
```

These names are implementation guidance, not a requirement to create all files in F18B. The cut must remain small and compile-safe.

Preferred first runtime cut:

```text
F18B - Transition primitives only
```

F18B implements the passive runtime shape under `Runtime/Transition/`:

```text
TransitionOperationId
TransitionKind
TransitionPhase
TransitionStatus
TransitionStep
TransitionPlan
TransitionResult
TransitionSnapshot
```

F18B also adds `FrameworkIdentityDomain.Transition` so operation identity stays typed instead of falling back to string/path/name. It does not integrate with Route/Activity flows.

---

## 7. Planned F18 Cuts

| Cut | Target | Boundary |
|---|---|---|
| F18A | ADR implementation plan | Documentation only. Accept boundary and define implementation sequence. |
| F18B | Transition primitives | CLOSED. Passive types for operation identity, kind, phase/status, plan/result/snapshot. |
| F18C | Transition diagnostics smoke | NEXT. Synthetic QA runner for valid/failed transition result shapes without scene changes. |
| F18D | Gate blocker relationship | Logical helper that maps active transition operations to Gate blockers without Pause or visual effects. |
| F18E | Route/Activity orchestration observation | Minimal integration with existing route/activity request path only if needed, preserving happy-path result kinds. |
| F18F | Closure / handoff to F19 | Document evidence and hand off to Transition Effects adapters. |

The sequence may be adjusted if implementation shows a smaller safe path, but F18 must remain orchestration-only.

---

## 8. F18B Implemented Evidence

F18B adds passive primitives only. The implemented boundary is:

```text
Runtime/Transition/TransitionOperationId.cs
Runtime/Transition/TransitionKind.cs
Runtime/Transition/TransitionPhase.cs
Runtime/Transition/TransitionStatus.cs
Runtime/Transition/TransitionStep.cs
Runtime/Transition/TransitionPlan.cs
Runtime/Transition/TransitionResult.cs
Runtime/Transition/TransitionSnapshot.cs
```

The identity domain is extended with:

```text
FrameworkIdentityDomain.Transition
```

F18B does not add runtime ownership, operation registry, route/activity integration, Gate blocker mapping, fade/loading/curtain, visual progress, UI, Pause or input.

## 9. Required Diagnostics

Transition diagnostics should be able to answer:

```text
which operation is active
which transition kind is running
which phase is current
which Gate blocker exists because of transition
which lifecycle result was observed
which release/load/readiness step failed or skipped
whether the operation completed, completed with warnings, failed or was rejected
```

Diagnostics must be facts/loggable summaries, not hidden state inside scene objects.

---

## 10. Excluded From F18A/F18B

F18A/F18B do not implement or require:

```text
fade visual
loading screen
curtain
DOTween
Asset Store dependency
Pause menu
Pause state
input implementation
Player/Actor lifecycle
gameplay reset
runtime visual adapter
service locator
singleton transition manager
```

---

## 11. Guardrails

- Transition is orchestration of flow, not a visual effect.
- Transition consumes Gate for blocking/release.
- Transition must not create lifecycle parallel to Session/Route/Activity/Scene Lifecycle.
- Missing required callbacks/adapters in future cuts must fail explicitly.
- Visual effects remain adapters and cannot be the core contract.
- F18 must not pull F19 visual effects, F20 Pause or F22+ gameplay consumers forward.
