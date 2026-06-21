# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current roadmap position

Current accepted phase:

```text
F0 — Closed / PASS
```

Next authorized step:

```text
F1A — ADR review and acceptance for API status, Identity and Diagnostics
```

F0A accepted the baseline ADRs. F0B applied the minimum hygiene required after those ADRs. F0C records that the F0 baseline smoke passed and no F0 blocker remains open.

## Active baseline decisions

| Area | F0B status | Consequence |
|---|---|---|
| Bootstrap, Game Application, Route, Activity and request triggers | `Experimental` | Usable for development, but identity/state semantics still need F1/F3/F4 refinement. |
| `ContentFlow` materializer/contribution vocabulary | `Experimental` | Preserved as vocabulary, not stable materialization API. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Additional scenes are declared for planning/diagnostics only; they are not loaded by F0B. |
| `RouteContentRuntime` | `Deferred` | Route local callbacks are frozen and hidden from authoring menus until F3 decides the Route baseline. |
| `CameraFlow` | `Removed from core baseline` | Camera is a future consumer. The core package no longer carries CameraFlow source or a mandatory Cinemachine dependency. |
| `FrameworkQaCanvas` | `Development Tooling` | Manual smoke tool compiled only in the Unity Editor or development builds. Not product API. |
| `ValidationMode` | `Experimental` | Concrete semantics are deferred to F1. |

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
```

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

## Next step: F1 ADR review

F1 must start with ADR review/acceptance before implementation. After the ADRs are accepted, F1 should introduce the first explicit API/identity/diagnostics foundation:

```text
1. Framework API status convention or code marker.
2. Typed identity policy.
3. FrameworkFact minimum.
4. ValidationMode concrete semantics.
5. Content identity ADR/review.
```

Do not start F1 implementation until the relevant F1 ADRs are accepted. Do not start F2 until F1 gives identity/status/diagnostics enough structure to prevent new public ambiguity.
