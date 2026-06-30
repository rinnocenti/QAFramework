# F10E — Pause Visual Materialization Proof

Status: Closed / PASS

## Purpose

F10E proves that the Pause visual surface selected by F10A and authored by F10B can be explicitly materialized through the RuntimeContent + ContentAnchor chain closed in F9R.

This is a capability proof, not a final product decision that Pause must be spawned every time.

## Why this proof still matters

The proof answers a framework capability question:

```text
Can a Pause visual surface be created as RuntimeContent,
bound to a ContentAnchor,
placed physically under the anchor Transform,
and cleaned up without leaving handles or bindings behind?
```

The answer validated by smoke is yes.

## Important design clarification

A visual Pause menu does not necessarily need to be runtime-materialized.

For a normal game, a resident Pause menu inside the canonical `UIGlobal` scene is often the simpler and better product path:

```text
UIGlobal scene
  -> resident Pause panel / canvas already exists
  -> logical Pause asks it to show/hide
```

The materialized path remains useful for cases where the Pause visual is not resident:

```text
- optional modular UI package;
- route-specific pause overlay;
- activity-specific presentation variant;
- streamed/DLC UI;
- debug/QA proof of RuntimeContent + ContentAnchor consumer behavior;
- future consumers that really are spawned content.
```

So F10E proves that Pause can use the materialization chain. It does not decide that Pause should use that chain as the canonical product UX.

## What changed

Runtime additions:

- `PauseVisualSurfaceMaterializationStatus`
- `PauseVisualSurfaceMaterializationResult`
- `PauseVisualSurfaceMaterializationExecutor`

QA addition:

- `Run Pause Visual Materialization Smoke`

Guide addition:

- `Packages/com.immersive.framework/Documentation~/Guides/F10E-Pause-Visual-Materialization-Usage.md`

## Execution chain

```text
PauseVisualSurfaceContract
  -> PauseVisualSurfaceBindingRequestFactory
  -> ContentAnchorBindingRequest
  -> UnityContentAnchorMaterializationPipeline
  -> Unity prefab materialization
  -> RuntimeContent materialized handle
  -> ContentAnchor binding
  -> physical placement under anchor Transform
```

## Smoke evidence

Validated terminal log shape:

```text
QA Pause Visual Materialization Smoke step completed.
step='pause-visual-materialization'
passed='True'
materialization='SucceededMaterialized'
pipeline='Succeeded'
binding='Succeeded'
materialized='Succeeded'
appliedMaterialization='Succeeded'
runtimeHandleMaterialized='True'
physicalEvidenceRecorded='True'
physicalPlacementApplied='True'
visualInstanceParented='True'
visualMaterialization='True'
materializationExplicit='True'
smokeCleanup='Succeeded'
smokeCleanupPhysicalRelease='True'
smokeCleanupLogicalRuntimeContentRelease='True'
smokeCleanupContentAnchorBindingCleanup='True'
```

## Guardrails

F10E does not implement:

- Pause toggle integration;
- InputMode changes;
- PlayerInput changes;
- `Time.timeScale` policy;
- Route/Activity auto-materialization;
- Route/Activity auto-release;
- lifecycle exit wiring;
- camera, audio, save, actor, pooling, PlayerJoin or gameplay/F34 consumers.

The smoke cleanup is explicit QA cleanup, not automatic lifecycle behavior.

## Decision follow-up

`F10F — Pause Presentation Model Decision` is selected as the next docs-only cut.

F10F must decide whether the canonical product path for Pause visual presentation is:

```text
Resident UIGlobal surface
```

or

```text
Runtime-materialized Pause visual surface
```

The current recommendation is to prefer resident `UIGlobal` for the canonical Pause menu, while keeping F10E as a valid materialized/optional path.
