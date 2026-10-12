---
name: qa-certification-review
description: Use when independently reviewing a QAFramework Scenario design, implementation, failed certification, or readiness to expand certification coverage.
---

# QA Certification Review

## Independent evidence gate

This skill reviews artifacts owned by QAFramework. For cross-repository ownership and baseline selection, follow immersive-ecosystem; inspect consumer versions only when consumer compatibility is part of the reviewed claim.

Review is read-only unless the user separately requests fixes. Read [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), then independently inspect current QAFramework composition, active package resolution, and relevant public Framework authoring/lifecycle/ownership/action/observability surfaces. Claims, skills, historical code, generated projects, and stale caches are not proof of current capability.

Use the [review contract](references/review-contract.md). For Unity setup or composition, also use the shared [Unity QA authoring reference](../qa-certification-implementation/references/unity-qa-authoring.md).

For compile-surface review, verify every called member against the resolved Framework/QA source, not only neighboring scenario call sites. Check enum members separately from result convenience properties, nested observation evidence flags and payloads, and the actual recorder API. Reconcile all available compiler diagnostics against the final source; classify cascading definite-assignment errors only after fixing the first invalid member. Do not claim compile success when Unity compilation was not run.

## Review sequence

Apply the shared evidence and acceptance policy at [design skill reference](../qa-certification-design/references/evidence-policy.md). Review individual results, property coverage, and gate disposition separately; assess any composite/conditional acceptance against applicable governance and preserve all limitations and provenance. Do not demand more infrastructure without the policy's risk, uncovered property, missing evidence, and least intervention analysis.

Before first Play Mode, reconstruct:

`Sources → Preflight → Construction → Persisted Assets → Bootstrap/Scenario → Evidence → Cleanup → Restoration → Verdict`

Inspect operation ordering, typed dependency resolution, Unity-reference lifetime across scene operations, generated serialized links, baseline completeness, ownership, partial-failure cleanup, and whether success is emitted only after required postflight/restoration. Static defects should be reported from static evidence; Unity-only validation remains pending unless execution evidence exists.

Classify each finding as `QA defect`, `Framework contract violation`, `environment/precondition`, or `observability gap`. Keep owner separate from verdict. Preserve `PASS`, `FAIL`, `BLOCKED`, and first-causal-divergence semantics from ADR-001.

Do not recommend reflection, internal-host access, service-locator bypass, private-state mutation, hidden lookup, silent fallback, test backdoors, or architecture forbidden/deferred by ADR-001. Do not demand generic infrastructure without evidence from representative slices.
