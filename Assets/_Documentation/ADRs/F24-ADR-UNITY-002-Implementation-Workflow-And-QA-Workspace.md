# F24 ADR UNITY 002 - Implementation Workflow and QA Workspace

## Status

Accepted

## Context

F24 moved the project from a synthetic core validation phase into a Unity-facing implementation phase.

The framework now needs concrete Unity materials, scenes, prefabs, assets and inspector surfaces for validation. At the same time, implementation must avoid polluting the baseline QA scenes or mixing project-specific configuration with reusable framework features.

The project also needs a clearer work mode for deciding when a cut should be handled directly and when it should be delegated to Codex with a coordination prompt.

## Decision

F24 adopts the following implementation workflow.

### 1. Codex delegation rule

Use Codex prompts only for:

- documentation cuts;
- complex cuts involving three or more coordinated modules;
- large migrations with many serialized references;
- changes where a scoped implementation plan is safer than direct incremental editing.

Handle directly in the current chat:

- simple cuts;
- primitive contracts;
- small authoring components;
- small assets/docs updates;
- focused analysis before implementation.

### 2. Unity Build Surface QA workspace

New Unity-facing features must receive isolated QA material before being mixed into the baseline QA scenes.

The preferred QA workspace is:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
  README.md
```

Initial validation should prefer one shared laboratory scene unless a feature needs strict scene isolation:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity
```

Feature-specific QA scenes may be added later, for example:

```text
TransitionSurfaceQA.unity
LoadingSurfaceQA.unity
PauseSurfaceQA.unity
```

### 3. Placement rule

Use these placement rules:

| Item type | Location |
|---|---|
| Product/game-specific configuration | `Assets/_Project` |
| Product/game-specific prefab/material/scene | `Assets/_Project` |
| Framework QA scene/material/prefab | `Assets/ImmersiveFrameworkQA` |
| Temporary experiment | `Assets/_Sandbox` |
| External/manual import | `Assets/_External` |
| Reusable framework primitive/contract | framework code area |
| Reusable advanced adapter | framework or adapter package area |

### 4. Framework isolation rule

A feature should enter the framework only when it is generic, reusable and not tied to one game configuration.

A singular configuration for one project or one authored test case must stay in `Assets/_Project` or `Assets/ImmersiveFrameworkQA`.

Adapter modules consume framework contracts. They must not redefine Route, Activity, Transition, Loading, Pause, Save, Runtime scope or Content Anchor lifecycle.

## Consequences

- F24A3 creates the Unity Build Surface QA workspace before Transition/Loading/Pause surfaces are implemented.
- Baseline QA scenes remain stable and should not absorb every new experiment.
- Future cuts must state whether they touch Framework Core / Contracts, Unity Build Surface, Adapter Modules, QA assets or project-specific assets.
- The first visual surfaces should be tested in the Unity Build Surface QA workspace.
- Documentation updates for small planning changes can be produced directly without Codex.

## Non-goals

This ADR does not implement:

- Transition runtime visuals;
- Loading screen;
- Pause overlay;
- Save backend;
- gameplay systems;
- player/camera/audio adapters;
- pooling;
- runtime materializers.

## Validation

This ADR is valid when:

- it exists under `Assets/_Documentation/ADRs`;
- `Assets/_Documentation/README.md` links to it;
- `F24-PLAN-Unity-Build-Surface.md` includes F24A3;
- no runtime or lifecycle code is changed by this documentation cut.
