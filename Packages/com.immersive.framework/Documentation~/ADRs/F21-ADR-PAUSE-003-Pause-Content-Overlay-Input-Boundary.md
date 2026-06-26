# F21-ADR-PAUSE-003 - Pause Content Overlay Input Boundary

Status: Planned  
Phase: F21 - Pause Content / Overlay / Input Boundary  
Type: Framework Consumer / Authoring / Input Boundary  
Last updated: 2026-06-26

---

## 1. Context

After Pause state and Pause Gate are planned, the framework can plan pause-facing content. Menus, overlays and input handling are consumers/boundaries, not the Pause Core.

---

## 2. Decision

Pause overlay/content is a consumer of Pause state and Gate.

Pause content should use existing framework concepts where applicable:

```text
Content Anchor
Content Anchor Binding
Runtime placement
Runtime content handle
explicit ownership
diagnostic facts
```

Pause content must not define the core Pause state or Gate behavior.

---

## 3. Input Boundary

Pause input is separaté from gameplay input.

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

## 6. Excluded Now

F21 does not implement:

```text
concrete menu prefab
input system integration
Time.timeScale adapter
UI toolkit/UGUI dependency decision
Pause as Activity
Player/Actor lifecycle
gameplay contextual reset
```

---

## 7. Guardrails

- Pause overlay/content is consumer space.
- Pause content uses Content Anchor/binding/runtime placement when applicable.
- Pause input is a boundary separaté from gameplay input.
- `Time.timeScale` is future adapter/policy, not the central contract.
- Overlay/content cannot bypass Gate to resume gameplay.
