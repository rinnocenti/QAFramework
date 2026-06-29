# F28F — Next Implementation Closeout

## Status

Closed / documentation-only / selects next code phase

## Purpose

F28F closes F28 by selecting the first post-reconciliation implementation phase and converting the F28A-F28E planning chain into concrete entry criteria, file placement rules and smoke evidence.

F28F does not implement runtime code. It decides what the next implementation is allowed to implement.

## Inputs

F28F starts from the accepted F28 sequence:

```text
F28A frozen baseline reconciliation
  -> F28B completion dependency map
  -> F28C adapter module taxonomy
  -> F28D Player / Actor / Unity Input ownership plan
  -> F28E InputMode and Pause integration plan
```

The selected implementation must preserve these decisions:

```text
InputMode is typed framework language, not a Unity action-map string.
Pause may request InputMode later, but does not own PlayerInput.
Unity Input target ownership must be explicit before action-map behavior is applied.
Player / Actor runtime spawning is deferred until runtime roots, RuntimeContentHandle and release policy are available.
Camera, Audio, Save, RuntimeSpawned and Gameplay adapters are separate lanes.
```

## Selected Next Phase

F28F selects:

```text
F29 — Unity Input Target Ownership Proof
```

F29 is the first code phase after F28.

The first proof is:

```text
QA-authored Unity input target proof
```

This is selected over a pure core `InputMode` model because the current missing boundary is not the name of a mode. The missing boundary is explicit ownership of Unity Input targets before any mode or Pause integration applies behavior to those targets.

## F29 Purpose

F29 proves that Unity Input targets are explicit, scoped and diagnosable.

F29 should establish enough vocabulary and Unity-facing evidence to answer:

```text
which object is the global UI / Pause intent input target;
which object is the gameplay command input target;
what happens when a required target is missing;
what happens when a target is duplicated;
whether PauseToggle/global UI input remains independent from gameplay command input;
where Unity Input System details live;
where framework typed language begins.
```

## F29 Placement Rules

| Concern | Placement |
|---|---|
| Framework target identity / result language | `Packages/com.immersive.framework` |
| Unity Input System authored adapter or target declaration | `Packages/com.immersive.framework` only if it remains a generic Unity adapter; otherwise separate optional package later |
| QA scene/prefab/input target evidence | `Assets/ImmersiveFrameworkQA` |
| Project-specific `InputActionAsset`, player prefab, controller or art | `Assets/_Project` |
| Experiments not part of canonical proof | `Assets/_Sandbox` |

F29 must not place product player prefabs, concrete game controllers or personal assets inside the framework package.

## F29 First Code Cut

The first F29 code cut should be:

```text
F29A — Unity Input Target Declaration Proof
```

F29A may introduce a minimal target declaration and diagnostics surface.

F29A must not implement full InputMode, action-map switching, player movement, player actor spawning, camera follow, audio, save, gameplay adapters or per-consumer Gate checks.

## Required Smoke Target

F29 must include one manual QA smoke target named conceptually:

```text
Unity Input Target Ownership Smoke
```

The smoke must prove:

| Step | Expected result |
|---|---|
| Valid target set | Exactly one required global UI / Pause intent target and one required gameplay command target can be resolved. |
| Missing required target | Blocking diagnostic is produced. |
| Duplicate target | Blocking diagnostic is produced. |
| Pause/global path independence | PauseToggle/global UI target remains conceptually independent from gameplay command target. |
| No behavior switching | No action-map switching, player spawning or gameplay adapter behavior is performed by this smoke. |

## F29 Entry Criteria

Before implementing F29A, confirm:

```text
F28A-F28F documentation is applied.
F27E remains cancelled.
F27B narrow PauseToggle adapter still works as existing evidence.
FrameworkStringExtensions remains the canonical text normalization helper for new diagnostic strings.
No new diagnostic helper duplicates existing Runtime/Common utilities.
```

## F29 Stop Conditions

Stop the cut if the implementation starts doing any of these:

```text
creating Player/Actor runtime lifecycle;
spawning or releasing player objects;
introducing movement controllers;
changing Time.timeScale;
switching Unity action maps as the primary proof;
making Pause own PlayerInput;
making ordinary gameplay consumers query Gate to ignore paused input;
adding Camera, Audio, Save, RuntimeSpawned or Gameplay module behavior.
```

These are valid future concerns, but they are not the first proof.

## Handoff

After F28F, the next implementation prompt should request:

```text
Implement F29A — Unity Input Target Declaration Proof.
Use the current package and Assets source boundary.
Create only the minimal target declaration, diagnostics/result model and QA smoke needed to prove explicit Unity Input target ownership.
Do not implement InputMode action-map behavior yet.
```

## Files Changed

Documentation only.

No runtime code, asmdef, QA scene, prefab, ScriptableObject or InputActionAsset changed in F28F.
