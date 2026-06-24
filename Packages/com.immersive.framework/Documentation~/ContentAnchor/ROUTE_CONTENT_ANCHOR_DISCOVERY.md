# Route Content Anchor Discovery

Status: `F7F — CLOSED / PASS`

`RouteContentAnchor` is now discovered after Route scene composition has loaded the Route scenes.

This is diagnostic discovery only. It does not validate required anchors, expose a registry, bind runtime content, instantiate prefabs, move objects, resolve consumers or block lifecycle.

## Runtime order

```text
Route request/startup
  -> Route scene composition
      -> Primary Scene / Single
      -> Additional Route Scenes / Additive
  -> Route Content Set
  -> Route Content Anchor discovery
  -> Route content enter callbacks
  -> Startup Activity
```

Discovery scans the loaded scenes reported by `RouteSceneCompositionResult` and looks for scene-authored `RouteContentAnchor` components.

## Accepted anchors

An anchor is accepted when:

```text
RouteContentAnchor.Route == active Route
Anchor Id is explicit
Kind is not Unknown
TryCreateDeclaration succeeds
```

Accepted anchors are converted to `ContentAnchorDeclaration` and collected into a local `ContentAnchorSet`.

## Diagnostics

Boot and route request logs include Content Anchor counters:

```text
contentAnchors
contentAnchorCandidates
contentAnchorIssues
contentAnchorInvalid
contentAnchorRouteMismatch
```

Expected baseline with no anchors authored:

```text
contentAnchors='0'
contentAnchorCandidates='0'
contentAnchorIssues='0'
contentAnchorInvalid='0'
contentAnchorRouteMismatch='0'
```

Expected route with one valid `RouteContentAnchor`:

```text
contentAnchors='1'
contentAnchorCandidates='1'
contentAnchorIssues='0'
contentAnchorInvalid='0'
contentAnchorRouteMismatch='0'
```

## What this cut does not do

F7F does not add:

```text
Activity-scoped discovery in this Route-only path
FrameworkAuthoringValidator rules
required anchor blocking
ContentAnchorRegistry
runtime placement/binding
RuntimeRootRegistry
materialização física runtime
Camera/Pause/UI/Actor consumers
```

Requiredness is recorded in the set but is not enforced by this cut.

## F7G diagnostics smoke

F7G adds `Run Content Anchor Diagnostics Smoke` to validate the discovery result and route-local `ContentAnchorSet` counts without adding runtime binding. F9G adds a separate Activity Content Anchor discovery path and smoke; it does not change this Route-only discovery contract.
