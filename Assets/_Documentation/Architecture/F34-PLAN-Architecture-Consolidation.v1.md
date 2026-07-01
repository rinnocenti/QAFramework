# F34-PLAN-Architecture-Consolidation.v1

Status: Accepted / Immutable  
Version: v1  
Last updated: 2026-07-01  
Supersedes: fragmented architecture consolidation and adapter/surface readiness plans  
Superseded by: none  
Decision file: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`  
Progress owner: `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

## Purpose

Define the approved route for Immersive Framework architecture consolidation and adapter/surface readiness without creating additional active architecture fragments.

This plan is immutable. Progress, drift, executed cuts, validation results and current status belong only in the tracker.

## Tracks

| Order | Track | Intent | Primary risks | Required validation class |
| ---: | --- | --- | --- | --- |
| 1 | COMMON - Internal Mechanics | Extract only small, repeated, domain-neutral mechanics. | Common becomes a generic dumping ground. | Static/doc review unless runtime call sites change. |
| 2 | CONS - Participant Consolidation | Keep participant primitives as a bounded pilot. | Pilot expands into unrelated domains. | Existing participant/reset smokes when runtime changes resume. |
| 3 | LIFECYCLE - Route/Activity Operation Kernel | Clarify route/activity operation ownership without merging public concepts. | Scene/content lifecycle drift or over-generalization. | Unity compile/import and lifecycle smokes for runtime cuts. |
| 4 | MAT - RuntimeContent/ContentAnchor Materialization | Keep reusable materialization orchestration outside MonoBehaviour bridges. | Unity side effects and rollback drift. | Unity compile/import and materialization/release smokes. |
| 5 | INPUT - Pause/InputMode Apply Boundary | Separate logical pause/input mode apply decisions from QA cleanup and presentation. | Logical Pause and Unity `PlayerInput` divergence. | Pause/InputMode smokes for runtime cuts. |
| 6 | GAMEFLOW - Lifecycle Request Envelope | Define request envelope only after lifecycle/input boundaries stabilize. | Old session shapes or global manager behavior leaks in. | Route/activity/request smokes for runtime cuts. |
| 7 | STATUS - Mapping Policy | Preserve original failure evidence while normalizing repeated mapping mechanics. | Universal status enum hides domain failures. | Domain-specific smokes for touched mappers. |
| 8 | FLOWTRIGGER - Request/Trigger/State | Extract trigger mechanics only if repeated shape remains after higher-priority work. | Serialized Unity trigger fields drift. | Flow trigger smokes for runtime cuts. |
| 9 | PAUSEVIS - Consumer Readiness | Decide readiness for pause visual consumers without presentation coupling. | Experimental API becomes compatibility baggage. | Doc decision first; visual/readiness smokes only after runtime scope. |
| 10 | EXT - Adapter and Surface Readiness | Define whether adapter/surface expansion is ready and in what order. | Candidate-rich modules get treated as broadly ready too early. | Doc-only until a pilot implementation is explicitly approved. |

## Planned order

1. Preserve closed COMMON and CONS status without reopening them.
2. Keep MAT closure evidence in the tracker; decide whether lifecycle operation kernel or adapter/surface readiness resumes first.
3. Define INPUT apply boundary before more Pause/InputMode adapter work.
4. Define STATUS mapping policy before adapter/surface failure contracts are treated as ready.
5. Complete EXT readiness gates before any broad Surface layer expansion.
6. Decide PAUSEVIS consumer readiness only after upstream apply/failure policy is clear.

## Adapter and Surface readiness answers

| Question | Answer |
| --- | --- |
| Is the framework ready to receive adapters? | Not for broad expansion. It has candidates and pilots, but lacks accepted extension model, inventory, apply boundary, failure policy and consumer readiness. |
| Is the framework ready to receive a Surface layer? | Not yet. Runtime-facing surfaces exist, but a formal Surface layer requires archetype definitions, ownership rules, readiness contracts and one bounded pilot. |
| Which modules are candidates? | Loading, Transition, Pause, RuntimeContent, ContentAnchor, GlobalUi, InputMode/UnityInput, RouteLifecycle, ActivityFlow, SceneLifecycle, GameFlow and flow triggers each map to candidate archetypes with different readiness. |
| What blocks readiness? | `EXT-SURFACE-1`, `SURFACE-AUDIT-1`, `INPUT-APPLY-1`, `STATUS-1`, `SURFACE-PILOT-1`, `PAUSEVIS-1`, plus lifecycle kernel gaps and materialization consumer readiness gates for broader consumers. |
| Minimum order to reach ideal point? | Extension model, inventory, input apply pilot, failure mapping policy, surface adapter contract pilot, pause visual readiness decision. |

## Minimal archetypes

| Archetype | Owns | Must not own |
| --- | --- | --- |
| Surface | Stable runtime capability, readiness, diagnostics and consumer-facing result language. | Concrete Unity side effects or generic lifecycle orchestration. |
| Adapter | Concrete side-effect execution against Unity or another subsystem, with local evidence. | Framework policy, service lookup or unrelated orchestration. |
| Bridge | Unity authoring wrapper: serialized references, validation, boundary invocation and diagnostics display. | Multi-stage runtime orchestration or hidden dependency resolution. |
| Operation Service | Non-MonoBehaviour multi-step operation with explicit dependencies, rollback and stage result. | Inspector fields, scene object ownership or service locator behavior. |
| Consumer | Explicit request intent and handling of unavailable/failed capability. | Fabricating missing identity or hiding required config with fallback. |
| Validator/Evidence | Preconditions, issue severity and original failure references. | Applying side effects or erasing domain-specific results. |
| QA Smoke Runner | Bounded scenario and observable diagnostics for validation. | Production ownership or architecture policy. |

## EXT readiness gates

| Order | Gate | Required output | Non-goals |
| ---: | --- | --- | --- |
| 1 | `EXT-SURFACE-1` | Accept minimal archetypes and ownership language. | No code, no adapter creation, no public API expansion. |
| 2 | `SURFACE-AUDIT-1` | Inventory current surfaces/adapters/bridges/services/consumers/smoke runners. | No cleanup disguised as inventory. |
| 3 | `INPUT-APPLY-1` | Define Pause/InputMode apply boundary request, result, failure and diagnostics. | No global input manager or player join/spawn ownership. |
| 4 | `STATUS-1` | Define adapter/surface failure mapping policy that preserves original evidence. | No universal status enum. |
| 5 | `SURFACE-PILOT-1` | Name one pilot surface, adapter, bridge/service split, consumer and QA smoke runner. | No broad migration or package split. |
| 6 | `PAUSEVIS-1` | Decide pause visual consumer readiness. | No presentation asset changes or fallback visuals. |

## Non-goals

- No code implementation in documentation cleanup cuts.
- No new adapters or surfaces from this plan alone.
- No runtime, asmdef, package, scene, prefab or serialized asset changes.
- No public API or Inspector field change before a specific gate authorizes it.
- No technical package changes.
- No direct Base 2.0 runtime port.
- No service locator, singleton shortcut, reflection route or hidden compatibility rail.
- No placeholder phase names in active architecture documentation.

## Validation policy

Documentation-only cuts require read validation only.

Runtime cuts require Unity compile/import and relevant manual smokes from the owner before any `Closed` status is claimed. Static checks may support the report, but do not replace Unity validation.

## Change policy

Progress is not tracked in this plan. Use:

`Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

If this route changes materially, create `F34-PLAN-Architecture-Consolidation.v2.md` and mark this file as superseded with minimal metadata only.
