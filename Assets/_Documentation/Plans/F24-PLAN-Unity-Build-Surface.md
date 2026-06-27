# F24 Plan — Unity Build Surface

## Status

Planned

## Source boundaries

A fonte operacional desta etapa depende da fronteira do corte:

- `Assets/` é a fonte operacional para assets, cenas, QA, documentação viva e configurações do projeto consumidor.
- `Packages/com.immersive.framework/` é a fonte operacional para contratos, lifecycle, runtime, diagnostics e surfaces genéricas do framework.
- Outros packages só entram quando o corte declarar explicitamente integração com adapter/package externo.

Não usar docs antigas fora de `Assets/_Documentation` como base de decisão desta etapa.

## Purpose

Dar forma Unity-facing às partes principais do framework antes de avançar para gameplay/adapters.

O objetivo de F24 é preparar o framework para ser usado por level/game designers com componentes, assets e inspectors compreensíveis.

## Implementation tracks

Este plano segue os ADRs:

- Framework Core / Contracts
- Unity Build Surface
- Adapter Modules

## F24 sequence

### F24A0 — Assets Structure Hygiene

Status: Closed / Smoke Pass

Escopo:

- reorganização de `Assets`;
- correção de editor creators;
- separação de `_Project`, `ImmersiveFrameworkQA`, `_Sandbox`, `_External` e `_Documentation`.

### F24A1 — Implementation Tracks ADR + Unity Plan

Status: Closed / Documentation Pass

Escopo:

- registrar os três trilhos;
- registrar a fonte operacional `Assets/` para assets/docs/QA/configurações;
- criar plano oficial da próxima etapa.

### F24A2 — Naming and Scene Path Reconciliation

Status: Closed / Standard Smoke Pass

Escopo:

- corrigir nomes ruins herdados e reconciliar nomenclatura visível;
- atualizar scene paths e serialized references;
- ajustar editor/build settings se necessário;
- não alterar lifecycle.

### F24A3 — Unity Build Surface QA Workspace

Status: Closed / Documentation + Workspace Pass

Escopo:

- criar workspace QA isolado para Unity Build Surface;
- preparar pastas de cenas, assets, prefabs, materials e sprites;
- não implementar runtime/visual.

### F24A4 — Unity Build Surface QA Scene Creator

Status: Closed / QA Scene Creator Pass

Escopo:

- criar ferramenta editor idempotente para gerar a cena QA de Unity Build Surface;
- não criar transition/loading/pause;
- não alterar lifecycle.

### F24A5 — Source Boundary Correction + Transition QA Route Plan

Status: Closed / Source Boundary Documentation Pass

Escopo:

- corrigir a regra “somente `Assets/`”;
- registrar que Framework Core / Contracts pode editar `Packages/com.immersive.framework`;
- planejar rotas/cenas específicas de teste para transitions;
- não implementar Transition Contract ainda.

### F24A6 — Transition QA Routes and Scenes

Status: Closed / Transition QA Fixtures Pass

Escopo:

- criar cenas de teste específicas para transition no workspace `Assets/ImmersiveFrameworkQA/UnityBuildSurface`;
- criar Route/Activity assets de QA específicos para transition;
- criar ferramenta editor idempotente para gerar/selecionar esses fixtures;
- adicionar as cenas de transition ao Build Settings quando o editor tool for executado;
- evitar reaproveitar cenas antigas cheias de QA baseline;
- não alterar framework core;
- não implementar transition wiring.

### F24B — Transition Contract Wiring

Status: Current

Escopo:

- editar `Packages/com.immersive.framework` quando necessário;
- garantir que Route/Activity requests passam por um contrato de transition;
- sem visual obrigatório;
- sem curtain/loading screen ainda.

### F24C — Transition Unity Surface

Status: Planned

Escopo:

- criar primeira surface Unity-facing para transição;
- naming e inspector orientados a designer;
- usar as cenas/assets QA isoladas de Unity Build Surface;
- não criar lifecycle paralelo.

### F24D — Loading Unity Surface

Status: Planned

Escopo:

- criar superfície de loading/progress;
- loading apresenta operação, não vira owner de scene lifecycle.

### F24E — Pause Unity Surface

Status: Planned

Escopo:

- criar superfície de pause;
- pause consome lifecycle/input/presentation;
- pause não controla Activity/Route diretamente.

### F24F — Save Moment Authoring

Status: Planned

Escopo:

- criar authoring mínimo de intenção de save;
- sem backend completo;
- sem snapshot gameplay ainda.

### F24G — Preferences Authoring

Status: Planned

Escopo:

- separar preferences de progression save;
- criar authoring mínimo para configurações persistentes.

### F24H — Designer Guide

Status: Planned

Escopo:

- documentar como montar Boot, Route, Activity, QA, Transition, Loading, Pause, Save Moment e Preferences;
- exemplos voltados a game/level designers.

## Inspector UX rule

Componentes Unity-facing devem seguir este padrão:

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
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface`: QA isolado para surfaces Unity-facing.
- `Assets/_Documentation`: documentação viva do projeto.
- `Assets/_External`: ferramentas externas e imports manuais.
- `Assets/_Sandbox`: experimentos descartáveis.
- `Packages/com.immersive.framework`: framework core e surfaces/adapters genéricos.
- `Assets/Settings`, `Assets/TextMesh Pro` e assets oficiais Unity permanecem separados.

## Operational workflow

Usar Codex principalmente para:

- documentação;
- cortes complexos;
- cortes que coordenam três ou mais módulos.

Fazer diretamente no chat:

- cortes simples;
- primitivos;
- criações pequenas;
- ajustes documentais pequenos.

## Non-goals for F24

F24 não implementa:

- player gameplay;
- actor system;
- projectile/damage/attributes;
- camera adapter completo;
- audio adapter completo;
- pooling gameplay;
- save backend completo;
- snapshot de gameplay completo.

## Acceptance criteria

- ADR dos três trilhos existe.
- ADR de source boundary existe.
- Plano F24 existe.
- README de documentação aponta para ADRs e plano.
- Novos cortes declaram se editam `Assets`, `Packages/com.immersive.framework` ou ambos.
- QA de Transition não depende das cenas antigas de QA baseline.
