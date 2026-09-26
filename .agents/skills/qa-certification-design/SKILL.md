---
name: qa-certification-design
description: Use when deciding whether an Immersive Framework contract belongs in Package/NUnit, QAFramework, or FIRSTGAME, or when designing a QAFramework Scenario.
---

# QA Certification Design

## Governing rule

**Repository reality precedes QA design.** Read the complete [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), then inspect the current workspace before proposing a Scenario. The ADR supplies architectural rules; it does not prove that an endpoint, lifecycle, composition, or capability exists.

Respect the task's access restrictions. Locate the active package source from the current manifest, inspect the relevant QA composition and source, and trace the current public `com.immersive.framework` authoring, lifecycle, ownership, action, and observability surfaces. Cite concrete files and symbols. Do not treat historical QA, generated project files, stale caches, memory, or examples in this skill as current evidence.

## Classify first

- **Package/NUnit:** pure logic, value objects, policies, deterministic transitions, serialization, structural invariants, isolated negative contracts, or fake-based proofs that do not require a materially instantiated runtime.
- **QAFramework:** controlled certification of the real framework runtime through supported public APIs and real authoring/lifecycle.
- **FIRSTGAME:** integrated consumer happy path of a correctly authored game.

Do not keep a deterministic contract in QAFramework for convenience. Do not turn QAFramework into a reference game.

## Design workflow

1. Establish the current repository and package reality.
2. Identify the public contract and its correct verification level.
3. Compare real candidate slices when selection is not predetermined.
4. Design the smallest independently reproducible causal lifecycle.
5. Classify any missing public observability or supported extension point.
6. Stop before implementation.

Use [the Scenario contract](references/scenario-contract.md) for the required design brief.

## Hard boundaries

Never solve a missing surface with reflection, internal-host access, service-locator bypass, opportunistic global lookup, private-state mutation, silent fallback, or a test-only framework backdoor. Do not depend on residual state or Full QA order.

Do not choose a Runner, Scenario base/interface, Environment type, result model, registry, Suite, menu, persistence format, timeout policy, boot grouping, directory layout, or asmdef topology before evidence from representative vertical slices justifies it. QA-NEW-001 is evidence, not a reusable architecture.

If the contract cannot be exercised or observed through supported surfaces, report the first concrete gap and its owner. Return a design verdict of `READY FOR IMPLEMENTATION` only when the complete causal contract is supported; otherwise return `BLOCKED`.
