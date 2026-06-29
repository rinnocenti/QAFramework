# F30 Plan — InputMode Identity and Request Result Model

## Status

Active. F30A closed; F30B is next.

## Purpose

F30 starts the InputMode track after F29 proved explicit Unity Input target ownership.

The phase defines the framework language for input posture before any Unity Input System action-map switching or PlayerInput ownership is introduced.

F30 answers:

```text
which input modes exist first;
how a mode change is requested;
which owner records the current mode;
how requests succeed, fail or get ignored;
how Pause may request a mode without owning PlayerInput or action-map names;
which future cut may connect the mode to Unity Input adapters.
```

## Starting Point

F29 is closed.

Available evidence:

```text
GlobalUiPause target declaration exists in QA;
GameplayCommands target declaration exists in QA;
missing required target diagnostics exist;
duplicate required target diagnostics exist;
loaded-scene fixture validation passes;
no action-map behavior exists yet.
```

## Initial Mode Vocabulary

Initial modes accepted from F28E:

| Mode | Meaning |
|---|---|
| `Gameplay` | Gameplay command posture. |
| `PauseOverlay` | Pause UI posture over gameplay. |
| `FrontendMenu` | Reserved non-gameplay menu posture. |
| `InputLocked` | Reserved transition/loading/exceptional hard suppression posture. |

## Cut Sequence

| Cut | Name | Type | Output |
|---|---|---|---|
| F30A | InputMode Identity / State / Request Result Contracts | Closed / runtime contracts + QA smoke | Passive mode identity, state snapshot, request/result vocabulary and pure smoke. |
| F30B | InputMode Owner Preview | Next / runtime owner + QA smoke | A single owner records current mode and processes requests without Unity action-map switching. |
| F30C | Pause InputMode Request Bridge | Runtime integration + QA smoke | Pause requests `PauseOverlay`/`Gameplay` through the mode owner, still without action-map switching. |
| F30D | Unity Input Adapter Planning Closeout | Docs/QA | Selects whether the next phase connects mode state to Unity Input System action maps or keeps one more preview cut. |

## F30A Rules

F30A created:

```text
InputMode identity/type vocabulary;
InputModeState state snapshot;
InputModeRequest;
InputModeRequestResult;
InputModeRequestStatus;
InputModeRequestIssue vocabulary;
pure QA smoke for valid, ignored and invalid request behavior.
```

F30A must not create:

```text
Unity action-map switching;
Unity PlayerInput ownership;
player movement;
player/actor spawning;
Camera/Audio/Save adapters;
Pause surface visual behavior;
per-consumer Gate query policy.
```

## Expected F30A Smoke

Manual smoke:

```text
InputMode Contract Smoke
```

Expected steps:

```text
contracts
initial-state
valid-gameplay-request
valid-pause-overlay-request
ignored-same-mode-request
invalid-mode-request-no-side-effects
no-action-map-switching
```

Expected result:

```text
pass when typed InputMode requests produce deterministic results without touching Unity Input System behavior.
```

F30A reference note:

```text
Assets/_Documentation/Notes/F30A-InputMode-Identity-State-Request-Result.md
```

## Placement

| Artifact | Placement |
|---|---|
| Framework mode identity/result language | `Packages/com.immersive.framework/Runtime/...` |
| Mode owner preview | `Packages/com.immersive.framework/Runtime/...` only if generic and Unity-safe |
| QA smoke runner | `Packages/com.immersive.framework/Runtime/Diagnostics` following existing QA style |
| Authored QA evidence | `Assets/ImmersiveFrameworkQA` only if a cut explicitly requires scene evidence |
| Project input assets / action maps | `Assets/_Project` or project-level assets, not framework package core |

## Closeout Criteria

F30 closes only when:

```text
InputMode identity and request/result language is stable;
there is a single clear mode owner;
Pause can request mode changes through the owner;
Unity Input target declarations from F29 remain the attachment point for later adapter behavior;
no action-map switching is hidden in contracts or Pause logic;
next phase explicitly selects the Unity Input adapter behavior cut.
```
