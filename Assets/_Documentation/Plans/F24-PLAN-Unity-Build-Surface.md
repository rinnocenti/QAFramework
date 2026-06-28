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

Status: Closed / Loading Unity Surface Cascade Pass

Escopo:

- criar superficie de loading/progress;
- loading apresenta operacao, nao vira owner de scene lifecycle.

### F24E - Canonical UIGlobal Scene

Status: Closed / Visual-runtime pass

Escopo:

- substituir surfaces app-scoped por prefab por uma cena UIGlobal canônica;
- carregar UIGlobal antes da Startup Route e persistir seus roots sob o FrameworkRuntimeHost;
- descobrir adapters de Transition/Loading na cena;
- manter Loading/Transition como visual/diagnostics, sem ownership de Route/Activity/SceneLifecycle.

### F24E1 - Surface/Loading Legacy Cleanup

Status: Closed / Documentation Pass

Escopo:

- remover os campos e caminhos runtime legados de Transition/Loading prefab;
- consolidar UIGlobal como única origem runtime para adapters visuais globais;
- manter a cascata visual já validada sem fallback silencioso.

### F24E2 - Route/Activity Visual Operation Policy

Status: Closed / Documentation Pass

Escopo:

- documentar UIGlobal como capability session-scoped;
- registrar TransitionSurface e LoadingSurface como capabilities de UI da sessão;
- registrar que Route exige transition e usa loading durante composition;
- registrar que Activity usa transition por policy e loading apenas quando houver scene/content loading real.

### F24E3 - Surface Adapter Inspector Cleanup

Status: Current / Documentation Pass

Escopo:

- limpar o Inspector dos adapters UnityFadeCurtainEffectAdapter, UnityLoadingSurfaceAdapter e QaLoadingSurfaceVisibilityHoldAdapter;
- ocultar diagnostics runtime e campos tecnicos/legados do authoring publico;
- registrar que o corte nao muda runtime, cascata visual nem ownership.

### F24F - Pause Unity Surface

Status: Deferred / implemented as F27A+

Escopo original:

- criar superficie de pause dentro do shape UIGlobal;
- pause consome lifecycle/input/presentation;
- pause nao controla Activity/Route diretamente.

Nota: o objetivo Unity-facing de Pause foi reaberto depois do fechamento F26 e passa a ser acompanhado em `Plans/F27-PLAN-Pause-UIGlobal-And-Input.md`. F27A implementa a surface visual baseline; input e policy ficam para F27B/F27C.

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


## F24D1B — Transition Curtain QA Warm Visible State

Status: Current until smoke validation.

Purpose: fix the QA transition curtain first-use visual miss by keeping the QA curtain panel active and hidden through CanvasGroup alpha instead of disabling its surface root.

Boundary: QA fixture only; no lifecycle, scene loading, transition contract or loading runtime changes.

## F24D4 — Awaitable Cascade Loading Hide Boundary

Status: Current until smoke validation.

Purpose: enforce the visual cascade `transition fade-in -> loading show/load/hide -> transition fade-out` by awaiting the loading hide boundary before executing the transition release phase.

Boundary: uses `UnityEngine.Awaitable`; no SceneLifecycle delay, no new scene loader, no lifecycle ownership transfer, no DOTween, no `Task.Delay`.


## F24D5 — Loading Surface Visibility And Warnings Fix

Status: implemented / smoke pending.

Corrige warnings CS1998 do eixo Awaitable e ajusta o prefab QA de loading para renderizar acima da cortina, sem alterar lifecycle de Route/Scene/Activity.

## F24F — Activity Transition Policy

Status: implemented / smoke pending.

Purpose: make Activity transition optional by Activity authoring policy while keeping Route transitions mandatory.

Accepted behavior:

- `ActivityAsset.Visual Transition Mode = Seamless` skips Activity transition by policy.
- `Fade` uses the Session `TransitionSurface` from `UIGlobal`.
- Historical F24F behavior: `FadeWithLoading` was unavailable for Activity loading before Activity Content Scene Composition existed and behaved as `Fade` with Loading skipped. Current F25 behavior supersedes this.
- `ActivityClear` uses the policy of the Activity being cleared.
- Route transition/loading behavior is unchanged.

Boundary: no Activity scene loading, no LoadingSurface for Activity without real content loading, no new lifecycle, no scene loader change.


## F24F1 — Activity Loading Reserved Finding

Status: implemented / smoke pending.

Purpose at F24F1 time: mark `FadeWithLoading` as unavailable for Activity loading until Activity Content Scene Composition existed. Current F25 behavior supersedes this historical state.

Accepted behavior:

- Historical F24F1 behavior only: `FadeWithLoading` ran the fade path and skipped Activity loading because Activity scene composition did not exist yet.
- Current F25 behavior: `FadeWithLoading` is active and may use LoadingSurface when the Activity operation requests loading presentation.
- F25 must own Activity content scene composition.

Boundary: no Activity scenes, no ActivityContentProfile, no Activity scene loader, no Activity release, no Route transition/loading change.

## F25 handoff — Activity Content Scene Composition

F24F/F24F1 closed the Activity visual transition policy before Activity had real scene/content loading. F25I1/F25I2 supersede the reserved-loading wording.

F25A opened the Activity Content Scene Composition track with the initial `ActivityContentProfile` contract. Later F25 cuts added operation planning, scene execution, release, ledger tracking and final visual/loading diagnostics.
