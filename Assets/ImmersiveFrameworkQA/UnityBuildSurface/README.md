# Unity Build Surface QA Workspace

Este workspace isola os testes Unity-facing da etapa F24.

Use esta pasta para validar surfaces, assets e cenas de QA ligados a:

- Transition
- Loading
- Pause
- Save Moment
- Preferences

## Regra de uso

- Este workspace e QA, nao produto.
- Objetos especificos de jogo ficam em `Assets/_Project`.
- Experimentos descartaveis ficam em `Assets/_Sandbox`.
- Componentes genericos reutilizaveis devem ir para o framework quando fizer sentido.
- Adapters avancados devem ficar no framework ou em modulo proprio, nao neste workspace.

## Estrutura

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
  README.md
```

## Cena inicial

A cena inicial deve ser criada dentro do Unity pelo menu:

```text
Immersive Framework > QA > Unity Build Surface > Create QA Scene
```

O menu cria, de forma idempotente:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity
```

Se a cena ja existir, o menu apenas seleciona o asset existente.

## Non-goals

Este workspace nao implementa lifecycle de Transition, Loading, Pause, Save ou Preferences.

Ele apenas prepara uma area isolada para os proximos cortes Unity Build Surface.
