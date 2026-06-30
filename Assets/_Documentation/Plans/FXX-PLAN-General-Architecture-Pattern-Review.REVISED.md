# FXX-PLAN — General Architecture Pattern Review

Status: Proposed / audit-only plan  
Scope: broad Codex-assisted review to find structural patterns across the framework that deserve the same type of correction proposed for `Participant` and `RuntimeContent/ContentAnchor Materialization`.

This document does **not** authorize implementation. It defines a review lane before new consolidation ADRs are accepted.

---

## 1. Goal

Run a general architectural review of `Packages/com.immersive.framework` to find places where the framework is repeating one of these failure modes:

```text
- A domain pattern copied across multiple modules instead of extracted once.
- A MonoBehaviour bridge doing orchestration instead of only reading Inspector data and delegating.
- A runtime/coordinator class accumulating multiple responsibilities.
- A side-effect operation manually coordinating logical state, physical Unity state, binding, placement and rollback.
- A rollback/compensation sequence written by hand in several failure branches.
- A chain of status/result remapping that hides the original failure cause.
- Public experimental API exposed before a real consumer proves the contract.
- Route/Activity or Session/Route/Activity mirrors evolving separately.
```

`Participant` and `RuntimeContent/ContentAnchor Materialization` are **seed examples**, not the target of confirmation. Codex should use them as pattern definitions, then search for similar issues in other areas.

---

## 2. Review questions

For each candidate found, answer:

```text
1. What is the repeated or over-orchestrated pattern?
2. Which files/classes/methods prove it by direct code reading?
3. Is the problem active now, or only likely to become a problem later?
4. Does it affect runtime side effects, authoring UX, maintenance, diagnostics, or all of them?
5. Is there at least one safe pilot cut?
6. What should remain domain-specific and must not be generalized?
7. What smoke/validator evidence would be needed before implementation?
```

The desired output is not a refactor proposal for everything. The desired output is a **ranked consolidation backlog** with enough evidence to decide which ADRs are worth writing.

---

## 3. Seed patterns

### 3.1. Seed A — Participant duplication

Known shape:

```text
I<Domain>Participant
I<Domain>ParticipantSource
<Domain>ParticipantDescriptor
<Domain>ParticipantEntry
<Domain>ParticipantRequiredness
<Domain>ParticipantResult
<Domain>ParticipantResultStatus
<Domain>Issue / IssueKind / IssueSeverity
Runtime executor loops participants, maps exceptions, aggregates blocking/non-blocking issues.
```

Review instruction:

```text
Do not re-audit only CycleReset/ObjectReset/Snapshot. Use this shape to find other duplicated domain families.
```

### 3.2. Seed B — RuntimeContent / ContentAnchor materialization orchestration

Known shape:

```text
Bridge reads Inspector fields but also builds contexts, adapters, declarations, requests and pipeline.
Pipeline coordinates materialize -> apply handle -> verify evidence -> bind -> place.
Failure branches call manual rollback/compensation.
Status is remapped through multiple wrapper layers.
RuntimeContentRuntime concentrates root/context/handle/request/release responsibilities.
```

Review instruction:

```text
Do not merely confirm the same path. Search for other operations that coordinate similar side-effect chains.
```

---

## 4. Areas to scan

Scan all runtime areas, but start with modules most likely to contain the same patterns:

```text
Packages/com.immersive.framework/Runtime/ActivityFlow
Packages/com.immersive.framework/Runtime/RouteLifecycle
Packages/com.immersive.framework/Runtime/GameFlow
Packages/com.immersive.framework/Runtime/RuntimeContent
Packages/com.immersive.framework/Runtime/ContentAnchor
Packages/com.immersive.framework/Runtime/SceneLifecycle
Packages/com.immersive.framework/Runtime/Loading
Packages/com.immersive.framework/Runtime/Transition
Packages/com.immersive.framework/Runtime/TransitionEffects
Packages/com.immersive.framework/Runtime/Pause
Packages/com.immersive.framework/Runtime/Snapshot
Packages/com.immersive.framework/Runtime/CycleReset
Packages/com.immersive.framework/Runtime/ObjectReset
Packages/com.immersive.framework/Runtime/LocalContribution
Packages/com.immersive.framework/Runtime/Common
```

If some paths do not exist in the current package, record them as `Not present in current snapshot` instead of inventing conclusions.

---

## 5. Candidate scoring

| Score | Meaning | Typical action |
|---:|---|---|
| 5 | Active side-effect orchestration is hard to reuse/debug and likely to be copied. | Write ADR + small pilot plan soon. |
| 4 | Repeated algorithm risks behavioral drift. | Write ADR after evidence table. |
| 3 | Large coordinator or status/result noise increases onboarding cost. | Audit/diagram first; defer implementation. |
| 2 | Cosmetic duplication or naming noise. | Track only if it blocks UX/docs. |
| 1 | Intentional domain-specific duplication. | Reject consolidation. |

---

## 6. Evidence table format

Codex should produce a table like:

| Rank | Candidate | Score | Evidence files | Pattern | Risk | Suggested action | First safe cut |
|---:|---|---:|---|---|---|---|---|
| 1 | Example | 5 | `Runtime/...` | Thick bridge + manual rollback | Reuse/debug | ADR | Audit-only diagram |

Each candidate must have direct evidence from code. Do not infer from file names alone.

---

## 7. Guardrails

```text
No implementation.
No code edits.
No asset, scene or prefab edits.
No asmdef/package changes.
No phase renumbering.
No generic abstraction unless at least two concrete use cases exist.
No replacement of public typed identities with generic string-like ids.
No public API change proposal without migration strategy.
No moving serialized fields casually.
No service locator/singleton/reflection as a simplification mechanism.
```

---

## 8. Output expected from Codex

Codex should produce one markdown audit report:

```text
Assets/_Documentation/Audits/FXX-AUDIT-General-Architecture-Pattern-Review.md
```

The report should include:

```text
1. Executive summary.
2. Ranked candidate backlog.
3. Evidence table per candidate.
4. Patterns rejected as intentional/domain-specific.
5. Suggested ADR candidates.
6. Suggested first implementation cut for each accepted candidate.
7. Non-goals and risks.
```

Do not create ADRs during this audit unless explicitly asked in a later step.
