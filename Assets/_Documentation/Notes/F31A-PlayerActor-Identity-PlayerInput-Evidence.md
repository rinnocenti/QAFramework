# F31A — PlayerActor Identity and PlayerInput Evidence

Status: Closed by implementation patch, pending Unity smoke.
Type: Runtime contracts + Unity-facing declaration + QA smoke.

## Decision

The framework needs the smallest possible actor identity surface before applying InputMode to gameplay.

`PlayerActor` is the first actor specialization:

```text
PlayerActor : IActor
```

A framework-recognized `PlayerActor` must provide evidence of Unity's official `PlayerInput` component.

This does not make the framework an input manager. Unity `PlayerInput` remains the official input component. Unity `PlayerInputManager` remains the optional singleton component for join/local multiplayer scenarios. The framework only validates identity and evidence.

## What was added

- `ActorId`
- `ActorKind`
- `IActor`
- `PlayerActorDeclaration`
- `PlayerActorDescriptor`
- `PlayerActorSet`
- `PlayerActorValidator`
- `PlayerActor Identity Smoke`

## First rule

```text
PlayerActor requires PlayerInput evidence.
```

A PlayerActor without PlayerInput is a blocking authoring/evidence issue.

## What this is not

F31A does not add:

- player movement;
- action-map switching;
- input reading;
- actor spawning;
- PlayerInputManager ownership;
- PlayerInput activation/deactivation;
- local multiplayer join policy;
- camera/audio/save/gameplay adapters.

## QA

Run:

```text
Run PlayerActor Identity Smoke
```

Expected steps:

```text
contracts
playeractor-with-playerinput-valid
missing-playerinput-blocking
duplicate-playeractor-id-blocking
no-input-behavior
```
