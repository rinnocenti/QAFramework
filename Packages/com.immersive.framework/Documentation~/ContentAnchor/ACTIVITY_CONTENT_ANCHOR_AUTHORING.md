# Activity Content Anchor Authoring

Status: `F9G — APPLIED / PENDING SMOKE`
Package: `com.immersive.framework`

---

## Purpose

`ActivityContentAnchor` is the Activity-scoped companion to `RouteContentAnchor`.

It declares a passive named anchor owned by one `ActivityAsset`. The component is authoring and diagnostics only. It does not register runtime content, bind content, move transforms, instantiate prefabs, unload scenes, use pooling or create placement behavior.

---

## Component

Add the component from:

```text
Immersive Framework > Activity Content Anchor
```

Fields:

| Field | Meaning |
|---|---|
| `Activity` | Explicit Activity owner. |
| `Anchor Id` | Stable authored id. GameObject names and hierarchy paths are diagnostics only. |
| `Kind` | `Root`, `Slot` or `Point`. |
| `Requiredness` | `Optional` or `Required` future validation policy. |
| `Display Name` | Optional diagnostic label. Not identity. |
| `Description` | Optional authoring note. No runtime behavior. |

Recommended ids:

```text
activity.root
activity.spawn.primary
activity.camera.target
activity.ui.slot
activity.interaction.point
```

---

## Declaration

A valid `ActivityContentAnchor` can produce a local `ContentAnchorDeclaration` with:

```text
Scope: Activity
Owner: Activity owner identity
Kind: Root / Slot / Point
AnchorId: explicit Anchor Id
Requiredness: Optional / Required
ResourceName: GameObject name for diagnostics only
ResourcePath: scene path + hierarchy path for diagnostics only
```

The stable identity is based on explicit Activity owner and Anchor Id, not on scene search order.

---

## Validation

F9G adds authoring validation for loaded `ActivityContentAnchor` components. It reports:

```text
missing Activity
missing Anchor Id
Kind = Unknown
invalid Requiredness
duplicate Content Anchor identity
duplicate owner + scope + Anchor Id
```

Run validation from the QA Canvas with:

```text
Validate Loaded Authoring
```

F9G does not enforce Required anchors in Activity lifecycle and does not add runtime placement or consumers.
