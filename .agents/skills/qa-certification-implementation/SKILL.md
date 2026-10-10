---
name: qa-certification-implementation
description: Use when an approved QAFramework Scenario design exists and the task explicitly requests implementation of one certification vertical slice.
---

# QA Certification Implementation

## Entry gate

Require an approved Scenario design and an explicit implementation request. Read [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), then reinspect the current QAFramework state, active Framework package resolution, and relevant public authoring/lifecycle/ownership/observability surfaces. If reality invalidates a premise, stop at that boundary and report it for redesign or gap classification.

## Authoring gates

Read the [Unity QA authoring reference](references/unity-qa-authoring.md) and [vertical-slice checklist](references/vertical-slice-checklist.md) when preparing Unity composition.

1. **Preflight:** verify active package/source; dependency types and assemblies; exact paths/GUIDs where relevant; typed assets, required object references, their composition roles and semantic validity; scene/camera prerequisites; collisions/residuals; and ownership/cleanup boundaries. Complete this before generating outputs.
2. **Construction:** materialize in dependency order. Keep stable paths across phases and reacquire Unity references after operations documented or demonstrated to invalidate wrappers. Do not reload mechanically after every AssetDatabase operation. Use supported authoring and public Framework APIs.
3. **Postflight:** persist, independently reload final assets/scenes/prefabs by canonical path, and validate their serialized links, cardinality, semantic configuration, scene bindings, and contract-specific baseline. Setup success follows this verification.
4. **Cleanup:** inventory exact QA-owned paths and environment mutations; handle partial preparation and reruns; delete or restore only owned state; preserve user/authored content; and report residuals. Never publish `PASS` before required runtime cleanup/restoration evidence.

## Ownership and verdicts

Implement one complete slice inside QAFramework. The state creator owns its release. Preserve the first causal divergence and keep execution, unwind, and cleanup outcomes separate. Use `PASS`, `FAIL`, and `BLOCKED` with the ADR-001 semantics; a setup/postflight result is not a runtime certification verdict.

Do not change Framework contracts, use reflection/internal hosts/private state/fallback, or create generic Runner, Scenario, Environment, registry, Suite, menu, or shared setup infrastructure from one slice. Report framework defects separately; repair them only with separate authorization.

## Completion

Perform allowed static checks only. Provide exact manual Unity steps and name compile/import/runtime/Player validation that was not performed. Never claim Unity validation without user-supplied execution evidence.
