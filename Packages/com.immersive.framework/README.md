# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current roadmap position

Closed phase:

```text
F0 — CLOSED / PASS
```

Current phase:

```text
F1 — OPEN
```

Completed F1 cuts:

```text
F1A — CLOSED / ACCEPTED
F1B — CLOSED / COMPILE-SMOKE PASS
```

Current F1 cuts pending validation:

```text
F1C — APPLIED / PENDING COMPILE-SMOKE
F1D — APPLIED / PENDING COMPILE-SMOKE
```

Next authorized step after F1C/F1D validation:

```text
F1E — Typed identity primitives mínimos
```

Status distinction:

```text
ADR status uses Accepted.
Cut/phase status uses Closed / PASS or Closed / Accepted.
```

F0 is closed. F1A accepted the ADRs for typed identity, structured diagnostics and content identity. Runtime implementation of F1 starts only after this ADR acceptance.

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
3. Documentation~/ADRs/F0A-baseline-adrs/
4. Documentation~/BASELINE_SMOKE.md
5. Documentation~/F0_CLOSURE.md
6. Documentation~/F1_ADR_ACCEPTANCE.md
7. Documentation~/API_STATUS_CONVENTION.md
8. Documentation~/F1B_CLOSURE.md
9. Documentation~/FRAMEWORK_FACT_MINIMAL_MODEL.md
10. Documentation~/VALIDATION_MODE_SEMANTICS.md
```

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

F1C introduces the minimal structured diagnostics model required by `ADR-DIAG-001`:

```text
Runtime/Diagnostics/FrameworkFact.cs
Runtime/Diagnostics/FrameworkFactCode.cs
Runtime/Diagnostics/FrameworkFactScope.cs
Runtime/Diagnostics/FrameworkFactSeverity.cs
Documentation~/FRAMEWORK_FACT_MINIMAL_MODEL.md
```

F1C does not create a fact recorder, service locator, event bus, telemetry, dashboard, persistence layer, validator integration or lifecycle behavior change.

## F1D ValidationMode semantics

F1D gives `ValidationMode` concrete minimum semantics:

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

Next authorized step after F1C/F1D compile-smoke validation:

```text
F1E — Typed identity primitives mínimos
```

Do not start F2 until F1 gives identity/status/diagnostics enough structure to prevent new public ambiguity.
