# F24 Plan - Unity Build Surface

## Status

Planned

## Source of truth

Para esta etapa, a fonte operacional e `Assets/`.

Nao usar package externo, docs antigas fora de `Assets` ou arquivos soltos como base de edicao deste plano.

## Purpose

Dar forma Unity-facing as partes principais do framework antes de avancar para gameplay/adapters.

O objetivo de F24 e preparar o framework para ser usado por level/game designers com componentes, assets e inspectors compreensiveis.

## Implementation tracks

Este plano segue os ADRs:

- `Assets/_Documentation/ADRs/F24-ADR-UNITY-001-Implementation-Tracks.md`
- `Assets/_Documentation/ADRs/F24-ADR-UNITY-002-Implementation-Workflow-And-QA-Workspace.md`

Trilhos:

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
- registrar a fonte operacional `Assets/`;
- criar plano oficial da proxima etapa.

### F24A2 - Naming and Scene Path Reconciliation

Status: Closed / Standard Smoke Pass

Escopo:

- corrigir nomes ruins herdados e reconciliar nomenclatura visivel;
- atualizar scene paths e serialized references;
- ajustar editor/build settings se necessario;
- nao alterar lifecycle.

### F24A3 - Unity Build Surface QA Workspace

Status: Current

Escopo:

- criar workspace QA isolado para Unity Build Surface;
- separar cenas/assets de teste de Transition, Loading, Pause, Save Moment e Preferences do QA baseline;
- registrar politica de criacao de assets de teste;
- nao implementar lifecycle;
- nao criar Transition/Loading/Pause ainda.

### F24B - Transition Contract Wiring

Status: Planned

Escopo:

- garantir que Route/Activity requests passam por um contrato de transition;
- sem visual obrigatorio;
- sem curtain/loading screen ainda.

### F24C - Transition Unity Surface

Status: Planned

Escopo:

- criar primeira surface Unity-facing para transicao;
- naming e inspector orientados a designer;
- nao criar lifecycle paralelo;
- usar o workspace QA de Unity Build Surface para validacao.

### F24D - Loading Unity Surface

Status: Planned

Escopo:

- criar superficie de loading/progress;
- loading apresenta operacao, nao vira owner de scene lifecycle;
- usar o workspace QA de Unity Build Surface para validacao.

### F24E - Pause Unity Surface

Status: Planned

Escopo:

- criar superficie de pause;
- pause consome lifecycle/input/presentation;
- pause nao controla Activity/Route diretamente;
- usar o workspace QA de Unity Build Surface para validacao.

### F24F - Save Moment Authoring

Status: Planned

Escopo:

- criar authoring minimo de intencao de save;
- sem backend completo;
- sem snapshot gameplay ainda;
- usar o workspace QA de Unity Build Surface para validacao.

### F24G - Preferences Authoring

Status: Planned

Escopo:

- separar preferences de progression save;
- criar authoring minimo para configuracoes persistentes;
- usar o workspace QA de Unity Build Surface para validacao.

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
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface`: QA isolado para surfaces Unity-facing de F24.
- `Assets/_Documentation`: documentacao viva do projeto.
- `Assets/_External`: ferramentas externas e imports manuais.
- `Assets/_Sandbox`: experimentos descartaveis.
- `Assets/Settings`, `Assets/TextMesh Pro` e assets oficiais Unity permanecem separados.

## Unity Build Surface QA workspace

Estrutura canonica inicial:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  README.md
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
```

Uso esperado:

- cenas e assets deste workspace validam surfaces Unity-facing;
- nao substituir as cenas baseline `StartupScene` e `SecondScene`;
- nao virar produto;
- nao conter assets singulares de jogo final;
- nao criar dependency obrigatoria para runtime core.

## Implementation workflow policy

- Codex: documentacao e cortes complexos com 3 ou mais modulos coordenados.
- Chat: cortes simples, primitivos, criacoes pequenas, analise e atualizacoes documentais pequenas.
- Se um corte simples comecar a tocar 3 ou mais modulos, reclassificar como corte Codex.
- Se um asset for singular de uma configuracao especifica de jogo, manter em `Assets/_Project`.
- Se for generico, reutilizavel ou adapter avancado, considerar framework/package.

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
- ADR de workflow/QA workspace existe.
- Plano F24 existe.
- README de documentacao aponta para ADRs e plano.
- Workspace `Assets/ImmersiveFrameworkQA/UnityBuildSurface` existe.
- Nenhum runtime foi alterado neste corte.
- Nenhum package foi alterado.
