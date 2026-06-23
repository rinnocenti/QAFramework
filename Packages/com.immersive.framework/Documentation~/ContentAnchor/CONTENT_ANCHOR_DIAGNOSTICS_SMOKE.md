# Content Anchor Diagnostics Smoke

Status: `F7G — CLOSED / PASS`.

This smoke validates the diagnostic-only Content Anchor discovery path introduced by F7F. It does not add validator rules, required-anchor blocking, Activity anchors, runtime binding, placement, RuntimeRoot materialization or gameplay consumers.

## QA Canvas button

Use:

```text
Run Content Anchor Diagnostics Smoke
```

The smoke requests the configured Content Anchor Route and validates the `ContentAnchorDiscoveryResult` plus the route-local `ContentAnchorSet`.

## Required setup

Configure the QA Canvas fields:

```text
Route Scene Composition Route = QA Canonical Route
Alternate Route = QA Alternate Route
Expected Content Anchor Count = 1
Expected Content Anchor Candidate Count = 1
Expected Content Anchor Issue Count = 0
Expected Content Anchor Invalid Count = 0
Expected Content Anchor Route Mismatch Count = 0
```

Optional exact expectations can be enabled by setting the fields to `0` or higher:

```text
Expected Content Anchor Required Count
Expected Content Anchor Optional Count
Expected Content Anchor Root Count
Expected Content Anchor Slot Count
Expected Content Anchor Point Count
```

A value below zero disables that exact check.

## Expected log

For one valid Route Content Anchor on the QA Canonical Route:

```text
QA Content Anchor Diagnostics Smoke step completed. step='anchors' route='QA Canonical Route' contentAnchors='1' contentAnchorCandidates='1' contentAnchorAccepted='1' contentAnchorIssues='0' contentAnchorInvalid='0' contentAnchorRouteMismatch='0'
QA Smoke completed. name='Content Anchor Diagnostics Smoke'.
```

## QA Canvas cleanup

F7G trims the visible QA buttons to the current validation path:

```text
Run Standard Smoke
Run Activity Baseline Smoke
Run Local Contribution Smoke
Validate Loaded Authoring
Reset QA Scenario
Run Route Scene Composition Smoke
Run Route Release Smoke
Run Content Anchor Diagnostics Smoke
```

Older edge/manual buttons remain as implementation helpers only if still referenced internally; they are no longer part of the default visible QA path.
