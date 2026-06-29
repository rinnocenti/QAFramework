# F28E — InputMode and Pause Integration Plan

## Status

Closed / documentation-only / no runtime changes

## Purpose

F28E defines the first typed InputMode semantics and the Pause integration contract after F28D clarified Player / Actor / Unity Input ownership.

This cut does not implement InputMode. It defines the shape that the next implementation phase must follow so Pause can request input behavior without owning Unity `PlayerInput`, action-map strings, player lifecycle or gameplay consumers.

## Inputs

F28E starts from the F28D ownership split:

```text
Project Integration owns concrete player prefabs, controllers, visuals and InputActionAssets.
Unity Input Adapter owns translation from Unity Input System targets to framework input language.
Framework Core owns typed InputMode language.
Pause Integration may request modes after InputMode exists, but does not own PlayerInput or action-map strings.
Runtime-spawned player/actor lifetime is deferred until RuntimeContentHandle, runtime roots and release policy exist.
```

## Accepted InputMode Semantics

InputMode is framework language for the current command execution posture of the application.

It is not:

```text
a Unity action-map name
an InputActionAsset path
a PlayerInput component owner
a movement controller toggle
a Gate substitute
Time.timeScale policy
```

The first mode vocabulary is:

| Mode | Meaning | First behavior expectation |
|---|---|---|
| `Gameplay` | Gameplay command posture. | Gameplay command input may drive gameplay when the active Activity permits it. |
| `PauseOverlay` | Pause UI posture over an existing gameplay route/activity. | UI input and PauseToggle/Cancel remain available; gameplay command input must not drive gameplay. |
| `FrontendMenu` | Non-gameplay menu posture. | Reserved for future startup/menu flows; not implemented by the first proof unless selected later. |
| `InputLocked` | Temporary hard input suppression posture. | Reserved for transition/loading/exceptional lock states; not a replacement for Gate diagnostics. |

These are typed semantic names. Unity action-map names are adapter/project configuration.

## Accepted Transition Semantics

The initial transition model is intentionally small:

| From | To | Trigger owner | Meaning |
|---|---|---|---|
| `Gameplay` | `PauseOverlay` | Pause Integration | Logical Pause was accepted and overlay is active. |
| `PauseOverlay` | `Gameplay` | Pause Integration | Logical Pause was released and gameplay posture resumes. |
| Any | `InputLocked` | Future lifecycle/operation owner | Reserved for transition/loading/hard-lock behavior. |
| `InputLocked` | Previous non-locked mode | Future lifecycle/operation owner | Reserved; must be explicit and auditable. |
| Any | `FrontendMenu` | Future frontend/menu owner | Reserved for menu flows. |

The first implementation must not infer mode transitions from Unity action-map names, GameObject names, scene names or component active state.

## Pause Integration Decision

Pause may request InputMode changes after InputMode exists.

Canonical Pause behavior:

```text
Running:
  InputMode = Gameplay

Pause accepted:
  Pause requests PauseOverlay
  UI input remains available
  PauseToggle / Cancel remains available
  gameplay command input stops driving gameplay

Pause released:
  Pause requests Gameplay
  gameplay command input may drive gameplay again when Activity state allows it
```

Pause does not:

```text
own PlayerInput
own InputActionAsset
decide action-map string names
spawn or release player/actor objects
own movement controllers
own camera/audio/save adapters
set Time.timeScale as part of this boundary
require every ordinary input consumer to query Gate
```

## UI and Gameplay Target Split

F28E accepts a logical split between:

| Target lane | Role |
|---|---|
| Global UI / Pause intent target | Keeps PauseToggle, Cancel and UI navigation available while PauseOverlay is active. |
| Gameplay command target | Drives gameplay commands only in Gameplay posture and only when Activity/lifecycle permits it. |

The split does not require two `PlayerInput` components yet. It requires the next implementation phase to prove explicit target ownership before applying real action-map behavior.

## Gate Position

Gate remains admission and hard-lock language.

Gate may still reject a Pause request, lifecycle request or stale/foreign operation. It must not become the normal path used by every gameplay input receiver to decide whether paused gameplay input is ignored.

Accepted split:

```text
Gate:
  Can this request or operation enter?

InputMode:
  Which command posture is currently active?

Unity Input Adapter:
  How does typed mode posture affect Unity Input System targets?

Gameplay consumer:
  Reacts to commands it receives; it does not own global pause/input policy.
```

## First Proof Requirements For Next Implementation

F28E does not select the next code cut. It constrains the options that F28F may choose.

A valid next implementation proof must satisfy all of these:

```text
explicit input target ownership
missing target reports blocking diagnostic
duplicate target reports blocking diagnostic
typed mode names are not Unity action-map strings
PauseToggle/global UI input path remains available during PauseOverlay
no Player/Actor runtime spawning
no camera/audio/save/gameplay adapter coupling
no per-consumer Gate query strategy
```

The preferred first code direction remains the F28D proof:

```text
QA-authored Unity input target proof
```

That proof may introduce authored target evidence and diagnostics. It must not implement full action-map switching until the owner and evidence surface are proven.

## F28F Handoff

F28F must choose one concrete next implementation phase.

Recommended F28F candidates:

| Candidate | Why it is viable | Risk |
|---|---|---|
| QA-authored Unity input target proof | Directly follows F28D and proves explicit ownership before InputMode applies behavior. | Could become an adapter too early if it starts switching action maps. |
| Core InputMode identity/result model | Establishes typed framework language before Unity adapter behavior. | Could be abstract if no Unity proof follows immediately. |
| Pause-to-InputMode request contract | Connects Pause to typed mode requests. | Premature unless target proof and InputMode identity are already selected. |

F28F must not select Camera, Audio, Save, RuntimeSpawned or Gameplay adapters as the next code cut unless it explicitly explains why Input/Player ownership is no longer the current priority.

## Files Changed

Documentation only.

No runtime code, asmdef, QA scene, prefab, scriptable object or Unity Input asset changed in F28E.
