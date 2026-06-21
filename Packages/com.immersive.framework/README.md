# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current roadmap position

Closed phases:

```text
F0 — CLOSED / PASS
F1 — CLOSED / PASS
F2 — CLOSED / PASS
```

Completed F1 cuts:

```text
F1A — CLOSED / ACCEPTED
F1B — CLOSED / COMPILE-SMOKE PASS
F1C — CLOSED / COMPILE-SMOKE PASS
F1D — CLOSED / COMPILE-SMOKE PASS
F1E — CLOSED / COMPILE-SMOKE PASS
F1E1 — CLOSED / DOCUMENTATION ONLY
F1F — CLOSED / COMPILE-SMOKE PASS
```

F1 closure:

```text
F1 closure — CLOSED / PASS
```

## F2A accepted ADRs

F2A accepted the ADRs required to start technical work on Session scope:

| Ordem no Plano | ADR | Decision |
|---|---|---|
| `F2-01` | `ADR-SESSION-001 — Session Scope and Owner` | Session is the top runtime scope; `FrameworkRuntimeHost` is the initial Session owner but not a service locator. |
| `F2-02` | `ADR-SESSION-002 — SessionContent Ownership Semantics` | `SessionContentSet` is Session-owned state/data with explicit `Registered`, `Owned` and `DiagnosticOnly` semantics. |
| `F2-03` | `ADR-SETTINGS-001 — Settings Source Policy` | `Resources` is accepted as the temporary explicit bootstrap settings source for F2; missing required settings must fail visibly. |

See:

```text
Documentation~/F2A_SESSION_SCOPE_ADR_ACCEPTANCE.md
```

Current F2 status:

```text
F2A — CLOSED / ADRS ACCEPTED
F2B — CLOSED / COMPILE-SMOKE PASS
F2C — CLOSED / COMPILE-SMOKE PASS
F2D — CLOSED / DOCUMENTATION ONLY
F2  — CLOSED / PASS
```

Current F3 status:

```text
F3A — CLOSED / ADRS ACCEPTED
F3B — CLOSED / COMPILE-SMOKE PASS
F3C — CLOSED / COMPILE-SMOKE PASS
F3D — CLOSED / COMPILE-SMOKE PASS
F3E — CLOSED / COMPILE-SMOKE PASS
F3F — CLOSED / CALLBACK-SMOKE PASS
F3F1 — CLOSED / COMPILE-SMOKE PASS
F3G — APPLIED / PENDING COMPILE-SMOKE
F3G1 — APPLIED / PENDING COMPILE-SMOKE
```

F3A accepted:

| Ordem no Plano | ADR | Decision |
|---|---|---|
| `F3-01` | `ADR-ROUTE-001 — RouteRuntimeState and RouteContentRuntime Status` | Route gets explicit runtime state. `RouteContentRuntime` becomes Active in F3 with scope limited to route-local callbacks in the loaded Primary Scene. |
| `F3-02` | `ADR-ROUTE-002 — RouteContentSet Semantics` | `RouteContentSet` is an immutable route snapshot; ownership must be explicit; release remains F6. |

See:

```text
Documentation~/F3A_ROUTE_BASELINE_ADR_ACCEPTANCE.md
```

Status distinction:

```text
ADR status uses Accepted.
Cut/phase status uses Closed / PASS or Closed / Accepted.
```

F0 is closed. F1 is closed. F2 is closed. F3 may start with Route baseline ADR review and acceptance. Do not skip directly to Surface, RuntimeMaterialization or consumers.


## F3B — RouteRuntimeState tipado

F3B implements the roadmap item:

```text
IF-FW-ROAD-3A — RouteRuntimeState tipado
```

It introduces:

```text
Runtime/RouteLifecycle/RouteRuntimeState.cs
```

`RouteLifecycleRuntime` now owns the active Route through `RouteRuntimeState`, which carries a typed `FrameworkIdentityKey` for the active Route. This does not implement `RouteExitResult`, active `RouteContentRuntime` callbacks, additive scene loading, Surface or RuntimeMaterialization.

Status:

```text
F3B — CLOSED / COMPILE-SMOKE PASS
```

See:

```text
Documentation~/ROUTE_RUNTIME_STATE_TYPED.md
Documentation~/F3B_CLOSURE.md
```

## F3C — RouteExitResult mínimo

F3C implements the roadmap item:

```text
IF-FW-ROAD-3B — RouteExitResult mínimo
```

It introduces:

```text
Runtime/RouteLifecycle/RouteExitResult.cs
```

`RouteLifecycleStartResult` now carries a minimal `RouteExitResult` for Route switches. This records the previous Route exit as explicit state/diagnostics. It does not execute Route content release, active `RouteContentRuntime` callbacks, additive scene loading, Surface or RuntimeMaterialization.

Status:

```text
F3C — CLOSED / COMPILE-SMOKE PASS
```

See:

```text
Documentation~/ROUTE_EXIT_RESULT_MINIMAL.md
Documentation~/F3C_CLOSURE.md
```

## F3D — RouteContentRuntime execution decision

F3D implements the roadmap item:

```text
IF-FW-ROAD-3C — RouteContentRuntime execution decision
```

`RouteContentRuntime` is now connected to `RouteLifecycleRuntime` for Route-local callbacks in the loaded Primary Scene. The canonical order is: previous Route Content exit before `Single` scene load, next Route Content enter after scene load, Startup Activity after Route Content enter.

Status:

```text
F3D — CLOSED / COMPILE-SMOKE PASS
```

See:

```text
Documentation~/ROUTE_CONTENT_RUNTIME_EXECUTION_DECISION.md
Documentation~/F3D_CLOSURE.md
```

## F3E — RouteContentSet semantics

F3E implements the roadmap item:

```text
IF-FW-ROAD-3D — RouteContentSet semantics
```

`RouteContentSet` remains an immutable snapshot of content known by the active Route. The cut adds explicit per-item ownership semantics through:

```text
Runtime/RouteLifecycle/RouteContentOwnership.cs
Runtime/RouteLifecycle/RouteContentEntry.cs
```

The baseline Primary Scene is now represented as required Route content with explicit `Owned` semantics. This does not implement release, additive scene loading, Surface, RuntimeMaterialization or consumers.

Status:

```text
F3E — CLOSED / COMPILE-SMOKE PASS
```

See:

```text
Documentation~/ROUTE_CONTENT_SET_SEMANTICS.md
Documentation~/F3E_CLOSURE.md
```

## F3F — Route local callback smoke

F3F implements the roadmap item:

```text
IF-FW-ROAD-3E — Route local callback smoke
```

It adds QA/development tooling to validate real local receivers under `RouteContentBinding` roots:

```text
Runtime/Diagnostics/RouteContentLifecycleSmokeProbe.cs
FrameworkQaCanvas — Run Route Callback Smoke
```

This smoke requires at least one local `IRouteContentLifecycleReceiver` in each QA route scene. Dispatch with zero receivers remains visible and should not be accepted as callback proof. F3F does not create scene assets, additive scene loading, release policy, Surface, RuntimeMaterialization or consumers.

## F3F1 — QA panel simplification

F3F1 reduces the default QA panel surface. The normal validation path now appears as core smokes, while manual route/activity requests and edge smokes are hidden under `Show advanced/manual controls`. A new `Run Standard Smoke` button executes the core route/activity/clear validation path in one sequence. The specialized Route Content callback smoke remains visible but explicitly documents that scene probes are required. See `Documentation~/QA_PANEL_SIMPLIFICATION.md`.

Status:

```text
F3F — CLOSED / CALLBACK-SMOKE PASS
F3F1 — CLOSED / COMPILE-SMOKE PASS
F3G — APPLIED / PENDING COMPILE-SMOKE
F3G1 — APPLIED / PENDING COMPILE-SMOKE
```

See:

```text
Documentation~/ROUTE_LOCAL_CALLBACK_SMOKE.md
```

## F3G — Route validator expansion

F3G implements the roadmap item:

```text
IF-FW-ROAD-3F — Route validator expansion
```

The validator now checks `RouteContentBinding` components in loaded scenes for missing Route references, scene/Route mismatches and missing `IRouteContentLifecycleReceiver` components. F3G1 keeps the binding Inspector minimal and exposes the loaded-scene validation through the QA panel. See `Documentation~/ROUTE_VALIDATOR_EXPANSION.md` and `Documentation~/QA_AUTHORING_VALIDATION_HYGIENE.md`.

F3G does not create scene objects, mutate scenes, load additive scenes, create Surface, create RuntimeMaterialization, execute release policy or introduce consumers.

## F2B — SessionRuntimeState explicit boundary

F2B introduces an explicit `SessionRuntimeState` boundary owned by the current `FrameworkRuntimeHost`.

```text
Runtime/SessionLifecycle/SessionRuntimeState.cs
```

The old `FrameworkRuntimeState` remains as a compatibility facade and delegates to `SessionRuntimeState`. F2B did not create `SessionContentSet`, persistent scenes, Route baseline, Surface, RuntimeMaterialization or consumers.

F2B is closed by compile-smoke. See:

```text
Documentation~/F2B_CLOSURE.md
```

## F2C — SessionContentSet minimal model

F2C introduces the minimal Session content model:

```text
Runtime/SessionLifecycle/SessionContentOwnership.cs
Runtime/SessionLifecycle/SessionContentEntry.cs
Runtime/SessionLifecycle/SessionContentSet.cs
```

The initial set can be empty. This cut defines ownership semantics only; it does not create loading, release, persistent scenes, Surface, RuntimeMaterialization or consumers.

F2C is closed by compile-smoke. See:

```text
Documentation~/SESSION_CONTENT_SET_MINIMAL_MODEL.md
Documentation~/F2C_CLOSURE.md
```

## F2 closure

F2 is formally closed. See:

```text
Documentation~/F2_CLOSURE.md
```

See:

```text
Documentation~/SESSION_RUNTIME_STATE_BOUNDARY.md
Documentation~/SESSION_CONTENT_SET_MINIMAL_MODEL.md
Documentation~/F2C_CLOSURE.md
Documentation~/F2_CLOSURE.md
```

## Active baseline decisions

| Area | F0B status | Consequence |
|---|---|---|
| Bootstrap, Game Application, Route, Activity and request triggers | `Experimental` | Usable for development, but identity/state semantics still need F1/F3/F4 refinement. |
| `ContentFlow` materializer/contribution vocabulary | `Experimental` | Preserved as vocabulary, not stable materialization API. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Additional scenes are declared for planning/diagnostics only; they are not loaded by F0B. |
| `RouteContentRuntime` | `Accepted for F3 activation` | F3A accepted activation of route-local callbacks during the Route baseline; implementation starts after `RouteRuntimeState`. |
| `CameraFlow` | `Removed from core baseline` | Camera is a future consumer. The core package no longer carries CameraFlow source or a mandatory Cinemachine dependency. |
| `FrameworkQaCanvas` | `Development Tooling` | Manual smoke tool compiled only in the Unity Editor or development builds. Not product API. |
| `ValidationMode` | `Experimental` | Minimal F1D semantics: required config fails in every mode; Strict promotes warnings; Standard keeps warnings; Release suppresses info diagnostics. |

## Core rule

```text
The framework core owns lifecycle, content, identity, diagnostics, contribution, surface and runtime ownership.
Consumers declare requirements and receive context.
Consumers do not own the lifecycle core.
```

Consumers such as Input, Save, Pause, Camera, Audio, Actor, Pooling, Projectile, Damage and Attributes must not dictate core shape before their required primitives exist.

## Required reading

Start here:

```text
Documentation~/README.md
```

Then follow the roadmap order:

```text
1. Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
2. Documentation~/Planning/Capability-Traceability-Matrix.md
3. Documentation~/ADR_NAMING_CONVENTION.md
4. Documentation~/ADRs/F0A-baseline-adrs/
5. Documentation~/BASELINE_SMOKE.md
6. Documentation~/F0_CLOSURE.md
7. Documentation~/F1_ADR_ACCEPTANCE.md
8. Documentation~/API_STATUS_CONVENTION.md
9. Documentation~/F1B_CLOSURE.md
10. Documentation~/FRAMEWORK_FACT_MINIMAL_MODEL.md
11. Documentation~/F1C_CLOSURE.md
12. Documentation~/VALIDATION_MODE_SEMANTICS.md
13. Documentation~/F1D_CLOSURE.md
14. Documentation~/TYPED_IDENTITY_PRIMITIVES.md
15. Documentation~/F1E_CLOSURE.md
16. Documentation~/F1E1_ADR_NAMING_ALIGNMENT.md
17. Documentation~/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md
18. Documentation~/F1F_CLOSURE.md
19. Documentation~/F1_CLOSURE.md
20. Documentation~/F2A_SESSION_SCOPE_ADR_ACCEPTANCE.md
21. Documentation~/SESSION_RUNTIME_STATE_BOUNDARY.md
Documentation~/SESSION_CONTENT_SET_MINIMAL_MODEL.md
Documentation~/F2C_CLOSURE.md
Documentation~/F2_CLOSURE.md
```

## ADR file naming

ADR files are now ordered by roadmap/cut first and by architectural ADR id second:

```text
<plan-order>-<adr-id>-<slug>.md
```

Example:

```text
F1A-01-ADR-ID-001-typed-identity-policy.md
F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md
F1A-03-ADR-CONTENT-001-content-identity-domain.md
```

This keeps execution order visible while preserving stable ADR ids. See `Documentation~/ADR_NAMING_CONVENTION.md` and `Documentation~/F1E1_ADR_NAMING_ALIGNMENT.md`.

## F0 closure status

| Item | Status | Evidence |
|---|---|---|
| `F0A` | `CLOSED / ADRS ACCEPTED` | Baseline ADRs accepted. |
| `F0B` | `CLOSED / HYGIENE APPLIED / SMOKE PASS` | Baseline hygiene applied and smoke passed. |
| `F0C` | `CLOSED / FORMAL CLOSURE` | `Documentation~/F0_CLOSURE.md`. |
| `F0` | `CLOSED / PASS` | No F0 blocker remains open. |

ADR files themselves keep `Status: Accepted`; they are decisions, not cuts. The F0 cut/phase status is `Closed`.

## F0A accepted ADRs

| ADR | Decision |
|---|---|
| `ADR-BL-001 — Baseline Reconciliation` | Classifies current ambiguous surfaces and defines F0B cleanup targets. |
| `ADR-BL-002 — Core vs Consumers` | Prevents concrete consumers from owning lifecycle core. |
| `ADR-BL-003 — Public API Status Policy` | Defines `Stable`, `Experimental`, `Internal`, `Deferred`, `Development Tooling` and `Removed`. |
| `ADR-BL-004 — QA and Diagnostics Boundary` | Separates logging/facts/validators from QA UI. |
| `ADR-BL-005 — Dependency Policy` | Keeps core dependencies minimal; Cinemachine is not core while CameraFlow is deferred. |

## F0B applied hygiene

```text
1. CameraFlow source removed from the core package.
2. com.unity.cinemachine removed from com.immersive.framework/package.json.
3. Unity.Cinemachine removed from Runtime asmdef references.
4. Optional/external project manifest entries for CameraFlow/Pooling/external timer utilities removed from this baseline package snapshot.
5. RouteContentRuntime family marked deferred and hidden from authoring menus.
6. ContentFlow source marked Experimental.
7. RouteContentProfile inspectors now show Planning-only / Deferred warnings.
8. FrameworkQaCanvas is development tooling only.
9. Editor namespace hygiene fixed from Immersive.Framework.Editor.Editor.* to Immersive.Framework.Editor.*.
10. Baseline smoke documentation added.
```

## What F0B does not implement

F0B does not implement:

```text
SessionContentSet
RouteContentRuntime connection
Route additive scene execution
Surface
RuntimeSpawned
Input
Save
Pause
Camera
Audio
Actor
Pooling
Projectile
Damage
Attributes
```

## F1A accepted ADRs

| ADR | Decision |
|---|---|
| `ADR-ID-001 — Typed Identity Policy` | New functional identities need explicit domains and typed wrappers. Strings remain valid for labels, source, reason and diagnostics. |
| `ADR-DIAG-001 — FrameworkFact vs Human Log` | Human logs and structured facts are separate. New validation/smoke should not parse log text as a functional contract. |
| `ADR-CONTENT-001 — Content Identity Domain` | Content identity is composed from owner, scope, kind and content id. Path/name alone are not stable public identity. |

## F1B API status convention

F1B adds a minimal source-level API status marker and applies it to the existing runtime surfaces.

```text
Runtime/ApiStatus/FrameworkApiStatus.cs
Runtime/ApiStatus/FrameworkApiStatusAttribute.cs
Documentation~/API_STATUS_CONVENTION.md
```

F1B does not create FrameworkFact, typed identity primitives, ContentIdentity final shape or ValidationMode semantics.

F1B was closed after compile-smoke validation. See:

```text
Documentation~/F1B_CLOSURE.md
```

## F1C FrameworkFact minimal model

F1C is closed after compile-smoke validation and introduces the minimal structured diagnostics model required by `ADR-DIAG-001`:

```text
Runtime/Diagnostics/FrameworkFact.cs
Runtime/Diagnostics/FrameworkFactCode.cs
Runtime/Diagnostics/FrameworkFactScope.cs
Runtime/Diagnostics/FrameworkFactSeverity.cs
Documentation~/FRAMEWORK_FACT_MINIMAL_MODEL.md
```

F1C does not create a fact recorder, service locator, event bus, telemetry, dashboard, persistence layer, validator integration or lifecycle behavior change.

See:

```text
Documentation~/F1C_CLOSURE.md
```

## F1D ValidationMode semantics

F1D is closed after compile-smoke validation and gives `ValidationMode` concrete minimum semantics:

```text
Strict   — required configuration fails; warnings are promoted to errors; info diagnostics are included.
Standard — required configuration fails; warnings remain warnings; info diagnostics are included.
Release  — required configuration fails; warnings remain warnings; info diagnostics are suppressed.
```

See:

```text
Documentation~/VALIDATION_MODE_SEMANTICS.md
```

F1D does not change Game Flow, Route Lifecycle, Activity Flow or Scene Lifecycle behavior.

See:

```text
Documentation~/F1D_CLOSURE.md
```

## F1E Typed identity primitives

F1E introduces minimal identity primitives required by `ADR-ID-001`:

```text
Runtime/Identity/FrameworkIdentityDomain.cs
Runtime/Identity/FrameworkIdentityValue.cs
Runtime/Identity/FrameworkIdentityKey.cs
Runtime/Identity/IFrameworkIdentity.cs
Documentation~/TYPED_IDENTITY_PRIMITIVES.md
```

F1E does not migrate existing serialized strings, does not create domain-specific `RouteId`/`ActivityId` types yet and does not alter lifecycle behavior.

F1E was closed after compile-smoke validation. See:

```text
Documentation~/F1E_CLOSURE.md
```

## F1F Content identity / FrameworkContentHandle review

F1F applies the content identity ADR to the existing `FrameworkContentHandle` without advancing SessionContentSet, Surface or RuntimeMaterialization.

New minimal content identity primitives:

```text
Runtime/ContentFlow/FrameworkContentId.cs
Runtime/ContentFlow/FrameworkContentIdentity.cs
Documentation~/CONTENT_IDENTITY_AND_HANDLE_REVIEW.md
```

`FrameworkContentHandle` now exposes a composed `Identity` and `OwnerIdentity`. The previous `Guid.NewGuid()` fallback in `RoutePrimaryScene` was removed; required content identity must be deterministic or fail visibly.

F1F was closed after compile-smoke validation. See:

```text
Documentation~/F1F_CLOSURE.md
```

## F1 closure

F1 is closed after F1F smoke validation.

F1 established the minimum baseline for:

```text
API status markers
FrameworkFact minimal model
ValidationMode semantics
typed identity primitives
content identity for FrameworkContentHandle
ADR naming aligned to roadmap order
```

See:

```text
Documentation~/F1_CLOSURE.md
```

## F2 closure

```text
F2A — CLOSED / ADRS ACCEPTED
F2B — CLOSED / COMPILE-SMOKE PASS
F2C — CLOSED / COMPILE-SMOKE PASS
F2D — CLOSED / DOCUMENTATION ONLY
F2  — CLOSED / PASS
```

F2 is closed. Do not start Surface, RuntimeMaterialization or consumers before their roadmap phases. The next authorized step is:

```text
F3A — Route baseline ADR review and acceptance
```
