# Activity Content Anchor Discovery

Status: `F9G — APPLIED / PENDING SMOKE`

`ActivityContentAnchor` is discovered during Activity startup after the Activity content lifecycle has applied the active Activity state.

This is diagnostic discovery only. It does not validate required anchors as blocking, expose a gameplay registry, bind runtime content, instantiate prefabs, move objects, resolve consumers or block lifecycle.

---

## Runtime order

```text
Route request/startup
  -> Route scene composition
  -> Route Content Anchor discovery
  -> Route content enter callbacks
  -> Startup Activity / Activity request
      -> Activity runtime root
      -> Activity content lifecycle
      -> Activity Content Anchor discovery
      -> Activity binding cleanup for previous Activity
      -> previous Activity root removal
```

Discovery scans the active Route primary scene for scene-authored `ActivityContentAnchor` components and accepts only anchors whose `Activity` matches the active Activity.

---

## Accepted anchors

An anchor is accepted when:

```text
ActivityContentAnchor.Activity == active Activity
Anchor Id is explicit
Kind is not Unknown
TryCreateDeclaration succeeds
```

Accepted anchors are converted to `ContentAnchorDeclaration` and collected into a diagnostic `ContentAnchorSet` on the `ActivityFlowStartResult`.

---

## Diagnostics

Boot, Route request and Activity request logs include Activity Content Anchor counters:

```text
activityContentAnchors
activityContentAnchorCandidates
activityContentAnchorIssues
activityContentAnchorInvalid
activityContentAnchorActivityMismatch
```

Expected baseline with no Activity anchors authored:

```text
activityContentAnchors='0'
activityContentAnchorCandidates='0'
activityContentAnchorIssues='0'
activityContentAnchorInvalid='0'
activityContentAnchorActivityMismatch='0'
```

---

## QA smoke

F9G adds:

```text
Run Activity Content Anchor Diagnostics Smoke
```

The smoke requests the configured Route, lets the startup Activity run and validates the `ActivityContentAnchorDiscoveryResult` plus the Activity-local `ContentAnchorSet` counts.

This smoke can pass with zero Activity anchors. To validate a positive path, add one valid `ActivityContentAnchor` for the startup Activity and set the expected count/candidate fields accordingly.

---

## What this cut does not do

F9G does not add:

```text
Transform placement
physical GameObject runtime root
Instantiate
Destroy
Prefab adapter
Scene adapter
Addressables adapter
Pooling adapter
Actor/Pause/Camera/UI/Input/Save consumers
required-anchor blocking
```
