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
| INPUT - Pause/InputMode Apply Boundary | Closed / owner-validated | INPUT-APPLY-1, INPUT-APPLY-2 | `F37-ADR-Pause-InputMode-Apply-Boundary.md` accepts the contract. `PauseInputModeApplyService` owns the runtime apply sequence; `PauseInputModeUnityPlayerInputRuntimeBridge` delegates to it; `InputModeUnityPlayerInputAdapter` remains the local `PlayerInput` side-effect executor. | `SURFACE-PILOT-1`, `PAUSEVIS-1` and `FLOWTRIGGER` remain pending. | None for INPUT apply boundary. | F38 owner Unity compile/import and required Pause/InputMode smokes reported validated. Rerun only if future runtime work touches this path. |
| GAMEFLOW - Lifecycle Request Envelope | Pending | None | No accepted envelope. | Define envelope after lifecycle and input boundaries stabilize. | `GAMEFLOW` | Lifecycle/request smokes after implementation. |
| STATUS - Mapping Policy | Accepted / doc-policy | STATUS-1 | `F39-ADR-Status-Mapping-Policy.md` accepts domain-first failure/status mapping policy. | Apply policy during future runtime gates without creating a universal enum or generic result type. | `PAUSEVIS-1` | Doc-policy only; domain-specific smokes after future implementation changes. |
| FLOWTRIGGER - Request/Trigger/State | Pending | None | No shared helper. | Confirm repeated trigger/request/state shape after higher-priority flow work. | `FLOWTRIGGER` | Flow trigger smokes when scoped. |
| PAUSEVIS - Consumer Readiness | Pending | None | No readiness boundary accepted. | Define consumer readiness without coupling to pause presentation or apply semantics. | `PAUSEVIS-1` | Doc-only unless runtime pilot is approved. |

## Adapter and Surface readiness board

| Area | Real status | Evidence | Coverage | Pending work | Next gate | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| Extension Surface Model | Applied / doc-only | `F35-ADR-Extension-Surface-Model.md` accepts the archetype language. | Surface, Adapter, Bridge, Operation Service, Consumer, Validator/Evidence, QA Smoke Runner and Runtime Surface Host are defined. | Keep model as classification language only. | `PAUSEVIS-1` | Doc-only; no runtime validation required. |
| Surface/Adapter Inventory | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies current candidates and blockers. | Loading, Transition, Pause, RuntimeContent, ContentAnchor, GlobalUi, participant/reset paths, lifecycle paths, flow triggers and QA runners are inventoried. | Use the inventory to drive finite gates; do not mark broad readiness. | `PAUSEVIS-1` | Doc/static review only. |
| Adapter readiness | Partial - contract pattern accepted; not ready for broad expansion | `F36-AUDIT-Surface-Adapter-Inventory.md`, `F39-ADR-Status-Mapping-Policy.md` and `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` identify concrete adapter candidates and accept Loading as bounded contract pilot. | One concrete Surface/Adapter Contract Pattern exists; broad adapter expansion remains unapproved. | Consumer readiness and later domain-specific pilots. | `PAUSEVIS-1` | Runtime smokes only after future implementation changes. |
| Surface readiness | Partial - Loading pilot accepted; broad layer still gated | `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` accepts Loading as the first Surface/Adapter Contract Pattern with no runtime patch required. | Existing surfaces can be classified and Loading can guide future contracts, but no broad Surface layer is accepted. | Pause visual consumer readiness and later flow/lifecycle gates. | `PAUSEVIS-1` | Doc/static review; no runtime patch in F40. |
| Current module classification | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies modules as Ready, Partial, Candidate, Experimental or Blocked. F40 upgrades Loading from candidate to accepted pilot reference. | Current implementation inventory is mapped to accepted archetypes and mapping hot spots are classified in F39/F40. | Keep classifications current as gates execute. | `PAUSEVIS-1` | Static/doc review. |
| Pause/InputMode apply boundary | Closed / owner-validated | `PauseInputModeApplyService`, `PauseInputModeApplyRequest`, `PauseInputModeApplyResult` and `PauseInputModeApplyStage` implement the F37 boundary. | Non-MonoBehaviour service owns preflight, Pause request submission, PlayerInput adapter call, failed stage and aggregate result; bridge keeps serialized fields and observable API. | None for INPUT apply boundary unless future runtime changes touch it. | `PAUSEVIS-1` | F38 owner Unity compile/import and required Pause/InputMode smokes reported validated. |
| Adapter/surface failure mapping | Accepted / doc-policy | `F39-ADR-Status-Mapping-Policy.md` accepts domain-first mapping rules and rejects universal enums/results. | Aggregate results must preserve original cause, boundary-local failed stage, adapter result when present, blocking issues and explicit optional/no-op evidence. | Apply policy during future implementation patches. | `PAUSEVIS-1` | Doc-policy only. |
| Surface adapter contract pilot | Accepted / no runtime patch required | `F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` accepts Loading as the bounded pilot. | Contract Pattern is accepted using Loading contracts, runtime, adapter, observation path, real internal consumers and diagnostics evidence. | Do not promote broad Surface layer; use the pattern only as a checklist for future domains. | `PAUSEVIS-1` | Doc/static validation only. |
| Pause visual consumer readiness | Blocked | PAUSEVIS remains pending and pause visual materialization is experimental. | No accepted consumer readiness contract. | Decision after apply and failure policy gates. | `PAUSEVIS-1` | Doc-only unless later pilot is approved. |

## Module classification snapshot

| Module or area | Readiness | Candidate archetypes |
| --- | --- | --- |
| Common internal mechanics | Ready | Validator/Evidence support |
| Participant primitives, CycleReset, ObjectReset | Ready | Consumer, Validator/Evidence |
| RuntimeContent | Partial | Operation Service, Validator/Evidence, Consumer |
| ContentAnchor | Partial | Adapter, Bridge, Operation Service, Validator/Evidence |
| Loading | Pilot accepted / reference pattern | Surface, Adapter, Validator/Evidence, Consumer |
| Transition and TransitionEffects | Candidate | Surface, Adapter, Operation Service, Consumer |
| Pause runtime surface | Partial | Surface, Adapter, Bridge, Consumer |
| Pause visual materialization | Experimental | Surface, Adapter, Consumer, QA Smoke Runner |
| InputMode and UnityInput bridge path | Partial - reference boundary validated; no Surface pilot | Bridge, Operation Service, Adapter |
| RouteLifecycle, ActivityFlow, SceneLifecycle | Partial | Operation Service, Consumer, Validator/Evidence |
| GameFlow and ApplicationLifecycle | Blocked | Consumer, Operation Service |
| GlobalUi | Candidate | Consumer, Surface host, Adapter host |
| Flow triggers | Blocked | Bridge, Consumer, Validator/Evidence |
| Status mapping across Loading/InputMode/Pause/materialization | Partial - policy accepted; implementation deferred to pilots | Validator/Evidence |

## Ordered next gates

1. `PAUSEVIS-1`
2. `FLOWTRIGGER`
3. `LIFECYCLE-KERNEL-REMAINING`
4. `GAMEFLOW`

## Active navigation policy

- Active architecture decisions: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- Active extension surface model: `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- Active surface/adapter inventory: `Assets/_Documentation/Architecture/F36-AUDIT-Surface-Adapter-Inventory.md`
- Active Pause/InputMode apply boundary contract: `Assets/_Documentation/Architecture/F37-ADR-Pause-InputMode-Apply-Boundary.md`
- Active adapter/surface failure mapping policy: `Assets/_Documentation/Architecture/F39-ADR-Status-Mapping-Policy.md`
- Active Loading Surface/Adapter contract pattern: `Assets/_Documentation/Architecture/F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md`
- Active immutable plan: `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- Active mutable tracker: `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

Old audits, plans, closeouts, prompts and notes remain historical source material unless explicitly removed in a later cleanup. They are not active navigation.

## Manual decisions needed

- Whether to delete or archive older historical placeholder-named files outside active architecture navigation.
- Whether `PAUSEVIS-1` should require result-returning evidence for Pause visual adapters before any Pause surface promotion.
- Whether `FLOWTRIGGER` should run before lifecycle kernel work because repeated trigger/request/state shapes are now more isolated.
- Whether a future Loading runtime patch should preserve named per-adapter result evidence in aggregate diagnostics.
