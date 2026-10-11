---
name: qa-certification-implementation
description: Use when an approved QAFramework Scenario design exists and the task explicitly requests implementation of one certification vertical slice.
---

# QA Certification Implementation

## Entry gate

This skill applies to implementation owned by QAFramework. Resolve the owner before local preflight via immersive-ecosystem; use QAFramework's effective package resolution for this certification, independent of a consumer's lockfile.

Require an approved Scenario design and an explicit implementation request. Read [ADR-001](../../../Documentation~/ADR-001-QA-Framework-Certification-Architecture.md), then reinspect the current QAFramework state, active Framework package resolution, and relevant public authoring/lifecycle/ownership/observability surfaces. If reality invalidates a premise, stop at that boundary and report it for redesign or gap classification.

## Authoring gates

Read the [Unity QA authoring reference](references/unity-qa-authoring.md) and [vertical-slice checklist](references/vertical-slice-checklist.md) when preparing Unity composition.

1. **Preflight:** verify active package/source; dependency types and assemblies; exact paths/GUIDs where relevant; typed assets, required object references, their composition roles and semantic validity; scene/camera prerequisites; collisions/residuals; and ownership/cleanup boundaries. Complete this before generating outputs.
2. **Construction:** materialize in dependency order. Keep stable paths across phases and reacquire Unity references after operations documented or demonstrated to invalidate wrappers. Do not reload mechanically after every AssetDatabase operation. Use supported authoring and public Framework APIs.
3. **Postflight:** persist, independently reload final assets/scenes/prefabs by canonical path, and validate their serialized links, cardinality, semantic configuration, scene bindings, and contract-specific baseline. Compare the postflight checks with the actual Framework authoring/bootstrap validators invoked for the target. If the same criteria cannot be exercised through a supported API, report the narrower checks by name and leave Inspector validation pending; do not label a scoped baseline as `GameApplication Valid`. Setup success follows this verification.
4. **Cleanup:** inventory exact QA-owned paths and environment mutations; handle partial preparation and reruns; delete or restore only owned state; preserve user/authored content; and report residuals. Never publish `PASS` before required runtime cleanup/restoration evidence.

## Compiler and contract checks

Before writing a Scenario or Editor setup, verify namespaces, signatures, overloads, Editor API names, and assembly references against the active package source and Unity version. Check each referenced enum member and each result object's public convenience properties separately: a successful result may expose `Succeeded` while its status enum uses a more specific value such as `SucceededLeft`. For nested observation contracts, use the actual evidence flag and nested snapshot member (for example, `HasCurrentActorEvidence` plus `CurrentActor.HasCurrentActor`), not a guessed flattened property. Inspect the QA recorder declaration and a current scenario before calling it; stage logging and terminal recording are distinct APIs. Match each `SerializedProperty` write to its value type and API (`stringValue`, `objectReferenceValue`, or enum APIs); keep helpers typed for `SerializedObject` versus `SerializedProperty`. In C# iterators, capture failures inside `catch` and emit `yield` after the block; C# rejects `yield` inside `catch`. A fragment compiling in another package or Unity version does not prove compatibility.

Treat compiler diagnostics as contract evidence. For each reported identifier, trace the exact source declaration and correct the caller to the owning public contract; do not add aliases or compatibility members to the Framework. Before delivery, statically audit every Framework and QA helper member used by the slice against the resolved source, including terminal recorder calls. A prior Slice 3 compiler pass exposed four recurring traps: enum/result vocabulary conflation, assuming a flattened observation property, definite-assignment cascades from an invalid observation member, and inventing a generic recorder `Record` method. Use those as a regression checklist for new scenarios.

## Ownership and verdicts

Implement one complete slice inside QAFramework. The state creator owns its release. Preserve the first causal divergence and keep execution, unwind, and cleanup outcomes separate. Use `PASS`, `FAIL`, and `BLOCKED` with the ADR-001 semantics; a setup/postflight result is not a runtime certification verdict.

Do not change Framework contracts, use reflection/internal hosts/private state/fallback, or create generic Runner, Scenario, Environment, registry, Suite, menu, or shared setup infrastructure from one slice. Report framework defects separately; repair them only with separate authorization.

## Completion

Perform allowed static checks only. Provide exact manual Unity steps and name compile/import/runtime/Player validation that was not performed. Never claim Unity validation without user-supplied execution evidence.
