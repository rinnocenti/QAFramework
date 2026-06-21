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
F2C — APPLIED / PENDING COMPILE-SMOKE
```

Current authorized validation:

```text
F2C — compile-smoke
```

Status distinction:

```text
ADR status uses Accepted.
Cut/phase status uses Closed / PASS or Closed / Accepted.
```

F0 is closed. F1 is closed. F2A accepted the Session scope ADRs. F2 may now move to the first technical cut, but must not skip directly to Route, Surface, RuntimeMaterialization or consumers.

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

See:

```text
Documentation~/SESSION_CONTENT_SET_MINIMAL_MODEL.md
```

See:

```text
Documentation~/SESSION_RUNTIME_STATE_BOUNDARY.md
```

## Active baseline decisions

| Area | F0B status | Consequence |
|---|---|---|
| Bootstrap, Game Application, Route, Activity and request triggers | `Experimental` | Usable for development, but identity/state semantics still need F1/F3/F4 refinement. |
| `ContentFlow` materializer/contribution vocabulary | `Experimental` | Preserved as vocabulary, not stable materialization API. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Additional scenes are declared for planning/diagnostics only; they are not loaded by F0B. |
| `RouteContentRuntime` | `Deferred` | Route local callbacks are frozen and hidden from authoring menus until F3 decides the Route baseline. |
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

## Current F2 validation

```text
F2C — APPLIED / PENDING COMPILE-SMOKE
```

F2B is closed by compile-smoke. F2C is the current technical Session scope cut and must be validated with the standard boot/route/activity/clear smoke before opening the final F2 checkpoint.

Do not start F3, Surface, RuntimeMaterialization or consumers before F2 technical closure.
