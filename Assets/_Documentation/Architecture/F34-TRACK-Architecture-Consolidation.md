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
| INPUT - Pause/InputMode Apply Boundary | Contract accepted / not implemented | INPUT-APPLY-1 | `F37-ADR-Pause-InputMode-Apply-Boundary.md` accepts Bridge, Trigger, Operation Service, Adapter, Consumer, Validator/Evidence and QA Smoke Runner responsibilities plus conceptual request/result/failure-stage contract. | Runtime boundary extraction, exact service/adapter names and implementation validation. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | F37 is doc-only; later runtime cut requires Unity compile/import and Pause/InputMode smokes. |
| GAMEFLOW - Lifecycle Request Envelope | Pending | None | No accepted envelope. | Define envelope after lifecycle and input boundaries stabilize. | `GAMEFLOW` | Lifecycle/request smokes after implementation. |
| STATUS - Mapping Policy | Pending | None | No shared status mapping policy. | Decide mapping ownership without universal status enum. | `STATUS-1` | Domain-specific smokes after implementation. |
| FLOWTRIGGER - Request/Trigger/State | Pending | None | No shared helper. | Confirm repeated trigger/request/state shape after higher-priority flow work. | `FLOWTRIGGER` | Flow trigger smokes when scoped. |
| PAUSEVIS - Consumer Readiness | Pending | None | No readiness boundary accepted. | Define consumer readiness without coupling to pause presentation or apply semantics. | `PAUSEVIS-1` | Doc-only unless runtime pilot is approved. |

## Adapter and Surface readiness board

| Area | Real status | Evidence | Coverage | Pending work | Next gate | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| Extension Surface Model | Applied / doc-only | `F35-ADR-Extension-Surface-Model.md` accepts the archetype language. | Surface, Adapter, Bridge, Operation Service, Consumer, Validator/Evidence, QA Smoke Runner and Runtime Surface Host are defined. | Keep model as classification language only. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Doc-only; no runtime validation required. |
| Surface/Adapter Inventory | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies current candidates and blockers. | Loading, Transition, Pause, RuntimeContent, ContentAnchor, GlobalUi, participant/reset paths, lifecycle paths, flow triggers and QA runners are inventoried. | Use the inventory to drive finite gates; do not mark broad readiness. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Doc/static review only. |
| Adapter readiness | Partial - candidates exist; not ready for broad expansion | `F36-AUDIT-Surface-Adapter-Inventory.md` and `F37-ADR-Pause-InputMode-Apply-Boundary.md` identify concrete adapter candidates and the required Pause/InputMode apply split. | Enough for later bounded pilots only; apply contract exists but runtime extraction is not done. | Runtime apply extraction, failure mapping, pilot contract and consumer readiness. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Doc-only until runtime cut is approved. |
| Surface readiness | Partial - runtime surfaces exist; layer model accepted, expansion still gated | `F36-AUDIT-Surface-Adapter-Inventory.md` identifies Loading as strongest later pilot candidate and F37 keeps Pause/InputMode apply separate from Pause visual surface. | Existing surfaces can be classified, but no broad Surface layer is accepted. | Apply runtime extraction, failure policy and one bounded pilot. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Doc-only until pilot. |
| Current module classification | Applied / doc-static review | `F36-AUDIT-Surface-Adapter-Inventory.md` classifies modules as Ready, Partial, Candidate, Experimental or Blocked. | Current implementation inventory is mapped to accepted archetypes. | Keep classifications current as gates execute. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Static/doc review. |
| Pause/InputMode apply boundary | Contract accepted / not implemented | `F37-ADR-Pause-InputMode-Apply-Boundary.md` accepts request/result/failure-stage and role separation. | Bridge/Trigger/Service/Adapter/Consumer/Validator/QA responsibilities are defined. | Extract runtime boundary without serialized field drift. | `INPUT-APPLY-2 - Runtime Boundary Extraction` | Doc-only first; runtime validation later. |
| Adapter/surface failure mapping | Blocked | STATUS track is pending and universal enums are rejected. | No shared policy. | Preserve original subsystem evidence across aggregation. | `STATUS-1` | Doc-only first. |
| Surface adapter contract pilot | Blocked | Candidates exist; no pilot contract is approved. | No accepted Surface + Adapter + Bridge/Service + Consumer + QA runner contract. | Requires earlier readiness gates. | `SURFACE-PILOT-1` | Static/doc validation before runtime. |
| Pause visual consumer readiness | Blocked | PAUSEVIS remains pending and pause visual materialization is experimental. | No accepted consumer readiness contract. | Decision after apply and failure policy gates. | `PAUSEVIS-1` | Doc-only unless later pilot is approved. |

## Module classification snapshot

| Module or area | Readiness | Candidate archetypes |
| --- | --- | --- |
| Common internal mechanics | Ready | Validator/Evidence support |
| Participant primitives, CycleReset, ObjectReset | Ready | Consumer, Validator/Evidence |
| RuntimeContent | Partial | Operation Service, Validator/Evidence, Consumer |
| ContentAnchor | Partial | Adapter, Bridge, Operation Service, Validator/Evidence |
| Loading | Candidate | Surface, Adapter, Validator/Evidence, Consumer |
| Transition and TransitionEffects | Candidate | Surface, Adapter, Operation Service, Consumer |
| Pause runtime surface | Partial | Surface, Adapter, Bridge, Consumer |
| Pause visual materialization | Experimental | Surface, Adapter, Consumer, QA Smoke Runner |
| InputMode and UnityInput bridge path | Blocked | Bridge, Operation Service, Adapter |
| RouteLifecycle, ActivityFlow, SceneLifecycle | Partial | Operation Service, Consumer, Validator/Evidence |
| GameFlow and ApplicationLifecycle | Blocked | Consumer, Operation Service |
| GlobalUi | Candidate | Consumer, Surface host, Adapter host |
| Flow triggers | Blocked | Bridge, Consumer, Validator/Evidence |
| Status mapping across Loading/InputMode/Pause/materialization | Blocked | Validator/Evidence |

## Ordered next gates

1. `INPUT-APPLY-2 - Runtime Boundary Extraction`
2. `STATUS-1`
3. `SURFACE-PILOT-1`
4. `PAUSEVIS-1`
5. `LIFECYCLE-KERNEL-REMAINING`
6. `GAMEFLOW`
7. `FLOWTRIGGER`

## Active navigation policy

- Active architecture decisions: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- Active extension surface model: `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- Active surface/adapter inventory: `Assets/_Documentation/Architecture/F36-AUDIT-Surface-Adapter-Inventory.md`
- Active Pause/InputMode apply boundary contract: `Assets/_Documentation/Architecture/F37-ADR-Pause-InputMode-Apply-Boundary.md`
- Active immutable plan: `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- Active mutable tracker: `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

Old audits, plans, closeouts, prompts and notes remain historical source material unless explicitly removed in a later cleanup. They are not active navigation.

## Manual decisions needed

- Whether to delete or archive older historical placeholder-named files outside active architecture navigation.
- Whether `INPUT-APPLY-2` should name the operation service `PauseInputModeApplyService` or `InputModeApplyService`.
- Whether Loading should be selected as the first `SURFACE-PILOT-1` candidate after `INPUT-APPLY-2` and `STATUS-1`.
