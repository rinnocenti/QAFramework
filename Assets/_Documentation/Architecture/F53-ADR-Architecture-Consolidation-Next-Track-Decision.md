# F53-ADR-Architecture-Consolidation-Next-Track-Decision

Status: Accepted / ADR-only  
Date: 2026-07-02  
Track: Architecture Consolidation / Next Track Selection  
Depends on: F34, F35, F36, F39, F40, F41, F43, F52

## Context

GameFlow F47-F52 closed the current request-envelope axis without creating a public/internal GameFlow request API. `FrameworkRuntimeHost` remains the supported Route/Activity request boundary, `GameFlowRuntime` remains internal execution/admission runtime, and `GameFlowRequestEnvelope` remains passive internal diagnostics.

The next architecture consolidation track should improve practical first-game usability without reopening deferred kernels. The framework already has startup, Route/Activity requests, lifecycle evidence, Loading reference hardening, GameFlow envelope projection and FlowTrigger trigger diagnostics. The most visible remaining usability gap for a real game flow is the reliability and diagnosability of visual transitions around Route and Activity operations.

## Decision

Choose Option A: Transition Surface / Effects Hardening.

F53 selects Transition and TransitionEffects as the next productive architecture axis. The next cut should define the bounded Transition surface/effects contract before runtime hardening. It must use Loading as a reference checklist for contract/evidence questions, but it must not copy Loading status names or create a broad Surface layer.

## GameFlow Closure Summary

- F47 accepted GameFlow request envelope as a request/diagnostics boundary.
- F48 created the passive internal request envelope shell and Route/Activity log projection.
- F49 kept envelope creation/projection in `FrameworkRuntimeHost`.
- F50 approved Route/Activity trigger adoption of the shared FlowTrigger helper.
- F51 implemented that trigger-local diagnostic migration and was owner-validated.
- F52 kept `FrameworkRuntimeHost` as the current Route/Activity request API boundary.

GameFlow should stay inactive until a real API consumer, repeated caller duplication, or factual orchestration bug proves the need to reopen it.

## Candidate Tracks

| Option | Candidate | Decision |
| --- | --- | --- |
| A | Transition Surface / Effects Hardening | Selected |
| B | RuntimeContent split | Deferred |
| C | ContentAnchor materialization productization | Deferred |
| D | Pause visual promotion | Rejected for now / frozen |
| E | Save/progression readiness | Too early |
| F | GlobalUi consolidation | Deferred unless driven by a surface contract |

## Selected Track

Transition is already in the active request path:

- Route and Activity operations execute before/after transition requests.
- Request logs project `transition*` and `transitionEffect*` diagnostics.
- `UIGlobal` already hosts Transition effect adapters alongside Loading and Pause adapters.
- Transition effects already expose adapter-oriented result/status/evidence concepts, including blocking issues.

This makes Transition the fastest practical axis for improving first-game usability: it is visible to players, touched by menu-to-game and area-change flows, and already connected to Route/Activity evidence without requiring a new public GameFlow API.

## Rejected Or Deferred Tracks

RuntimeContent split remains deferred because the MAT core is already extracted and validated, and splitting ownership now would be mostly technical cleanup rather than immediate first-game usability.

ContentAnchor materialization productization remains deferred because it is useful after a concrete visual/content consumer proves the required authoring and runtime evidence.

Pause visual promotion remains frozen by F41. Resident Pause is the supported path; RuntimeContent/ContentAnchor Pause visual materialization needs a real consumer and stronger result-returning adapter evidence before promotion.

Save/progression readiness is too early because the framework still needs stronger navigation and visual transition usability before adding persistence semantics.

GlobalUi consolidation is deferred because `UIGlobal` is a surface host, not a universal manager. Any cleanup should be driven by a bounded surface contract such as Transition, not by a broad host refactor.

## Why This Helps First Practical Game

A real first game needs Route/Activity movement to feel intentional and diagnosable. Transition hardening directly affects:

- startup route presentation;
- menu-to-game flow;
- activity enter/exit and clear operations;
- loading/transition separation;
- missing or failed visual adapter diagnosis.

This is more immediately useful than reopening GameFlow API design or promoting experimental Pause visual materialization.

## Next Track Scope

The next gate is `F54 - TRANSITION-1 - Transition Surface / Effects Contract`.

F54 should be contract-first and may:

- inventory the current Transition runtime/effects path;
- define Transition surface, effect adapter, request/result and evidence language;
- state how Transition evidence relates to Route/Activity logs, Loading diagnostics and `UIGlobal`;
- identify required/optional/no-op behavior and blocking issue semantics;
- define the minimum runtime hardening cut after the contract is accepted.

## Forbidden Scope

F53 does not authorize:

- runtime C# changes;
- scene, prefab, serialized asset or ProjectSettings changes;
- package, asmdef or csproj changes;
- QA Canvas changes;
- GameFlow API reopening;
- deeper envelope ownership movement;
- content dispatch kernel, readiness kernel or full lifecycle orchestration kernel;
- Pause visual/materialization promotion;
- broad Surface layer or universal adapter result type.

## Validation Plan

F53 is decision-only. No Unity compile, import, PlayMode, smoke or batchmode validation is required for this cut.

Manual validation for F53:

1. Confirm this ADR selects Transition Surface / Effects Hardening.
2. Confirm F34 points to F54 as the next ordered gate.
3. Confirm package docs say GameFlow API remains deferred and Transition is the next hardening track.
4. Confirm no runtime, scene, prefab, serialized asset, package, asmdef or project files changed.

## Next Gate

`F54 - TRANSITION-1 - Transition Surface / Effects Contract`
