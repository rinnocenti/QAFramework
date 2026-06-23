# Content Anchor Identity Primitives

Status: F7B closed / PASS  
Package: `com.immersive.framework`  
Scope: Passive identity primitives only

---

## Purpose

F7B introduces the minimal typed primitives required to describe a Content Anchor without creating authoring components, discovery, validators, registries or runtime consumers.

The cut is intentionally inert. It does not search scenes, bind objects, materialize prefabs or expose any global lookup surface.

---

## Added runtime primitives

| Primitive | Purpose |
|---|---|
| `ContentAnchorId` | Explicit stable authored id for a Content Anchor. |
| `ContentAnchorScope` | Owning lifecycle/authored scope: Route, Activity or Local. |
| `ContentAnchorKind` | Passive declaration intent: Root, Slot or Point. |
| `ContentAnchorRequiredness` | Authoring validation requirement: Optional or Required. |

---

## Identity rule

`ContentAnchorId` wraps `FrameworkIdentityValue` and reports `FrameworkIdentityDomain.ContentAnchor`.

The id must be explicit. It must not be inferred from:

- `GameObject.name`;
- hierarchy path;
- scene name;
- scene path;
- Unity instance id;
- global discovery order.

Recommended id examples:

```text
gameplay.world
player.spawn.primary
camera.default-target
ui.hud-root
pause.overlay-root
```

---

## Scope rule

F7B authorizes only:

```text
Route
Activity
Local
```

Do not add Session/global anchors in F7. If that becomes necessary, it must be covered by a separate ADR.

---

## Kind rule

The kind is descriptive only:

| Kind | Meaning |
|---|---|
| `Root` | Semantic content root/container. |
| `Slot` | Future placement or mount slot. |
| `Point` | Semantic reference/pose point that does not imply mounting. |

F7B does not create `ContentAnchorRoot`, `ContentAnchorSlot` or `ContentAnchorPoint` declaration models. Those were introduced later by F7C.

---

## Requiredness rule

`Optional` is the default enum value. Validators introduced later may treat `Required` anchors as blocking authoring issues when missing or duplicated according to the scoped duplicate policy.

F7B does not run validation.

---

## Explicit non-goals

F7B does not add:

```text
RouteContentAnchor
ActivityContentAnchor
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
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

## Follow-up

F7C introduced the passive declaration model. F7D introduced `RouteContentAnchor` authoring. F7E introduced the passive `ContentAnchorSet`. F7F/F7G are closed/pass; F7H authoring validation is closed/pass. F7 is closed as Content Anchor declaration baseline.
