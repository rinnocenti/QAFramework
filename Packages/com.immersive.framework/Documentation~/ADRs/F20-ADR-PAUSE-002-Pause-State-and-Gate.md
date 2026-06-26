# F20-ADR-PAUSE-002 - Pause State and Gate

Status: Planned  
Phase: F20 - Pause State and Pause Gate  
Type: Framework Core / Pause / Gate Consumer  
Last updated: 2026-06-26

---

## 1. Context

F10 recorded Pause as a consumer so it would not capture Route/Activity lifecycle. After Gate and Transition planning, Pause needs an operational core boundary.

Pause must not be reduced to a menu, overlay or `Time.timeScale`.

---

## 2. Decision

Pause is state plus Gate blocker.

Pause belongs to Framework Core only for:

```text
pause state
pause request/result facts
pause policy
Gate blocker relationship
diagnostics
```

Pause is not:

```text
Activity
Route lifecycle
Activity lifecycle
menu
overlay
input system
Time.timeScale contract
```

---

## 3. Gate Relationship

Pause consumes Gate to block gameplay and interaction scopes while allowing explicitly permitted requests.

Examples of allowed requests during Pause:

```text
resume
pause UI navigation
settings UI
diagnostic-safe framework requests
explicitly permitted transition requests
```

Examples of generally blocked scopes during Pause:

```text
gameplay mutation
gameplay input
world interaction
unapproved route/activity requests
```

---

## 4. Lifecycle Boundary

Pause does not own Route or Activity lifecycle.

Pause may coexist with Route/Activity state, but it must not model itself as an Activity or force a Route/Activity transition to pause gameplay.

---

## 5. Time Scale Boundary

`Time.timeScale` is a possible future adapter/policy, not the central Pause contract.

The canonical contract must remain pause state, Gate effects and explicit facts. A future time-scale adapter must be optional/required by policy and fail explicitly when required but absent.

---

## 6. Excluded Now

F20 does not implement:

```text
Pause menu
Pause overlay content
input implementation
Time.timeScale adapter
Pause modeled as Activity
Route/Activity lifecycle ownership
gameplay contextual reset
```

---

## 7. Guardrails

- Pause is state + Gate blocker.
- Pause is not Activity.
- Pause does not control Route/Activity lifecycle.
- Pause blocks gameplay, but does not necessarily block UI, resume or allowed requests.
- Pause must not use service locator/global singleton as the canonical API.
- Pause must not rely on `Time.timeScale` as the framework contract.
