# F23-ADR-PAUSE-003 - Pause Boundary Intent and Adapter Deferral

Status: Accepted / F23E Applied / Boundary Reclassified  
Phase: F23 - Pause Content / Overlay / Input Boundary  
Type: Framework Intent / Requirement Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as the logical Pause core. That core owns Pause state, request/result, snapshot/facts and the passive Gate blocker relationship. It intentionally does not own visual overlay, menu content, input binding, Canvas/prefab setup or `Time.timeScale`.

F21 closed Save / Snapshot / Preferences / Progression Save Foundation. F22 closed Loading Operation / Progress / Readiness Boundary, including readiness observations and loading result/issues.

A pre-F24 review identified that F23 was drifting toward adapter/build-surface language. That would duplicate the job of F24 — Unity Build Surface / Lifecycle Wiring. F23 is therefore reclassified as an intent/requirement boundary only.

---

## 2. Decision

F23 may describe what Pause needs, but it must not materialize it.

F23 owns framework-only intent records:

```text
PauseContentRequirement
PausePresentationIntent
PauseInputIntent
```

F23 does not own:

```text
Content Anchor binding execution
RuntimeContentAnchorBinding
Canvas or prefab setup
overlay adapter execution
Input System action maps
input polling or dispatch
Time.timeScale policy
gameplay adapters
```

Those belong to F24 when they are Unity build-surface work, or to F25+ when they are gameplay/product behavior.

---

## 3. Corrected F23 plan

```text
F23A — Pause Content / Overlay / Input ADR Plan
F23B — Pause Content Requirement Contracts
F23C — Pause Presentation Intent Contracts
F23D — Pause Input Intent Contracts
F23E — Pause Boundary Intent Smoke + Adapter Deferral
F23F — Closure + Usage Guide
```

The earlier adapter/consumer naming is superseded:

```text
Pause Content Anchor Consumer -> Pause Content Requirement
Pause Overlay Adapter -> Pause Presentation Intent
Pause Input Resolver -> Pause Input Intent
```

---

## 4. F23B/F23C/F23D corrected result

F23 now uses these framework-only contracts under `Runtime/Pause`:

```text
PauseContentRequirementId
PauseContentRequirementPurpose
PauseContentRequirement
PausePresentationIntent
PauseInputActionId
PauseInputCommandKind
PauseInputSourceKind
PauseInputSignal
PauseInputIntent
```

The conceptual flow is:

```text
PauseSnapshot
  -> PauseContentRequirement
  -> PausePresentationIntent

PauseInputSignal
  -> PauseInputIntent
```

There is no interface/port that performs overlay show/hide, input resolution or Content Anchor binding in F23.

---

## 5. F23E result — Pause Boundary Intent Smoke

F23E adds a synthetic smoke:

```text
Run Pause Boundary Intent Smoke
```

Expected steps:

```text
contracts
content-requirement-intent
presentation-intent
input-intent
canonical-boundary
```

The smoke must prove that F23 remains intent-only:

```text
anchorBinding = none
overlayAdapter = none
inputAdapter = none
inputSystem = none
ui = none
timeScale = none
gameplayAdapters = none
unityBuild = deferredToF24
```

---

## 6. Deferral to F24

F24 remains the proper phase for concrete Unity build-surface wiring:

```text
F24B — Transition ↔ GameFlow Runtime Integration
F24C — Transition Curtain Unity Build
F24D — Loading Screen Unity Adapter Build
F24E — Pause Overlay Unity Build
F24F — Save Moment Authoring Boundary
F24G — Preferences Authoring Boundary
```

Pause overlay materialization starts at F24E, after F23 closes.

---

## 7. Non-goals

F23 does not create:

```text
UI
Canvas
prefab
ScriptableObject
Input System asset
action map
device binding
input polling
overlay adapter
Content Anchor binding execution
RuntimeContentAnchorBinding
TransitionEffect execution
Time.timeScale policy
Route/Activity lifecycle ownership
gameplay adapter
asmdef change
```

---

## 8. Consequences

F23 becomes safer and smaller. It records the language Pause will use when a later Unity build-surface asks for overlay/input/content, but it does not create that surface.

F24 must not reinterpret F23 as permission to build a full menu. It should implement only the minimal Unity surface needed to prove the framework contracts.
