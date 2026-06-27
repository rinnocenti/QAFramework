# F24A7 — Transition QA Game Application

## Goal

Create an isolated Game Application for Unity Build Surface transition validation.

The baseline framework QA application remains available for core boot/route/activity smoke. Transition-specific tests should not depend on the canonical QA baseline scenes.

## Asset created by the editor tool

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications/QA_TransitionGameApplication.asset
```

The asset uses:

```text
Application Name: QA Transition Game Application
Startup Route: Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes/QA_TransitionRouteA.asset
Validation Mode: Standard
```

## Editor menu

```text
Immersive Framework > QA > Unity Build Surface > Create Transition QA Game Application
Immersive Framework > QA > Unity Build Surface > Set Transition QA Game Application Active
```

The create menu is idempotent. It creates or refreshes the Game Application and selects it.

The set-active menu is explicit. It updates:

```text
Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset
```

so boot starts from the Transition QA route.

## Validation

1. Run `Create Transition QA Routes and Scenes` first.
2. Run `Create Transition QA Game Application`.
3. Run `Set Transition QA Game Application Active` only when validating transition fixtures.
4. Enter Play Mode.
5. Confirm boot starts from `TransitionRouteA`.
6. Use Transition QA route requests to switch to `TransitionRouteB` when available.

## Non-goals

- No transition visual.
- No loading screen.
- No pause overlay.
- No gameplay adapter.
- No framework lifecycle change.
