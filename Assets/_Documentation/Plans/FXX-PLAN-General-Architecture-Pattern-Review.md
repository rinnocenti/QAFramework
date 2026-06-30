# FXX-PLAN — General Architecture Pattern Review

Status: Proposed / audit-only plan  
Scope: broad review pass to find architectural repetition, thick bridges, status-remapping chains and manual orchestration patterns before they become framework conventions.

This is a planning document for Codex-assisted review. It does not authorize implementation.

---

## 1. Goal

Find systemic patterns similar to:

```text
- Participant pattern duplicated across domains;
- RuntimeContent/ContentAnchor materialization orchestration trapped behind a MonoBehaviour bridge;
- god runtime/coordinator classes with too many responsibilities;
- repeated Result/Status/Issue containers;
- repeated rollback/compensation logic;
- status mapping chains that hide original failure cause.
```

The output should be a ranked list of consolidation candidates, not code changes.

---

## 2. Review rules

```text
1. Audit by reading code, not by file names only.
2. Do not implement changes.
3. Do not propose generic abstraction unless at least two real use cases exist.
4. Preserve typed identity and explicit result objects.
5. Preserve Unity authoring boundaries; do not move serialized fields casually.
6. Separate internal maintenance simplification from game-designer UX simplification.
7. Prefer small pilot migrations over “fix all domains now”.
```

---

## 3. Detection heuristics

Flag candidates when any of these are true:

| Heuristic | Why it matters |
|---|---|
| Class over ~400 lines and more than ~15 public/internal methods | Possible god coordinator. |
| MonoBehaviour over ~250 lines coordinating runtime services | Bridge may be doing orchestration instead of authoring. |
| Same suffix family repeated in 3+ domains: Descriptor/Entry/Result/Status/Issue | Candidate for Common primitive. |
| Same side-effect chain repeated or likely to be reused | Candidate for service/coordinator. |
| Multiple wrapper status enums over one operation | Debugging friction. |
| Rollback/compensation manually invoked from several failure points | Candidate for operation runner, but only after a second use case. |
| Public experimental API without consumer | Risk of premature surface freezing. |
| Unity side effect hidden behind many layers but not diagrammed | Onboarding/diagnostics issue. |

---

## 4. Areas to scan first

```text
Packages/com.immersive.framework/Runtime/RuntimeContent
Packages/com.immersive.framework/Runtime/ContentAnchor
Packages/com.immersive.framework/Runtime/ActivityFlow
Packages/com.immersive.framework/Runtime/RouteLifecycle
Packages/com.immersive.framework/Runtime/Transition
Packages/com.immersive.framework/Runtime/TransitionEffects
Packages/com.immersive.framework/Runtime/Loading
Packages/com.immersive.framework/Runtime/Pause
Packages/com.immersive.framework/Runtime/GameFlow
Packages/com.immersive.framework/Runtime/Snapshot
Packages/com.immersive.framework/Runtime/CycleReset
Packages/com.immersive.framework/Runtime/ObjectReset
```

---

## 5. Output format expected from Codex

Codex should produce:

```text
1. Ranked candidate list.
2. Evidence table: files, methods, repeated pattern, direct side effects, risk.
3. Recommendation: accept / watch / reject.
4. Suggested ADR name if accepted.
5. Suggested pilot cut that changes the fewest files.
6. Explicit non-goals.
```

---

## 6. Priority scoring

| Score | Meaning |
|---:|---|
| 5 | Active side-effect orchestration is hard to reuse or debug. Fix soon. |
| 4 | Repeated algorithm risks behavioral drift. Pilot consolidation recommended. |
| 3 | Large class or API surface is becoming hard to explain. Audit/diagram first. |
| 2 | Cosmetic duplication or naming noise. Defer. |
| 1 | Intentional domain-specific duplication. Do not consolidate. |

---

## 7. Current seed candidates

| Candidate | Initial score | Note |
|---|---:|---|
| Participant executor pattern | 4 | Already has a specific FXX ADR/plan. |
| RuntimeContent/ContentAnchor materialization orchestration | 5 | Coordinates Unity side effects, rollback and multiple status layers. |
| RuntimeContentRuntime responsibility split | 4 | Should follow service extraction, not precede it. |
| FlowRequestTrigger Activity/Route mirror | 3 | Must consider Unity serialization risk. |
| Status/result/issue containers across domains | 3 | Should be addressed incrementally via Common primitives. |

---

## 8. Non-goals

```text
No implementation.
No renumbering roadmap phases.
No moving files.
No changing asmdefs.
No touching assets/scenes/prefabs.
No replacing explicit domain contracts with generic names in public API.
```
