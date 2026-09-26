---
name: qa-certification-review
description: Use when independently reviewing a QAFramework Scenario design, implementation, failed certification, or readiness to expand certification coverage.
---

# QA Certification Review

## Independent evidence gate

Review is read-only unless the user separately requests fixes. Read the complete [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md).

**Repository reality precedes QA design and review.** Independently inspect the current QAFramework composition and source, active package resolution, and only the relevant current public `com.immersive.framework` authoring, lifecycle, ownership, action, and observability surfaces. Cite concrete files, lines, and symbols. A design, implementation note, prior verdict, skill, historical QA, generated project file, stale cache, or remembered endpoint is not proof of current capability.

## Review contract

Use [the review contract](references/review-contract.md). Trace the first causal divergence through:

`valid environment -> baseline -> supported action -> evidence -> terminal condition -> cleanup -> restoration -> verdict`

Audit whether expectations were known before execution, missing evidence is detectable, result/cardinality is typed where applicable, and the first divergence survives secondary cleanup failures.

Classify every finding with the correct owner:

- `QA defect`
- `framework contract violation`
- `environment/precondition`
- `observability gap`

Keep classification separate from Scenario verdict. A framework defect discovered before the Scenario action is validly accepted can leave the Scenario `BLOCKED`; it does not become a Scenario `FAIL` merely because the defect owner is the framework.

## Hard boundaries

Absence of evidence is never success. Do not silently repair findings or recommend reflection, internal-host access, service-locator bypass, opportunistic global lookup, private-state mutation, `ConfigureForQa`, fallback, test backdoors, residual-state fixtures, or Full QA order.

Do not require a Runner, Scenario base/interface, Environment abstraction, result architecture, registry, fixture, Suite, menu, timeout policy, boot grouping, directory layout, or asmdef topology without evidence from representative slices. Distinguish certification of one Scenario from certification of QAFramework as a whole.

Report findings first, ordered by severity and causal impact. If runtime evidence was supplied, state exactly what it proves and what it does not. Do not claim unexecuted Unity validation.
