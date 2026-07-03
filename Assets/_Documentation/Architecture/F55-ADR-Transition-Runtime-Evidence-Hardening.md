# F55-ADR-Transition-Runtime-Evidence-Hardening

Status: Implemented locally / pending Unity validation  
Date: 2026-07-02  
Track: TRANSITION-2 / Transition Runtime Evidence Hardening  
Depends on: F39, F43, F54

## Context

F54 accepted the Transition Surface / Effects Contract and identified the main runtime gap: Transition had adapter count, effect status, blocking issue count and issue text, but did not preserve named aggregate adapter evidence like Loading does with `LoadingSurfaceAdapterEvidence`.

The goal of F55 is to harden Transition-specific evidence without changing Route/Activity behavior, GameFlow API, Loading, Pause, FlowTriggers, visual timing, fade duration, before/after order or scene/content lifecycle ownership.

## Decision

Add Transition-specific named adapter evidence and project it in existing Route/Activity logs.

F55 adds internal `TransitionEffectAdapterEvidence` as a domain-specific evidence value for Transition Effect adapter execution. `TransitionResult` now preserves a defensive collection of that evidence inside the runtime assembly, and `FrameworkTransitionDiagnostics` aggregates before/after evidence for request-level logs.

## Runtime Changes

Created:

- `Runtime/TransitionEffects/TransitionEffectResult.cs` (`TransitionEffectAdapterEvidence`)

Changed:

- `TransitionResult` now carries internal `EffectAdapterEvidence` plus aggregate counts for applied, skipped, failed and blocking issue evidence.
- `TransitionEffectOrchestrator` records evidence for each called matching adapter.
- Required missing Transition surface/adapter paths now produce explicit missing-adapter evidence.
- `FrameworkTransitionDiagnostics` aggregates before/after adapter evidence counts, names and statuses.
- `FrameworkRuntimeHost` Route/Activity logs now include additive `transitionEffectAdapterEvidence*` fields.

## New Evidence Fields

`TransitionEffectAdapterEvidence` preserves:

- adapter name;
- Transition Effect status;
- issue count;
- blocking issue count;
- short diagnostic message;
- applied/skipped/failed projections.

Route/Activity logs now add:

- `transitionEffectAdapterEvidenceCount`
- `transitionEffectAdapterEvidenceApplied`
- `transitionEffectAdapterEvidenceSkipped`
- `transitionEffectAdapterEvidenceFailed`
- `transitionEffectAdapterEvidenceBlockingIssues`
- `transitionEffectAdapterEvidenceNames`
- `transitionEffectAdapterEvidenceStatuses`

## Preserved Fields

F55 preserves existing Route/Activity log fields:

- `transition`
- `transitionScope`
- `transitionBefore`
- `transitionAfter`
- `transitionBlockingIssues`
- `transitionVisual`
- `transitionEffect`
- `transitionEffectBefore`
- `transitionEffectAfter`
- `transitionEffectBlockingIssues`
- `transitionEffectAdapterCount`
- `gameFlowEnvelope*`
- `lifecycleOperation*`
- `loadingAdapterEvidence*`

## Behavior Preservation

F55 does not change:

- visual behavior;
- fade duration;
- async settle behavior;
- before/after order;
- Route/Activity lifecycle behavior;
- GameFlowRuntime behavior;
- Loading behavior;
- Pause behavior;
- FlowTrigger behavior;
- scene, prefab or serialized asset setup.

The same adapters are called in the same order. F55 only records structured evidence from existing adapter results and projects it additively.

## Boundary Rules

This cut does not create:

- broad Surface layer;
- universal adapter result;
- universal status enum;
- public Transition API;
- service locator;
- global manager;
- new smoke runner;
- new QA Canvas button.

The evidence is Transition-specific and uses Transition Effect statuses, not Loading status values.

## Validation

Static validation required:

1. `git diff --check`
2. Confirm old Transition log fields remain present.
3. Confirm new `transitionEffectAdapterEvidence*` fields exist in Route/Activity logs.
4. Confirm no scenes, prefabs, serialized assets, ProjectSettings, package metadata, asmdefs or csproj changed.
5. Confirm GameFlowRuntime, triggers, FlowTriggers, Loading runtime and Pause runtime were not changed.

Unity validation remains pending:

1. Unity import/compile.
2. Standard Smoke.
3. Activity Baseline Smoke.
4. Route Scene Composition Smoke.
5. Route Release Smoke.
6. Composite Lifecycle Release Smoke, if available.

## Next Gate

Pending owner validation for F55. After validation, Transition runtime evidence hardening can be closed unless a real smoke/runtime failure identifies a narrower follow-up.
