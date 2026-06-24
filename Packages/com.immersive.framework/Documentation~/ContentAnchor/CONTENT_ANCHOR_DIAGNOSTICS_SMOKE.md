# Content Anchor Diagnostics Smoke

Status: `F9J — CLOSED / LOGICAL CONTENT ANCHOR BINDING PASS`.

This smoke validates the diagnostic-only Content Anchor discovery path introduced by F7F. It does not add validator rules, required-anchor blocking, runtime binding, placement, RuntimeRoot materialization or gameplay consumers.

## QA Canvas button

Use:

```text
Run Content Anchor Diagnostics Smoke
Run Activity Content Anchor Diagnostics Smoke
Run Activity Content Anchor Positive Smoke
Run Activity Content Anchor Binding Smoke
```

The Route smoke requests the configured Content Anchor Route and validates the `ContentAnchorDiscoveryResult` plus the route-local `ContentAnchorSet`. The Activity diagnostics smoke requests the configured Route, lets the startup Activity run and validates `ActivityContentAnchorDiscoveryResult`; it can pass with zero Activity anchors. The Activity positive smoke creates one temporary QA-only `ActivityContentAnchor`, re-enters the configured Activity and validates that the anchor is accepted by discovery. The Activity binding smoke creates the same kind of temporary Activity anchor, binds synthetic Activity-scoped runtime content to it, validates idempotency, releases the handle logically and verifies Activity exit cleanup removes the binding.

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
Run Activity Content Anchor Diagnostics Smoke
Run Activity Content Anchor Positive Smoke
Run Activity Content Anchor Binding Smoke
```

Older edge/manual buttons remain as implementation helpers only if still referenced internally; they are no longer part of the default visible QA path.


## Expected Activity positive-path log

For the F9H QA fixture path:

```text
QA Activity Content Anchor Positive Smoke step completed. step='activity-anchor-positive' activity='QA Primary Content Activity' activityContentAnchors='1' activityContentAnchorCandidates='1' activityContentAnchorAccepted='1' activityContentAnchorIssues='0' activityContentAnchorInvalid='0' activityContentAnchorActivityMismatch='0'
QA Smoke completed. name='Activity Content Anchor Positive Smoke'.
```

This smoke does not create placement, instantiate prefabs, bind consumers, or create physical runtime roots.


## Expected Activity binding log

For the F9I QA fixture path:

```text
QA Activity Content Anchor Binding Smoke step completed. step='activity-anchor-binding' activity='QA Primary Content Activity' binding='Succeeded' idempotentBinding='SucceededAlreadyBound' release='Succeeded' releaseUnregistered='True' activityCleanup='Succeeded' activityCleanupRemoved='1' bindingCount='0'
QA Smoke completed. name='Activity Content Anchor Binding Smoke'.
```

This smoke does not create placement, instantiate prefabs, bind gameplay consumers, or create physical runtime roots.
