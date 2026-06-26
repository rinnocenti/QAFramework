# F23-ADR-PAUSE-003 - Pause Content Overlay Input Boundary

Status: Planned / Blocked until F22G  
Phase: F23 - Pause Content / Overlay / Input Boundary  
Type: Framework Consumer / Authoring / Input Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as logical Pause core. That core owns Pause state, request/result, snapshot/facts and the passive Gate blocker relationship. It intentionally does not own visual overlay, menu content, input binding, Canvas/prefab setup or `Time.timeScale`.

F21 now opens Save / Snapshot / Preferences / Progression Save Foundation. F22 now owns Loading Operation / Progress / Readiness Boundary. Pause visual/content/input therefore moves to F23 so those consumers do not precede core Save and Loading contracts. A pre-F23 lifecycle audit found that F22 still needs a small F22G readiness/result primitive closure before this ADR can move from planned boundary to implementation plan.

---

## 2. Decision

Pause overlay/content is a consumer of Pause state and Gate. It is not Pause core.

Pause content should use existing framework concepts where applicable:

```text
Content Anchor
Content Anchor Binding
Runtime placement
Runtime content handle
explicit ownership
diagnostic facts
```

Pause content must not define the core Pause state, Gate behavior, Route lifecycle or Activity lifecycle.

---

## 3. Input Boundary

Pause input is separate from gameplay input.

Pause input may allow:

```text
resume
menu navigation
settings actions
accessibility actions
explicitly allowed framework requests
```

Gameplay input should remain blocked by Pause Gate unless explicitly allowed by policy.

---

## 4. Overlay Boundary

Pause overlay is presentation/content.

It must not own:

```text
Route lifecycle
Activity lifecycle
Gate rules
Transition orchestration
Time.timeScale policy
gameplay state
```

---

## 5. Time Scale Boundary

`Time.timeScale` remains a future adapter/policy, not the central Pause contract.

Pause content may show UI while time-scale policy is absent, optional or required by future configuration. Required policy absence must fail explicitly in that future cut.

---

## 6. Excluded Until F23 Starts

This ADR does not currently implement:

```text
concrete menu prefab
input system integration
Time.timeScale adapter
UI Toolkit/UGUI dependency decision
Pause as Activity
Player/Actor lifecycle
gameplay contextual reset
```

---

## 7. Guardrails

- Pause overlay/content is consumer space.
- Pause content uses Content Anchor/binding/runtime placement when applicable.
- Pause input is separate from gameplay input.
- `Time.timeScale` is a future adapter/policy, not the central contract.
- Overlay/content cannot bypass Gate to resume gameplay.

## 8. Precondition before F23A

Do not start F23A until F22G has either:

```text
closed Loading readiness/result primitives
```

or explicitly narrowed F22 so `Readiness` is no longer claimed as part of the closed Loading boundary.

F23 must consume Pause, Gate, Content Anchor, Loading and Preferences boundaries. It must not create a parallel loading/readiness model inside Pause overlay/input.
