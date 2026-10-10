---
name: qa-certification-review
description: Use when independently reviewing a QAFramework Scenario design, implementation, failed certification, or readiness to expand certification coverage.
---

# QA Certification Review

## Independent evidence gate

Review is read-only unless the user separately requests fixes. Read [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), then independently inspect current QAFramework composition, active package resolution, and relevant public Framework authoring/lifecycle/ownership/action/observability surfaces. Claims, skills, historical code, generated projects, and stale caches are not proof of current capability.

Use the [review contract](references/review-contract.md). For Unity setup or composition, also use the shared [Unity QA authoring reference](../qa-certification-implementation/references/unity-qa-authoring.md).

## Review sequence

Before first Play Mode, reconstruct:

`Sources → Preflight → Construction → Persisted Assets → Bootstrap/Scenario → Evidence → Cleanup → Restoration → Verdict`

Inspect operation ordering, typed dependency resolution, Unity-reference lifetime across scene operations, generated serialized links, baseline completeness, ownership, partial-failure cleanup, and whether success is emitted only after required postflight/restoration. Static defects should be reported from static evidence; Unity-only validation remains pending unless execution evidence exists.

Classify each finding as `QA defect`, `Framework contract violation`, `environment/precondition`, or `observability gap`. Keep owner separate from verdict. Preserve `PASS`, `FAIL`, `BLOCKED`, and first-causal-divergence semantics from ADR-001.

Do not recommend reflection, internal-host access, service-locator bypass, private-state mutation, hidden lookup, silent fallback, test backdoors, or architecture forbidden/deferred by ADR-001. Do not demand generic infrastructure without evidence from representative slices.
