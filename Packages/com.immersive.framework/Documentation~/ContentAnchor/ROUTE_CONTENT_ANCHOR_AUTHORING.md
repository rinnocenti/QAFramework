# Route Content Anchor Authoring

Status: F7D closed / PASS; F7E applied / pending compile-smoke  
Package: `com.immersive.framework`

---

## Purpose

`RouteContentAnchor` is the first public authoring component for Route-scoped Content Anchors.

It declares a passive named point inside loaded Route content. It does not discover, validate, register, materialize, bind, move or instantiate runtime content.

---

## Component

Add the component from:

```text
Immersive Framework > Route Content Anchor
```

Fields:

| Field | Meaning |
|---|---|
| `Route` | Explicit Route owner. |
| `Anchor Id` | Stable authored id. GameObject names and hierarchy paths are diagnostics only. |
| `Kind` | `Root`, `Slot` or `Point`. |
| `Requiredness` | `Optional` or `Required` future validation policy. |
| `Display Name` | Optional diagnostic label. Not identity. |
| `Description` | Optional authoring note. No runtime behavior. |

Recommended ids:

```text
gameplay.world
player.spawn.primary
camera.default-target
ui.hud-root
pause.overlay-root
```

---

## Declaration

A valid `RouteContentAnchor` can produce a local `ContentAnchorDeclaration` with:

```text
Scope: Route
Owner: Route owner identity
Kind: Root / Slot / Point
AnchorId: explicit Anchor Id
Requiredness: Optional / Required
ResourceName: GameObject name for diagnostics only
ResourcePath: scene path + hierarchy path for diagnostics only
```

The stable identity is based on explicit Route owner and Anchor Id, not on scene search order.

---

## Not in F7D

F7D intentionally does not add:

```text
ContentAnchorSet
ContentAnchorRegistry
Loaded scene discovery
FrameworkAuthoringValidator rules
QA smoke
Runtime binding
RuntimeRootRegistry
Prefab materialization
Camera/Pause/UI/Actor consumers
```

---

## Validation

For F7D, validation is compile and regression smoke only:

```text
Unity compile
Standard Smoke
Route Scene Composition Smoke
Route Release Smoke
Local Contribution Smoke
Authoring Validation
```

A dedicated Content Anchor smoke starts only after `ContentAnchorSet` and loaded Route discovery exist.
