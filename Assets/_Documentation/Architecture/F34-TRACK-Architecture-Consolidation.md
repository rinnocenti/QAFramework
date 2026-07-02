# F34-TRACK-Architecture-Consolidation

Status: Active / Mutable  
Last updated: 2026-07-01  
ADR: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`  
Plan: `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`

This is the only mutable architecture consolidation tracker. It owns current status, drift, executed cuts, pending gates and validation notes.

## Current board

| Track | Real status | Executed cuts | Real coverage | Pending work | Next action | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| COMMON - Internal Mechanics | Closed | COMMON-B, COMMON-C, COMMON-D, COMMON-E | `FrameworkEnumValidation`, `FrameworkCollectionCopy` and `FrameworkIssueCounting` accepted as bounded internal mechanics. | Result/status container and Snapshot migration remain outside the closed track. | Reopen only with a new concrete repeated mechanic. | Static/doc review unless future runtime call sites change. |
| CONS - Participant Consolidation | Closed pilot | CONS-A, CONS-B, CONS-C, CONS-D, CONS-F | Participant primitives and selected pilots closed. | No broad expansion to Snapshot, ActivityContentExecution, LocalContribution or flow triggers. | None for pilot; future expansion needs a new gate. | Existing participant/reset smokes remain reference validation. |
| LIFECYCLE - Route/Activity Operation Kernel | Partial | LIFECYCLE-C, C1, D, E, F | Scope-tail release/cleanup mechanics closed. | Broader operation kernel: orchestration, content dispatch, readiness, ledger/progress and evidence policy. | `LIFECYCLE-KERNEL-REMAINING` | Unity validation only when runtime work resumes. |
| MAT - RuntimeContent/ContentAnchor Materialization | Closed / core service extracted and validated | MAT-1, MAT-2, MAT-3, MAT-4 | Physical/logical release helpers, binding cleanup, `ContentAnchorMaterializationService`, stage-oriented result and rollback are covered and owner-validated. | `RuntimeContentRuntime` split remains deferred. Broad materialization consumers depend on adapter/surface readiness gates. | None for MAT core. | MAT-4 owner validation passed; rerun materialization/release smokes only if future runtime work touches this path. |
| PAUSE cleanup | Closed | PAUSE-1, PAUSE-2 | Legacy QA warning cleanup and documentation alignment closed. | Does not close InputMode apply boundary. | None for cleanup. | Pause/InputMode smokes only if future runtime changes touch it. |
| INPUT - Pause/InputMode Apply Boundary | Closed / owner-validated | INPUT-APPLY-1, INPUT-APPLY-2 | `F37-ADR-Pause-InputMode-Apply-Boundary.md` accepts the contract. `PauseInputModeApplyService` owns the runtime apply sequence; `PauseInputModeUnityPlayerInputRuntimeBridge` delegates to it; `InputModeUnityPlayerInputAdapter` remains the local `PlayerInput` side-effect executor. | `FLOWTRIGGER`, `LIFECYCLE-KERNEL-REMAINING` and `GAMEFLOW` remain pending. | None for INPUT apply boundary. | F38 owner Unity compile/import and required Pause/InputMode smokes reported validated. Rerun only if future runtime work touches this path. |
| GAMEFLOW - Lifecycle Request Envelope | Pending | None | No accepted envelope. | Define envelope after lifecycle and input boundaries stabilize. | `GAMEFLOW` | Lifecycle/request smokes after implementation. |
| STATUS - Mapping Policy | Accepted / doc-policy | STATUS-1 | `F39-ADR-Status-Mapping-Policy.md` accepts domain-first failure/status mapping policy. | Apply policy during future runtime gates without creating a universal enum or generic result type. | `FLOWTRIGGER` | Doc-policy only; domain-specific smokes after future implementation changes. |
| FLOWTRIGGER - Request/Trigger/State | Closed / helper extracted and owner-validated | FLOWTRIGGER-1 | `F42-ADR-FlowTrigger-Request-State-Helper.md` accepts a neutral non-MonoBehaviour helper under `Runtime/Common/FlowTriggers`. `PauseRequestTrigger` and `PauseInputActionRuntimeBridgeTrigger` use it for local state/diagnostics. Route/Activity triggers were inspected but left untouched because they live under `Runtime/GameFlow`, which F42 did not allow changing. | `GAMEFLOW` and `LIFECYCLE-KERNEL-REMAINING` remain pending. Route/Activity trigger migration remains deferred to a future GameFlow-scoped cut. | None for F42. | Owner validation passed: Pause InputAction Runtime Bridge Trigger Smoke, Pause Runtime PlayerInput Bridge Smoke, Pause Logical Toggle Resident Surface Smoke, Cycle Reset Trigger Smoke and Object Reset Trigger Smoke. |
| PAUSEVIS - Consumer Readiness | Accepted / resident-only | PAUSEVIS-1 | `F41-ADR-Pause-Visual-Consumer-Readiness.md` accepts resident Pause surface as the current supported presentation path and keeps Pause visual/materialization experimental/frozen. | Future Pause visual promotion requires a real product/runtime consumer and stronger result/evidence contract. | `FLOWTRIGGER` | Doc-only; no runtime/editor/package/scene/prefab changes. |

## Adapter and Surface readiness board

| Area | Real status | Evidence | Coverage | Pending work | Next gate | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| Extension Surface Model | Applied / doc-only | `F35-ADR-Extension-Surface-Model.md` accepts the archetype language. | Surface, Adapter, Bridge, Operation Service, Consumer, Validator/Evidence, QA Smoke Runner and Runtime Surface Host are defined. | Keep model as classification language only. | `FLOWTRIGGER` | Doc-only; no runtime validation required. |
| Surface/Adapter Inventory | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies current candidates and blockers. | Loading, Transition, Pause, RuntimeContent, ContentAnchor, GlobalUi, participant/reset paths, lifecycle paths, flow triggers and QA runners are inventoried. | Use the inventory to drive finite gates; do not mark broad readiness. | `FLOWTRIGGER` | Doc/static review only. |
| Adapter readiness | Partial - Loading reference hardened; broad expansion still gated | `F36-AUDIT-Surface-Adapter-Inventory.md`, `F39-ADR-Status-Mapping-Policy.md`, `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md`, `F41-ADR-Pause-Visual-Consumer-Readiness.md` and `F43-ADR-Loading-Runtime-Reference-Hardening.md` identify concrete adapter candidates, accept Loading as bounded contract pilot, harden Loading aggregate adapter evidence and keep Pause visual frozen. | Loading now preserves named per-adapter evidence in aggregate results. Broad adapter expansion remains unapproved. Pause resident adapter remains usable but partial because result-returning evidence is weak. | Later domain-specific pilots; no Pause visual expansion without real consumer evidence. | `LIFECYCLE-KERNEL-REMAINING` | Static validation for F43; Unity compile/import and Loading Result Smoke after owner import. |
| Surface readiness | Partial - Loading reference hardened; broad layer still gated | `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` accepts Loading as the first Surface/Adapter Contract Pattern. `F43-ADR-Loading-Runtime-Reference-Hardening.md` adds domain-specific aggregate adapter evidence without creating a broad Surface layer. `F41-ADR-Pause-Visual-Consumer-Readiness.md` accepts resident-only Pause presentation and freezes visual/materialization. | Existing surfaces can be classified and Loading can guide future contracts, but no broad Surface layer is accepted. | Later flow/lifecycle gates; Pause visual requires a future real-consumer evidence cut before promotion. | `LIFECYCLE-KERNEL-REMAINING` | Static validation for F43; Unity compile/import and Loading Result Smoke after owner import. |
| Current module classification | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies modules as Ready, Partial, Candidate, Experimental or Blocked. F40 upgrades Loading from candidate to accepted pilot reference; F41 keeps Pause resident-only and freezes visual/materialization. | Current implementation inventory is mapped to accepted archetypes and mapping hot spots are classified in F39/F40/F41. | Keep classifications current as gates execute. | `FLOWTRIGGER` | Static/doc review. |
| Pause/InputMode apply boundary | Closed / owner-validated | `PauseInputModeApplyService`, `PauseInputModeApplyRequest`, `PauseInputModeApplyResult` and `PauseInputModeApplyStage` implement the F37 boundary. | Non-MonoBehaviour service owns preflight, Pause request submission, PlayerInput adapter call, failed stage and aggregate result; bridge keeps serialized fields and observable API. | None for INPUT apply boundary unless future runtime changes touch it. | `FLOWTRIGGER` | F38 owner Unity compile/import and required Pause/InputMode smokes reported validated. |
| Adapter/surface failure mapping | Accepted / doc-policy | `F39-ADR-Status-Mapping-Policy.md` accepts domain-first mapping rules and rejects universal enums/results. | Aggregate results must preserve original cause, boundary-local failed stage, adapter result when present, blocking issues and explicit optional/no-op evidence. | Apply policy during future implementation patches. | `FLOWTRIGGER` | Doc-policy only. |
| Surface adapter contract pilot | Accepted / runtime reference hardened | `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` accepts Loading as the bounded pilot. `F43-ADR-Loading-Runtime-Reference-Hardening.md` adds `LoadingSurfaceAdapterEvidence` to aggregate Loading surface results. | Contract Pattern is accepted using Loading contracts, runtime, adapter, observation path, real internal consumers and diagnostics evidence. Aggregate Loading results now preserve named adapter evidence without parsing issue text. | Do not promote broad Surface layer; use the pattern only as a checklist for future domains. | `LIFECYCLE-KERNEL-REMAINING` | Static validation for F43; Unity compile/import and Loading Result Smoke after owner import. |
| Pause visual consumer readiness | Accepted / resident-only | `F41-ADR-Pause-Visual-Consumer-Readiness.md` chooses Keep Resident Only. | Resident Pause surface is the current supported presentation path; Pause visual/materialization remains experimental/frozen. | A future Pause visual cut must first identify a real product/runtime consumer and result/evidence requirements. | `FLOWTRIGGER` | Doc-only; no Unity validation performed. |

## Module classification snapshot

| Module or area | Readiness | Candidate archetypes |
| --- | --- | --- |
| Common internal mechanics | Ready | Validator/Evidence support |
| Participant primitives, CycleReset, ObjectReset | Ready | Consumer, Validator/Evidence |
| RuntimeContent | Partial | Operation Service, Validator/Evidence, Consumer |
| ContentAnchor | Partial | Adapter, Bridge, Operation Service, Validator/Evidence |
| Loading | Pilot accepted / runtime reference hardened | Surface, Adapter, Validator/Evidence, Consumer |
| Transition and TransitionEffects | Candidate | Surface, Adapter, Operation Service, Consumer |
| Pause runtime surface | Partial / resident-only accepted | Surface, Adapter, Bridge, Consumer |
| Pause visual materialization | Experimental / freeze | Surface candidate, Adapter candidate, Consumer, QA Smoke Runner |
| InputMode and UnityInput bridge path | Partial - reference boundary validated; no Surface pilot | Bridge, Operation Service, Adapter |
| RouteLifecycle, ActivityFlow, SceneLifecycle | Partial | Operation Service, Consumer, Validator/Evidence |
| GameFlow and ApplicationLifecycle | Blocked | Consumer, Operation Service |
| GlobalUi | Candidate | Consumer, Surface host, Adapter host |
| Flow triggers | Closed / helper extracted and owner-validated | Bridge, Consumer, Validator/Evidence |
| Status mapping across Loading/InputMode/Pause/materialization | Partial - policy accepted; implementation deferred to pilots | Validator/Evidence |

## Ordered next gates

1. `LIFECYCLE-KERNEL-REMAINING`
2. `GAMEFLOW`

## Active navigation policy

- Active architecture decisions: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- Active extension surface model: `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- Active surface/adapter inventory: `Assets/_Documentation/Architecture/F36-AUDIT-Surface-Adapter-Inventory.md`
- Active Pause/InputMode apply boundary contract: `Assets/_Documentation/Architecture/F37-ADR-Pause-InputMode-Apply-Boundary.md`
- Active adapter/surface failure mapping policy: `Assets/_Documentation/Architecture/F39-ADR-Status-Mapping-Policy.md`
- Active Loading Surface/Adapter contract pattern: `Assets/_Documentation/Architecture/F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md`
- Active Pause visual consumer readiness decision: `Assets/_Documentation/Architecture/F41-ADR-Pause-Visual-Consumer-Readiness.md`
- Active FlowTrigger helper decision: `Assets/_Documentation/Architecture/F42-ADR-FlowTrigger-Request-State-Helper.md`
- Active Loading runtime reference hardening: `Assets/_Documentation/Architecture/F43-ADR-Loading-Runtime-Reference-Hardening.md`
- Active immutable plan: `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- Active mutable tracker: `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

Old audits, plans, closeouts, prompts and notes remain historical source material unless explicitly removed in a later cleanup. They are not active navigation.

## Manual decisions needed

- Whether to delete or archive older historical placeholder-named files outside active architecture navigation.
- Whether a future GameFlow-scoped trigger cut should migrate `RouteRequestTrigger` and `ActivityRequestTrigger`.
- Whether a future Pause visual evidence cut should require result-returning adapter evidence before any visual/materialization promotion.
