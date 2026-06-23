# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current status

Use `Documentation~/COMPLETENESS_TRACKER.md` as the authoritative status file.

Current consolidated position:

```text
F0 — CLOSED / PASS
F1 — CLOSED / PASS
F2 — CLOSED / PASS
F3 — CLOSED / PASS
F4 — CLOSED / ACTIVITY BASELINE PASS
F5 — CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS
F6 — CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS
F7 — CLOSED / CONTENT ANCHOR DECLARATION BASELINE PASS
F8 — OPEN / RUNTIME ROOTS AND MATERIALIZATION
```

F6 closes the first Route scene composition and release baseline:

- `RouteContentProfileAsset` can declare additional Route scenes.
- Primary Scene still loads through `LoadSceneMode.Single` and remains the active scene.
- Owned additional Route scenes load additively.
- Owned additional Route scenes are explicitly released on Route exit.
- Release is represented by `ContentReleasePlan` and `ContentReleaseResult`.

## Documentation entry points

```text
Documentation~/COMPLETENESS_TRACKER.md
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Documentation~/Planning/Capability-Traceability-Matrix.md
Documentation~/Planning/F6-Route-Scene-Composition-Audit.md
Documentation~/Planning/F7-Content-Anchor-Declaration-Audit.md
Documentation~/Planning/F8-Runtime-Roots-Materialization-Audit.md
Documentation~/RuntimeContent/RUNTIME_CONTENT_HANDLE.md
Documentation~/RuntimeContent/RUNTIME_SCOPE_ROOT_REGISTRY.md
Documentation~/RuntimeContent/RUNTIME_CONTENT_RUNTIME.md
Documentation~/RuntimeContent/RUNTIME_ROOT_LIFECYCLE_INTEGRATION.md
Documentation~/RuntimeContent/RUNTIME_MATERIALIZATION_REQUEST_RESULT.md
Documentation~/Route/ROUTE_CONTENT_PROFILE_USAGE.md
Documentation~/Route/ROUTE_SCENE_COMPOSITION_SMOKE.md
Documentation~/Route/ROUTE_RELEASE_SMOKE.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_SET.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md
Documentation~/ADRs/
```

## Accepted F6 ADRs

| ADR | Decision |
|---|---|
| `F6-01 — ADR-RELEASE-001` | Release is planned/executed through `ContentReleasePlan`/`ContentReleaseResult`, guided by explicit ownership. F6 execution is limited to owned additive Route scene unload. |
| `F6-02 — ADR-SCENE-001` | Route scene composition is planned/executed through `RouteSceneCompositionPlan`/`RouteSceneCompositionResult`; additional Route scenes are loaded additively from `RouteContentProfileAsset`. |

## Current hard boundary

The framework currently has lifecycle/content/contribution foundations plus Route scene composition/release, Content Anchor declaration and RuntimeContent contracts/guardrails and logical release through F8K in the uploaded package.

F8 remains the active gate before F9+ code starts. F8 may continue only with F8 closure smoke. F8 must not create Content Anchor binding, Activity anchors, Actor, Pause, Camera, UI, Save, Input or Pooling consumers.

F9+ has been realigned as documentation so that missing `NewScripts` capabilities are tracked without creating side paths:

```text
F9   Content Anchor binding/runtime placement
F10  Transition, loading and Activity content execution
F11  Participation, live capability inventory and local lifecycle participants
F12  Input, Snapshot/Save and Pause
F13  Advanced consumers: Camera, Audio, Actor, Pooling, transition presentation adapters
F14  Gameplay capabilities
F15/FX Productization, tooling and hardening
```

Authoritative roadmap files:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Documentation~/Planning/F9Plus-Roadmap-Realignment.md
Documentation~/Planning/Foundation-Hardening-Backlog.md
```

Next technical work remains inside F8 until F8 closes:

```text
F8J — runtime release policy / logical release execution [APPLIED / COMPILE-SMOKE PASS]
F8K — runtime request/guard/release-policy smoke and F8 closure [APPLIED / PENDING COMPILE + SMOKE]
```

