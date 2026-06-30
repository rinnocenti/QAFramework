# CODEX PROMPT — FXX General Architecture Pattern Audit

You are working in a Unity 6.5 project. The framework package is under:

```text
Packages/com.immersive.framework
```

Task type: **audit-only**. Do not modify code, assets, asmdefs, package metadata, scenes or prefabs.

## Goal

Find whether other parts of the framework have the same classes of architectural problems already identified in two seed cases:

```text
Seed A — Participant pattern duplication:
Repeated Descriptor/Entry/Result/Status/Issue/Executor shapes across multiple domains.

Seed B — RuntimeContent/ContentAnchor materialization orchestration:
A thick bridge/pipeline manually coordinates logical runtime state, physical Unity side effects, binding, placement, rollback and status remapping.
```

Your job is **not** to confirm these two seed cases again. Use them as pattern definitions, then scan the rest of the framework for similar issues.

## Areas to inspect

Start here, then expand if references lead elsewhere:

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

If a path does not exist in the current project snapshot, state that explicitly and continue.

## Patterns to detect

Flag candidates with direct code evidence when you find:

```text
1. Domain pattern copied in 3+ modules instead of extracted once.
2. Route/Activity or Session/Route/Activity mirror classes evolving separately.
3. MonoBehaviour bridge above ~250 lines doing orchestration rather than only authoring delegation.
4. Runtime/coordinator class above ~400 lines with many public/internal responsibilities.
5. Manual side-effect sequence: prepare -> execute Unity side effect -> register logical handle -> bind -> place -> release/rollback.
6. Rollback/compensation manually repeated in multiple failure branches.
7. Result/status enums remapped through multiple wrapper layers for one operation.
8. Experimental public API with no real consumer.
9. Repeated validation helpers or enum checks that belong in Common.
10. Operations that are hard to explain as a small sequence diagram.
```

## Required output

Produce markdown only. Do not patch files unless the user later asks.

Use this structure:

```markdown
# FXX-AUDIT — General Architecture Pattern Review

## 1. Executive summary

## 2. Ranked candidate backlog
| Rank | Candidate | Score | Evidence | Pattern | Risk | Suggested action | First safe cut |

## 3. Candidate details
### Candidate 1 — <name>
- Evidence files/classes/methods:
- What repeats or over-orchestrates:
- Why it matters:
- What must remain domain-specific:
- Suggested ADR:
- Suggested first safe cut:
- Non-goals:

## 4. Rejected candidates
Patterns reviewed but intentionally domain-specific or not worth consolidating.

## 5. Recommended next steps
```

## Scoring

```text
5 = active side-effect orchestration is hard to reuse/debug and likely to be copied.
4 = repeated algorithm risks behavioral drift.
3 = large coordinator/status noise increases onboarding cost; audit/diagram first.
2 = cosmetic duplication or naming noise; defer.
1 = intentional domain-specific duplication; reject consolidation.
```

## Guardrails

```text
No implementation.
No code edit.
No scene/prefab/asset edit.
No asmdef/package change.
No service locator, singleton or reflection recommendation.
No generic abstraction without at least two concrete use cases.
No public API change without migration strategy.
No serialized field rename.
No behavior change.
```

## Important interpretation

Do not write “the materialization issue is confirmed” as the main result. The main result should be a ranked map of **other framework areas** where the same correction style may apply.
