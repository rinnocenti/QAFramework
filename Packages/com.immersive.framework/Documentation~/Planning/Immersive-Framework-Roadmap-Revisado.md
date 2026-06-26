# Immersive Framework Roadmap Revisado

Plano canonico do package `com.immersive.framework`.

Este e o unico arquivo de planejamento do framework. Decisoes aceitas, historico F0-F12, progresso real da F13 e a ordem futura ficam resumidos aqui para evitar fontes paralelas.

ADRs aceitos ficam em `Documentation~/ADRs/ADR-INDEX.md`. ADRs registram decisoes estaveis e nao substituem este roadmap operacional.

## Decisao central

`com.immersive.framework` e o package de produto que define lifecycle, identidade, ownership, policy, diagnostics, validacao e contratos de execucao do framework.

O core do framework consome `com.immersive.foundation`, `com.immersive.logging` e `com.immersive.pooling`; ele nao reimplementa esses packages tecnicos. Operacoes concretas de Unity ficam em adapters. Comportamento de jogo fica em consumers de gameplay.

## Estado consolidado

| Faixa | Status | Leitura oficial |
|---|---|---|
| F0-F12 | `CLOSED / APPLIED` | Historico real resumido neste documento. |
| F13 | `IN PROGRESS THROUGH F13H` | Fundacao passiva implementada; ownership, discovery scoped e snapshot lifecycle ainda impedem fechamento. |
| F14-F18 | `PLANNED / REVISED ORDER` | Ordem futura mantida, bloqueada pelo fechamento da F13. |

## Historico real F0-F12

| Fase | Status | Resultado fechado |
|---|---|---|
| F0 | `CLOSED / APPLIED` | Baseline minimo do package e reconciliacao de fronteiras iniciais. |
| F1 | `CLOSED / APPLIED` | Convencao de API status, identidade tipada, diagnostics e separacao entre fact tecnico e log humano. |
| F2 | `CLOSED / APPLIED` | Escopo de Session, ownership de Session content e policy de fonte de settings. |
| F3 | `CLOSED / APPLIED` | Route runtime state, status de Route content runtime e semantica de Route content set. |
| F4 | `CLOSED / APPLIED` | Activity content set, readiness baseline e binding minimo observavel de Activity content. |
| F5 | `CLOSED / APPLIED` | Local contribution foundation, identidade local e requiredness/discovery inicial. |
| F6 | `CLOSED / APPLIED` | Route scene composition e release baseline, ainda sem converter core em executor fisico generico. |
| F7 | `CLOSED / APPLIED` | Content Anchor declaration baseline como contrato logico de posicionamento. |
| F8 | `CLOSED / APPLIED` | Runtime roots, handles, materialization request/result, release logico e boundaries para adapters. |
| F9 | `CLOSED / APPLIED` | Logical Content Anchor Binding; fechou binding logico, nao placement fisico. |
| F10 | `CLOSED / APPLIED` | Activity Content Execution Core e decisoes de consumer intermediario para Input, Snapshot e Pause sem mover ownership para gameplay. |
| F11 | `CLOSED / APPLIED` | Cycle Reset Foundation: contratos, executor, runtime request path, QA Canvas smoke e triggers publicos de Route/Activity Cycle Reset, sem reset fisico. |
| F12 | `CLOSED / APPLIED` | Cycle Reset Integration & Authoring UX: guardrails, result UX, trigger smoke e bridge smoke opcional, sem reset local/fisico. |

F10 encerrou a execucao logica de Activity content no core. Ele nao adicionou authoring real de participants, scene scan, placement fisico, prefab/Addressables execution, pooling gameplay use, audio, camera, actor/player mutation ou reset fisico.

F11 criou o caminho canonico de reset de ciclo.

F12 tornou esse caminho utilizavel e validavel por authoring/QA sem transformar reset de ciclo em reset de objeto.

## Decisoes arquiteturais aceitas

| Tema | Decisao aceita |
|---|---|
| Package boundary | `com.immersive.framework` e o owner de comportamento especifico do framework; packages tecnicos permanecem genericos e consumidos pelo framework. |
| Core vs Unity | Framework Core define contratos, lifecycle, identity, ownership, policy, diagnostics e readiness; Unity adapters executam operacoes concretas de engine. |
| Core vs Gameplay | Gameplay consumers consomem contratos do core/adapters e nao redefinem identidade, ownership, lifecycle ou policy. |
| Identity | Identidades tipadas pertencem ao dominio correto; nao fabricar identidade por parsing de string nem comparar dominios diferentes. |
| Session/Route/Activity | Session, Route e Activity sao camadas de lifecycle do framework, nao managers globais. |
| Runtime Content | Runtime roots e handles sao ownership logico; `GameObject`, `Transform`, prefab, Addressables e hierarchy sao detalhes de adapter. |
| Content Anchor | Content Anchor e contrato logico de declaracao/binding, nao materializer, registry global ou service locator. |
| Activity execution | Activity Content Execution usa participants explicitos, collection/ordering, phase plan e runtime executor logico. |
| Cycle Reset | Cycle Reset cobre Route/Activity cycle reset; nao e object reset, component reset, reload, release, snapshot restore ou pool return. |
| Trigger UX | Triggers sao entry points principais; Unity Event Bridges sao opcionais para callbacks de resultado por Inspector. |
| Object Entry | F13 e catalogo logico passivo. Nao e GameObject binding, registry vivo, reset inventory ou service locator. |
| Diagnostics | Falhas de contrato/config obrigatoria devem ser explicitas. Nao ha fallback silencioso. |
| Authoring UX | Nomes publicos devem expressar intencao de uso, nao detalhes internos de pipeline. |

## Boundary atual

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
- reset contracts;
- cycle reset contracts;
- object entry contracts;
- participant/player entry contracts.

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

Unity adapters futuros traduzem contratos do core para operacoes Unity concretas: scenes, prefabs, Addressables, `Transform` placement, hierarchy, physical release e resets minimos de engine.

Gameplay consumers futuros possuem comportamento de produto/jogo. Camera, Audio, Actor e gameplay Pooling pertencem a F17. Projectile, Damage, Attributes e Powerups pertencem a F18.

## Plano revisado F11-F18

| Fase | Nome | Owner | Objetivo |
|---|---|---|---|
| F11 | Cycle Reset Foundation | Framework Core | `CLOSED / APPLIED`: contratos centrais de reset de ciclo, request/result, policy, diagnostics, executor minimo, smoke runtime-host e triggers publicos, sem reset fisico. |
| F12 | Cycle Reset Integration & Authoring UX | Framework Core + Editor/Authoring | `CLOSED / APPLIED`: validar e documentar UX/authoring dos triggers e bridges opcionais, sem reset fisico/local. |
| F13 | Object Entry Foundation | Framework Core | `IN PROGRESS`: identidade, descriptor, declaration, set, diagnostics e snapshot passivo existem; falta fechar ownership, discovery scoped e refresh/invalidation. Readiness real fica para F16. |
| F14 | Local/Object Reset Foundation | Framework Core | Definir reset local/de objeto depois de Object Entry existir; nao misturar com reset de ciclo. |
| F15 | Unity Reset Adapters minimos | Unity Adapter | Criar adapters minimos para traducao Unity de reset local/object aprovado pelo core, sem gameplay consumers. |
| F16 | Player/Participant Entry Baseline | Framework Core + Authoring | Definir baseline de entrada de player/participant sobre Object Entry, sem Actor/Camera/Audio/Pooling. |
| F17 | Advanced Consumers | Gameplay Consumer | Abrir consumers avancados somente depois de core reset/object entry estar estavel. Inclui Camera, Audio, Actor e gameplay Pooling quando aprovados. |
| F18 | Gameplay Capabilities | Gameplay Consumer | Abrir capacidades finais de gameplay como Projectile, Damage, Attributes e Powerups. |

## Fechamento real F11 — Cycle Reset Foundation

F11 fechou o primeiro momento do reset: o reset canonico de ciclo. O objetivo era criar o formato do core antes de reset local, player, actor ou gameplay.

| Corte | Status | Resultado |
|---|---|---|
| F11A | `CLOSED / COMPILE PASS` | Contratos centrais e executor isolado de Cycle Reset. |
| F11B | `CLOSED / SYNTHETIC SMOKE EVOLVED` | Smoke sintetico/probe consolidado no runner de QA. |
| F11C | `CLOSED / RUNTIME PATH PASS` | Request canonico interno: `FrameworkRuntimeHost -> GameFlowRuntime -> RouteLifecycleRuntime -> CycleResetRuntime`. |
| F11D/F11E | `CLOSED / QA CANVAS SMOKE PASS` | Botao `Run Cycle Reset Runtime Host Smoke` valida Route e Activity reset com participantes sinteticos. |
| F11F | `CLOSED / TRIGGER PASS` | `RouteCycleResetTrigger` e `ActivityCycleResetTrigger` solicitam reset de ciclo via UI/objetos de cena. |
| F11G | `CLOSED / DOCS` | Fechamento documental da fase e fronteira para F12. |

Evidencia aceita de F11E:

```text
QA Smoke completed. name='Cycle Reset Runtime Host Smoke'.
Route step: status='Succeeded', participants='3', blockingIssues='0'.
Activity step: status='Succeeded', participants='2', blockingIssues='0'.
```

Evidencia aceita de F11F:

```text
Cycle Reset Request completed. scope='Route' source='RouteCycleResetTrigger' status='SucceededNoParticipants' blockingIssues='0'.
Cycle Reset Request completed. scope='Activity' source='ActivityCycleResetTrigger' status='SucceededNoParticipants' blockingIssues='0'.
```

`SucceededNoParticipants` e resultado valido para triggers reais em F11 porque ainda nao existe discovery real nem participantes fisicos. Participantes sinteticos existem apenas no smoke de QA.

F11 nao implementa:

- Object Reset;
- Component Reset;
- Player Reset;
- Actor Reset;
- Transform/Rigidbody/Animator reset;
- pool return;
- scene reload;
- snapshot restore;
- gameplay mutation.

A fronteira de F11 e: o core ja sabe receber, planejar, executar e diagnosticar reset de Route/Activity. O comportamento concreto de objetos fica para F14/F15+ depois de Object Entry.

## Fechamento real F12 — Cycle Reset Integration & Authoring UX

F12 fechou a superficie de uso/authoring do Cycle Reset. A fase nao adicionou reset fisico nem reset local; ela tornou os triggers e a UX de resultado verificaveis.

| Corte | Status | Resultado |
|---|---|---|
| F12A | `CLOSED / AUTHORING VALIDATION PASS` | Inspectors/guardrails e validacao de triggers carregados: contadores de Route/Activity Cycle Reset Trigger no QA Authoring Validation. |
| F12B | `CLOSED / TRIGGER RESULT UX PASS` | Triggers passam a expor resumo do ultimo resultado, contadores e status para Inspector/UX. |
| F12C | `CLOSED / TRIGGER SMOKE PASS` | `Run Cycle Reset Trigger Smoke` valida entry points publicos dos triggers. |
| F12D | `CLOSED / BRIDGE SMOKE PASS` | `Run Cycle Reset Bridge Smoke` valida bridges opcionais e eventos UnityEvent esperados. |
| F12E | `CLOSED / DOCS` | Fechamento documental da fase e fronteira para F13. |

Evidencia aceita de F12A:

```text
QA Authoring Validation completed.
routeCycleResetTriggers='1'
activityCycleResetTriggers='1'
issues='0'
```

Evidencia aceita de F12C:

```text
QA Smoke completed. name='Cycle Reset Trigger Smoke'.
Route trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
Activity trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
```

Evidencia aceita de F12D:

```text
QA Smoke completed. name='Cycle Reset Bridge Smoke'.
Route bridge: submitted='1', succeeded='1', succeededNoParticipants='1', completed='1', failed='0', ignored='0'.
Activity bridge: submitted='1', succeeded='1', succeededNoParticipants='1', completed='1', failed='0', ignored='0'.
```

### Regra de opcionalidade das bridges

O componente obrigatorio para uso e o trigger:

```text
RouteCycleResetTrigger
ActivityCycleResetTrigger
```

Botoes/UI podem chamar diretamente:

```text
RouteCycleResetTrigger.RequestRouteCycleReset()
ActivityCycleResetTrigger.RequestActivityCycleReset()
```

A Unity Event Bridge e opcional. Ela existe somente para expor callbacks de resultado por Inspector/UnityEvent, como request submitted, succeeded, failed, completed, succeeded no participants ou completed with warnings.

Modelo recomendado:

```text
1 trigger por objeto de reset.
0 ou 1 bridge opcional no mesmo GameObject do trigger, apenas quando callbacks UnityEvent forem necessarios.
Validator nao exige bridge.
Guia de uso deve mostrar primeiro o caminho simples sem bridge.
```

F12 nao implementa:

- Object Reset;
- Component Reset;
- Player Reset;
- Actor Reset;
- Transform/Rigidbody/Animator reset;
- pool return;
- scene reload;
- snapshot restore;
- gameplay mutation.

A fronteira de F12 e: Cycle Reset e utilizavel via QA, triggers, Inspector e bridges opcionais. O reset de objetos reais continua bloqueado ate Object Entry e Local/Object Reset.

## Progresso real F13 — Object Entry Foundation

F13 comecou como fundacao passiva de objetos logicos. Ela nao executa entrada fisica e nao transforma `ObjectEntryDeclaration` em binding para o proprio GameObject.

| Corte | Status | Resultado |
|---|---|---|
| F13A | `PASS` | Primitivas: `ObjectEntryId`, scope, source kind, requiredness, descriptor, request/result, issues e set imutavel. |
| F13B | `PASS` | Synthetic Set Smoke validou scopes, required/optional e rejeicao de identity duplicada. |
| F13C | `PASS` | `ObjectEntryDeclaration` scene-authored passiva e authoring validation. |
| F13C Fix01 | `PASS` | Remocao de `GetInstanceID()` para Unity 6.5. |
| F13D | `PASS` | Declaration Source converte declarations em `ObjectEntrySet`. |
| F13E | `PASS` | Diagnostics separam declarations, candidates, accepted, rejected e aggregate status. |
| F13F | `PASS` | Runtime integration smoke coletou declarations carregadas, inclusive inactive. |
| F13G | `PASS` | `ObjectEntryRuntimeContextSnapshot` passivo e consultavel. |
| F13H | `PASS` | Runtime Host guarda/expoe o ultimo snapshot por refresh explicito. |
| F13I | `DOC/AUDIT` | Reconciliacao entre ADR original, implementacao real e criterios restantes. |

Evidencia aceita de F13H:

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

F13 ainda nao esta fechada. A auditoria apos F13H encontrou tres lacunas:

```text
OwnerIdentity existe no descriptor, mas declarations coletadas continuam ownerless.
CollectLoadedSceneDeclarations faz all-loaded scan e nao pode virar autoridade final do lifecycle.
O snapshot do host so muda por refresh manual e pode ficar stale apos Route/Activity change.
```

Sequencia proposta, pendente de aprovacao do ADR F13:

| Corte | Objetivo |
|---|---|
| F13J | Owner tipado por scope e collection boundary scoped pelo contexto ativo. |
| F13K | Policy de refresh/invalidation do snapshot em boundaries de Route/Activity. |
| F13L | Smoke de ownership, foreign/stale filtering e fechamento documental. |

Readiness por objeto nao entra artificialmente na F13. Ela pertence a F16, quando existir Participant Entry executavel.

## Guardrails pos-F12

- Core lifecycle antes de gameplay.
- Reset de ciclo antes de reset local.
- Objeto/player so depois de Object Entry.
- Camera, Audio, Actor e Pooling so depois do core de reset/object entry.
- Projectile, Damage, Attributes e Powerups ficam no fim.
- F11 e `Cycle Reset Foundation`; F12 e `Cycle Reset Integration & Authoring UX`.
- Reset fisico nao entra no core antes dos contratos logicos e adapters minimos estarem definidos.
- Adapter Unity nao deve virar gameplay consumer.
- Gameplay consumer nao deve redefinir lifecycle, identity, ownership, policy ou diagnostics do core.
- Documento obsoleto/substituido nao deve permanecer como arquivo separado.

## O que nao mudar agora

- Nao criar lifecycle novo por causa da consolidacao documental.
- Nao criar ADR separado, roadmap paralelo, tracker paralelo, closure por fase ou smoke documental separado.
- Nao mover Camera, Audio, Actor, Pooling, Projectile, Damage, Attributes ou Powerups para F13.
- Nao copiar arquitetura Base 2.0 para o framework package.
- Nao usar Cycle Reset como atalho para Object Reset ou Player Reset.

## Fase ativa

```text
F13 — Object Entry Foundation (in progress through F13H; reconciliation at F13I)
```

Proximo passo: revisar o ADR F13 e aprovar ou corrigir a sequencia F13J-F13L antes de novo patch runtime.
