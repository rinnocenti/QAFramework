# F30 Plan — InputMode Identity and Request Result Model

## Status

Closed. F30A, F30B, F30C, F30C1, F30D and F30E are complete.

## Purpose

F30 defines the framework language for input posture before any Unity Input System behavior is introduced.

The phase exists to answer:

```text
which input modes exist first;
how a mode change is requested;
how requests succeed, fail or get ignored;
how Pause can request a mode without owning Unity input;
why the framework must not replace Unity PlayerInput / PlayerInputManager;
which canonical references are required before real input application.
```

## Accepted boundary

F30 is passive.

| Concern | Decision |
|---|---|
| InputMode identity/state/request/result | Framework-owned contracts. |
| Pause-to-InputMode mapping | Framework-owned passive request mapping. |
| Unity `PlayerInput` execution | Unity official component. |
| Unity `PlayerInputManager` execution/join/local multiplayer | Unity official component. |
| Action-map switching | Deferred to explicit Unity adapter cuts. |
| Player target | Requires F31 `PlayerActor : IActor` + `PlayerInput` evidence. |
| Session manager | Requires F31 Session-scoped `PlayerInputManager` evidence. |

## Closed cut sequence

| Cut | Status | Output |
|---|---|---|
| F30A | Closed / QA PASS | Passive `InputMode` identity, state, request/result contracts and evaluator. |
| F30B | Closed / corrective docs | Rejects a framework-owned input manager; keeps Unity official components as execution authority. |
| F30C | Closed / QA PASS | Validates official Unity Input component evidence without behavior. |
| F30C1 | Closed / QA PASS | Cleans duplicate PlayerInputManager smoke warning by using passive evidence count. |
| F30D | Closed / QA PASS | Maps Pause state/result to `InputModeRequest`. |
| F30E | Closed / docs closeout | Records F30 closure and prerequisites for later Unity Input adapter work. |

## Initial mode vocabulary

| Mode | Meaning |
|---|---|
| `Gameplay` | Gameplay command posture. |
| `PauseOverlay` | Pause UI posture over gameplay. |
| `FrontendMenu` | Reserved non-gameplay menu posture. |
| `InputLocked` | Reserved transition/loading/exceptional hard suppression posture. |

## QA evidence

F30 is covered by:

```text
InputMode Contract Smoke
Unity Input Official Component Evidence Smoke
Pause InputMode Request Boundary Smoke
```

All F30 smokes must preserve:

```text
actionMapSwitching='False'
inputBehavior='False'
```

## Reference notes

```text
Assets/_Documentation/Notes/F30A-InputMode-Identity-State-Request-Result.md
Assets/_Documentation/Notes/F30B-Unity-PlayerInput-Integration-Boundary.md
Assets/_Documentation/Notes/F30C-Unity-PlayerInput-Component-Evidence-Validation.md
Assets/_Documentation/Notes/F30C1-PlayerInputManager-Smoke-Warning-Cleanup.md
Assets/_Documentation/Notes/F30D-Pause-InputMode-Request-Boundary.md
Assets/_Documentation/Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md
```

## Explicit non-goals

F30 does not:

```text
own PlayerInput;
own PlayerInputManager;
call SwitchCurrentActionMap;
call ActivateInput or DeactivateInput;
call JoinPlayer;
spawn player prefabs;
move actors;
create a custom input manager;
connect Pause to concrete Unity input behavior.
```

## Closure

F30 is closed because the passive language is stable and the ownership boundary is corrected.

Any later action-map switching must be implemented by a named Unity Input adapter cut that consumes F29/F30/F31 references. It must not be introduced silently inside InputMode, Pause or Actor identity contracts.
