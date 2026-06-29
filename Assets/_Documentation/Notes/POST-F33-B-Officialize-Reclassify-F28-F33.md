# POST-F33-B — Officialize/Reclassify F28-F33

Status: Accepted

## Scope

This cut is docs-only. It reclassifies F28-F33 against the matrix after `POST-F33-A — Matrix Reconciliation Closeout`.

F28-F33 remain closed. This reclassification does not reopen those phases for code, does not renumber them and does not select a new implementation phase.

F28-F33 do not authorize F34, gameplay, camera, audio, save/progression, pooling/runtime-spawned work or actor materialization.

## Reclassification Table

| Phase | Official status | Classification | What it closed | What it did not close | Matrix impact |
|---|---|---|---|---|---|
| F28 | Official | Official planning/governance | Roadmap reconciliation, adapter module spine, dependency ordering and the F29 selection path. | Runtime code, materialization, ContentAnchor binding, runtime handles, release policy and consumers. | Clarifies ordering after the F27D freeze, but does not satisfy F8/F9 blockers. |
| F29 | Official | Official Unity Input target evidence | Unity Input target declaration/evidence, target diagnostics and QA fixture proof. | InputMode runtime behavior, action-map switching, `PlayerInput` ownership, player/actor spawn, gameplay, camera, audio or save. | Anticipates part of the input ownership path while leaving RuntimeContent and ContentAnchor blockers untouched. |
| F30 | Official | Official passive InputMode / Pause request language | Passive `InputMode` identity/state/request/result language and Pause-to-InputMode request mapping. | Unity input side effects, `PlayerInput` activation/deactivation, action-map switching, join, spawn, actor movement or concrete input behavior. | Officializes passive input/pause language without unblocking consumers. |
| F31 | Official | Official PlayerActor identity and Session PlayerInputManager evidence | Minimal `PlayerActor` identity, required Unity `PlayerInput` evidence and Session-scoped `PlayerInputManager` evidence. | Actor materialization, player prefab spawn, `PlayerInputManager.JoinPlayer`, movement, gameplay commands or custom input manager behavior. | Provides identity/evidence needed by later input application, but not runtime actor/materialization ownership. |
| F32 | Official closed | Controlled anticipation — explicit PlayerInput application lane | Explicit `InputMode`/`PauseResult` to Unity `PlayerInput` application through named adapters. | Automatic `PauseRuntime` wiring, automatic global `FrameworkRuntimeHost` wiring, join, prefab/player spawn, movement, gameplay command reading or custom input manager. | Anticipates a later matrix input capability, but remains bounded and does not resolve F8/F9 runtime materialization blockers. |
| F33 | Official closed | Controlled anticipation — opt-in Pause runtime to PlayerInput wiring | Authored opt-in Pause input path through `PauseInputActionRuntimeBridgeTrigger` and `PauseInputModeUnityPlayerInputRuntimeBridge`. | F34/gameplay, automatic global host wiring, `PlayerInputManager.JoinPlayer`, prefab/player spawn, PlayerActor movement, gameplay command reading, camera, audio, save or pooling consumers. | Anticipates Pause/Input/PlayerInput integration only; it does not select the next feature or unblock consumers. |

## F32/F33 Unity Side Effects

F32/F33 have real Unity side effects, but only through explicit adapters and authored opt-in bridges.

Allowed side effects:

```text
PlayerInput.ActivateInput()
PlayerInput.DeactivateInput()
PlayerInput.SwitchCurrentActionMap(...)
```

These side effects are not a framework-owned input manager and are not a general gameplay input system.

F32/F33 do not perform:

```text
PlayerInputManager.JoinPlayer
prefab/player spawn
PlayerActor movement
gameplay command reading
custom input manager
automatic global FrameworkRuntimeHost wiring
```

## Technical Next Action

The next technical action remains blocked until `F8R-A — RuntimeContent / ContentAnchor Materialization Audit`.

`F8R-A` is the first technical candidate after this reclassification. It is audit-only unless the user explicitly selects and approves a later implementation cut.

## Non-goals

- No runtime changes.
- No scene or prefab changes.
- No asmdef changes.
- No code creation.
- No F34.
- No new implementation phase selection.
