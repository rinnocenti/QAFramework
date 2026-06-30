# IF-FW-F9R-T — QA Canvas Smoke Button Cleanup

Status: Closed / PASS.

## Intent

Reduce `FrameworkQaCanvas` to the smoke buttons that are still useful after the F9R materialization/release hardening sequence.

This cut removes obsolete, intermediate and superseded QA buttons from the visible panel instead of only hiding them behind foldouts.

## Scope

Runtime tooling only:

- `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`

No framework lifecycle behavior is changed.

## Buttons retained

### Core

- `Run Standard Smoke`
- `Run Activity Baseline Smoke`
- `Validate Loaded Authoring`
- `Reset QA Scenario`

### Route / Content

- `Run Route Scene Composition Smoke`
- `Run Route Release Smoke`
- `Run Content Anchor Diagnostics Smoke`
- `Run Activity Content Anchor Diagnostics Smoke`
- `Run Activity Content Execution Participant Source Smoke`

### Foundation / F9R

- `Run Local Contribution Smoke`
- `Run Runtime Content Smoke`
- `Run Content Anchor Materialization Diagnostics Snapshot Smoke`
- `Run Content Anchor Materialization Bridge Set Rollback Smoke`
- `Run Composite Lifecycle Release Smoke`

### Advanced/manual controls

- Manual route requests.
- Manual activity requests.
- Clear active activity.
- Baseline reset diagnostics labels.

## Removed from the visible QA panel

Removed diagnostic families:

- Gate / Transition / Effect diagnostics.
- Pause diagnostics.
- Unity Input diagnostics.
- Save / Snapshot diagnostics.
- Loading diagnostics.
- Reset / Object diagnostics.

Removed optional/edge smoke panel:

- No-Activity Route Smoke.
- No-Content Activity Smoke.
- Negative Smoke.

Removed F9R intermediate buttons superseded by later terminal proofs:

- Lifecycle Materialization Registry Contract Smoke.
- Bridge Lifecycle Registry Registration Smoke.
- Lifecycle Registry Release Plan Smoke.
- Lifecycle Registry Release Execution Smoke.
- Runtime Prefab Materialization Smoke.
- Content Anchor Physical Placement Smoke.
- Content Anchor Materialization Pipeline Smoke.
- Content Anchor Materialization Scope Release Smoke.
- Content Anchor Materialization Bridge Smoke.
- Content Anchor Materialization Bridge Set Smoke.
- Content Anchor Materialization Bridge Set Preflight Smoke.
- Content Anchor Materialization Authoring Validation Smoke.
- Content Anchor Materialization Runtime Authoring Gate Smoke.

Removed route/content intermediate buttons superseded by later diagnostics or composite release evidence:

- Activity Content Anchor Positive Smoke.
- Activity Content Anchor Binding Smoke.
- Activity Content Execution Runtime Smoke.
- Activity Content Execution Lifecycle Transition Smoke.
- Content Anchor Binding Smoke.
- Content Anchor Binding Cleanup Smoke.

## Non-goals

This cut does not implement:

- Route/Activity auto-release.
- Route/Activity auto-materialization.
- Lifecycle exit wiring.
- Pause, Camera, Audio, Save, Actor, Pooling, PlayerJoin, F34 or gameplay consumers.
- New runtime contracts.
- New editor tooling.
- New scenes, prefabs or assets.

## Validation

Validated after applying the patch:

1. Unity compile succeeded.
2. QA Canvas remained callable after removing obsolete/intermediate buttons.
3. `Run Standard Smoke` completed.
4. `Run Composite Lifecycle Release Smoke` completed.
5. Runtime lifecycle behavior remained unchanged.

Observed smoke evidence:

```text
QA Smoke completed. name='Standard Smoke'.
QA Composite Lifecycle Release Smoke step completed. step='unity-content-anchor-composite-lifecycle-release' passed='True' physicalRelease='True' logicalRuntimeContentRelease='True' contentAnchorBindingCleanup='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False'.
QA Smoke completed. name='Composite Lifecycle Release Smoke'.
```

## Outcome

`F9R-T` is closed. The QA surface was reduced without changing runtime lifecycle behavior or unlocking consumers.
