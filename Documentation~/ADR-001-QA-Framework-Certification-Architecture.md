# ADR-001 — QA Framework Certification Architecture

**Status:** Accepted  
**Date:** 2026-09-24  
**Scope:** Immersive Framework QA architecture

## 1. Context

The previous QAFramework evolved incrementally alongside the Immersive Framework and accumulated incompatible execution models: in-scene panel runners, Editor-driven persisted phase machines, and Editor async regressions executed during Play Mode.

The legacy implementation was removed. The new QAFramework starts from a clean baseline. Legacy knowledge may be used as historical evidence for contracts and failure cases, but legacy implementation architecture is not a compatibility requirement.

FIRSTGAME and QAFramework have distinct responsibilities and must remain separate.

## 2. Decision

Immersive Framework verification is divided into three levels:

```text
PACKAGE / NUNIT
        ↓
isolated contracts

QAFRAMEWORK
        ↓
controlled runtime certification
boundary / adversarial / negative

FIRSTGAME
        ↓
integrated consumer happy path
```

### Package / NUnit

Package tests prove contracts that do not require a materially instantiated game runtime. Pure logic, value objects, policies, validation, deterministic state transitions, serialization contracts, structural invariants, isolated negative contracts and fake-based implementation tests belong here.

Mocks and fakes are acceptable at this level.

### QAFramework

QAFramework is the controlled certification laboratory for the real Immersive Framework runtime.

It answers:

> Given a valid and known starting state, does the real framework preserve its public contract under nominal, boundary, adversarial, and negative runtime conditions?

QAFramework uses real framework lifecycle, real Unity composition where required, public framework APIs, supported authoring and real integrations such as Input System and camera runtime when those integrations are part of the contract.

QA does not create a parallel runtime.

### FIRSTGAME

FIRSTGAME is the integrated consumer happy path.

It answers:

> Can a correctly authored real game combine supported Immersive Framework features and produce the expected gameplay experience?

FIRSTGAME is not the primary environment for destructive, artificial, adversarial or intentionally invalid certification.

## 3. QAFramework is a laboratory, not a reference game

The new QAFramework must not become another FIRSTGAME.

A QA Test Environment provides only the minimum real composition required for a class of scenarios. Environment boundaries are derived from the actual framework composition required by the contract, not mechanically from source-code folders or namespaces.

## 4. Valid baseline principle

Every runtime QA Scenario starts from a known valid baseline:

```text
valid environment
→ known baseline
→ supported action
→ condition under test
→ observable framework response
→ terminal evidence
→ cleanup
→ known baseline
```

Residual state is not a fixture. Execution order must not silently become part of a Scenario precondition.

If the original baseline cannot safely be restored during the current runtime, the Scenario requires a fresh boot.

## 5. Scenario

Scenario is the conceptual unit of certification.

A Scenario contains conceptually:

```text
precondition / baseline
action
observation
expected evidence
terminal condition
cleanup obligation
```

A Scenario may prove multiple observations only when they belong to the same causal lifecycle. It must not become a general-purpose suite.

The term is architectural; this ADR does not require a concrete class or interface named Scenario.

## 6. Scenario categories

QAFramework may exercise:

- **Nominal:** valid configuration and expected operation, when needed as a baseline proof.
- **Boundary:** valid operation at or across a contract boundary.
- **Adversarial:** valid environment with deliberately difficult timing, ordering, lifecycle or ownership.
- **Negative:** a supported request expected to be rejected without corrupting valid state.

Intentionally invalid authoring is not automatically runtime QA. If the contract can be proven deterministically without a materialized runtime, it belongs at the lower test level.

## 7. Public contract rule

Runtime QA proves the framework as a consumer can use it.

QA scenarios use supported public APIs and supported authoring surfaces.

Runtime QA must not depend on private reflection, service locators used to bypass ownership, opportunistic global discovery, internal runtime hosts, silent fallback, or direct mutation of private framework state.

External systems may be controlled when legitimately outside the framework contract, such as synthetic Input System devices.

If an important contract cannot be exercised or observed through supported surfaces, classify the gap before changing the framework:

```text
package/internal test concern
missing public observability
missing supported extension point
contract that should not be externally certified
```

The framework must not acquire test-only backdoors merely to make QA convenient.

## 8. Test Environment

A Test Environment is real framework composition required to execute Scenarios.

It contains composition, not assertions.

Canonical environments should be persisted and inspectable when appropriate. Normal QA execution validates the environment rather than silently repairing it.

Setup/rebuild operations are authoring operations and are distinct from Scenario execution.

Temporary composition is allowed when the proof requires destructive mutation, intentionally invalid authoring, specialized topology or isolation that cannot safely be restored in place. Temporary state has explicit ownership and must be destroyed or restored by its owner.

## 9. Scenario isolation and cleanup

A Scenario must be independently reproducible from its declared baseline and must not require leftovers from another Scenario.

After execution, one outcome is explicit:

```text
baseline restored
```

or:

```text
fresh runtime boot required
```

Cleanup is part of the Scenario contract.

Ownership follows:

> The component that creates temporary state owns its release.

Runtime resources are released in reverse ownership order. Environment-level mutations are restored by the environment/runner owner. Cleanup must execute on success and failure and be represented in terminal evidence.

A certification verdict must not be emitted before required cleanup and restoration complete.

## 10. Runner

Execution mechanics are shared infrastructure and must not be reinvented independently by each domain.

The new QAFramework will have one coherent execution model responsible for concerns such as:

```text
Edit Mode preparation
runtime entry
Play Mode transitions
domain reload survival
execution phase persistence
timeouts
interrupted-run recovery
runtime exit
environment restoration
terminal result publication
```

Historical CAMERA-032 is evidence for these requirements, not an implementation to copy wholesale.

Concrete Runner classes, interfaces, inheritance, serialization and file structure remain intentionally undecided until vertical slices demonstrate the minimum common requirements.

## 11. Async causal proof

When an intermediate asynchronous state is part of the contract, QA controls the condition that permits completion:

```text
start operation
→ observe required pending/intermediate state
→ release controlled condition
→ await terminal state
→ verify terminal evidence
```

QA must not race a naturally short async window when that intermediate state is what the Scenario claims to prove.

## 12. Evidence

A Scenario has a known expectation set before execution.

Evidence conceptually includes:

```text
scenario identity
expected evidence/cases
observed evidence/cases
terminal result
first causal failure when applicable
cleanup result
restore result when applicable
```

The execution owner must be able to detect missing evidence.

Terminal semantics distinguish conceptually:

- **PASS:** contract proved.
- **FAIL:** contract was exercised correctly and violated.
- **BLOCKED:** environment, precondition or observability prevented the contract from being exercised correctly.

Secondary failures must not replace the first causal divergence.

## 13. Suite and Full QA

A Suite selects independently valid Scenarios.

It does not reimplement environment setup, Scenario actions, assertions, cleanup or framework lifecycle.

Full QA is orchestration, not a mega-smoke.

A Scenario that only works when executed through Full QA is incorrectly isolated.

Scenarios may share a Play Mode boot only when their baselines are independently verifiable, each restores its owned state, no Scenario relies on residue, and one failure does not invalidate another's evidence.

Performance does not justify hidden lifecycle coupling.

## 14. Greenfield strategy

The legacy QA implementation has already been removed.

The new project proceeds as greenfield:

```text
1. Preserve the architectural decisions and historical audit as documentation.
2. Implement the minimum execution infrastructure required by one vertical slice.
3. Implement one representative runtime Scenario.
4. Validate it manually in Unity.
5. Implement a second deliberately difficult Scenario.
6. Reassess the common architecture.
7. Only then expand certification coverage systematically.
```

There is no requirement to recreate legacy class names, menus, fixtures, panels, mega-suites, orchestrators, setup architecture, folder structure, asmdefs, or the historical Smoke/Regression distinction.

## 15. Architectural validation

Before broad expansion, the architecture must prove at least two vertical slices.

The first demonstrates:

```text
known environment
→ baseline
→ supported action
→ evidence
→ cleanup
→ baseline restored
→ verdict
```

The second must exercise at least one difficult lifecycle property:

```text
controlled async
multi-phase execution
fresh boot requirement
adversarial timing
ownership conflict
runtime interruption/recovery
```

If the second Scenario requires a separate execution architecture, the common model is not yet proven.

## 16. Deliberately unresolved implementation decisions

This ADR intentionally does not decide:

- concrete class names;
- whether Scenario is an interface, base class, component or another representation;
- whether Environment is represented by a type;
- result representation;
- directory structure;
- asmdef topology;
- Scenario discovery/registration;
- menu organization;
- persistent result format;
- exact timeout values;
- exact boot grouping algorithm;
- number of canonical environments.

These decisions require evidence from the first vertical slices.

## 17. Rejected alternatives

### Use FIRSTGAME as QA runtime environment

Rejected because FIRSTGAME represents integrated consumer happy path while QAFramework intentionally exercises controlled boundary, adversarial and negative conditions.

### Create a reference game inside QAFramework

Rejected because it duplicates FIRSTGAME and encourages application-level coupling inside certification.

### Mock framework runtime in QAFramework

Rejected because it proves the mock rather than the materialized framework lifecycle. Fake-based proofs belong at Package/NUnit level when appropriate.

### One specialized harness per domain

Rejected because Edit/Play transitions, recovery, evidence, timeout, cleanup and restore are execution infrastructure rather than Player-, Camera- or GameFlow-specific behavior.

### Recreate the legacy architecture

Rejected because the previous project demonstrated incompatible execution models and accidental coupling. Historical coverage informs new Scenarios; historical architecture does not constrain them.

## 18. Governing principle

```text
Package tests prove components.

QAFramework certifies framework contracts
under controlled runtime conditions.

FIRSTGAME proves the supported happy path
of a real consumer.
```

For QAFramework specifically:

> **QA proves the real framework from a known valid baseline, deliberately exercises the condition under test through supported surfaces, collects explicit evidence, and returns ownership to a known state.**

QA does not create a parallel runtime.

QA does not depend on accidental residue.

QA does not repair its own environment silently.

QA does not preserve legacy architecture merely because historical coverage remains valuable.
