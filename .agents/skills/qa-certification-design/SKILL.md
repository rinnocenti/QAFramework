---
name: qa-certification-design
description: Use when deciding whether an Immersive Framework contract belongs in Package/NUnit, QAFramework, or FIRSTGAME, or when designing a QAFramework Scenario.
---

# QA Certification Design

## Governing rule

Repository reality precedes QA design. Read the complete [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), inspect the current QAFramework state, and verify the relevant public `com.immersive.framework` contract. The ADR and this skill define constraints; neither proves a current endpoint, dependency, or composition.

## Workflow

1. Classify the verification level: Package/NUnit for deterministic contracts without material runtime; QAFramework for controlled real-runtime certification; FIRSTGAME for an integrated consumer happy path.
2. Compare relevant existing QAs and authoring patterns. Use the shared [Unity QA authoring reference](../qa-certification-implementation/references/unity-qa-authoring.md) when scenes, assets, prefabs, or serialized composition are involved.
3. Produce the evidence-backed [Scenario design brief](references/scenario-contract.md). Verify preexisting dependencies against repository reality. Planned resources may be generated later if their type, role, dependencies, supported creation surface, owner/lifetime, materialization order, and post-persistence validation criteria are known.
4. Mark `READY FOR IMPLEMENTATION` only when a supported, demonstrable path exists to materialize and observe the required composition. Otherwise report the concrete gap and use `BLOCKED`.
5. Stop before implementation.

## Boundaries

Use only supported public actions and authoring. Do not solve gaps with reflection, internal hosts, service-locator bypass, opportunistic global lookup, private-state mutation, fallback, or test-only framework backdoors. Do not settle ADR-001's intentionally open Runner, Scenario, Environment, result, registry, folder, timeout, or boot-grouping decisions from a single slice.
