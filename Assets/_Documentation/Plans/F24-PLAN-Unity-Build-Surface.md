# F24 Plan - Unity Build Surface

## Status

In Progress

## Source boundaries

For project assets, QA fixtures, scenes, documentation and project-specific configuration, use `Assets/`.

For generic framework core/contracts, use `Packages/com.immersive.framework/`.

Do not create framework core contracts under `Assets/_Project/Scripts/Runtime` unless they are intentionally project-specific.

## Purpose

Dar forma Unity-facing as partes principais do framework antes de avancar para gameplay/adapters.

O objetivo de F24 e preparar o framework para ser usado por level/game designers com componentes, assets e inspectors compreensiveis.

## Implementation tracks

Este plano segue os ADRs de F24:

- Framework Core / Contracts
- Unity Build Surface
- Adapter Modules

## F24 sequence

### F24A0 - Assets Structure Hygiene

Status: Closed / Smoke Pass

Escopo:

- reorganizacao de `Assets`;
- correcao de editor creators;
- separacao de `_Project`, `ImmersiveFrameworkQA`, `_Sandbox`, `_External` e `_Documentation`.

### F24A1 - Implementation Tracks ADR + Unity Plan

Status: Closed / Documentation Pass

Escopo:

- registrar os tres trilhos;
- registrar a fonte operacional de assets/configuracao;
- criar plano oficial da proxima etapa.

### F24A2 - Naming and Scene Path Reconciliation

Status: Closed / Standard Smoke Pass

Escopo:

- corrigir nomes ruins herdados e reconciliar nomenclatura visivel;
- atualizar scene paths e serialized references;
- ajustar editor/build settings se necessario;
- nao alterar lifecycle.

### F24A3 - Unity Build Surface QA Workspace

Status: Closed / Documentation + Workspace Pass

Escopo:

- criar workspace QA isolado para Unity Build Surface;
- separar fixtures de Transition/Loading/Pause/Save/Preferences do QA baseline.

### F24A4 - Unity Build Surface QA Scene Creator

Status: Closed / QA Scene Creator Pass

Escopo:

- criar ferramenta editor idempotente para gerar a cena QA inicial;
- nao criar visual/lifecycle novo.

### F24A5 - Project and Framework Source Boundary

Status: Closed / Documentation Pass

Escopo:

- corrigir a regra de fonte operacional;
- declarar `Assets/` como fonte de assets/configuracao/docs;
- declarar `Packages/com.immersive.framework/` como fonte do Framework Core / Contracts.

### F24A6 - Transition QA Routes and Scenes

Status: Closed / Transition QA Fixtures Pass

Escopo:

- criar rotas/cenas/assets especificos para QA de Transition;
- nao reutilizar apenas `StartupScene`/`SecondScene` para transition;
- nao alterar lifecycle.

### F24A7 - Transition QA Game Application

Status: Closed / Transition QA Game Application Pass

Escopo:

- criar Game Application especifica para QA de Transition;
- permitir ativacao explicita dessa aplicacao nos settings do framework;
- bootar em `TransitionRouteA`.

### F24A8 - Transition QA Route Switch Panels

Status: Closed / Transition QA Route Switch Panel Pass

Escopo:

- instalar painéis QA runtime para alternar `TransitionRouteA` e `TransitionRouteB`;
- validar Route Request com diagnostico de transition;
- nao criar visual de transition.

### F24A9 - Transition QA Activity Switch Panels

Status: Closed / Transition QA Activity Switch Panel Pass

Escopo:

- instalar painéis QA runtime para Activity Request e Activity Clear;
- validar Activity/ActivityClear com diagnostico de transition;
- nao criar visual de transition.

### F24B - Transition Contract Wiring

Status: Closed / Route + Activity Transition Contract Pass

Escopo:

- garantir que Route/Activity requests passam por contrato de transition;
- sem visual obrigatorio;
- sem curtain/loading screen ainda.

### F24B1 - Temporary QA Tooling Cleanup

Status: Current

Escopo:

- remover ferramentas editor-only temporarias usadas para gerar fixtures F24A4-F24A9;
- manter assets, cenas e painéis runtime de QA;
- nao alterar framework core.

### F24C - Transition Unity Surface

Status: Closed / Transition Unity Surface Wiring Pass

Escopo:

- criar primeira surface Unity-facing para transicao;
- naming e inspector orientados a designer;
- nao criar lifecycle paralelo.

### F24D - Loading Unity Surface

Status: Closed / Loading Unity Surface Wiring Pass

Escopo:

- criar superficie de loading/progress;
- loading apresenta operacao, nao vira owner de scene lifecycle.

### F24D1 - Loading Surface QA Visibility Hold

Status: Current

Escopo:

- manter a loading surface QA visivel tempo suficiente para validacao humana;
- nao simular load no framework core;
- nao atrasar Route, Activity, SceneLifecycle ou GameFlow.

### F24E - Pause Unity Surface

Status: Planned

Escopo:

- criar superficie de pause;
- pause consome lifecycle/input/presentation;
- pause nao controla Activity/Route diretamente.

### F24F - Save Moment Authoring

Status: Planned

Escopo:

- criar authoring minimo de intencao de save;
- sem backend completo;
- sem snapshot gameplay ainda.

### F24G - Preferences Authoring

Status: Planned

Escopo:

- separar preferences de progression save;
- criar authoring minimo para configuracoes persistentes.

### F24H - Designer Guide

Status: Planned

Escopo:

- documentar como montar Boot, Route, Activity, QA, Transition, Loading, Pause, Save Moment e Preferences;
- exemplos voltados a game/level designers.

## Inspector UX rule

Componentes Unity-facing devem seguir este padrao:

1. Owner
2. Intent / Role
3. Requiredness / Policy
4. References
5. Runtime Preview
6. Authoring Validation
7. Advanced Diagnostics

## Folder policy

- `Assets/_Project`: produto/projeto consumidor.
- `Assets/ImmersiveFrameworkQA`: QA manual e smokes.
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface`: fixtures isoladas da etapa F24.
- `Assets/_Documentation`: documentacao viva do projeto.
- `Assets/_External`: ferramentas externas e imports manuais.
- `Assets/_Sandbox`: experimentos descartaveis.
- `Packages/com.immersive.framework`: framework core/contracts genericos.
- `Assets/Settings`, `Assets/TextMesh Pro` e assets oficiais Unity permanecem separados.

## Non-goals for F24

F24 nao implementa:

- player gameplay;
- actor system;
- projectile/damage/attributes;
- camera adapter completo;
- audio adapter completo;
- pooling gameplay;
- save backend completo;
- snapshot de gameplay completo.

## Acceptance criteria

- ADR dos tres trilhos existe.
- Plano F24 existe.
- README de documentacao aponta para ADR e plano.
- Framework core fica em `Packages/com.immersive.framework`.
- Assets/configuracoes/QA/docs ficam em `Assets`.
- F24B foi validado para Route, Activity e ActivityClear.
