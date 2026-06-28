# IF-FW-F25I1 — Activity Operation Visual Mode Scope Correction

## Status

Implemented.

## Purpose

Correct the F25I/F25R visual-mode rule so `Seamless` can intentionally load/release Activity content without a transition curtain or canonical loading screen.

## Canonical rule

Visual mode controls presentation, not permission to execute Activity scene operations.

```text
Seamless
  Activity scene load/release may execute.
  TransitionSurface is skipped.
  LoadingSurface is skipped.

Fade
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is skipped.

FadeWithLoading
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is used when the operation has scene side-effects.
```

## Runtime changes

`ActivityOperationPlan` no longer adds blocking issues for:

```text
Seamless + Activity scene side-effect
Fade + Activity scene side-effect
```

`FadeWithLoading + no scene side-effect` remains a warning because the LoadingSurface is not required by that operation.

## Validator changes

`FrameworkAuthoringValidator` no longer reports errors when an Activity with an Activity Content Profile uses `Seamless` or `Fade`.

The validator now reports informational guidance describing the presentation semantics of each mode.

Structural authoring errors remain unchanged:

- missing required scene reference;
- cached scene name without scene path;
- duplicate content id inside one profile.

## Non-goals

F25I1 does not change:

- Activity scene composition/release execution;
- Activity Scene Ledger;
- Route startup path;
- LoadingSurface implementation;
- TransitionSurface implementation;
- Addressables;
- loading progress.
