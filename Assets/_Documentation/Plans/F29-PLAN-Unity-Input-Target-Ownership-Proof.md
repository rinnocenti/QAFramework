# F29 Plan — Unity Input Target Ownership Proof

## Status

Closed / F29A PASS / F29B PASS / F29C closed

## Purpose

F29 is the first code phase after the F28 roadmap reconciliation.

It proves explicit Unity Input target ownership before the framework implements InputMode behavior or Pause-driven action-map changes.

F29 answers:

```text
where Unity Input System targets are declared;
which targets are required first;
how missing/duplicate targets are diagnosed;
how the global UI / Pause intent path stays independent from gameplay command input;
where future InputMode behavior may safely attach.
```

## Starting Point

F28 selected F29 as the next implementation phase.

Relevant closed planning notes:

```text
Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md
Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md
Assets/_Documentation/Notes/F28F-Next-Implementation-Closeout.md
```

## Scope

F29 owns only the explicit target proof for Unity Input.

Accepted first target lanes:

| Lane | Meaning |
|---|---|
| Global UI / Pause intent target | Input target that keeps PauseToggle, Cancel and UI navigation conceptually available. |
| Gameplay command target | Input target that will later drive gameplay commands only when InputMode and Activity state allow it. |

F29 does not require the final production player prefab.

The first proof may use QA-authored objects and the current `Assets/InputSystem_Actions.inputactions` as test evidence, but game-specific player prefabs and production controllers remain project assets.

## Cut Sequence

| Cut | Name | Type | Output |
|---|---|---|---|
| F29A | Unity Input Target Declaration Proof | Runtime/QA minimal | Closed. Target declaration language, diagnostics/result surface and manual QA smoke for valid/missing/duplicate targets. |
| F29B | Input Target QA Authoring Fixture | QA scene + smoke | Closed. Canonical QA StartupScene target declarations and loaded-scene smoke validation. |
| F29C | Input Target Closeout | Docs/QA | Closed. Confirms target ownership evidence and selects F30 — InputMode Identity and Request Result Model. |

## F29A Closure

F29A adds the declaration proof under `Packages/com.immersive.framework/Runtime/UnityInput` and the QA smoke runner `UnityInputTargetOwnershipQaSmokeRunner`.

F29A keeps the implementation declaration-only. It proves target ownership diagnostics without reading input, switching action maps, creating InputMode runtime, spawning players/actors or moving project assets into the package.

Reference note:

```text
Assets/_Documentation/Notes/F29A-Unity-Input-Target-Declaration-Proof.md
```


## F29B Closure

F29B adds authored QA evidence in the canonical StartupScene. The scene now contains one `UnityInputTargetDeclaration` for `GlobalUiPause` and one for `GameplayCommands`.

The Unity Input Target Ownership Smoke now includes the `loaded-scene-fixture` step, which validates declarations found in loaded scenes.

Reference note:

```text
Assets/_Documentation/Notes/F29B-Input-Target-QA-Authoring-Fixture.md
```


## F29C Closure

F29C closes the phase after user smoke validation confirmed the loaded-scene fixture step passes with two targets, zero issues and zero blocking issues.

F29 selects F30 as the next implementation phase:

```text
F30 — InputMode Identity and Request Result Model
```

Reference note:

```text
Assets/_Documentation/Notes/F29C-Input-Target-Closeout.md
```

## F29A Rules

F29A may create:

```text
typed target role/id/result vocabulary;
minimal Unity-facing target declaration if needed;
diagnostics for missing/duplicate required targets;
manual QA smoke runner/button if consistent with existing QA style;
documentation note for the cut.
```

F29A must not create:

```text
full InputMode runtime;
Unity action-map switching;
player movement;
player/actor spawning;
RuntimeContentHandle-based actor lifetime;
camera/audio/save/gameplay adapters;
per-consumer Gate query policy.
```

## Expected Smoke

Manual smoke concept:

```text
Unity Input Target Ownership Smoke
```

Expected steps:

```text
valid-target-set
missing-required-target
duplicate-target
global-ui-and-gameplay-target-split
no-action-map-switching
loaded-scene-fixture
```

Expected result:

```text
pass when explicit targets are resolvable and invalid configurations produce blocking diagnostics without applying input behavior.
```

## Placement

| Artifact | Placement |
|---|---|
| Framework target identity/result language | `Packages/com.immersive.framework/Runtime/...` |
| Generic Unity Input target declaration | `Packages/com.immersive.framework/Runtime/...` only if generic and package-safe |
| QA smoke runner | `Packages/com.immersive.framework/Runtime/Diagnostics` if it follows existing QA runner pattern |
| QA scene/prefab/object evidence | `Assets/ImmersiveFrameworkQA` |
| Project player prefab/controller/assets | `Assets/_Project` |

## Closeout Criteria

F29 is closed because:

```text
valid input target ownership is visible in QA;
missing target and duplicate target cases are diagnosable;
Pause/global input path and gameplay command target are separated by declared role;
no action-map behavior is hidden inside the proof;
next phase is selected explicitly.
```

Selected next phase after F29:

```text
F30 — InputMode Identity and Request Result Model
```

F30 may start with F30A because F29 target ownership is proven by synthetic and authored QA smoke evidence.
