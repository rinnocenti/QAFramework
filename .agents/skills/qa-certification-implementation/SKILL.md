---
name: qa-certification-implementation
description: Use when an approved QAFramework Scenario design exists and the task explicitly requests implementation of one certification vertical slice.
---

# QA Certification Implementation

## Entry gate

Require an approved Scenario design and an explicit implementation request. Read the complete [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md) and the approved design before editing.

**Repository reality precedes QA design and implementation.** Reinspect the current QAFramework files, active package resolution, and the exact public `com.immersive.framework` authoring, lifecycle, ownership, action, and observability surfaces used by the design. Cite current files and symbols. The design records intent; it is not evidence that those surfaces still exist.

If reality invalidates a premise, stop at the first causal boundary. Report the mismatch for redesign or gap classification. Do not invent replacement architecture.

## Implement one complete slice

Use [the vertical-slice checklist](references/vertical-slice-checklist.md). Materialize only the minimum composition and concrete Scenario behavior needed to prove:

`valid environment -> known baseline -> supported action -> expected evidence -> terminal condition -> cleanup -> baseline restored or fresh boot -> verdict`

Use real framework lifecycle, supported authoring, and public APIs. Expected evidence must be declared before the action. Preserve the first causal divergence and distinguish:

- `PASS`: the exercised contract was proved and restoration completed.
- `FAIL`: the contract was validly exercised and violated.
- `BLOCKED`: environment, precondition, or observability prevented valid exercise.

Never publish `PASS` before cleanup and restoration evidence. Cleanup runs on success, assertion failure, timeout, and interruption. Direct containment may restore QA-owned state after a framework failure, but it must not erase or convert that failure.

## Ownership boundary

This skill edits QAFramework only. A deterministic framework defect is classified and reported to its owner; it is not repaired, bypassed, or hidden in the Scenario unless the user separately authorizes that framework change.

Keep the Scenario verdict separate from defect ownership. If deterministic investigation finds a framework contract violation while the Scenario is still establishing readiness, the Scenario remains `BLOCKED` because its supported action was never validly exercised; report the framework violation separately. `FAIL` begins only after the Scenario action is validly accepted and the exercised contract diverges.

Never use reflection, internal runtime hosts, service-locator bypasses, opportunistic global lookup, private-state mutation, `ConfigureForQa`, silent fallback, test-only backdoors, parallel runtime, residual state, or Full QA order.

Do not create a generic Runner, Scenario base/interface, Environment abstraction, result architecture, registry, fixture/helper, Suite, shared menu, global timeout policy, fresh-boot infrastructure, or asmdef topology from one slice. QA-NEW-001 is evidence, not a base class.

## Completion

Perform static validation that does not invoke Unity. Provide an exact manual Unity checklist and name all unexecuted validation. Do not claim runtime certification without runtime evidence.
