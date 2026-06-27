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

- `ADRs/F24-ADR-UNITY-001-Implementation-Tracks.md`
- `ADRs/F24-ADR-UNITY-002-Implementation-Workflow-And-QA-Workspace.md`

Trilhos:

- Framework Core / Contracts
- Unity Build Surface
- Adapter Modules

## Work mode

Usar Codex somente para:

- documentacao;
- cortes complexos;
- cortes que coordenam tres ou mais modulos;
- migracoes grandes com muitas referencias serializadas.

Resolver diretamente no chat:

- cortes simples;
- primitivos;
- criacoes pequenas;
- ajustes documentais pequenos;
- analise antes de implementacao.

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

Resultado esperado validado:

- route switch usa `SecondScene`;
- Standard Smoke conclui sem erro bloqueante;
- Activity switch, clear e restore continuam funcionando.

### F24A3 - Unity Build Surface QA Workspace

Status: Current

Escopo:

- criar workspace isolado para testes Unity-facing;
- criar estrutura de QA para futuras surfaces de Transition, Loading, Pause, Save Moment e Preferences;
- nao alterar lifecycle;
- nao implementar Transition/Loading/Pause ainda;
- nao misturar novos testes visuais nas cenas baseline.

Estrutura alvo:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
  README.md
```

Cena inicial preferida:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity
```

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
- nao criar lifecycle paralelo.

### F24D - Loading Unity Surface

Status: Planned

Escopo:

- criar superficie de loading/progress;
- loading apresenta operacao, nao vira owner de scene lifecycle.

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
- `Assets/ImmersiveFrameworkQA`: QA manual, smokes e workspace Unity Build Surface.
- `Assets/_Documentation`: documentacao viva do projeto.
- `Assets/_External`: ferramentas externas e imports manuais.
- `Assets/_Sandbox`: experimentos descartaveis.
- `Assets/Settings`, `Assets/TextMesh Pro` e assets oficiais Unity permanecem separados.

## Placement policy

| Tipo | Local |
|---|---|
| Cena de teste de framework | `Assets/ImmersiveFrameworkQA/...` |
| Asset de QA do framework | `Assets/ImmersiveFrameworkQA/...` |
| Configuracao especifica do jogo | `Assets/_Project/...` |
| Material/prefab singular de um jogo | `Assets/_Project/...` |
| Componente generico reutilizavel | framework |
| Adapter reutilizavel | framework ou package adapter |
| Experimento descartavel | `Assets/_Sandbox/...` |

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
- README de documentacao aponta para os ADRs e o plano.
- F24A2 esta marcado como Closed / Standard Smoke Pass.
- F24A3 esta marcado como Current.
- Nenhum runtime foi alterado por cortes documentais.
- Novas surfaces Unity-facing devem ter QA isolado antes de entrar em cenas baseline.
- Nenhum package externo orienta esta etapa.
