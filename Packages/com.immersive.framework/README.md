# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current roadmap position

Current accepted phase:

```text
F0A — Baseline ADRs
```

Next technical phase:

```text
F0B — Baseline hygiene
```

F0A is a reconciliation phase. It does not add runtime behavior. Its purpose is to decide what the current package baseline officially means before new primitives are introduced.

## Active baseline decisions

| Area | F0A status | Consequence |
|---|---|---|
| Bootstrap, Game Application, Route, Activity and request triggers | `Experimental` | Usable for development, but identity/state semantics still need F1/F3/F4 refinement. |
| `ContentFlow` materializer/contribution vocabulary | `Experimental` | Preserved as vocabulary, not stable materialization API. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Additional scenes are not executed by the active baseline. |
| `RouteContentRuntime` | `Deferred` | Route local content callbacks are not canonical until F3 decides/connects them. |
| `CameraFlow` | `Deferred` | Camera is a future consumer; Cinemachine must not remain a mandatory core dependency after F0B. |
| `FrameworkQaCanvas` | `Development Tooling` | Manual smoke tool, not product API. |
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
```

## F0A accepted ADRs

| ADR | Decision |
|---|---|
| `ADR-BL-001 — Baseline Reconciliation` | Classifies current ambiguous surfaces and defines F0B cleanup targets. |
| `ADR-BL-002 — Core vs Consumers` | Prevents concrete consumers from owning lifecycle core. |
| `ADR-BL-003 — Public API Status Policy` | Defines `Stable`, `Experimental`, `Internal`, `Deferred`, `Development Tooling` and `Removed`. |
| `ADR-BL-004 — QA and Diagnostics Boundary` | Separates logging/facts/validators from QA UI. |
| `ADR-BL-005 — Dependency Policy` | Keeps core dependencies minimal; Cinemachine is not core while CameraFlow is deferred. |

## What F0A does not do

F0A does not implement:

```text
SessionContentSet
RouteContentRuntime connection
additive scenes
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

## Next cut: F0B

F0B should apply only the minimum hygiene needed to make code, package metadata and docs stop contradicting the accepted ADRs.

Expected F0B targets:

```text
1. Remove or freeze CameraFlow from the active core baseline.
2. Remove Cinemachine as mandatory core dependency unless an explicit optional split is created.
3. Mark RouteContentRuntime as deferred until F3 or remove misleading docs.
4. Mark RouteContentProfileAsset as planning-only in Inspector/docs.
5. Keep FrameworkQaCanvas as development tooling, not product API.
6. Add/update baseline smoke documentation.
```

Do not start F1 until F0B compiles and the baseline smoke still passes.
