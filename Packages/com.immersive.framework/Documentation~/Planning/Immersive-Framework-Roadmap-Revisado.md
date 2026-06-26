# Immersive Framework Roadmap Revisado

Plano canônico do package `com.immersive.framework`.

Este é o único arquivo de planejamento do framework. Decisões aceitas, histórico F0-F17 e a ordem futura ficam resumidos aqui para evitar fontes paralelas.

ADRs aceitos ficam em `Documentation~/ADRs/ADR-INDEX.md`. ADRs registram decisões estáveis e não substituem este roadmap operacional.

## Decisão central

`com.immersive.framework` é o package de produto que define lifecycle, identidade, ownership, policy, diagnostics, validação e contratos de execução do framework.

O core do framework consome `com.immersive.foundation`, `com.immersive.logging` e `com.immersive.pooling`; ele não reimplementa esses packages técnicos. Operacoes concretas de Unity ficam em adapters. Comportamento de jogo fica em consumers de gameplay.

## Estado consolidado

| Faixa | Status | Leitura oficial |
|---|---|---|
| F0-F17 | `CLOSED / APPLIED` | Histórico real resumido neste documento. |
| F17 | `CLOSED / F17E QA PASS` | Gate Foundation fechada. F17A realinhou ADRs/plano; F17B introduziu primitivas passivas; F17C integrou admissão de requests existentes via Gate; F17D adicionou smoke sintético de diagnóstico; F17E fechou a fase e preparou F18. |
| F18 | `IN PROGRESS / F18A DOCS` | Transition Orchestration Foundation iniciada como plano de implementação. Sem fade/loading visual, Pause, Input ou gameplay. |
| F19-F21 | `PLANNED / REVISED ORDER` | Transition effects/loading/fade adapters, Pause state/gate e Pause content/input boundary. |
| F22+ | `DEFERRED` | Consumers avançados, gameplay capabilities e contextual reset de Player/Actor/NPC/Timer/Door/Pickup ficam bloqueados até Gate/Transition/Pause e um modelo maduro de gameplay object/actor/player. |

## Histórico real F0-F17

| Fase | Status | Resultado fechado |
|---|---|---|
| F0 | `CLOSED / APPLIED` | Baseline mínimo do package e reconciliação de fronteiras iniciais. |
| F1 | `CLOSED / APPLIED` | Convencao de API status, identidade tipada, diagnostics e separação entre fact técnico e log humano. |
| F2 | `CLOSED / APPLIED` | Escopo de Session, ownership de Session content e policy de fonte de settings. |
| F3 | `CLOSED / APPLIED` | Route runtime state, status de Route content runtime e semântica de Route content set. |
| F4 | `CLOSED / APPLIED` | Activity content set, readiness baseline e binding mínimo observável de Activity content. |
| F5 | `CLOSED / APPLIED` | Local contribution foundation, identidade local e requiredness/discovery inicial. |
| F6 | `CLOSED / APPLIED` | Route scene composition e release baseline, ainda sem converter core em executor físico genérico. |
| F7 | `CLOSED / APPLIED` | Content Anchor declaration baseline como contrato lógico de posicionamento. |
| F8 | `CLOSED / APPLIED` | Runtime roots, handles, materialization request/result, release lógico e boundaries para adapters. |
| F9 | `CLOSED / APPLIED` | Logical Content Anchor Binding; fechou binding lógico, não placement físico. |
| F10 | `CLOSED / APPLIED` | Activity Content Execution Core e decisões de consumer intermediário para Input, Snapshot e Pause sem mover ownership para gameplay. |
| F11 | `CLOSED / APPLIED` | Cycle Reset Foundation: contratos, executor, runtime request path, QA Canvas smoke e triggers públicos de Route/Activity Cycle Reset, sem reset físico. |
| F12 | `CLOSED / APPLIED` | Cycle Reset Integration & Authoring UX: guardrails, result UX, trigger smoke e bridge smoke opcional, sem reset local/físico. |
| F13 | `CLOSED / APPLIED` | Object Entry Foundation: identidade, declaration, owner tipado, coleta scoped, snapshot lifecycle e closure smoke, sem binding/reset físico. |
| F14 | `CLOSED / APPLIED` | Local/Object Reset Foundation: target derivado de Object Entry atual, participant source explícita, plan/runtime executor, Runtime Host, trigger público e bridge opcional, sem adapters Unity ou gameplay reset. |
| F15 | `CLOSED / APPLIED` | Unity Reset Adapters mínimos: source Unity explícita, Transform Reset Participant com baseline local authored, guardrails para adapter/baseline required ausente, UX de authoring e closure smoke. |
| F16 | `CLOSED / APPLIED` | GameObject Active State Reset Adapter: restaura apenas `activeSelf` authored, com guardrails required/optional e closure smoke, sem gameplay reset contextual. |

F10 encerrou a execução lógica de Activity content no core. Ele não adicionou authoring real de participants, scene scan, placement físico, prefab/Addressables execution, pooling gameplay use, audio, camera, actor/player mutation ou reset físico.

F11 criou o caminho canônico de reset de ciclo.

F12 tornou esse caminho utilizável e validável por authoring/QA sem transformar reset de ciclo em reset de objeto.

F13 criou o catalogo lógico owned/scoped de objetos que desbloqueou os contratos de Local/Object Reset da F14.

F14 fechou a orquestração lógica de Object Reset sem executar reset físico Unity.

F15 fechou os Unity Reset Adapters mínimos: adapters sao consumers técnicos de Object Reset, usam Object Entry identity, registram-se por participant source do framework e não podem mascarar adapter/baseline required ausente com fallback silencioso. O adapter físico fechado na F15 e Transform local baseline reset.

F16 fechou o segundo adapter primitivo: GameObject activeSelf baseline reset. Ele é útil para props simples e como peça de composição, mas Player/Actor/Timer/NPC/Pickup/Door contextual reset continuam fora.

Decisão de realinhamento pós-F16: não avancar agora para contextual reset de Player/Actor/NPC/Timer/Door/Pickup. O framework já possui participants, Object Entry e Object Reset primitivo, mas ainda não possui um modelo maduro de objeto lógico de gameplay. O próximo eixo volta ao core principal: Gate, Transition e Pause.

## Decisões arquiteturais aceitas

| Tema | Decisão aceita |
|---|---|
| Package boundary | `com.immersive.framework` e o owner de comportamento específico do framework; packages técnicos permanecem genéricos e consumidos pelo framework. |
| Core vs Unity | Framework Core define contratos, lifecycle, identity, ownership, policy, diagnostics e readiness; Unity adapters executam operações concretas de engine. |
| Core vs Gameplay | Gameplay consumers consomem contratos do core/adapters e não redefinem identidade, ownership, lifecycle ou policy. |
| Identity | Identidades tipadas pertencem ao domínio correto; não fabricar identidade por parsing de string nem comparar domínios diferentes. |
| Session/Route/Activity | Session, Route e Activity sao camadas de lifecycle do framework, não managers globais. |
| Runtime Content | Runtime roots e handles sao ownership lógico; `GameObject`, `Transform`, prefab, Addressables e hierarchy sao detalhes de adapter. |
| Content Anchor | Content Anchor e contrato lógico de declaração/binding, não materializer, registry global ou service locator. |
| Activity execution | Activity Content Execution usa participants explícitos, collection/ordering, phase plan e runtime executor lógico. |
| Cycle Reset | Cycle Reset cobre Route/Activity cycle reset; não é object reset, component reset, reload, release, snapshot restore ou pool return. |
| Trigger UX | Triggers sao entry points principais; Unity Event Bridges sao opcionais para callbacks de resultado por Inspector. |
| Object Entry | F13 fechou catalogo lógico passivo, owner tipado, collection scoped e snapshot lifecycle. Não e GameObject binding, registry vivo, reset inventory ou service locator. |
| Object Reset | F14 fechou target/resolution/plan/runtime/host/trigger/bridge, mas não executa adapters Unity. |
| Unity Reset Adapters | F15 fechou Transform local baseline reset; F16 fechou GameObject activeSelf reset. Ambos sao primitive technical participants de Object Reset, não gameplay reset contextual. |
| Gate | Gate não é UI, readiness nem input system. Ele decide se request, input, interação ou gameplay pode ser admitido em um escopo explícito naquele momento. |
| Transition | Transition é orquestração de fluxo. Fade, loading e curtain sao efeitos/adapters e não substituem Gate. |
| Pause | Pause é estado + Gate blocker. Pause não e Activity, não controla Route/Activity lifecycle e não tem `Time.timeScale` como contrato canônico. |
| Diagnostics | Falhas de contrato/config obrigatória devem ser explícitas. Não há fallback silencioso. |
| Authoring UX | Nomes públicos devem expressar intencao de uso, não detalhes internos de pipeline. |

## Boundary atual

Framework Core pode definir:

- lifecycle;
- identity;
- ownership;
- request/result contracts;
- policy;
- readiness;
- diagnostics;
- lógical binding;
- lógical release;
- reset contracts;
- cycle reset contracts;
- object entry contracts;
- participant/player entry contracts futuros;
- gate decisions/results/facts;
- transition orchestration contracts;
- pause state and pause gate policy.

Framework Core não pode executar:

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

Unity adapters futuros traduzem contratos do core para operações Unity concretas: scenes, prefabs, Addressables, `Transform` placement, hierarchy, physical release e resets mínimos de engine. F15/F16 já fecharam dois adapters primitivos: Transform local baseline e GameObject activeSelf baseline.

Gameplay consumers futuros possuem comportamento de produto/jogo. Player, Actor, Timer, NPC, Camera, Audio e gameplay Pooling dependem de decisão contextual antes de virar implementação. Essa decisão contextual fica deferida para F22+.

## Plano revisado F11-F22+

| Fase | Nome | Owner | Objetivo |
|---|---|---|---|
| F11 | Cycle Reset Foundation | Framework Core | `CLOSED / APPLIED`: contratos centrais de reset de ciclo, request/result, policy, diagnostics, executor mínimo, smoke runtime-host e triggers públicos, sem reset físico. |
| F12 | Cycle Reset Integration & Authoring UX | Framework Core + Editor/Authoring | `CLOSED / APPLIED`: validar e documentar UX/authoring dos triggers e bridges opcionais, sem reset físico/local. |
| F13 | Object Entry Foundation | Framework Core | `CLOSED / APPLIED`: identidade, descriptor, declaration, typed ownership, scoped collection, snapshot invalidation/refresh e closure smoke. Readiness real de gameplay fica deferida para depois de Gate/Transition/Pause e modelo de gameplay object. |
| F14 | Local/Object Reset Foundation | Framework Core | `CLOSED / APPLIED`: target canônico deriva de Object Entry atual; participant source explícita; plan/runtime executor; Runtime Host; trigger público; bridge opcional; sem Unity adapters ou gameplay reset. |
| F15 | Unity Reset Adapters mínimos | Unity Adapter | `CLOSED / APPLIED`: source Unity explícita, Transform Reset Participant com baseline local authored, guardrails required/optional, UX e closure smoke, sem gameplay consumers. |
| F16 | GameObject Active State Reset Adapter | Unity Adapter | `CLOSED / APPLIED`: reset primitivo de `activeSelf` authored, com source explícita e guardrails. |
| F17 | Gate Foundation | Framework Core | `CLOSED / F17E QA PASS`: F17A definiu boundary documental; F17B introduziu primitivas de scope/domain/decision/blocker/snapshot; F17C integrou bloqueios already-in-flight de Route/Activity/CycleReset/ObjectReset por Gate; F17D validou diagnóstico de admissão por smoke sintético; F17E fechou o handoff para F18. |
| F18 | Transition Orchestration Foundation | Framework Core | `IN PROGRESS / F18A DOCS`: orquestrar fluxo consumindo Gate, Scene Lifecycle, release, readiness e callbacks. Não é fade visual. |
| F19 | Transition Effects / Loading and Fade Adapters | Unity Adapter / Optional Effects | Efeitos de fade/loading/curtain como adapters depois do contrato lógico. Sem dependência obrigatória de DOTween/Asset Store e sem fallback silencioso para adapter required ausente. |
| F20 | Pause State and Pause Gate | Framework Core | Pause como estado + Gate blocker. Não é Activity, menu ou lifecycle de Route/Activity. |
| F21 | Pause Content / Overlay / Input Boundary | Framework Consumer / Authoring / Input Boundary | Overlay/content de Pause como consumer, usando Content Anchor/binding/runtime placement quando aplicável. Input de Pause separado de input de gameplay. |
| F22+ | Advanced Consumers / Gameplay Capabilities | Gameplay Consumer | Camera, Audio, Actor, gameplay Pooling, Projectile, Damage, Attributes, Powerups e contextual reset entram somente depois dos eixos Gate/Transition/Pause e do modelo de gameplay object amadurecerem. |

## Fechamento real F11 — Cycle Reset Foundation

F11 fechou o primeiro momento do reset: o reset canônico de ciclo. O objetivo era criar o formato do core antes de reset local, player, actor ou gameplay.

| Corte | Status | Resultado |
|---|---|---|
| F11A | `CLOSED / COMPILE PASS` | Contratos centrais e executor isolado de Cycle Reset. |
| F11B | `CLOSED / SYNTHETIC SMOKE EVOLVED` | Smoke sintético/probe consolidado no runner de QA. |
| F11C | `CLOSED / RUNTIME PATH PASS` | Request canônico interno: `FrameworkRuntimeHost -> GameFlowRuntime -> RouteLifecycleRuntime -> CycleResetRuntime`. |
| F11D/F11E | `CLOSED / QA CANVAS SMOKE PASS` | Botao `Run Cycle Reset Runtime Host Smoke` valida Route e Activity reset com participantes sintéticos. |
| F11F | `CLOSED / TRIGGER PASS` | `RouteCycleResetTrigger` e `ActivityCycleResetTrigger` solicitam reset de ciclo via UI/objetos de cena. |
| F11G | `CLOSED / DOCS` | Fechamento documental da fase e fronteira para F12. |

Evidência aceita de F11E:

```text
QA Smoke completed. name='Cycle Reset Runtime Host Smoke'.
Route step: status='Succeeded', participants='3', blockingIssues='0'.
Activity step: status='Succeeded', participants='2', blockingIssues='0'.
```

Evidência aceita de F11F:

```text
Cycle Reset Request completed. scope='Route' source='RouteCycleResetTrigger' status='SucceededNoParticipants' blockingIssues='0'.
Cycle Reset Request completed. scope='Activity' source='ActivityCycleResetTrigger' status='SucceededNoParticipants' blockingIssues='0'.
```

`SucceededNoParticipants` é resultado valido para triggers reais em F11 porque ainda não existe discovery real nem participantes físicos. Participantes sintéticos existem apenas no smoke de QA.

F11 não implementa:

- Object Reset;
- Component Reset;
- Player Reset;
- Actor Reset;
- Transform/Rigidbody/Animator reset;
- pool return;
- scene reload;
- snapshot restore;
- gameplay mutation.

A fronteira de F11 é: o core já sabe receber, planejár, executar e diagnosticar reset de Route/Activity. O comportamento concreto de objetos fica para F14/F15+ depois de Object Entry.

## Fechamento real F12 — Cycle Reset Integration & Authoring UX

F12 fechou a superfície de uso/authoring do Cycle Reset. A fase não adicionou reset físico nem reset local; ela tornou os triggers e a UX de resultado verificaveis.

| Corte | Status | Resultado |
|---|---|---|
| F12A | `CLOSED / AUTHORING VALIDATION PASS` | Inspectors/guardrails e validação de triggers carregados: contadores de Route/Activity Cycle Reset Trigger no QA Authoring Validation. |
| F12B | `CLOSED / TRIGGER RESULT UX PASS` | Triggers passam a expor resumo do último resultado, contadores e status para Inspector/UX. |
| F12C | `CLOSED / TRIGGER SMOKE PASS` | `Run Cycle Reset Trigger Smoke` valida entry points públicos dos triggers. |
| F12D | `CLOSED / BRIDGE SMOKE PASS` | `Run Cycle Reset Bridge Smoke` valida bridges opcionais e eventos UnityEvent esperados. |
| F12E | `CLOSED / DOCS` | Fechamento documental da fase e fronteira para F13. |

Evidência aceita de F12A:

```text
QA Authoring Validation completed.
routeCycleResetTriggers='1'
activityCycleResetTriggers='1'
issues='0'
```

Evidência aceita de F12C:

```text
QA Smoke completed. name='Cycle Reset Trigger Smoke'.
Route trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
Activity trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
```

Evidência aceita de F12D:

```text
QA Smoke completed. name='Cycle Reset Bridge Smoke'.
Route bridge: submitted='1', succeeded='1', succeededNoParticipants='1', completed='1', failed='0', ignored='0'.
Activity bridge: submitted='1', succeeded='1', succeededNoParticipants='1', completed='1', failed='0', ignored='0'.
```

### Regra de opcionalidade das bridges

O componente obrigatório para uso é o trigger:

```text
RouteCycleResetTrigger
ActivityCycleResetTrigger
```

Botoes/UI podem chamar diretamente:

```text
RouteCycleResetTrigger.RequestRouteCycleReset()
ActivityCycleResetTrigger.RequestActivityCycleReset()
```

A Unity Event Bridge é opcional. Ela existe somente para expor callbacks de resultado por Inspector/UnityEvent, como request submitted, succeeded, failed, completed, succeeded no participants ou completed with warnings.

Modelo recomendado:

```text
1 trigger por objeto de reset.
0 ou 1 bridge opcional no mesmo GameObject do trigger, apenas quando callbacks UnityEvent forem necessários.
Validator não exige bridge.
Guia de uso deve mostrar primeiro o caminho simples sem bridge.
```

F12 não implementa:

- Object Reset;
- Component Reset;
- Player Reset;
- Actor Reset;
- Transform/Rigidbody/Animator reset;
- pool return;
- scene reload;
- snapshot restore;
- gameplay mutation.

A fronteira de F12 é: Cycle Reset e utilizável via QA, triggers, Inspector e bridges opcionais. O reset de objetos reais continua bloqueado até Object Entry e Local/Object Reset.

## Fechamento real F13 — Object Entry Foundation

F13 começou como fundacao passiva de objetos lógicos. Ela não executa entrada física e não transforma `ObjectEntryDeclaration` em binding para o próprio GameObject.

| Corte | Status | Resultado |
|---|---|---|
| F13A | `PASS` | Primitivas: `ObjectEntryId`, scope, source kind, requiredness, descriptor, request/result, issues e set imutável. |
| F13B | `PASS` | Synthetic Set Smoke validou scopes, required/optional e rejeição de identity duplicada. |
| F13C | `PASS` | `ObjectEntryDeclaration` scene-authored passiva e authoring validation. |
| F13C Fix01 | `PASS` | Remoção de `GetInstanceID()` para Unity 6.5. |
| F13D | `PASS` | Declaration Source converte declarations em `ObjectEntrySet`. |
| F13E | `PASS` | Diagnostics separam declarations, candidates, accepted, rejected e aggregate status. |
| F13F | `PASS` | Runtime integration smoke coletou declarations carregadas, inclusive inactive. |
| F13G | `PASS` | `ObjectEntryRuntimeContextSnapshot` passivo e consultável. |
| F13H | `PASS` | Runtime Host guarda/expoe o último snapshot por refresh explícito. |
| F13I | `CLOSED / DOC-AUDIT` | Reconciliacao entre ADR original, implementação real e critérios restantes. |
| F13J | `CLOSED / PASS` | Owner tipado por scope, coleta pelas cenas da Route ativa e filtering de foreign owners. |
| F13K | `CLOSED / PASS` | Invalidation/refresh automático do snapshot em startup e boundaries de Route/Activity. |
| F13L | `CLOSED / PASS + DOCS` | Closure smoke, QA panel hygiene e fechamento documental. |

Evidência aceita de F13H:

```text
QA Smoke completed. name='Object Entry Runtime Host Snapshot Exposure Smoke'.
hostSnapshotAvailable='True'
snapshotAvailable='True'
resultStatus='Accepted'
declarations='3'
acceptedDeclarations='3'
rejectedDeclarations='0'
qaRouteFound='True'
qaActivityFound='True'
blockingIssues='0'
```

As três lacunas encontradas na auditoria F13I foram resolvidas:

```text
OwnerIdentity e obrigatória no snapshot autoritativo e validada por domain/scope.
O Runtime Host coleta somente cenas da composição da Route ativa.
Startup e boundaries de Route/Activity invalidam e reconstroem o snapshot.
```

Evidência final aceita:

```text
QA Smoke completed. name='Object Entry Foundation Closure Smoke'.
snapshotAvailable='True'
lifecycleSource='True'
ownerDomainsValid='True'
activeOwnersValid='True'
countInvariant='True'
revision='1'
invalidations='1'
resultStatus='Accepted'
filteredDeclarations='1'
blockingIssues='0'
nonBlockingIssues='0'
```

Higiene do QA Canvas no fechamento:

```text
Os sete botões intermediários de Object Entry foram removidos do painel.
Run Object Entry Foundation Closure Smoke e o único botão canônico da F13.
Runners intermediários permanecem internos para regressao/evidência.
```

Readiness por objeto não entra artificialmente na F13. Ela pertence a uma fase futura de Participant/Contextual Reset executavel.

## Fechamento real F14 — Local/Object Reset Foundation

F14 fechou a orquestração lógica direcionada a um Object Entry específico. Ela reutiliza o padrão descriptor/source/plan/result do Cycle Reset, mas mantém target e executor separados.

Decisões aplicadas:

```text
ObjectResetTarget = ObjectEntryId + ObjectEntryScope + OwnerIdentity.
Target deve existir no ObjectEntryRuntimeContextSnapshot atual.
IObjectResetParticipant é o único participant contract; não existe ILocalResetParticipant paralelo.
IObjectResetParticipantSource fornece participants conhecidos sem scene scan.
Ordering usa Order + sourceIndex + ParticipantId estável.
Runtime Host expoe RequestObjectResetAsync(...).
ObjectResetTrigger é o entry point público para UI/Inspector.
ObjectResetTriggerUnityEventBridge e opcional.
Reset Baseline payload pertence aos adapters concretos da F15.
Cycle Reset não chama Object Reset automaticamente.
```

| Corte | Status | Objetivo |
|---|---|---|
| F14A | `CLOSED / ADR` | Reconciliar e aceitar Local/Object Reset após o fechamento real da F13. |
| F14B | `CLOSED / PASS` | Target, request, policy, status, issues e synthetic target smoke. |
| F14C | `CLOSED / PASS` | Participant descriptor/interface/source, validation e ordering. |
| F14D | `CLOSED / PASS` | Plan, context, participant result, executor e aggregate result. |
| F14E | `CLOSED / PASS` | Runtime Host resolve target contra snapshot atual e executa participant source explícita. |
| F14F | `CLOSED / PASS` | Trigger público com result UX para Inspector/UI. |
| F14G | `CLOSED / PASS` | Bridge opcional de UnityEvent para callbacks de resultado. |
| F14H | `CLOSED / DOCS + QA HYGIENE` | Closure smoke canônico, limpeza de botões intermediários e documentacao. |

Smoke canônico final:

```text
Run Object Reset Foundation Closure Smoke
```

## Fechamento real F17 — Gate Foundation

F17 fechou a fundação de Gate como linguagem canônica de admissão do framework. A fase não criou um registry global de Gate, não criou authoring asset, não criou Pause, Transition, Input, UI ou gameplay object model.

| Corte | Status | Resultado |
|---|---|---|
| F17A | `CLOSED / DOCUMENTATION PASS` | Roadmap e ADRs realinhados: Gate antes de Transition e Pause; contextual reset e consumers avançados movidos para F22+. |
| F17B | `CLOSED / PRIMITIVES` | Primitivas passivas: `GateScope`, `GateDomain`, `GateDecisionStatus`, `GateDecision`, `GateBlocker`, `GateEvaluationResult` e `GateSnapshot`. |
| F17C | `CLOSED / REGRESSION SMOKE PASS` | Admissão already-in-flight de Route/Activity/CycleReset/ObjectReset passa por `GateEvaluationResult`, preservando result kinds existentes. |
| F17D | `CLOSED / QA SMOKE PASS` | `Run Gate Admission Diagnostics Smoke` valida admissões allowed/blocked por diagnóstico sintético estável. |
| F17E | `CLOSED / DOCS + HANDOFF` | Fechamento da fase e preparação explícita para F18 Transition Orchestration. |

Evidência aceita de F17C:

```text
Standard Smoke completed.
Activity Baseline Smoke completed.
Cycle Reset Bridge Smoke completed.
Object Reset GameObject Active Closure Smoke completed.
Object Reset Unity Adapters Closure Smoke completed.
```

Evidência aceita de F17D:

```text
QA Smoke completed. name='Gate Admission Diagnostics Smoke'.
step='allowed' passed='True' status='Allowed'.
step='route-in-flight' passed='True' status='Blocked' expectedBlocker='route-request-in-flight'.
step='activity-in-flight' passed='True' status='Blocked' expectedBlocker='activity-request-in-flight'.
step='cycle-reset-in-flight' passed='True' status='Blocked' expectedBlocker='cycle-reset-request-in-flight'.
step='object-reset-in-flight' passed='True' status='Blocked' expectedBlocker='object-reset-request-in-flight'.
```

F17 não implementa:

- Gate registry global;
- Gate authoring asset;
- Gate editor tooling;
- request queue;
- Pause state/runtime;
- Transition runtime;
- Input binding;
- gameplay interaction gate real;
- Player/Actor/NPC/Timer/Door/Pickup contextual reset;
- lifecycle paralelo ou service locator.

A fronteira de F17 é: o core possui linguagem de admissão, decisão bloqueada/permitida, blockers/facts/snapshot e diagnóstico QA para request admission. F18 pode consumir Gate para Transition Orchestration sem transformar fade/loading em Gate.


## Implementação F18A — Transition Orchestration Plan

F18A aceita a fronteira operacional de Transition antes de qualquer runtime visual. A fase deve começar por contrato lógico e diagnóstico, não por fade/loading.

F18A decide que Transition é o relato operacional de uma mudança de fluxo:

```text
request admitted
transition opened
Gate blocker applied
previous scope exit observed
content release observed
scene/content operation observed
next scope enter observed
readiness observed
transition completed/failed
Gate blocker released
facts emitted
```

F18A não cria código C#, não cria adapter visual, não altera Route/Activity flow e não cria lifecycle paralelo.

Sequência planejada de F18:

| Corte | Status | Objetivo |
|---|---|---|
| F18A | `CLOSED / ADR IMPLEMENTATION PLAN` | Aceitar ADR operacional e definir sequência segura para F18. |
| F18B | `NEXT` | Primitivas passivas de Transition: operação, tipo, fase/status, plano/resultado/snapshot. |
| F18C | `PLANNED` | Smoke/diagnóstico sintético de resultados de Transition sem trocar cenas. |
| F18D | `PLANNED` | Relação lógica entre operação ativa de Transition e Gate blocker. |
| F18E | `PLANNED` | Observação mínima de Route/Activity orchestration, se necessária, preservando happy path e result kinds existentes. |
| F18F | `PLANNED` | Fechamento da fase e handoff para F19 Transition Effects. |

F18 não implementa:

- fade visual;
- loading screen;
- curtain;
- DOTween;
- Pause state/runtime;
- input real;
- gameplay object model;
- contextual reset;
- service locator;
- manager global de Transition.

## Guardrails pós-F13

- Core lifecycle antes de gameplay.
- Reset de ciclo antes de reset local.
- Local/Object Reset so depois do fechamento de Object Entry.
- Gate vem antes de Transition e Pause.
- Transition e Pause consomem Gate.
- Transition Effects vem depois de Transition Orchestration.
- Pause vem depois de Gate e depois de Transition Orchestration.
- Camera, Audio, Actor e Pooling so depois de Gate/Transition/Pause e do modelo de gameplay object amadurecer.
- Projectile, Damage, Attributes e Powerups ficam no fim.
- F11 é `Cycle Reset Foundation`; F12 e `Cycle Reset Integration & Authoring UX`; F13 e `Object Entry Foundation`.
- Reset físico não entra no core antes dos contratos lógicos e adapters mínimos estarem definidos.
- Adapter Unity não deve virar gameplay consumer.
- Gameplay consumer não deve redefinir lifecycle, identity, ownership, policy ou diagnostics do core.
- Documento obsoleto/substituído não deve permanecer como arquivo separado.

## O que não mudar agora

- Não criar lifecycle novo por causa da consolidação documental.
- Não criar ADR separado, roadmap paralelo, tracker paralelo, closure por fase ou smoke documental separado.
- Não mover Camera, Audio, Actor, Pooling, Projectile, Damage, Attributes ou Powerups para F17-F21.
- Não criar visual Transition, Pause, Input ou gameplay antes das fases corretas. F18 pode criar apenas core lógico de Transition.
- Não tratar Gate como UI, readiness ou input system.
- Não tratar fade/loading visual como substituto de Gate.
- Não tratar Pause como Activity, menu ou `Time.timeScale` canônico.
- Não copiar arquitetura Base 2.0 para o framework package.
- Não usar Cycle Reset como atalho para Object Reset ou Player Reset.

## Próximo corte

```text
F18B - Transition Primitives
```

Entrada de F18B: criar primitivas passivas de Transition para operação, tipo, fase/status, plano/resultado e snapshot/diagnóstico, consumíveis por Gate e pelos lifecycles existentes em cortes posteriores.

F18B não deve integrar visual fade/loading, Pause menu, input real, gameplay object model, reset contextual de Player/Actor/NPC/Timer/Door/Pickup, lifecycle paralelo ou service locator. Fade/loading/curtain ficam para F19 como adapters/effects depois do contrato lógico.
