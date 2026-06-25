# Immersive Framework Roadmap Revisado

Plano unico e autoritativo para o pacote `com.immersive.framework`.

Este documento substitui roadmaps paralelos, matrizes separadas de rastreabilidade e auditorias avulsas como fonte de decisao para proximas fases. `Documentation~/COMPLETENESS_TRACKER.md` registra apenas estado resumido e aponta para este plano.

## Estado atual

| Faixa | Estado | Observacao |
|---|---|---|
| F0-F8 | `CLOSED` | Baselines tecnicas e documentacao historica permanecem como evidencia. |
| F9 | `CLOSED / LOGICAL CONTENT ANCHOR BINDING PASS` | Fechou binding logico de Content Anchor. Nao fechou placement fisico nem adapters concretos. |
| F10 | `CLOSED / ACTIVITY CONTENT EXECUTION CORE PASS` | Fechou contratos, aggregate result, participant contract, collection/ordering model, phase plan/request factory, runtime executor, lifecycle diagnostic integration, participant source boundary e smokes com empty source e explicit synthetic source. |
| F11+ | `PROPOSED / PENDING HUMAN APPROVAL` | Sequencia abaixo permanece proposta para fases futuras. |

## Regra de autoridade

Este arquivo e a unica fonte para:

- ordem de fases F11+;
- ownership por camada;
- fronteiras entre Framework Core, Unity Adapter e Gameplay Consumer;
- guardrails de linguagem para proximos ADRs;
- estado consolidado de F10 como fase fechada de Activity Content Execution Core.

Nao recriar documentos paralelos de roadmap. Novos ADRs devem ser pequenos, alinhados a uma fase aprovada e nao devem redefinir a sequencia geral.

## Resumo F0-F10

| Phase | Status | Owner atual | Evidencia principal |
|---|---|---|---|
| F0 | `CLOSED / PASS` | Framework Core | `Core/BASELINE_SMOKE.md` |
| F1 | `CLOSED / PASS` | Framework Core | `Core/API_STATUS_CONVENTION.md`, `Core/TYPED_IDENTITY_PRIMITIVES.md` |
| F2 | `CLOSED / PASS` | Framework Core | `Session/SESSION_RUNTIME_STATE_BOUNDARY.md` |
| F3 | `CLOSED / PASS` | Framework Core | `Route/ROUTE_RUNTIME_STATE_TYPED.md` |
| F4 | `CLOSED / ACTIVITY BASELINE PASS` | Framework Core | `Activity/ACTIVITY_RUNTIME_STATE_REFINED.md` |
| F5 | `CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS` | Framework Core | `Local/LOCAL_CONTENT_IDENTITY.md` |
| F6 | `CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS` | Framework Core + Unity scene primitive | `Planning/F6-Route-Scene-Composition-Audit.md` |
| F7 | `CLOSED / CONTENT ANCHOR DECLARATION BASELINE PASS` | Framework Core | `Planning/F7-Content-Anchor-Declaration-Audit.md` |
| F8 | `CLOSED / RUNTIME CONTENT SMOKE PASS` | Framework Core | `Planning/F8-Runtime-Roots-Materialization-Audit.md` |
| F9 | `CLOSED / LOGICAL CONTENT ANCHOR BINDING PASS` | Framework Core | `ContentAnchor/CONTENT_ANCHOR_BINDING_RUNTIME.md` |
| F10 | `CLOSED / ACTIVITY CONTENT EXECUTION CORE PASS` | Framework Core | `Activity/ACTIVITY_CONTENT_EXECUTION_CORE_CLOSURE.md` |

F9 encerra apenas o binding logico entre declaracoes de Content Anchor e contratos de RuntimeContent. F10 encerra apenas a execucao logica de conteudo de Activity por contracts/sources/participants fornecidos. Transform placement, prefab/Addressables materialization, physical release, UI concrete show/hide, camera blend, actor/player mutation, pooling gameplay use e audio gameplay use continuam fora do core atual.

## F10 closure

F10 implementou a tubulacao core de Activity Content Execution:

```text
ActivityFlowRuntime
  -> IActivityContentExecutionParticipantSource
      -> ActivityContentExecutionParticipantCollection
          -> ActivityContentExecutionRequestFactory
              -> ActivityContentExecutionPhasePlan
                  -> ActivityContentExecutionRuntime
                      -> ActivityContentExecutionAggregateResult
```

F10 fechou estes cortes:

| Corte | Entrega | Status |
|---|---|---|
| F10A | ADRs aceitos para Activity Content Execution Core. | `CLOSED` |
| F10B | Contracts por item de execucao. | `CLOSED` |
| F10C | Aggregate result por fase. | `CLOSED` |
| F10D | Participant contract. | `CLOSED` |
| F10E | Participant collection e ordering model. | `CLOSED` |
| F10F | Request factory e phase plan. | `CLOSED` |
| F10G | Runtime executor para phase plans fornecidos. | `CLOSED` |
| F10H | Runtime smoke com participants sinteticos. | `PASS` |
| F10I | Lifecycle integration com empty source/default no ActivityFlow. | `PASS` |
| F10J | Lifecycle transition smoke para clear/restore. | `PASS` |
| F10K | Participant source boundary explicita. | `PASS` |
| F10L | Explicit participant source smoke pelo lifecycle real. | `PASS` |
| F10M | Documentacao de closure. | `APPLIED` |

Smokes de F10 validados:

```text
Run Activity Content Execution Runtime Smoke
Run Activity Content Execution Lifecycle Transition Smoke
Run Activity Content Execution Participant Source Smoke
```

F10 nao adiciona:

```text
authoring real de participants
scene scan
GameObject.Find
FindObjectsOfType
Transform placement
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
physical reset
Transition/Loading
```

## Sequencia proposta F11+

| Phase | Layer | Nome | Estado |
|---|---|---|---|
| F11 | Framework Core | Reset Foundation | `PROPOSED / PENDING HUMAN APPROVAL` |
| F12 | Framework Core | Transition / Loading Orchestration | `PROPOSED / PENDING HUMAN APPROVAL` |
| F13 | Framework Core | Participation / Capability Contracts | `PROPOSED / PENDING HUMAN APPROVAL` |
| F14 | Unity Adapter | Adapter Boundaries | `PROPOSED / PENDING HUMAN APPROVAL` |
| F15 | Unity Adapter | Physical Placement / Hierarchy Adapter | `PROPOSED / PENDING HUMAN APPROVAL` |
| F16 | Unity Adapter | Scene / Prefab / Addressables Adapters | `PROPOSED / PENDING HUMAN APPROVAL` |
| F17 | Gameplay Consumer | Presentation | `PROPOSED / PENDING HUMAN APPROVAL` |
| F18 | Gameplay Consumer | Input / Pause / Camera / Save | `PROPOSED / PENDING HUMAN APPROVAL` |
| F19 | Gameplay Consumer | Actor / Player / Audio / Pooling | `PROPOSED / PENDING HUMAN APPROVAL` |
| F20+ | Productization | Snapshot / Restore / Versioning / Migration / Productization | `PROPOSED / PENDING HUMAN APPROVAL` |

## Framework Core boundary

Framework Core pode definir:

- lifecycle;
- identity;
- ownership;
- request/result contracts;
- policy;
- readiness;
- diagnostics;
- logical binding;
- logical release;
- Activity entry/exit/reset contracts;
- participation contracts.

Framework Core nao pode executar:

- `Instantiate`;
- `Destroy`;
- `Addressables.Load`;
- `Addressables.Release`;
- pool rent/return;
- `Animator` reset;
- camera blend;
- UI concrete show/hide;
- player/actor mutation;
- gameplay state mutation.

## Unity Adapter boundary

Unity adapters futuros possuem a execucao concreta Unity que o core nao deve assumir:

- scene adapter;
- prefab adapter;
- Addressables adapter;
- `Transform` placement;
- hierarchy adapter;
- physical release.

Unity adapters nao devem virar gameplay consumers. Eles traduzem contratos do core para operacoes Unity concretas.

## Gameplay Consumer boundary

Gameplay consumers futuros possuem comportamento de produto/jogo:

- Presentation concreta;
- Actor;
- Player;
- NPC;
- Camera behavior;
- Pause;
- Input;
- Save;
- Audio;
- Pooling gameplay use.

Gameplay consumers consomem contratos do core e adapters, mas nao redefinem identidade, ownership, lifecycle ou policy do Framework Core.

## Capability matrix consolidada

| Capability | Owner correto | Estado |
|---|---|---|
| Session/Route/Activity lifecycle | Framework Core | `CLOSED BASELINE` |
| Typed identity primitives | Framework Core | `CLOSED BASELINE` |
| Content Anchor declaration | Framework Core | `CLOSED F7` |
| RuntimeContent logical roots/handles | Framework Core | `CLOSED F8` |
| Content Anchor logical binding | Framework Core | `CLOSED F9` |
| Activity entry/exit content execution core | Framework Core | `CLOSED F10` |
| Reset request/result/policy foundation | Framework Core | `PROPOSED F11` |
| Transition/loading orchestration contracts | Framework Core | `PROPOSED F12` |
| Participation/capability contracts | Framework Core | `PROPOSED F13` |
| Adapter interfaces and diagnostics | Unity Adapter | `PROPOSED F14` |
| Physical placement/hierarchy | Unity Adapter | `PROPOSED F15` |
| Scene/prefab/Addressables execution | Unity Adapter | `PROPOSED F16` |
| Presentation behavior | Gameplay Consumer | `PROPOSED F17` |
| Input/Pause/Camera/Save behavior | Gameplay Consumer | `PROPOSED F18` |
| Actor/Player/Audio/Pooling behavior | Gameplay Consumer | `PROPOSED F19` |
| Snapshot/restore/versioning/migration | Productization | `PROPOSED F20+` |

## Guardrails de linguagem

- Nao tratar `Presentation` como nome canonico de core.
- Nao tratar `PrefabContentMaterializer` como core.
- Nao tratar Runtime Root como `GameObject` ou `Transform`.
- Nao tratar physical reset como core atual.
- Nao misturar Reset, Activity Entry/Exit Content Execution e Transition/Loading na mesma fase.
- Nao reabrir F10 para authoring/discovery real de participants; isso deve pertencer a fase/adapters futuros, se aprovado.

## ADR policy

ADRs aceitos de F0-F10 continuam preservados como historico porque descrevem decisoes ja aplicadas e evidencias dos cortes fechados.

ADRs antigos F10-F13 em `HOLD`, ou baseados na sequencia mista anterior, foram removidos porque competiam com este plano e misturavam core, adapter e consumer antes de a nova fronteira ser aprovada.

Novos ADRs F11+ podem ser criados apenas quando:

- a fase correspondente estiver aprovada;
- o ADR tiver owner claro;
- o ADR nao redefinir este roadmap;
- o ADR nao mover execucao Unity concreta para o Framework Core.

## Itens superseded

Estao superseded e nao autorizam trabalho futuro:

- roadmap F9+ anterior;
- audit post-F9 separado;
- matriz de capability separada;
- ADRs antigos de F10/F11/F12/F13 com plano misto;
- `PrefabContentMaterializer` como conceito de core;
- `Presentation` como fase/core canonico;
- Runtime Root fisico como `GameObject`/`Transform`;
- physical reset dentro do core atual.

## Proxima decisao humana

F10 esta fechado. A proxima decisao humana e aprovar ou ajustar a abertura de F11:

```text
F11 — Framework Core — Reset Foundation
```

F11 deve comecar por ADRs novos e limpos, sem implementar physical reset, prefab reset, pool reset, snapshot restore real ou gameplay mutation.
