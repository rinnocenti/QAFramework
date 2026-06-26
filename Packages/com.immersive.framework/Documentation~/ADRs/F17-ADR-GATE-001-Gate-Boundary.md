# F17-ADR-GATE-001 - Gate Boundary

Status: Accepted / F17C Request Admission  
Phase: F17 - Gate Foundation  
Type: Framework Core / Flow Admission / Boundary  
Last updated: 2026-06-26

---

## 1. Context

F0-F16 closed the first framework foundations through Object Entry, Object Reset and primitive Unity reset adapters. The next cut must not jump to Player/Actor/NPC/Timer/Door/Pickup contextual reset because the framework does not yet have a mature gameplay object model.

The next core axis is Gate. Gate must define whether the framework can admit a request, input, interaction or gameplay action at a given moment.

F17A was documentation/ADR only. F17B introduced passive runtime primitives for Gate decisions, blockers and snapshots. F17C integrates those primitives with existing request-admission guards for Route, Activity, Cycle Reset and Object Reset. It does not add Pause, Transition, Input, UI, gameplay object model or a global Gate registry.

---

## 2. Decision

Gate belongs to `com.immersive.framework` as Framework Core.

Gate is not:

```text
UI
readiness
input system
pause menu
transition effect
service locator
global manager
```

Gate decides admission. A Gate decision answers whether a scoped operation can proceed, should be blocked, should be queued by a future policy, or should fail with explicit facts.

Examples of admission subjects:

```text
Route request
Activity request
Scene transition request
input acceptance
interaction acceptance
gameplay acceptance
pause/resume request
content request
```

---

## 3. Scopes

Gate must use explicit scopes/domains instead of string parsing or GameObject hierarchy.

Initial planning scopes:

```text
Session
Route
Activity
GameFlow
Scene
Content
Input
Interaction
Gameplay
Pause
Transition
```

Scopes are architectural domains, not UI tabs or authoring categories.

---

## 4. Decision Output

Gate must produce explicit decision/result/facts.

Minimum conceptual output:

```text
decision status
subject/request kind
scope/domain
owner identity when applicable
blocking reasons
non-blocking facts
policy source
diagnostic facts
```

The decision cannot be hidden behind bool-only APIs in the canonical contract.

---

## 5. Consumers

Transition and Pause must consume Gate.

Transition consumes Gate to block/release flow during scene lifecycle, release, readiness and callbacks.

Pause consumes Gate to block gameplay while still allowing allowed requests such as resume, UI navigation or explicitly permitted system requests.

Advanced consumers and gameplay capabilities must not bypass Gate when they need flow admission.

---

## 6. F17B Runtime Primitives

F17B introduces only passive primitives:

```text
GateScope
GateDomain
GateDecisionStatus
GateDecision
GateBlocker
GateEvaluationResult
GateSnapshot
```

These types define admission language and diagnostics. They do not register blockers globally, mutate lifecycle, queue requests, bind input, show UI, run transitions or pause gameplay by themselves.

## 7. F17C Request Admission Integration

F17C uses the F17B primitives to evaluate existing request-admission guards. The integration is intentionally narrow:

```text
Route request already-in-flight admission
Activity request already-in-flight admission
Clear Activity request already-in-flight admission
Cycle Reset request already-in-flight admission
Object Reset request already-in-flight admission
```

F17C preserves existing result categories such as `IgnoredAlreadyInFlight` and `RejectedInvalidRequest`; the difference is that the blocking decision now comes from a `GateEvaluationResult` with explicit blocker diagnostics.

F17C does not create a Gate registry, authoring asset, editor UI, queue, Pause runtime, Transition runtime or input binding.

## 8. Excluded Now

F17C does not implement:

```text
Gate runtime registry
Gate authoring asset
Editor UI
request queue
global singleton
service locator
input binding
Pause menu
fade/loading
Scene Transition runtime
Player/Actor/NPC/Timer/Door/Pickup reset
gameplay object model
```

---

## 9. Guardrails

- Gate is not UI.
- Gate is not readiness.
- Gate is not the input system.
- Gate must not fabricate identity from `GameObject.name`, hierarchy path, tags or scene path.
- Gate must not create a parallel lifecycle.
- Gate must not be a manager/coordinator hiding ownership.
- Gate failures and missing required policy must be explicit.
- Transition and Pause consume Gate instead of reimplementing blocker logic.

---

## 10. Consequences

Positive:

- Transition and Pause get a shared admission model.
- Flow blocking becomes diagnosable instead of implicit.
- Advanced consumers remain deferred until the core can reject or admit their requests consistently.

Costs:

- Useful gameplay consumers remain delayed.
- F17 must define language carefully before runtime contracts are created.
