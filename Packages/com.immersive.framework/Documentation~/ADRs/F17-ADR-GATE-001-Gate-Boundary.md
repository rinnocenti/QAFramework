# F17-ADR-GATE-001 - Gate Boundary

Status: Planned / F17A ADR Only  
Phase: F17 - Gate Foundation  
Type: Framework Core / Flow Admission / Boundary  
Last updated: 2026-06-26

---

## 1. Context

F0-F16 closed the first framework foundations through Object Entry, Object Reset and primitive Unity reset adapters. The next cut must not jump to Player/Actor/NPC/Timer/Door/Pickup contextual reset because the framework does not yet have a mature gameplay object model.

The next core axis is Gate. Gate must define whether the framework can admit a request, input, interaction or gameplay action at a given moment.

F17A is documentation/ADR only. It does not create runtime, editor, authoring components, assets or configuration.

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

## 6. Excluded Now

F17A does not implement:

```text
runtime Gate API
Editor UI
authoring asset
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

## 7. Guardrails

- Gate is not UI.
- Gate is not readiness.
- Gate is not the input system.
- Gate must not fabricaté identity from `GameObject.name`, hierarchy path, tags or scene path.
- Gate must not create a parallel lifecycle.
- Gate must not be a manager/coordinator hiding ownership.
- Gate failures and missing required policy must be explicit.
- Transition and Pause consume Gate instead of reimplementing blocker logic.

---

## 8. Consequences

Positive:

- Transition and Pause get a shared admission model.
- Flow blocking becomes diagnosable instead of implicit.
- Advanced consumers remain deferred until the core can reject or admit their requests consistently.

Costs:

- Useful gameplay consumers remain delayed.
- F17 must define language carefully before runtime contracts are created.
