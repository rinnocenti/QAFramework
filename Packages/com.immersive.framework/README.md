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

The framework currently has lifecycle/content/contribution foundations plus Route scene composition/release for scene content. It is not yet a Content Anchor, RuntimeSpawned, Actor, Camera, Input, Save, Reset or Pooling framework.

F7 is closed as the Content Anchor declaration baseline. F8 is now allowed only as Runtime Roots and Materialization groundwork: ownership primitives, scoped runtime roots, runtime content handles, request/result models, prefab materialization and runtime release. F8 must not create Content Anchor binding, Activity anchors, Actor, Pause, Camera, UI, Save, Input or Pooling consumers. F8B adds passive runtime ownership primitives: scope, owner, typed content id, identity and state vocabulary. F8C adds a passive `RuntimeContentHandle` surface for state transitions and release diagnostics without executing materialization or release. F8D adds a logical `RuntimeScopeRoot` and internal `RuntimeRootRegistry` for explicit root/handle registration without creating hierarchy GameObjects. F8E adds the internal `RuntimeContentRuntime` owner and explicit `RuntimeScopeContext` for scoped handle operations before any materialization request/result model. F8F connects Session, Route and Activity lifecycles to logical runtime root/context creation and removal without materializing or releasing objects physically. F8G adds `RuntimeMaterializationRequest`, `RuntimeMaterializationResult`, `RuntimeMaterializationResource` and `RuntimeMaterializationStatus` as explicit contracts before any concrete materializer. F8H adds transition guard and scoped cancellation tokens so requests cannot cross stale Route/Activity/Session scope transitions.

## F7 Content Anchor boundary

`Content Anchor` is the canonical name for authored placement/reference points inside loaded content.

F7 may define passive identity, declaration, authoring, discovery, diagnostics and validation for anchors. F7 must not create prefab materialization, runtime binding, Camera, Pause, UI, Actor, Save, Input or Pooling consumers.

- `Documentation~/ContentAnchor/ROUTE_CONTENT_ANCHOR_DISCOVERY.md` — F7F diagnostic discovery of Route Content Anchors into a local ContentAnchorSet.
- `Documentation~/ContentAnchor/CONTENT_ANCHOR_DIAGNOSTICS_SMOKE.md` — F7G QA smoke for Content Anchor diagnostics.
- `Documentation~/ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md` — F7H authoring validation for loaded Route Content Anchors.


## F8 Runtime Roots and Materialization boundary

F8 separates three concepts:

| Concept | Role |
|---|---|
| `Content Anchor` | Passive authored placement/reference point inside loaded content. |
| `Runtime Root` | Runtime-owned container for instances created by the framework. |
| `RuntimeContentHandle` | Canonical reference/release handle for a runtime-created instance. |

F8 does not connect materialization to Content Anchors. That belongs to F9.

F8 was realigned after F8D: materialization request/result must not be introduced before the framework has an internal runtime owner and explicit scope context for roots/handles.

Applied cuts:

```text
F8B — Runtime ownership primitives
F8C — RuntimeContentHandle passive and release state
F8D — RuntimeScopeRoot + internal minimal registry
F8E — RuntimeContentRuntime + RuntimeScopeContext
F8F — lifecycle integration for RuntimeContentRuntime roots
F8G — RuntimeMaterializationRequest / RuntimeMaterializationResult
F8H — transition guard + scoped cancellation
```

Next authorized cut:

```text
F8I — PrefabContentMaterializer
```
