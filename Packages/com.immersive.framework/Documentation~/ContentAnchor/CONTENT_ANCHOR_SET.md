# Content Anchor Set

Status: F7E closed / PASS  
Package: `com.immersive.framework`

---

## Purpose

`ContentAnchorSet` is a passive immutable collection of `ContentAnchorDeclaration` items.

It exists so later F7 cuts can place discovered Route or Activity anchors into a scoped result without inventing registry, lifecycle binding or materialization rules in the same step.

---

## Added types

```text
ContentAnchorSet
ContentAnchorSetIssue
ContentAnchorSetIssueKind
```

---

## What the set records

A `ContentAnchorSet` stores unique valid declarations and exposes counts for:

```text
total
Route / Activity / Local
Root / Slot / Point
Required / Optional
issues
duplicate identity
duplicate anchor id
invalid declaration
```

The set also supports passive lookup/filter helpers:

```text
TryGetByIdentity
TryGetByAnchorId
GetByScope
GetByKind
GetByRequiredness
Contains
```

---

## Duplicate semantics

F7E detects duplicates locally while building the set.

Two duplicate modes are recorded:

| Issue | Meaning |
|---|---|
| `DuplicateIdentity` | Same full declaration identity already exists. |
| `DuplicateAnchorId` | Same owner + scope + anchor id already exists, even if kind differs. |

Duplicate declarations are not added to the set. They are recorded as `ContentAnchorSetIssue` diagnostics.

---

## Invalid declarations

Invalid declarations are not added to the set.

They produce:

```text
ContentAnchorSetIssueKind.InvalidDeclaration
```

This is still a local set diagnostic, not a global authoring validator result.

---

## Not in F7E

F7E intentionally does not add:

```text
RouteContentAnchor discovery
ActivityContentAnchor
FrameworkAuthoringValidator rules
RouteLifecycleRuntime integration
ContentAnchorRegistry
Content Anchor smoke
Runtime binding
RuntimeRootRegistry
Materialização física runtime
Camera/Pause/UI/Actor consumers
```

---

## Validation

F7E closed by compile/regression smoke:

```text
Unity compile
Standard Smoke
Route Scene Composition Smoke
Route Release Smoke
Local Contribution Smoke
Authoring Validation
```

Dedicated Content Anchor smoke starts after loaded Route discovery exists.


## F7F — Route Content Anchor discovery

F7F discovers valid `RouteContentAnchor` components from loaded Route scenes into a local diagnostic `ContentAnchorSet`. It is discovery-only and does not create validators, required-anchor blocking, runtime binding, placement or consumers.
