# Content Anchor Declaration Model

Status: F7C closed / PASS; F7D applied / pending compile-smoke  
Package: `com.immersive.framework`  
Scope: Passive declaration model only

---

## Purpose

F7C introduces the passive declaration model for Content Anchors. A declaration records an explicit anchor id, owner, scope, kind, requiredness and optional diagnostic text/resource metadata.

This cut does not create authoring components, scene discovery, validators, registries, sets, runtime binding or materialization.

---

## Added runtime declarations

| Type | Purpose | Side effects? |
|---|---|---|
| `ContentAnchorDeclaration` | Canonical passive record for one authored anchor. | No |
| `ContentAnchorRoot` | Typed wrapper for declarations whose kind is `Root`. | No |
| `ContentAnchorSlot` | Typed wrapper for declarations whose kind is `Slot`. | No |
| `ContentAnchorPoint` | Typed wrapper for declarations whose kind is `Point`. | No |

`ContentAnchorDeclaration` is the canonical data shape. The `Root`, `Slot` and `Point` wrappers are intent-specific views; they do not create GameObjects, Transforms, runtime roots or consumer bindings.

---

## Declaration fields

| Field | Meaning |
|---|---|
| `Owner` | Domain-qualified owner identity. |
| `Scope` | `Route`, `Activity` or `Local`. |
| `Kind` | `Root`, `Slot` or `Point`. |
| `AnchorId` | Explicit stable authored id. |
| `Requiredness` | `Optional` or `Required`. |
| `DisplayName` | Optional human-facing label. Not functional identity. |
| `Description` | Optional explanation for authoring/debugging. |
| `ResourceName` | Optional diagnostic resource name. |
| `ResourcePath` | Optional diagnostic resource path. |

Functional identity is not derived from display name, description, resource name or resource path.

---

## Stable identity

The declaration stable text is composed from:

```text
ContentAnchor:{Scope}:{Kind}:{Owner}:{AnchorId}
```

This keeps duplicate policy and future diagnostics scoped. It also prevents a generic global anchor registry from becoming the default model.

---

## Kind semantics

| Kind | Meaning | Explicit non-meaning |
|---|---|---|
| `Root` | Semantic content root/container. | Not a `RuntimeRootRegistry`. |
| `Slot` | Future placement/mount slot. | Does not instantiate prefabs. |
| `Point` | Semantic reference/pose point. | Does not bind camera, actor, input or pause systems. |

---

## Explicit non-goals

F7C does not add:

```text
RouteContentAnchor
ActivityContentAnchor
ContentAnchorSet
ContentAnchorRegistry
Content Anchor discovery
Content Anchor validators
smoke buttons
RuntimeRootRegistry
Prefab materialization
Runtime binding
Camera/Pause/UI/Actor consumers
```

---

## Next cut

```text
F7D — Route Content Anchor authoring
```

F7D introduced the first public Route-scope authoring component. F7E may introduce the scoped result model, but it should still avoid discovery, validators, runtime materialization and gameplay consumers.
