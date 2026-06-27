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

Este plano segue o ADR:

- Framework Core / Contracts
- Unity Build Surface
- Adapter Modules

## Implementation workflow

- Documentacao e etapas complexas que envolvem 3 ou mais modulos podem ir para prompt de Codex.
- Cortes simples, primitivos, criacoes pequenas e ajustes documentais pequenos podem ser feitos diretamente no chat.
- Novos elementos Unity Build Surface devem ter assets/cenas de QA proprios sempre que isso reduzir acoplamento com o QA baseline.
- Configuracoes singulares de jogo ficam em `Assets/_Project`.
- Elementos genericos ou adapters reutilizaveis podem entrar no framework quando fizer sentido.

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

Status: Closed / Workspace Pass

Escopo:

- criar workspace isolado em `Assets/ImmersiveFrameworkQA/UnityBuildSurface`;
- separar cenas, assets, prefabs, materiais e sprites de QA desta etapa;
- registrar regra de uso do workspace;
- nao criar runtime/lifecycle novo.

### F24A4 - Unity Build Surface QA Scene Creator

Status: Current

Escopo:

- criar um editor tool simples para gerar a cena QA inicial dentro do Unity;
- cena alvo: `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity`;
- criacao idempotente;
- nao editar YAML de cena manualmente;
- nao alterar lifecycle.

### F24B - Transition Contract Wiring

Escopo:

- garantir que Route/Activity requests passam por um contrato de transition;
- sem visual obrigatorio;
- sem curtain/loading screen ainda.

### F24C - Transition Unity Surface

Escopo:

- criar primeira surface Unity-facing para transicao;
- naming e inspector orientados a designer;
- nao criar lifecycle paralelo.

### F24D - Loading Unity Surface

Escopo:

- criar superficie de loading/progress;
- loading apresenta operacao, nao vira owner de scene lifecycle.

### F24E - Pause Unity Surface

Escopo:

- criar superficie de pause;
- pause consome lifecycle/input/presentation;
- pause nao controla Activity/Route diretamente.

### F24F - Save Moment Authoring

Escopo:

- criar authoring minimo de intencao de save;
- sem backend completo;
- sem snapshot gameplay ainda.

### F24G - Preferences Authoring

Escopo:

- separar preferences de progression save;
- criar authoring minimo para configuracoes persistentes.

### F24H - Designer Guide

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
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface`: QA isolado da etapa Unity Build Surface.
- `Assets/_Documentation`: documentacao viva do projeto.
- `Assets/_External`: ferramentas externas e imports manuais.
- `Assets/_Sandbox`: experimentos descartaveis.
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
- Workspace QA de Unity Build Surface existe.
- Cena QA inicial pode ser criada pelo editor tool de F24A4.
- Nenhum runtime lifecycle foi alterado em F24A4.
- Nenhum package foi alterado em F24A4.
