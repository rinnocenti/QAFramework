# Unity Build Surface QA

This workspace contains isolated QA fixtures for the Unity-facing phase of Immersive Framework.

It is separate from the baseline framework QA scenes. Use it for Transition, Loading, Pause, Save Moment and Preferences authoring tests.

## Folders

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities
Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications
Assets/ImmersiveFrameworkQA/UnityBuildSurface/ScriptableObjects
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Prefabs
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Materials
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Sprites
```

## Editor tools

Create the empty QA scene:

```text
Immersive Framework > QA > Unity Build Surface > Create QA Scene
```

Create transition-specific routes and scenes:

```text
Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes
```

Create or refresh the Transition QA Game Application:

```text
Immersive Framework > QA > Unity Build Surface > Create Transition QA Game Application
```

Set the Transition QA Game Application as the active boot application:

```text
Immersive Framework > QA > Unity Build Surface > Set Transition QA Game Application Active
```

Use the set-active command only when validating Transition QA fixtures. Restore the canonical QA application through Project Settings when returning to baseline framework smokes.

## Current fixtures

```text
Scenes/UnityBuildSurfaceQA.unity
Scenes/TransitionRouteA.unity
Scenes/TransitionRouteB.unity
Routes/QA_TransitionRouteA.asset
Routes/QA_TransitionRouteB.asset
Activities/QA_TransitionActivityA.asset
Activities/QA_TransitionActivityB.asset
GameApplications/QA_TransitionGameApplication.asset
```

## Rules

- Do not use this workspace for product content.
- Do not put gameplay-specific configuration here.
- Do not let transition visual tests pollute the baseline QA scenes.
- Generic framework behavior belongs in `Packages/com.immersive.framework`.
- Project-specific configuration belongs in `Assets/_Project`.
