# Player QA Harness

Player is one QA feature with one setup, Hub entry, primary scene and consolidated panel.

```text
1. Immersive Framework > QA > Setup > Player > Configure Player QA
2. Open Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity
3. Enter Play Mode
4. Open Player QA
5. Run All Player QA
6. Inspect the consolidated PASS / FAIL output
```

The setup is idempotent. It authors shared fixtures, Manager-Provisioned and
Scene-Provided hosts, Session profiles, the Player Route/Activity topology,
wires the canonical QA Game Application / `QA_UIGlobal` provisioning, and
validates authoring plus Pause/Input/Gate composition.

## Internal groups

```text
Authoring
  input, slots, actors, presentations, hosts, Session profiles

Pause / Input / Gate
  UnityPlayerInputGateAdapter owns PlayerInput and Gameplay map
  PlayerPauseInput authors only Pause

Manager-Provisioned runtime
  scoped access, join, observation, default/replace actor,
  second player, joining open/close, commands, leave, rejoin,
  stale leave and wrong-scope negatives

Spatial / Relocation
  RoutePlayerSpatialEntryAuthoring
  ActivityPlayerRelocationAuthoring
  exact Presentation spatial authority and same-occurrence replacement pose
```

The runtime suite uses official public components only:

```text
PlayerSessionScopedAccessConsumer
PlayerSessionObserver
PlayerSessionJoinCommandTrigger
PlayerSessionLeaveCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
RoutePlayerSpatialEntryAuthoring
ActivityPlayerRelocationAuthoring
LocalPlayerHostAuthoring
SceneProvidedLocalPlayerAuthoring
PlayerActorRuntimeHost
```

It does not use reflection, service locators or a parallel Player runtime.

The generic Player Actor Runtime Host fixture has no required physical-body
component. The exact materialized Presentation is the spatial target for Route
placement and Activity relocation; prepared Actor replacement preserves its world
pose on the replacement Presentation.

## Generated / maintained QA assets

```text
Assets/ImmersiveFrameworkQA/Player/Input/QA_PlayerInputActions.inputactions
Assets/ImmersiveFrameworkQA/Player/Prefabs/QA_ManagerLocalPlayerHost.prefab
Assets/ImmersiveFrameworkQA/Player/Prefabs/QA_SceneLocalPlayerHost.prefab
Assets/ImmersiveFrameworkQA/Player/Prefabs/QA_PlayerActorRuntimeHost.prefab
Assets/ImmersiveFrameworkQA/Player/Profiles/QA_PlayerSession_Manager.asset
Assets/ImmersiveFrameworkQA/Player/Profiles/QA_PlayerSession_Scene.asset
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_Player.unity
Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerSceneProvided.unity
Assets/ImmersiveFrameworkQA/Player/Routes/QA_PlayerRoute.asset
Assets/ImmersiveFrameworkQA/Player/Routes/QA_PlayerSceneProvidedRoute.asset
```

## Full Player QA

```text
Immersive Framework > QA > Player > Run Full Player QA
```

- Edit Mode: authoring + Pause/Input/Gate composition.
- Play Mode: locates the Player QA panel, or requests the Player route from
  Hub, then runs the same `Run All Player QA` suite.

Expected runtime log:

```text
[QA_PLAYER_FULL] status='Passed' verdict='PLAYER QA CERTIFIED'
```

## Scene-Provided

Scene-Provided hosts, Session profile and Hub entry `Player Scene-Provided`
are part of this functional. Live Scene admission requires the Game Application
default Session profile to be `QA_PlayerSession_Scene`. The canonical Full
Player QA certifies Manager-Provisioned runtime plus Scene-Provided authoring.

## What this QA does not prove

- Activity participation, readiness or projection (Game Flow)
- Pause runtime state machine (Game Flow Pause contract)
- Camera override authority (Camera QA)
- Historical P3/M07 cut numbering
