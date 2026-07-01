# F35-ADR-Extension-Surface-Model

Status: Accepted / Doc-only
Last updated: 2026-07-01
Supersedes: none
Superseded by: none

## Context

The framework already has useful extension candidates: Loading, Transition, Pause, `UIGlobal`, RuntimeContent, ContentAnchor, `ContentAnchorMaterializationService`, Unity materialization bridges, Pause/InputMode bridges, participant execution and QA smoke runners.

Those pieces are not enough to authorize broad adapter or Surface expansion. Without shared language, future work can repeat the known failure modes: thick `MonoBehaviour` bridges, orchestration in authoring components, status laundering, silent fallback, QA smokes treated as product consumers and public APIs created before a real consumer proves the contract.

## Decision

Accept the Extension Surface Model as framework architecture language.

This ADR defines ownership vocabulary only. It does not authorize new adapters, new surfaces, public API expansion, serialized field changes, runtime changes or package splits. Broad adapter/surface expansion remains gated by `SURFACE-AUDIT-1`, `INPUT-APPLY-1`, `STATUS-1`, `SURFACE-PILOT-1` and `PAUSEVIS-1`.

## Archetypes

| Archetype | Responsibility | May do | Must not do | Current examples | Create new when | Reuse existing when |
| --- | --- | --- | --- | --- | --- | --- |
| Surface | Runtime capability boundary exposed to consumers through stable intent, readiness and diagnostics. | Define capability language, availability/readiness contract, result semantics and consumer expectations. | Mean "any visual component"; execute Unity side effects directly; own lifecycle policy for unrelated domains. | Loading, Transition, Pause runtime surface. | A stable capability has at least one real consumer and clear failure/readiness semantics. | Existing Loading/Transition/Pause semantics already express the capability. |
| Adapter | Concrete integration that executes one local side effect and returns local evidence. | Apply Unity or external subsystem changes, report native result/evidence and expose local failure details. | Orchestrate multi-stage framework operations; decide lifecycle policy; hide failure behind fallback. | `UnityLoadingSurfaceAdapter`, `UnityFadeCurtainEffectAdapter`, `UnityPauseResidentSurfaceAdapter`, Unity ContentAnchor placement/materialization adapters. | A new external subsystem or Unity side effect needs explicit evidence. | Existing adapter already owns the same side effect and evidence shape. |
| Bridge | Unity-authored wrapper between Inspector data and an explicit runtime boundary. | Read serialized references, validate authoring locally, call one boundary and expose diagnostics. | Own multi-stage orchestration, service discovery, rollback, lifecycle sequencing or status laundering. | `UnityContentAnchorMaterializationBridge`, `UnityContentAnchorMaterializationBridgeSet`, `PauseInputModeUnityPlayerInputRuntimeBridge`, `PauseInputActionRuntimeBridgeTrigger`. | Inspector-authored setup must invoke an existing service/surface/apply boundary. | Existing authoring wrapper can delegate to the same boundary without new serialized fields. |
| Operation Service | Non-MonoBehaviour runtime boundary for a multi-step operation. | Orchestrate sequence, dependencies, rollback, failed stage and original evidence preservation. | Read Inspector fields, own scene objects, act as service locator or absorb unrelated domain policy. | `ContentAnchorMaterializationService`, participant executor mechanics. | At least one operation has repeated multi-stage sequencing or rollback that must be reusable outside a bridge. | A current service already owns the full sequence and result contract. |
| Consumer | Runtime, product, QA or authoring path that requests a capability or service. | Declare dependency, request intent, handle unavailable capability and handle explicit failure. | Fabricate missing identity, compare identities across domains or use silent fallback for required config. | Route/Activity consumers of Loading/Transition, RuntimeContent/ContentAnchor callers, QA Canvas as QA-only consumer. | A real workflow depends on a capability and can define failure handling. | Existing consumer can request the capability through the accepted boundary. |
| Validator / Evidence | Boundary that checks readiness, inputs or results and reports structured evidence. | Validate preconditions, severity, blocking/non-blocking issues and original failure references. | Apply side effects or collapse domain results into a universal status enum. | Common validation helpers, authoring validation, materialization evidence checks, loading/readiness diagnostics. | A repeated validation shape has domain-neutral mechanics or a domain-owned evidence contract. | Existing validation/evidence contract already preserves the original cause. |
| QA Smoke Runner | Validation entry point that proves a bounded contract. | Set up a scenario, execute expected path and report observable diagnostics. | Become product runtime policy, create fallback behavior or stand in for real consumer readiness. | QA Canvas and package smoke runners. | A boundary needs repeatable evidence before acceptance. | Existing smoke already proves the same contract and diagnostic expectations. |
| Runtime Surface Host | Runtime location that makes shared surfaces available to consumers. | Host app/session scoped surface adapters, report missing required surfaces and expose explicit no-op behavior when configured as optional. | Become a universal manager, own route/activity content, hide required surface absence or decide unrelated lifecycle policy. | `UIGlobal`, `GlobalUiSceneRuntime`. | A scope needs to host multiple shared surfaces with clear availability policy. | `UIGlobal` already hosts the app/session scoped presentation capability. |

## Anti-patterns

| Anti-pattern | Decision |
| --- | --- |
| Thick `MonoBehaviour` bridge | Rejected. Bridge reads Inspector, validates authoring and delegates to an explicit boundary. |
| Adapter orchestration | Rejected. Adapter executes a local side effect and returns evidence; it does not own multi-stage framework flow. |
| Silent fallback for required capability | Rejected. Required config must fail visibly through diagnostics. |
| Status laundering | Rejected. Aggregate status must preserve original subsystem result or failed stage. |
| Universal result/status enum | Rejected. Mapping policy may be shared, but domain status ownership remains local. |
| QA smoke as product consumer | Rejected. A smoke proves a contract; it does not prove product readiness by itself. |
| Public API before consumer proof | Rejected. Surface expansion requires a real consumer and a pilot gate. |

## Required ownership rules

- A `MonoBehaviour` bridge must not orchestrate a multi-stage operation.
- An Adapter executes a specific side effect and returns local evidence.
- An Operation Service owns sequence, rollback, failed stage and evidence preservation.
- A Surface is a stable runtime capability boundary, not a synonym for any visual component.
- A Consumer must explicitly handle absent or failed capability without silent fallback.
- A Validator/Evidence boundary must not apply side effects.
- A QA Smoke Runner proves a contract but must not become product policy.
- Status/result mapping must preserve original cause and avoid a universal enum.
- A broad Surface layer is not ready yet; this ADR only defines language and ownership.

## Current implementation coverage

The model can classify current Loading, Transition, Pause, `UIGlobal`, RuntimeContent, ContentAnchor, materialization service, Unity bridges, Pause/InputMode bridges, participant execution and QA smoke runners.

The model has not yet been applied as a full inventory. `SURFACE-AUDIT-1` owns that next step.

## Pending decisions

- Which existing surface/adapter path should be selected for `SURFACE-PILOT-1`.
- Whether Pause/InputMode apply becomes the first bridge-to-service pilot.
- Which status/result mappings require policy in `STATUS-1`.
- Whether Pause visual materialization remains experimental or becomes consumer-ready in `PAUSEVIS-1`.
