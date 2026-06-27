# Unity Build Surface QA

Workspace isolado para testar a etapa F24 — Unity Build Surface.

Este espaço é QA do framework. Não é produto final e não deve ser usado como cena/base de gameplay.

## Estrutura

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  Scenes/
  Routes/
  Activities/
  GameApplications/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
  Scripts/
```

## Transition QA

Fixtures principais:

```text
Scenes/TransitionRouteA.unity
Scenes/TransitionRouteB.unity
Routes/QA_TransitionRouteA.asset
Routes/QA_TransitionRouteB.asset
Activities/QA_TransitionActivityA.asset
Activities/QA_TransitionActivityB.asset
GameApplications/QA_TransitionGameApplication.asset
```

Menus úteis:

```text
Immersive Framework > QA > Unity Build Surface > Create QA Scene
Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes
Immersive Framework > QA > Unity Build Surface > Create Transition QA Game Application
Immersive Framework > QA > Unity Build Surface > Set Transition QA Game Application Active
Immersive Framework > QA > Unity Build Surface > Install Transition QA Route Switch Panels
```

## Regra

- QA fixtures ficam aqui.
- Configuração singular do jogo fica em `Assets/_Project`.
- Core genérico fica em `Packages/com.immersive.framework`.
- Este workspace não deve criar lifecycle paralelo.
