# F29A — Unity Input Target Declaration Proof

## Status

Closed / runtime + QA minimal / pending Unity compile-smoke by user

## Purpose

F29A starts the first code phase selected by F28F.

It proves that Unity Input targets can be declared explicitly and diagnosed before the framework introduces InputMode behavior, action-map switching, Player/Actor runtime ownership or gameplay commands.

## Implemented Scope

F29A adds a narrow framework package surface under:

```text
Packages/com.immersive.framework/Runtime/UnityInput
```

The surface contains:

| Artifact | Purpose |
|---|---|
| `UnityInputTargetRole` | Declares the first two target lanes: `GlobalUiPause` and `GameplayCommands`. |
| `UnityInputTargetId` | Stable typed identity for an input target. |
| `UnityInputTargetDescriptor` | Passive declaration snapshot. |
| `UnityInputTargetDeclaration` | Unity-facing component for authored target declarations. |
| `UnityInputTargetSet` | Immutable validation snapshot. |
| `UnityInputTargetSetIssue` / `UnityInputTargetSetIssueKind` | Blocking diagnostics for missing, duplicate or invalid declarations. |
| `UnityInputTargetValidator` | Loaded-scene / explicit declaration validator. |

It also adds:

```text
Packages/com.immersive.framework/Runtime/Diagnostics/UnityInputTargetOwnershipQaSmokeRunner.cs
```

and exposes the smoke through:

```text
Framework QA Canvas > Phase Diagnostics > Show Unity Input diagnostics > Run Unity Input Target Ownership Smoke
```

## Accepted Roles

| Role | Meaning |
|---|---|
| `GlobalUiPause` | Global UI and Pause intent target. Keeps Pause/UI path conceptually separate from gameplay input. |
| `GameplayCommands` | Future gameplay command target. It may drive gameplay only after InputMode and Activity ownership exist. |

## Smoke Contract

Expected smoke name:

```text
Unity Input Target Ownership Smoke
```

Expected steps:

```text
contracts
valid-target-set
missing-required-target
duplicate-target
global-ui-and-gameplay-target-split
no-action-map-switching
declaration-component
```

The smoke is synthetic and declaration-only. It validates:

- valid target set succeeds;
- missing `GameplayCommands` target reports one blocking issue;
- duplicated `GlobalUiPause` role reports one blocking issue;
- `GlobalUiPause` and `GameplayCommands` are separate roles and separate identities;
- the proof does not apply input behavior and does not switch action maps;
- the Unity-facing declaration component can produce descriptors.

## Non-Goals

F29A does not implement:

```text
InputMode runtime
action-map switching
PlayerInput ownership
player movement
player/actor spawning
camera/audio/save/gameplay adapters
per-consumer Gate checks
Time.timeScale policy
```

## Ownership Boundary

F29A establishes only target declaration ownership.

```text
Framework Core:
  typed role/id/result/issue vocabulary

Unity Adapter Surface:
  generic target declaration component
  target validator

QA Evidence:
  synthetic smoke and QA Canvas button

Project Assets:
  still owns product player prefab, concrete controller and concrete InputActionAsset policy
```

## Next Cut

F29B can add a QA authoring fixture only if manual Unity validation needs scene/prefab evidence.

F29C closes the target proof and decides whether the next runtime phase is:

```text
F30 — InputMode Identity and Request Result Model
```

or a narrower Unity Input action-map adapter proof.
