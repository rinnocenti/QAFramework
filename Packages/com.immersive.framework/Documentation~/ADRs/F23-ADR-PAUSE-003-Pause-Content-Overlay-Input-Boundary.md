# F23-ADR-PAUSE-003 - Pause Content Overlay Input Boundary

Status: Accepted / F23C Applied / Overlay Adapter Boundary  
Phase: F23 - Pause Content / Overlay / Input Boundary  
Type: Framework Consumer / Authoring / Input Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as the logical Pause core. That core owns Pause state, request/result, snapshot/facts and the passive Gate blocker relationship. It intentionally does not own visual overlay, menu content, input binding, Canvas/prefab setup or `Time.timeScale`.

F21 closed Save / Snapshot / Preferences / Progression Save Foundation. F22 closed Loading Operation / Progress / Readiness Boundary, including pre-F23 debt closure for readiness observations and loading result/issues. Pause visual/content/input can now start without pulling missing Save or Loading core concepts into Pause.

F23 therefore opens only the Pause consumer boundary:

```text
Pause Content
Pause Overlay
Pause Input
```

F23 must not turn Pause UI into a lifecycle owner, a Unity build-surface phase or a gameplay adapter foundation.

---

## 2. Decision

Pause overlay/content is a consumer of Pause state and Gate. It is not Pause core.

Pause content should consume existing framework concepts where applicable:

```text
PauseRuntime / PauseState / PauseResult
PauseGateBlockerPolicy
Content Anchor declaration/binding vocabulary
Runtime placement / Runtime content handle vocabulary
Transition Effects boundary for fade/curtain when explicitly requested
Loading boundary for operation/progress/readiness reporting when explicitly observed
Preferences boundary for settings menu values when explicitly wired later
```

Pause content must not define or own:

```text
core Pause state
Gate behavior
Route lifecycle
Activity lifecycle
Transition orchestration
SceneLifecycle execution
LoadingOperation execution
Progression Save requests
gameplay state
```

---

## 3. F23 implementation plan

The accepted plan is:

```text
F23A — Pause Content / Overlay / Input ADR Plan
F23B — Pause Content Anchor Consumer Contracts
F23C — Pause Overlay Adapter Boundary
F23D — Pause Input Boundary Contracts
F23E — Pause Content / Overlay / Input Diagnostics Smoke
F23F — Closure + Usage Guide
```

F23A is documentation-only. It accepts the boundary and updates roadmap/index/status. It does not add runtime code, asmdef changes, UI, scene objects, prefabs, ScriptableObjects, input assets or gameplay adapters.

F23B adds passive Pause Content Anchor consumer contracts. It lets Pause express a request for an existing Content Anchor and prepare canonical `ContentAnchorBindingRequest` data for a future adapter. It does not create anchors, bind anchors physically, materialize UI, bind input, execute Transition Effects, mutate Pause state or own lifecycle.

---

## 4. F23B result — Pause Content Anchor Consumer Contracts

F23B introduces these contracts under `Runtime/Pause`:

```text
PauseContentAnchorRequestId
PauseContentAnchorPurpose
PauseContentAnchorRequest
PauseContentAnchorConsumerStatus
PauseContentAnchorConsumerResult
IPauseContentAnchorConsumer
```

The contract flow is:

```text
PauseContentAnchorRequest
  -> IPauseContentAnchorConsumer.Prepare(request, ContentAnchorSet)
  -> PauseContentAnchorConsumerResult
  -> optional ContentAnchorBindingRequest for a future adapter
```

This is still a consumer boundary. It does not create anchors, discover scene objects, instantiate prefabs, show overlays, bind input, mutate `PauseRuntime`, execute Transition Effects or change `Time.timeScale`.

Validation for F23B is compile/import only. The F23 diagnostics smoke is planned for F23E after overlay and input contracts exist.

## 5. F23C result — Pause Overlay Adapter Boundary

F23C introduces these visual-facing contracts under `Runtime/Pause`:

```text
IPauseOverlayAdapter
PauseOverlayPresentation
PauseOverlayAdapterAction
PauseOverlayAdapterStatus
PauseOverlayAdapterResult
```

The contract flow is:

```text
PauseSnapshot
  -> PauseOverlayPresentation
  -> IPauseOverlayAdapter.Show/Update/Hide(presentation)
  -> PauseOverlayAdapterResult
```

`PauseOverlayPresentation` may carry an optional prepared `PauseContentAnchorConsumerResult` from F23B, but it does not execute Content Anchor binding. The overlay adapter boundary presents state only; it does not request Pause, bind input, change `Time.timeScale`, execute Transition Effects, create anchors, discover scene objects, instantiate prefabs, own Route/Activity lifecycle or become a gameplay adapter.

Validation for F23C is compile/import only. The F23 diagnostics smoke is planned for F23E after input contracts exist.

## 6. Pause Content Boundary

Pause content is framework consumer content. It may represent:

```text
pause menu shell
resume/settings/menu actions
visual overlay root request
allowed framework actions
diagnostics of active Pause state
```

Pause content does not own the Pause state. It observes Pause state and asks the existing Pause runtime to resume/toggle/request state changes.

Pause content must not be authored as an Activity and must not create a parallel lifecycle. If a future game wants an Activity-like pause menu, that belongs to a gameplay/product decision after the framework boundary exists.

---

## 7. Overlay Boundary

Pause overlay is presentation/content.

It may expose a future adapter boundary for:

```text
show pause overlay
update pause overlay
hide pause overlay
report adapter failure
report unsupported operation
```

It must not own:

```text
Route lifecycle
Activity lifecycle
Gate rules
Transition orchestration
Loading operation execution
Time.timeScale policy
gameplay state
```

A future concrete UI adapter may be UGUI, UI Toolkit or project-specific. F23C does not choose or require one.

---

## 8. Input Boundary

Pause input is separate from gameplay input.

Pause input may allow:

```text
pause toggle
resume
menu navigation
settings actions
accessibility actions
explicitly allowed framework requests
```

Gameplay input should remain blocked by Pause Gate unless explicitly allowed by policy.

F23 input contracts must avoid assuming a specific Unity Input System asset, action map or device layout. Concrete binding remains adapter territory.

---

## 9. Time Scale Boundary

`Time.timeScale` remains a future adapter/policy, not the central Pause contract.

Pause content may show UI while time-scale policy is absent, optional or required by future configuration. Required policy absence must fail explicitly in that future cut.

F23C does not add a `Time.timeScale` adapter.

---

## 10. Relationship to F21 Save / Preferences

F23 may later display settings UI, but the Preferences store remains owned by F21. Pause UI must not create a second preferences backend.

Progression Save remains owned by F21. Pause UI may later trigger explicit save/load requests only through the Progression Save runtime/port; it must not know JSON paths or slot file names.

Snapshot remains backend-agnostic and participant-driven. Pause content must not invent its own snapshot system.

---

## 11. Relationship to F22 Loading

F22 owns Loading operation/progress/readiness/result vocabulary. Pause overlay can display loading information in the future only by consuming canonical Loading records.

Pause must not create a second loading progress model under Pause UI.

---

## 12. Relationship to Gameplay Adapter Foundation

Gameplay adapters remain deferred to F25.

F24 is the intermediate Unity Build Surface / Lifecycle Wiring phase after F23. F23 may define Pause content/overlay/input contracts, but concrete Unity build surfaces that prove framework wiring in scenes/prefabs belong to F24 when they require real object lifecycle proof.

F23 must not introduce:

```text
Player adapter
Actor adapter
NPC adapter
Camera gameplay adapter
Audio gameplay adapter
Projectile/Damage/Attribute adapter
gameplay reset adapter
```

F23 may define framework-facing ports that gameplay can consume later, but it must not bind to gameplay object behavior.

F23 must not skip F24 by turning Pause overlay work into a product gameplay adapter or a full game UI build.

---

## 13. Exclusions

F23A and the initial F23 boundary do not implement:

```text
concrete menu prefab
input asset binding
Canvas setup
UI Toolkit setup
UGUI dependency decision
Time.timeScale adapter
Pause as Activity
Player/Actor lifecycle
gameplay contextual reset
Progression Save UI
Loading screen UI
Unity Build Surface / Lifecycle Wiring
```

---

## 14. Guardrails

- Pause overlay/content is consumer space.
- Pause content consumes Pause core; it does not redefine Pause core.
- Pause content uses Content Anchor/binding/runtime placement vocabulary when applicable.
- Pause input is separate from gameplay input.
- `Time.timeScale` is a future adapter/policy, not the central contract.
- Overlay/content cannot bypass Gate to resume gameplay.
- Pause UI must not create a parallel Save, Loading, Transition or lifecycle track.
- Pause UI must not bypass F24 Unity build/lifecycle wiring before F25 gameplay adapters.

---

## 15. F23A/F23B/F23C result

F23A accepts this ADR and realigns documentation only.

F23B applies the Pause Content Anchor consumer contracts and keeps the cut asset-free and UI-free. F23C applies the Pause Overlay adapter boundary and keeps the cut asset-free, UI-free and lifecycle-free.

Next cut:

```text
IF-FW-F23D — Pause Input Boundary Contracts
```

After F23 closes, the next framework phase is:

```text
F24 — Unity Build Surface / Lifecycle Wiring
```
