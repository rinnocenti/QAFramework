# Content Anchor Authoring Validation

Status: `F7H — CLOSED / PASS`

F7H adds authoring-validation coverage for `RouteContentAnchor` without adding runtime binding, placement, required-anchor enforcement, Activity anchors or gameplay consumers.

## What is validated

Loaded scene-authored `RouteContentAnchor` components are checked for:

```text
missing Route
missing Anchor Id
Kind = Unknown
invalid Requiredness
authored scene not declared by the assigned Route
duplicate Content Anchor identity
duplicate owner + scope + Anchor Id
```

Duplicate validation uses the passive `ContentAnchorSet` model from F7E. Duplicate issues are reported as authoring errors, but they do not block Route lifecycle in F7H.

## Route scene ownership rule

A Route Content Anchor is considered scene-aligned when its GameObject is in:

```text
the Route Primary Scene
or an additional scene declared by the RouteContentProfileAsset
```

A mismatch is reported as a warning because discovery is scoped to scenes loaded by the active Route composition.

## QA Canvas

The visible QA button is now:

```text
Validate Loaded Authoring
```

It validates the loaded authoring surface currently used by the framework:

```text
RouteContentBinding
ActivityLocalVisibilityAdapter
RouteContentAnchor
LocalContributionSet
ContentAnchorSet duplicate diagnostics
```

Expected healthy log for the F7 baseline:

```text
QA Authoring Validation completed. scope='Loaded Authoring' routeBindings='1' activityAdapters='1' routeContentAnchors='1' issues='0' contentAnchors='1' contentAnchorIssues='0' contentAnchorDuplicateIdentity='0' contentAnchorDuplicateId='0'
```

## What F7H does not do

F7H does not add:

```text
required-anchor lifecycle blocking
ActivityContentAnchor
ContentAnchorRegistry
runtime placement/binding
RuntimeRootRegistry
materialização física runtime
Camera/Pause/UI/Actor consumers
```

F7H is authoring-validation only and closed/pass by smoke. It does not alter Route lifecycle behavior.
