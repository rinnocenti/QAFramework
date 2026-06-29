# F32 — InputMode Unity Adapter Application

Status: Open.

## Goal

Move from passive `InputModeRequest` semantics to a Unity Input adapter path that can eventually apply modes through official Unity Input System components.

The framework must not become an input manager. It must integrate:

```text
Unity PlayerInput
Unity PlayerInputManager
project-owned InputActionAsset/action maps
framework-owned lifecycle/input mode request language
```

## Sequence

### F32A — InputMode Unity Application Preview

Status: Closed by patch / awaiting smoke.

Create a side-effect-free preview evaluator that maps successful logical `InputModeRequestResult` values to required Unity Input evidence:

- `Gameplay` requires `GameplayCommands` target, `PlayerActor` evidence and Session `PlayerInputManager` evidence.
- `PauseOverlay` requires the `GlobalUiPause` target only.
- `FrontendMenu` uses the `GlobalUiPause` target for now.
- `InputLocked` is accepted as no-target-required preview.

No action-map switching or input behavior.

### F32B — Unity Action Map Application Boundary

Define where action-map names live and how an adapter may translate framework modes into Unity calls later.

Must not hard-code action-map strings in framework core.

### F32C — Unity Input Adapter Dry Run

Create a dry-run adapter result that reports what would be applied without calling Unity Input behavior.

### F32D — Unity Input Adapter First Side Effect

Only after F32B/F32C, allow a narrow, QA-only side effect if the boundary is stable.

## Guardrails

- `PlayerInput` remains an official Unity component.
- `PlayerInputManager` remains Session-scoped evidence and Unity authority.
- `PlayerActor` is the framework-recognized player entity, but movement/spawn stays out.
- Framework core owns typed `InputMode` language, not Unity action-map strings.
- Pause may request modes but does not own PlayerInput.
