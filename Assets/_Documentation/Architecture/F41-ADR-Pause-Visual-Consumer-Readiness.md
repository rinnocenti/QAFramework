# F41-ADR-Pause-Visual-Consumer-Readiness

Status: Accepted / resident-only
Last updated: 2026-07-01
Depends on:

- `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- `Assets/_Documentation/Architecture/F35-ADR-Extension-Surface-Model.md`
- `Assets/_Documentation/Architecture/F36-AUDIT-Surface-Adapter-Inventory.md`
- `Assets/_Documentation/Architecture/F39-ADR-Status-Mapping-Policy.md`
- `Assets/_Documentation/Architecture/F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md`

## Context

Pause has several working paths: logical Pause state, resident Pause surface application, an authored Unity resident adapter, and the Pause/InputMode apply boundary extracted in F38. It also has experimental visual/materialization code that composes RuntimeContent and ContentAnchor to create a Pause visual prefab at an anchor.

F35 rejects QA smoke runners as product consumers. F36 classifies Pause visual materialization as experimental/frozen. F39 records weak Pause adapter evidence because `IPauseSurfaceAdapter.Apply` is void. F40 accepts Loading as the only current Surface Adapter Contract Pattern and explicitly does not promote a broad Surface layer.

## Decision

Choose **Option B - Keep Resident Only**.

The supported Pause presentation path is the resident `UIGlobal` Pause surface:

- `PauseRuntime` owns logical Pause state and Pause Gate evidence.
- `PauseSurfaceRuntime` applies a `PauseSnapshot` to explicit `IPauseSurfaceAdapter` instances.
- `UnityPauseResidentSurfaceAdapter` shows or hides an already-authored resident hierarchy.

Pause visual/materialization remains **Experimental / Freeze**. It must not become a broad Surface, Adapter contract, materialization contract or public compatibility surface from F41 alone.

## Direct answers

| Question | Answer |
| --- | --- |
| Does Pause visual have a real consumer today? | No product/runtime consumer was found for visual materialization. Current visual/materialization execution is QA/proof-style and explicit experimental code. |
| Is the consumer product/runtime, framework internal, QA-only or experimental? | Resident surface has product/runtime intent through `UIGlobal`. Visual materialization is experimental and currently proven by QA-only paths. |
| Is Pause resident surface sufficient as the current Surface? | Yes for current Pause presentation scope: it reflects logical Pause state through resident UI without materialization, input, time scale or lifecycle ownership. |
| Should Pause visual materialization stay frozen? | Yes. It has useful evidence, but not consumer readiness. |
| Is result-returning adapter evidence needed for Pause visual? | Yes before any future promotion. The current resident adapter contract is `void Apply`, and visual materialization evidence is not a Pause surface adapter result. |
| Does Pause visual depend on RuntimeContent/ContentAnchor materialization? | The experimental materialization path does. The resident surface path does not. |
| Should Pause visual be separated from InputMode apply? | Yes. InputMode apply is a different boundary with PlayerInput evidence. |
| Should Pause visual be separated from logical Pause? | Yes. Logical Pause owns state/gates; visual presentation only reflects snapshots. |
| What is the next allowed cut? | `FLOWTRIGGER` is the recommended next gate. A future Pause visual cut may only define evidence requirements and a real consumer, not a broad Surface layer. |

## Current path classification

| Item | Archetype | Readiness | Consumer status | Evidence strength | Decision |
| --- | --- | --- | --- | --- | --- |
| `PauseRuntime` | Operation Service, Validator/Evidence | Ready for current scope | Product/runtime | Strong | Keep as logical Pause owner only. |
| `PauseSurfaceRuntime` | Surface, Operation Service | Partial | Product/runtime | Sufficient for resident scope | Keep as resident snapshot application boundary. |
| `IPauseSurfaceAdapter` | Adapter | Partial | Product/runtime | Weak | Keep unchanged; do not use as broad adapter contract yet. |
| `UnityPauseResidentSurfaceAdapter` | Adapter | Partial | Product/runtime | Sufficient for resident scope | Keep as supported resident authoring path. |
| `PauseVisualSurfaceMaterializationExecutor` | Consumer, Operation Service, Experimental | Freeze | QA-only / experimental | Weak for product readiness | Keep frozen; no promotion. |
| `PauseVisualSurfaceAuthoring` | Bridge, Authoring, Experimental | Freeze | No real consumer | Weak | Keep as experimental authoring contract only. |
| RuntimeContent/ContentAnchor-based Pause visual paths | Operation Service, Adapter, Bridge, Experimental | Freeze | QA-only / experimental | Strong as materialization evidence, weak as Pause product consumer evidence | Do not treat as broad Pause Surface readiness. |
| QA Pause logical toggle resident surface smoke | QA Smoke Runner | Ready for evidence only | QA-only | Sufficient for resident evidence | Use as validation evidence, not product readiness. |
| Pause Runtime PlayerInput Bridge Smoke | QA Smoke Runner | Ready for evidence only | QA-only | Strong for InputMode bridge behavior | Does not prove Pause visual readiness. |
| Pause InputAction Runtime Bridge Trigger Smoke | QA Smoke Runner | Ready for evidence only | QA-only | Strong for trigger-to-bridge behavior | Does not close `FLOWTRIGGER` or Pause visual readiness. |

## Required rules

- Logical Pause, Pause visual and InputMode apply are separate boundaries.
- Pause resident surface remains the current supported presentation path.
- Pause visual/materialization must not become a broad Surface without a real consumer.
- QA-only smokes do not prove product consumer readiness.
- Any future Pause visual adapter promotion needs result-returning evidence or an equivalent explicit evidence contract.
- Required visual capability absence must fail visibly.
- Optional visual absence must be explicit skipped/no-op behavior.
- Pause visual must not decide InputMode, `Time.timeScale`, route/activity lifecycle, player/actor spawn or global UI creation.
- RuntimeContent/ContentAnchor evidence may support a future visual materialization cut, but it does not by itself prove Pause Surface readiness.

## Consequences

Adapter readiness remains partial. Surface readiness remains partial. Loading remains the accepted pilot Surface Adapter Contract Pattern. Pause resident surface is usable, but Pause visual/materialization stays frozen until a future cut proves a real product/runtime consumer and stronger adapter/result evidence.

This decision does not change runtime, editor, asmdefs, package metadata, scenes, prefabs, serialized assets, QA Canvas or smoke runners.

## Next gate

Recommended next gate: `FLOWTRIGGER`.

`GAMEFLOW` and `LIFECYCLE-KERNEL-REMAINING` remain pending. A future Pause visual evidence cut is allowed only after a real consumer is identified and scoped; it must not open a broad Surface layer.
