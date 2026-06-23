# Route Content Anchor Authoring

Status: F7D closed / PASS; F7H authoring validation closed / PASS  
Package: `com.immersive.framework`

---

## Purpose

`RouteContentAnchor` is the first public authoring component for Route-scoped Content Anchors.

It declares a passive named point inside loaded Route content. F7F discovery and F7H authoring validation can inspect it; the component itself does not register, materialize, bind, move or instantiate runtime content.

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

## Validation

F7H adds authoring validation for loaded `RouteContentAnchor` components. It reports:

```text
missing Route
missing Anchor Id
Kind = Unknown
invalid Requiredness
scene not declared by assigned Route
duplicate Content Anchor identity
duplicate owner + scope + Anchor Id
```

Run validation from the QA Canvas with:

```text
Validate Loaded Authoring
```

F7H does not enforce Required anchors in Route lifecycle and does not add runtime binding, placement or consumers.

## F7F/F7G

F7F discovers valid `RouteContentAnchor` components from loaded Route scenes into a local diagnostic `ContentAnchorSet`. F7G adds the dedicated Content Anchor diagnostics smoke.
