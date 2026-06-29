# F31 Plan — PlayerActor Identity and Unity Input Evidence

## Status

Closed. F31A, F31B, F31B1 and F31C are complete.

## Purpose

F31 creates the minimal canonical reference layer needed before applying InputMode to Unity input objects.

F31 answers:

```text
what is the first actor identity in the framework;
how a PlayerActor is recognized;
why a PlayerActor requires Unity PlayerInput evidence;
where the canonical PlayerInputManager evidence belongs;
which behavior remains deferred.
```

## Ownership rule

| Concern | Owner |
|---|---|
| `PlayerInput` execution | Unity official component. |
| `PlayerInputManager` join/local multiplayer | Unity official component, declared as Session-scoped evidence. |
| `IActor` / `ActorId` | Framework identity language. |
| `PlayerActorDeclaration` evidence | Framework Unity adapter surface. |
| InputActionAsset/action maps | Project configuration. |
| Player movement and gameplay behavior | Project/gameplay code or later adapters. |

## Closed cuts

| Cut | Status | Output |
|---|---|---|
| F31A | Closed / QA PASS | Minimal `IActor`, `ActorId`, `PlayerActorDeclaration` and PlayerInput evidence validation. |
| F31B | Closed / QA PASS | Session-scoped `PlayerInputManager` declaration/evidence and singleton validation. |
| F31B1 | Closed / compile warning fix | Removes redundant smoke contract comparison. |
| F31C | Closed / docs closeout | Records PlayerActor/Session input reference closure and next direction. |

## QA evidence

F31 is covered by:

```text
PlayerActor Identity Smoke
Session PlayerInputManager Boundary Smoke
```

Expected behavior remains evidence-only:

```text
actionMapSwitching='False'
inputBehavior='False'
actorSpawning='False'
playerJoin='none'
playerPrefabSpawn='none'
```

## Reference notes

```text
Assets/_Documentation/Notes/F31A-PlayerActor-Identity-PlayerInput-Evidence.md
Assets/_Documentation/Notes/F31B-Session-PlayerInputManager-Boundary.md
Assets/_Documentation/Notes/F31B1-Session-PlayerInputManager-Smoke-Warning-Fix.md
Assets/_Documentation/Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md
```

## Explicit non-goals

F31 does not:

```text
spawn actors;
join players;
instantiate player prefabs;
switch action maps;
read input actions;
move a player;
create a custom input manager;
connect Pause to concrete Unity input behavior.
```

## Closure

F31 is closed because the framework now has canonical references for later input application:

```text
PlayerActor : IActor + PlayerInput evidence;
Session PlayerInputManager evidence.
```

The next input cut may plan or preview Unity `PlayerInput` action-map application, but only as an explicit adapter consuming these references.
