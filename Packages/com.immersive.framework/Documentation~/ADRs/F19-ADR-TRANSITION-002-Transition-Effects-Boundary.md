# F19-ADR-TRANSITION-002 - Transition Effects Boundary

Status: Planned  
Phase: F19 - Transition Effects / Loading and Fade Adapters  
Type: Unity Adapter / Optional Effects / Boundary  
Last updated: 2026-06-26

---

## 1. Context

Transition orchestration must exist before concrete effects. Fade, loading and curtain behavior are useful, but they are not the framework core.

F19 plans the boundary for effect adapters after F18 defines orchestration.

---

## 2. Decision

Transition effects are adapters/consumers of Transition Orchestration.

Examples:

```text
fade adapter
loading overlay adapter
curtain adapter
progress display adapter
audio/visual stinger adapter
```

They may be optional or required by explicit policy.

---

## 3. Required vs Optional

Required effect adapter missing:

```text
explicit failure
blocking issue
diagnostic fact
no silent fallback
```

Optional effect adapter missing:

```text
allowed continuation
non-blocking issue or fact when useful
no fake success that hides configuration problems
```

---

## 4. Dependencies

No mandatory dependency on:

```text
DOTween
Timeline
Addressables
Asset Store fade package
custom loading screen package
```

Specific integrations can be future adapters with their own dependency boundary.

---

## 5. Unity Adapter Boundary

Unity adapters come after the lógical contract.

They may reference Unity APIs, but they must not redefine:

```text
Gate
Transition orchestration
Scene Lifecycle
Route lifecycle
Activity lifecycle
Pause state
```

---

## 6. Excluded Now

F19 does not define:

```text
canonical visual design
mandatory fade
mandatory loading overlay
DOTween integration
runtime implementation in F17A
Pause menu
gameplay contextual reset
```

---

## 7. Guardrails

- Fade/loading/curtain never substitute Gate.
- Effects are adapters, not Transition Core.
- Required adapter absence cannot silently degrade.
- Optional adapter absence must not fabricaté success facts.
- Visual implementation must not create global manager/service locator.
