# F24A8 — Transition QA Route Switch Panels

## Goal

Adicionar controles QA simples para alternar entre `TransitionRouteA` e `TransitionRouteB` sem reutilizar o QA baseline `StartupScene`/`SecondScene`.

## Scope

- QA-only runtime panel em `Assets/ImmersiveFrameworkQA/UnityBuildSurface`.
- Editor installer idempotente para aplicar o painel nas cenas de transition.
- Nenhum visual de transition.
- Nenhum loading screen.
- Nenhum pause.
- Nenhum lifecycle novo.

## Usage

1. Criar as fixtures, se ainda não existirem:

```text
Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes
```

2. Criar/ativar a Game Application QA de transition:

```text
Immersive Framework > QA > Unity Build Surface > Create Transition QA Game Application
Immersive Framework > QA > Unity Build Surface > Set Transition QA Game Application Active
```

3. Instalar os painéis nas cenas:

```text
Immersive Framework > QA > Unity Build Surface > Install Transition QA Route Switch Panels
```

4. Entrar em Play Mode. O boot deve iniciar em `TransitionRouteA`.

5. Usar o botão IMGUI para alternar:

```text
TransitionRouteA -> TransitionRouteB -> TransitionRouteA
```

## Validation

Logs esperados após F24B:

```text
transition='SucceededNoVisual'
transitionScope='Route'
transitionBefore='SucceededNoVisual'
transitionAfter='SucceededNoVisual'
transitionBlockingIssues='0'
```
