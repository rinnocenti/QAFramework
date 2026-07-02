# Architecture

This page describes the stable package architecture for users of `com.immersive.framework`. It is not a cut log, roadmap tracker or internal ADR index.

## Package model

`com.immersive.framework` owns framework-specific runtime contracts, Unity adapters, authoring assets and diagnostics. Generic technical primitives belong to the technical packages:

- `com.immersive.foundation`
- `com.immersive.logging`
- `com.immersive.pooling`

The framework package may consume those packages, but package docs should not present copied primitives as framework-owned architecture.

## Extension archetypes

Framework extension work uses these archetypes:

- Surface: a stable runtime capability boundary, such as Loading, Transition or Pause.
- Adapter: concrete side-effect execution with local evidence.
- Bridge: Unity authoring wrapper that reads Inspector data, validates locally, calls an explicit boundary and exposes diagnostics.
- Operation Service: non-MonoBehaviour orchestration for multi-step operations, rollback, failed stage and evidence preservation.
- Consumer: code or tooling that requests a capability and handles absence/failure explicitly.
- Validator/Evidence: readiness or result validation without side effects.
- QA Smoke Runner: validation entry point that proves a bounded contract, not product runtime policy.
- Runtime Surface Host: host for shared surface availability, such as `UIGlobal`.

This model guides development language and ownership. It does not authorize broad adapter or Surface layer expansion; that remains gated by architecture readiness work.

## Runtime shape

| Surface | Owner | Purpose |
| --- | --- | --- |
| Game Application | `GameApplicationAsset`, `FrameworkRuntimeHost` | Startup application context, startup route and optional `UIGlobal` scene loading. |
| Route | `RouteAsset`, route lifecycle runtime | Top-level navigation and route content lifecycle. |
| Activity | `ActivityAsset`, activity flow runtime | Activity selection, activity content lifecycle and activity scene operation planning/execution. |
| UIGlobal | `GlobalUiSceneRuntime` | App/session scoped scene for shared Transition, Loading and Pause visual adapters. |
| Transition | Transition runtime/effect adapters | Route/activity visual envelopes and transition effects. |
| Loading | Loading surface runtime/adapters | Loading presentation, progress and readiness reporting. |
| Pause | Pause runtime/surface/input bridges | Logical Pause state, resident Pause presentation and explicit InputMode/PlayerInput synchronization. |
| RuntimeContent | Runtime content runtime, handles and release requests | Logical content identity, handle state, materialization/release request language and lifecycle registry evidence. |
| ContentAnchor | ContentAnchor declarations, binding runtime and Unity adapters | Logical anchor declarations/bindings plus explicit Unity materialization, placement and release helpers. |

Route and Activity request logs also project lifecycle-local operation evidence through `lifecycleOperation*` fields. This is a diagnostic ledger over existing Route/Activity evidence; it is not a GameFlow envelope and does not replace Route or Activity result/status types.

F45 adds lifecycle-local content/readiness evidence projection to those request logs through `lifecycleContent*` and `lifecycleReadiness*` fields. The projection is observational: Route, ActivityFlow, RuntimeContent and ContentAnchor keep their execution and domain status ownership. It does not create a content dispatch kernel or readiness kernel.

F44, F45 and F46 close the initial lifecycle evidence stabilization. F47 accepts GameFlow request envelope as a request/diagnostics boundary. F48 adds the first internal passive runtime shell and projects `gameFlowEnvelope*` fields into Route and Activity request logs. The shell summarizes existing evidence only: it does not execute flow, decide policy, replace Route/Activity results, migrate triggers or change Route, Activity, Scene, Content, Loading or Transition ownership.

## Current boundary rules

- `GameApplicationAsset` is the authoring root for app startup and `UIGlobal` policy.
- Route owns route lifecycle; Activity owns activity lifecycle below the active route.
- `UIGlobal` owns shared visual surfaces; route/activity content should not own global Pause, Loading or Transition surfaces.
- Loading is not a fade effect. Transition effects and Loading surfaces are separate runtime surfaces.
- Pause presentation is resident by default. RuntimeContent + ContentAnchor materialization remains available for explicit modular content paths.
- Pause input must synchronize logical Pause, `InputMode` and Unity `PlayerInput` through the runtime bridge path.
- RuntimeContent handles are logical state, not Unity object references.
- ContentAnchor binding is logical; physical placement belongs to Unity ContentAnchor adapters/services.

## Internal history

Historical ADRs, old roadmaps and phase notes remain under `ADRs/`, `Planning/` and `Guides/` for deep reference only. They are not the active package architecture model.
