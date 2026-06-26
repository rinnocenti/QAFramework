# Immersive Framework Roadmap Revisado

Plano canônico do package `com.immersive.framework`.

Este é o único arquivo de planejamento do framework. Decisões aceitas, histórico F0-F22 e a ordem futura ficam resumidos aqui para evitar fontes paralelas.

ADRs aceitos ficam em `Documentation~/ADRs/ADR-INDEX.md`. ADRs registram decisões estáveis e não substituem este roadmap operacional.

## Decisão central

`com.immersive.framework` é o package de produto que define lifecycle, identidade, ownership, policy, diagnostics, validação e contratos de execução do framework.

O core do framework consome `com.immersive.foundation`, `com.immersive.logging` e `com.immersive.pooling`; ele não reimplementa esses packages técnicos. Operacoes concretas de Unity ficam em adapters. Comportamento de jogo fica em consumers de gameplay.

## Estado consolidado

| Faixa | Status | Leitura oficial |
|---|---|---|
| F0-F22 | `CLOSED / APPLIED` | Histórico real resumido neste documento. |
| F17 | `CLOSED / F17E QA PASS` | Gate Foundation fechada. F17A realinhou ADRs/plano; F17B introduziu primitivas passivas; F17C integrou admissão de requests existentes via Gate; F17D adicionou smoke sintético de diagnóstico; F17E fechou a fase e preparou F18. |
| F18 | `CLOSED / F18F QA PASS + USAGE` | Transition Orchestration Foundation fechada. F18A aceitou o plano; F18B criou primitivas passivas; F18C adicionou diagnostics smoke; F18D definiu relação passiva com Gate blocker; F18E observou Route/Activity orchestration; F18F fechou a fase e criou Usage Guide. Sem fade/loading visual, Pause, Input ou gameplay. |
| F19 | `CLOSED / F19F QA PASS + USAGE` | Transition Effects fechada. Effects são adapters/consumers de F18: primitivas passivas, diagnostics smoke, adapter Unity mínimo CanvasGroup fade/curtain, policy required/optional e Usage Guide. Sem registry, ScriptableObject obrigatório, DOTween ou fallback silencioso. |
| F20 | `CLOSED / F20F QA PASS + USAGE` | Pause State/Gate fechado como core lógico. F20B adicionou primitivas passivas; F20C adicionou diagnostics smoke; F20D adicionou policy passiva Pause-to-Gate blocker; F20E adicionou request path mínimo em memória via `FrameworkRuntimeHost`/`PauseRuntime`; F20F criou Usage Guide. Sem Gate registry real, overlay, input ou `Time.timeScale`. |
| F21 | `CLOSED / F21H QA PASS + USAGE` | Save / Snapshot / Preferences / Progression Save Foundation fechada. F21B adicionou Snapshot Envelope primitives. F21C adicionou participant contracts/smoke. F21D adicionou Preferences store/PlayerPrefs adapter. F21E adicionou Progression Save port/slot/manifest. F21F adicionou JSON backend adapter/smoke. F21G adicionou runtime request path e autosave moment contracts. F21H criou Usage Guide e fechou a fase. |
| F22 | `CLOSED / F22H QA PASS` | Loading Operation / Progress / Readiness Boundary fechada. F22A-F22F fecharam operação/progresso/adapters/guide; F22G fechou readiness observation; F22H fechou result/issues. |
| F23 | `F23C APPLIED / NEXT F23D` | Pause Content Anchor Consumer e Pause Overlay Adapter Boundary aplicados; próximo corte cria contratos de Pause Input. |
| F24 | `PLANNED AFTER F23` | Unity Build Surface / Lifecycle Wiring prova contratos framework em superfícies Unity reais antes de gameplay adapters. |
| F25 | `DEFERRED` | Gameplay Adapter Foundation e consumers avançados ficam bloqueados até F24 provar build surface/lifecycle wiring. |

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
| Save | Snapshot não conhece backend. Preferences não usa slot de progressão. Progression Save usa backend port substituível. JSON futuro e adapter inicial, não contrato canônico. Backend premium futuro troca atrás da mesma interface. |
| Loading | Loading e operação/progresso/readiness. Não e fade, curtain, loading screen prefab nem substituto de SceneLifecycle. Visual de Loading fica para adapter posterior. |
| Loading orphan policy | F22A reconcilia termos existentes; F22B cria o trilho canonico em `Runtime/Loading` / `Immersive.Framework.Loading`, evitando shapes paralelos em SceneLifecycle, Transition, TransitionEffects, Save ou Pause. |
| Unity Build Surface | F24 monta superfícies Unity mínimas para provar contratos framework em cenas/prefabs reais. Não é gameplay adapter, não é montar o jogo e não substitui lifecycle owners. |
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
- pause state and pause gate policy;
- snapshot envelope contracts;
- preferences store contracts;
- progression save slot/manifest/request contracts;
- loading operation/progress/readiness contracts;
- Unity build surface/lifecycle wiring contracts.

Framework Core não pode executar:

- `Instantiate`;
- `Destroy`;
- `Addressables.Load`;
- `Addressables.Release`;
- pool rent/return;
- `Animator` reset;
- camera blend;
- UI concrete show/hide;
- save backend persistence details;
- loading screen visual presentation;
- player/actor mutation;
- gameplay state mutation.

Unity adapters futuros traduzem contratos do core para operações Unity concretas: scenes, prefabs, Addressables, `Transform` placement, hierarchy, physical release e resets mínimos de engine. F15/F16 já fecharam dois adapters primitivos: Transform local baseline e GameObject activeSelf baseline.

Gameplay consumers futuros possuem comportamento de produto/jogo. Player, Actor, Timer, NPC, Camera, Audio e gameplay Pooling dependem de decisão contextual antes de virar implementação. Essa decisão contextual fica deferida para F25+.

## Plano revisado F11-F25

| Fase | Nome | Owner | Objetivo |
|---|---|---|---|
| F11 | Cycle Reset Foundation | Framework Core | `CLOSED / APPLIED`: contratos centrais de reset de ciclo, request/result, policy, diagnostics, executor mínimo, smoke runtime-host e triggers públicos, sem reset físico. |
| F12 | Cycle Reset Integration & Authoring UX | Framework Core + Editor/Authoring | `CLOSED / APPLIED`: validar e documentar UX/authoring dos triggers e bridges opcionais, sem reset físico/local. |
| F13 | Object Entry Foundation | Framework Core | `CLOSED / APPLIED`: identidade, descriptor, declaration, typed ownership, scoped collection, snapshot invalidation/refresh e closure smoke. Readiness real de gameplay fica deferida para depois de Gate/Transition/Pause e modelo de gameplay object. |
| F14 | Local/Object Reset Foundation | Framework Core | `CLOSED / APPLIED`: target canônico deriva de Object Entry atual; participant source explícita; plan/runtime executor; Runtime Host; trigger público; bridge opcional; sem Unity adapters ou gameplay reset. |
| F15 | Unity Reset Adapters mínimos | Unity Adapter | `CLOSED / APPLIED`: source Unity explícita, Transform Reset Participant com baseline local authored, guardrails required/optional, UX e closure smoke, sem gameplay consumers. |
| F16 | GameObject Active State Reset Adapter | Unity Adapter | `CLOSED / APPLIED`: reset primitivo de `activeSelf` authored, com source explícita e guardrails. |
| F17 | Gate Foundation | Framework Core | `CLOSED / F17E QA PASS`: F17A definiu boundary documental; F17B introduziu primitivas de scope/domain/decision/blocker/snapshot; F17C integrou bloqueios already-in-flight de Route/Activity/CycleReset/ObjectReset por Gate; F17D validou diagnóstico de admissão por smoke sintético; F17E fechou o handoff para F18. |
| F18 | Transition Orchestration Foundation | Framework Core | `CLOSED / F18F QA PASS + USAGE`: contrato lógico passivo, diagnostics smoke, relação passiva com Gate blocker e observação de Route/Activity orchestration, sem visual effects ou lifecycle paralelo. |
| F19 | Transition Effects / Loading and Fade Adapters | Unity Adapter / Optional Effects | `CLOSED / F19F QA PASS + USAGE`: effects fechados como adapters/consumers de F18 Transition Orchestration. F19B criou primitivas passivas; F19C validou diagnostics; F19D adicionou adapter Unity mínimo CanvasGroup fade/curtain; F19E fechou policy/guardrails required/optional; F19F adiciona usage guide e compacta o QA Canvas. Sem dependência obrigatória de DOTween/Asset Store, sem registry, sem ScriptableObject obrigatório e sem fallback silencioso para adapter required ausente. |
| F20 | Pause State and Pause Gate | Framework Core | `CLOSED / F20F QA PASS + USAGE`: Pause como estado + Gate blocker. F20B primitives; F20C diagnostics smoke; F20D relação passiva Pause-to-Gate; F20E request path mínimo em memória; F20F Usage Guide. Não é Activity, menu, overlay, input system, `Time.timeScale` contract ou lifecycle de Route/Activity. |
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Framework Core + Save Module | `CLOSED / F21H QA PASS + USAGE`: ADR plan aceito, Snapshot Envelope primitives aplicadas, participant contracts/smoke sintetico aplicados, Preferences store/PlayerPrefs adapter aplicado, Progression Save port/slot/manifest primitives aplicadas, JSON backend/smoke aplicados, runtime request path/autosave moment contracts aplicados e Usage Guide criado. |
| F22 | Loading Operation / Progress / Readiness Boundary | Framework Core + Loading Module | `CLOSED / F22H`: operação, steps, progresso ponderado, observation adapter, loading screen adapter boundary, readiness observation, result/issues e usage guide. Loading não é visual, fade, curtain, prefab, TransitionEffect vocabulary ou substituto de SceneLifecycle. |
| F23 | Pause Content / Overlay / Input Boundary | Framework Consumer / Authoring / Input Boundary | `F23C APPLIED`: Pause Content Anchor Consumer e Pause Overlay Adapter Boundary adicionados. Próximo: F23D Pause Input Boundary Contracts. |
| F24 | Unity Build Surface / Lifecycle Wiring | Framework Unity Build Surface / Lifecycle Wiring | `PLANNED AFTER F23`: prova contratos framework em Unity real antes de gameplay adapters. Inclui wiring explícito, objetos/prefabs mínimos e smokes reais quando aplicável. Não cria gameplay adapters nem monta o jogo. |
| F25 | Gameplay Adapter Foundation | Gameplay Adapter / Consumer Boundary | Camera, Audio, Actor, gameplay Pooling, Projectile, Damage, Attributes, Powerups e contextual reset entram somente depois de F24 e do modelo de gameplay object amadurecerem. |

## Plano F19 — Transition Effects / Loading and Fade Adapters

F19 implementa effects como adapters depois da orquestração lógica da F18.

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F19A | `CLOSED / ADR PLAN ACCEPTED` | Aceitar boundary/implementation plan para effects. | Nenhum. Sem cena, objeto ou ScriptableObject. |
| F19B | `CLOSED / PRIMITIVES APPLIED` | Criar primitivas/contratos passivos de effects: id, kind, requiredness, status, request, result, plan e snapshot. | Nenhum. Sem cena, objeto ou ScriptableObject. |
| F19C | `CLOSED / DIAGNOSTICS SMOKE APPLIED` | Smoke sintético de diagnostics de effects: request, plan, succeeded result, optional skipped result, required missing adapter result e snapshot. | Nenhum. Sem cena, objeto ou ScriptableObject. |
| F19D | `CLOSED / UNITY ADAPTER BOUNDARY APPLIED` | Primeiro adapter Unity mínimo para fade/curtain boundary: `ITransitionEffectAdapter`, `UnityFadeCurtainEffectAdapter` e smoke transitório. | Smoke canônico não exige cena salva. Setup visual manual opcional: GameObject com CanvasGroup + UnityFadeCurtainEffectAdapter; guia em `Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md`. |
| F19E | `CLOSED / POLICY GUARDRAILS APPLIED` | Required/optional effect policy e guardrails de authoring: required missing adapter bloqueia, optional missing adapter não bloqueia, duplicate effect id bloqueia. | Nenhum setup de cena/SO. A policy usa lista explícita de adapters passada pelo caller/smoke; sem registry/discovery. |
| F19F | `CLOSED / QA PASS + USAGE` | Fechamento, Usage Guide, compactação do QA Canvas e handoff para F20 Pause State/Gate. | Usage guide criado em `Documentation~/Guides/F19-Transition-Effects-Usage.md`. |

F19 não autoriza DOTween, Asset Store, loading screen canônico, Pause menu, input real, gameplay object model ou lifecycle paralelo. F19D autoriza apenas o adapter mínimo CanvasGroup fade/curtain, sem tweening e sem integração real com Transition runtime. F19E autoriza apenas policy/guardrails passivos sobre lista explícita de adapters, sem ScriptableObject, registry ou discovery. F19F fecha a fase com usage guide e reduz a superfície padrão do QA Canvas: smokes principais ficam visíveis e diagnósticos de fase ficam colapsados.

## Plano F20 — Pause State and Pause Gate

F20 implementa Pause como estado lógico e relação com Gate. Ele não começa por menu, overlay, input ou `Time.timeScale`.

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F20A | `CLOSED / ADR PLAN ACCEPTED` | Aceitar boundary/implementation plan para Pause State/Gate. | Nenhum. Documentação apenas. |
| F20B | `CLOSED / PRIMITIVES APPLIED` | Criar primitivas passivas de Pause: state, request, result, reason/source, snapshot e issue/fact shape. | Nenhum. Sem cena, objeto ou ScriptableObject. |
| F20C | `CLOSED / DIAGNOSTICS SMOKE APPLIED` | Smoke sintético de diagnostics de Pause: request, pause applied, resume applied, toggle target, idempotência, rejected result e snapshot. | Nenhum. Sem cena, objeto, Canvas, input, Gate real ou `Time.timeScale`. |
| F20D | `CLOSED / PAUSE GATE BLOCKER POLICY APPLIED` | Relação passiva Pause-to-Gate blocker e smoke. | Nenhum. Sem Gate registry/runtime global. |
| F20E | `CLOSED / MINIMAL RUNTIME REQUEST PATH APPLIED` | Request path mínimo de Pause via `FrameworkRuntimeHost`/`PauseRuntime`, com snapshot e GateSnapshot derivados. | Nenhum setup salvo. Sem overlay/input/`Time.timeScale`/Gate registry. |
| F20F | `CLOSED / QA PASS + USAGE` | Fechamento, Usage Guide e handoff para F21 Save / Snapshot / Preferences / Progression Save Foundation. | `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`. |

F20B/F20C/F20D/F20E/F20F não autorizam Pause menu, overlay, input real, `Time.timeScale` adapter, loading screen, fade/curtain ownership, Pause como Activity ou lifecycle paralelo. F20E adiciona apenas request path mínimo em memória, sem Gate registry real. Setup visual/manual move para F23.

### Próximo corte recomendado

```text
F22D - SceneLifecycle / Transition Loading Observation Adapter
```

## Plano F21 — Save / Snapshot / Preferences / Progression Save Foundation

F21 abre Save antes de Pause visual/gameplay. O objetivo e separar Snapshot, Preferences e Progression Save sem acoplar contrato canonico a um backend.

Decisoes de boundary:

```text
Snapshot does not know backend.
Preferences does not use progression slots.
Progression Save uses a replaceable backend port.
Future JSON backend is the initial adapter, not the canonical contract.
Future premium backend must swap behind the same interface.
```

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F21A | `APPLIED / DOCS ONLY` | Aceitou plano ADR de Save/Snapshot/Preferences/Progression e realinhou F22-F23. | Nenhum. Documentacao apenas. |
| F21B | `APPLIED / PRIMITIVES` | Snapshot Envelope Primitives: id, scope, schema id/version, payload format, payload e envelope. | Nenhum. Sem backend, PlayerPrefs, JSON, participante, UI ou asmdef. |
| F21C | `APPLIED / PARTICIPANT CONTRACTS + SYNTHETIC SMOKE` | Snapshot Participant Contracts + Diagnostics Smoke. | Nenhum setup salvo. Smoke sintetico via QA Canvas. |
| F21D | `APPLIED / PREFERENCES STORE + PLAYERPREFS ADAPTER` | Preferences Store Contracts + PlayerPrefs Backend. | PlayerPrefs existe apenas como adapter de Preferences; não é Snapshot ou Progression Save. |
| F21E | `APPLIED / PROGRESSION PORT + SLOT/MANIFEST PRIMITIVES` | Progression Save Port + Slot/Manifest Primitives. | Nenhum backend concreto. Sem JSON, file paths, PlayerPrefs, autosave/load moment, runtime request path, UI ou asmdef. |
| F21F | `APPLIED / JSON BACKEND + DIAGNOSTICS SMOKE` | JSON Progression Backend + Diagnostics Smoke. | JSON e adapter inicial por trás de `IProgressionSaveStore`; não é contrato canônico. Sem runtime request path. |
| F21G | `APPLIED / RUNTIME REQUEST PATH + AUTOSAVE MOMENTS` | Progression Save Runtime Request Path + Autosave Moment Contracts. | `ProgressionSaveRuntime`, explicit save/load/delete requests, passive moments and diagnostics smoke. Sem UI, scene object, ScriptableObject, Snapshot capture or autosave scheduler. |
| F21H | `CLOSED / QA PASS + USAGE` | Closure + Usage Guide. | `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. |

F21A nao implementa codigo, runtime, backend, PlayerPrefs, JSON, UI, scene object, prefab, ScriptableObject ou asmdef. F21B implementa apenas primitivas passivas de Snapshot Envelope em `Runtime/Snapshot`, sem backend, participante, capture/restore runtime ou Progression Save. F21C implementa participant contracts e smoke sintetico, sem discovery, orchestration runtime, backend, PlayerPrefs, JSON, slots, UI ou asmdef. F21D implementa `Runtime/Preferences`, `IPreferencesStore`, `PlayerPrefsPreferencesStore` e `Run Preferences Store Diagnostics Smoke`; PlayerPrefs fica limitado a Preferences, com marcador de tipo por chave para evitar fallback silencioso. F21E implementa `Runtime/ProgressionSave`, slot/record/backend identities, payload/record/manifest primitives, status/result primitives e `IProgressionSaveStore`; nenhum backend concreto, JSON, file path, PlayerPrefs, autosave/load moment, runtime request path, UI ou asmdef entra nesse corte. F21F implementa `JsonProgressionSaveStore` atras do port. F21G implementa `ProgressionSaveRuntime`, requests explicitos save/load/delete e `ProgressionSaveMoment` passivo; nao cria Snapshot capture, autosave scheduler, Route/Activity hook, UI ou setup asset. F21H fecha a fase com usage guide. Snapshot segue a decisao F10: Snapshot e diferente de Reset; Reset Baseline nao e Save Snapshot. F10 fica como historico conceitual; o trilho operacional canonico e F21.


## F21A result — Save/Loading ADR Plan

F21A is documentation-only. It updates roadmap, README and ADR index, accepts `F21-ADR-SAVE-001`, creates/accepts `F22-ADR-LOADING-001` as the F22 boundary plan, and moves Pause Content/Overlay/Input to F23. F24 later becomes Unity Build Surface / Lifecycle Wiring, and Gameplay Adapter Foundation moves to F25.

Preserved boundaries:

```text
Snapshot: runtime state capture/restore contracts only; no backend.
Preferences: user/application settings only; no progression slot.
Progression Save: slots/manifests/requests plus replaceable backend port; no concrete backend as canonical contract.
Loading: operation/progress/readiness reporting only; no fade, curtain, screen prefab or SceneLifecycle replacement.
Pause visual/content/input: deferred to F23.
Gameplay adapters: deferred to F25 after F24 Unity Build Surface / Lifecycle Wiring.
```

F21A added no runtime code, asmdef, backend, PlayerPrefs, JSON, UI, scene object, prefab or ScriptableObject.



## F21B result — Snapshot Envelope Primitives

F21B adds passive Snapshot primitives under `Runtime/Snapshot`:

```text
SnapshotEnvelopeId
SnapshotScope
SnapshotSchemaId
SnapshotSchemaVersion
SnapshotPayloadFormat
SnapshotPayload
SnapshotEnvelope
```

The cut also adds `Snapshot` as an explicit `FrameworkIdentityDomain` so Snapshot envelope/schema identities do not reuse Save, Content, Diagnostics or Progression semantics.

F21B deliberately does not add backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave/load moments, participant contracts, capture/restore execution, UI, scene object, prefab, ScriptableObject or asmdef change.

## F21C result — Snapshot Participant Contracts + Diagnostics Smoke

F21C adds backend-agnostic Snapshot participant contracts under `Runtime/Snapshot`:

```text
SnapshotParticipantId
SnapshotParticipantRequiredness
SnapshotParticipantResultStatus
SnapshotParticipantDescriptor
SnapshotCaptureContext
SnapshotRestoreContext
SnapshotParticipantCaptureResult
SnapshotParticipantRestoreResult
ISnapshotParticipant
```

F21C adds `Run Snapshot Participant Diagnostics Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. The smoke validates descriptor/context matching, synthetic capture, synthetic restore, foreign-envelope rejection, optional skip semantics and the boundary between canonical Save Snapshot contracts and older diagnostic/runtime snapshots.

F21C deliberately does not add participant discovery, Snapshot orchestration runtime, backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave/load moments, UI, scene object, prefab, ScriptableObject or asmdef change.

Known non-save snapshot types after F21C remain outside the canonical Save Snapshot namespace: `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot`, `ObjectEntryRuntimeContextSnapshot`, and CycleReset immutable copy helpers.

## F21E result — Progression Save Port + Slot/Manifest Primitives

F21E adds passive Progression Save primitives under `Runtime/ProgressionSave`:

```text
ProgressionSaveSlotId
ProgressionSaveRecordId
ProgressionSaveBackendId
ProgressionSavePayloadFormat
ProgressionSavePayload
ProgressionSaveSlotRecord
ProgressionSaveManifestEntry
ProgressionSaveManifest
ProgressionSaveReadStatus
ProgressionSaveWriteStatus
ProgressionSaveDeleteStatus
ProgressionSaveReadResult
ProgressionSaveWriteResult
ProgressionSaveDeleteResult
ProgressionSaveManifestReadResult
ProgressionSaveManifestWriteResult
IProgressionSaveStore
```

F21E adds `ProgressionSave` to `FrameworkIdentityDomain`. Progression Save now has logical slots, stored records, manifest metadata and a replaceable store port. It does not add a concrete backend, JSON serialization, file paths, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef change.


## F21F result — JSON Progression Backend + Diagnostics Smoke

F21F adds `JsonProgressionSaveStore` behind the existing `IProgressionSaveStore` port. It writes local JSON records/manifests as an adapter detail and does not make JSON, file path, manifest file name or slot file name part of the canonical framework contract.

F21F also adds `Run Progression Save JSON Backend Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. The smoke validates missing manifest/slot, write/read roundtrip, manifest update, corrupt slot detection, delete cleanup and boundary separation.

F21F does not add Snapshot backend usage, Preferences usage, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

## F21G result — Progression Save Runtime Request Path + Autosave Moment Contracts

F21G adds an explicit runtime request path under `Runtime/ProgressionSave`:

```text
ProgressionSaveRequestId
ProgressionSaveMomentId
ProgressionSaveRequestKind
ProgressionSaveMomentKind
ProgressionSaveRequestStatus
ProgressionSaveMoment
ProgressionSaveRequest
ProgressionSaveRequestResult
ProgressionSaveRuntime
```

`ProgressionSaveRuntime` executes explicit save/load/delete requests against an injected `IProgressionSaveStore`. The runtime does not discover Snapshot participants, capture gameplay state, schedule autosave, listen to Route/Activity lifecycle, create UI, create scene objects or own a backend singleton.

F21G also adds `Run Progression Save Runtime Request Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. The smoke validates request/moment contracts, manual save, load, passive autosave moment save, missing load, delete cleanup and boundary separation.

F21G does not add Snapshot capture orchestration, Preferences usage, PlayerPrefs usage, autosave scheduler, Route/Activity lifecycle hook, UI, scene object, prefab, ScriptableObject or asmdef changes.

## F21H result — Closure + Usage Guide

F21H closes Save / Snapshot / Preferences / Progression Save Foundation with `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. No runtime code, backend behavior, PlayerPrefs behavior, JSON behavior, UI, scene object, prefab, ScriptableObject or asmdef is added in the closure cut.

F21 closed evidence:

```text
Run Snapshot Participant Diagnostics Smoke: PASS
Run Preferences Store Diagnostics Smoke: PASS
Run Progression Save JSON Backend Smoke: PASS
Run Progression Save Runtime Request Smoke: PASS
```

F22G/F22H debt closure accepted. F23A applied. F23B Pause Content Anchor Consumer Contracts applied. F23C Pause Overlay Adapter Boundary applied. Next cut: F23D — Pause Input Boundary Contracts.

## Plano F22 — Loading Operation / Progress / Readiness Boundary

F22 define Loading como contrato de operacao/progresso/readiness. Loading nao substitui SceneLifecycle e nao e visual.

Decisoes de boundary:

```text
Loading is not fade.
Loading is not curtain.
Loading is not a loading screen prefab.
Loading is not a SceneLifecycle replacement.
Loading visual belongs to a later adapter.
```

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F22A | `APPLIED / DOCS ONLY` | Loading Architecture ADR Plan. | Nenhum. Documentacao apenas. |
| F22B | `APPLIED / PRIMITIVES` | Loading Operation / Step / Weighted Progress Primitives. | Nenhum. Sem UI/backend. |
| F22C | `APPLIED / AGGREGATION SMOKE` | Loading Progress Aggregation Smoke. | Smoke sintetico em QA Canvas; sem SceneLifecycle/Transition/UI. |
| F22D | `APPLIED / OBSERVATION ADAPTER` | SceneLifecycle / Transition Loading Observation Adapter. | Adapter de observacao, sem substituir lifecycle. |
| F22E | `APPLIED / LOADING SCREEN ADAPTER BOUNDARY` | Loading Screen Adapter Boundary. | Contratos e smoke sintético; sem UI/prefab concreto. |
| F22F | `APPLIED / CLOSED` | Closure + Usage Guide. | Usage guide criado em `Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. |
| F22G | `APPLIED / READINESS QA PASS` | Loading Readiness Observation Primitives. | Smoke sintético em QA Canvas; sem mutation/lifecycle/gameplay. |
| F22H | `APPLIED / RESULT QA PASS` | Loading Result / Issue Primitive Closure. | Smoke sintético em QA Canvas; sem retry/fallback/lifecycle. |

F22 nao cria fade, curtain, loading screen prefab, UI concreta, scene object ou lifecycle paralelo. F19 continua owner de transition effects/fade/curtain; F22 apenas relata loading operation/progress/readiness.

F22A result: documentation-only architecture plan accepted. F22A reconciles existing loading-like names so they do not become ghosts or parallel tracks. `SceneLifecycle` remains the owner of Unity scene load/unload execution. Route Scene Composition remains route scene evidence. `Transition` remains flow orchestration. `TransitionEffects.LoadingScreen` and `TransitionEffects.LoadingProgress` remain visual/effect-facing vocabulary, not the canonical F22 Loading operation/progress model. No runtime code, asmdef, primitive, smoke, UI, scene object, prefab, ScriptableObject, backend, PlayerPrefs or JSON is added by F22A.

F22B result: primitive-only Loading namespace applied. Added `Runtime/Loading` with operation id, step id, operation/step statuses, normalized progress, step weight, weighted progress, step record and operation record. Added `FrameworkIdentityDomain.Loading`. F22B does not add aggregation runtime, smoke, SceneLifecycle/Transition adapter, readiness wait, LoadingResult/LoadingFailure, visual adapter, UI, fade, curtain, scene object, prefab, ScriptableObject, backend, PlayerPrefs, JSON or asmdef changes.

F22C result: Loading progress aggregation smoke applied. Added `LoadingProgressAggregationStatus`, `LoadingProgressAggregationResult`, `LoadingProgressAggregator` and `Run Loading Progress Aggregation Smoke` under QA Canvas `Show Loading diagnostics`. The smoke validates weighted running progress, completed-with-skipped aggregation, failed aggregation, no-step aggregation and the canonical boundary. F22C does not add SceneLifecycle/Transition observation adapters, readiness wait contracts, LoadingResult/LoadingFailure records, UI, fade, curtain, loading screen prefab, scene object, prefab, ScriptableObject, backend, PlayerPrefs, JSON or asmdef changes.

F22D result: SceneLifecycle / Transition Loading observation adapter applied. Added `LoadingObservationAdapter` and `Run Loading Observation Adapter Smoke`. The adapter maps existing SceneLifecycle and Transition diagnostic results into canonical Loading progress records without executing lifecycle, replacing Transition, running effects, mutating readiness, creating UI, adding scene objects/prefabs/ScriptableObjects, using backend/PlayerPrefs/JSON or changing asmdefs.


## Plano F23 — Pause Content / Overlay / Input Boundary

F23 define Pause visual/content/input como consumer boundary sobre o Pause core de F20. F23 não reabre Save, Loading, Transition, SceneLifecycle ou gameplay adapters.

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F23A | `APPLIED / DOCS ONLY` | Pause Content / Overlay / Input ADR Plan. | Nenhum. Documentação apenas. |
| F23B | `APPLIED / CONTRACTS` | Pause Content Anchor Consumer Contracts. | Compile/import. Smoke dedicado fica em F23E. |
| F23C | `APPLIED / CONTRACTS` | Pause Overlay Adapter Boundary. | Sem prefab/UI obrigatória; compile/import only. |
| F23D | `NEXT / PLANNED` | Pause Input Boundary Contracts. | Sem input asset obrigatório. |
| F23E | `PLANNED` | Pause Content / Overlay / Input Diagnostics Smoke. | QA Canvas. |
| F23F | `PLANNED` | Closure + Usage Guide. | Guia em `Documentation~/Guides/`. |

Guardrails de F23:

```text
Pause content consumes Pause core; it does not redefine Pause core.
Pause overlay is presentation/content; it does not own lifecycle.
Pause input is separate from gameplay input.
Time.timeScale remains future adapter/policy.
F24 Unity Build Surface / Lifecycle Wiring comes after F23.
Gameplay adapters remain F25+.
```

## Plano F24 — Unity Build Surface / Lifecycle Wiring

F24 é a fase intermediária de framework entre F23 Pause Content/Overlay/Input e F25 Gameplay Adapter Foundation. Ela existe para provar contratos em Unity real antes de entregar a superfície para gameplay adapters.

F24 não é gameplay adapter e não deve virar "montar o jogo".

| Corte | Status | Objetivo | Setup manual esperado |
|---|---|---|---|
| F24A | `PLANNED / ADR PLAN` | Unity Build / Lifecycle Wiring ADR Plan. | Nenhum. Documentação apenas. |
| F24B | `PLANNED / FIRST TECHNICAL CUT` | Transition ↔ GameFlow Runtime Integration. | Route switch real deve gerar TransitionSnapshot e executar sequência completa. |
| F24C | `PLANNED` | Transition Curtain Unity Build. | Route switch real executa fade curtain visual via UnityFadeCurtainEffectAdapter. |
| F24D | `PLANNED` | Loading Screen Unity Adapter Build. | Route switch real mostra/atualiza/esconde loading screen adapter concreto. |
| F24E | `PLANNED` | Pause Overlay Unity Build. | RequestPause abre/fecha overlay real sem gameplay adapter. |
| F24F | `PLANNED` | Save Moment Authoring Boundary. | Save manual/checkpoint authoring dispara ProgressionSaveRuntime sem gameplay payload. |
| F24G | `PLANNED` | Preferences Authoring Boundary. | Preferences authoring escreve/lê chave sem fallback silencioso. |
| F24H | `PLANNED` | Closure + Usage Guide. | Guia em `Documentation~/Guides/`. |

F24B deve ser o primeiro corte técnico da fase porque Transition existe como linguagem, mas `RouteRequestTrigger` / `GameFlow` ainda precisam passar por `TransitionPlan` real. Sem isso, curtain/loading visual podem virar superfícies montadas mas desconectadas do lifecycle real.

Guardrails de F24:

```text
F24 comes after F23.
F24 comes before gameplay adapters.
F24 does not create gameplay adapters.
F24 does not create Player/Actor/NPC/Door/Inventory/Combat adapters.
F24 builds minimal Unity surfaces to prove framework contracts in real scenes/prefabs.
F24 uses real-object smokes when applicable.
F24 follows the F19D pattern: minimal object, explicit wiring, real smoke, limited scope.
F24 must not become "build the game".
F24 must not create a full menu, full UI system, full save system or gameplay adapter.
```




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
| F17A | `CLOSED / DOCUMENTATION PASS` | Roadmap e ADRs realinhados: Gate antes de Transition e Pause; contextual reset e consumers avançados foram diferidos novamente e agora ficam em F25+. |
| F17B | `CLOSED / PRIMITIVES` | Primitivas passivas: `GateScope`, `GateDomain`, `GateDecisionStatus`, `GateDecision`, `GateBlocker`, `GateEvaluationResult` e `GateSnapshot`. |
| F17C | `CLOSED / REGRESSION SMOKE PASS` | Admissão already-in-flight de Route/Activity/CycleReset/ObjectReset passa por `GateEvaluationResult`, preservando result kinds existentes. |
| F17D | `CLOSED / QA SMOKE PASS` | `Run Gate Admission Diagnostics Smoke` valida admissões allowed/blocked por diagnóstico sintético estável. |
| F17E | `CLOSED / DOCS + HANDOFF` | Fechamento da fase, Usage Guide de Gate Foundation e preparação explícita para F18 Transition Orchestration. |

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
| F18B | `CLOSED / PRIMITIVES APPLIED` | Primitivas passivas de Transition: operação, tipo, fase/status, plano/resultado/snapshot. |
| F18C | `CLOSED / DIAGNOSTICS SMOKE APPLIED` | Smoke/diagnóstico sintético de resultados de Transition sem trocar cenas. |
| F18D | `CLOSED / GATE BLOCKER RELATIONSHIP APPLIED` | Relação lógica entre operação ativa de Transition e Gate blocker, validada por smoke sintético. |
| F18E | `CLOSED / ORCHESTRATION OBSERVATION APPLIED` | Observação sintética de Route/Activity orchestration por TransitionPlan/TransitionResult/TransitionSnapshot, sem alterar happy path ou result kinds existentes. |
| F18F | `CLOSED / DOCS + USAGE + HANDOFF` | Fechamento da fase, Usage Guide de Transition Orchestration e handoff para F19 Transition Effects. |

Evidência F18C aceita:

```text
QA Smoke completed. name='Transition Diagnostics Smoke'.
Steps: plan, succeeded-result, warnings-result, failed-result, snapshot.
```

Evidência F18D aceita:

```text
QA Smoke completed. name='Transition Gate Blocker Relationship Smoke'.
Steps: blocker-created, running-blocks-lifecycle, completed-releases-blocker, failed-releases-blocker.
```

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
- Pause core vem depois de Gate e depois de Transition Orchestration; Pause visual/input fica em F23.
- Save vem antes de Pause visual/gameplay.
- Loading operation/progress/readiness vem antes de Pause visual/gameplay.
- F24 Unity Build Surface / Lifecycle Wiring vem depois de F23 e antes de gameplay adapters.
- F24B Transition ↔ GameFlow Runtime Integration deve ser o primeiro corte técnico de F24.
- Camera, Audio, Actor e Pooling so depois de F24 e do modelo de gameplay object amadurecer.
- Projectile, Damage, Attributes e Powerups ficam no fim.
- F11 é `Cycle Reset Foundation`; F12 e `Cycle Reset Integration & Authoring UX`; F13 e `Object Entry Foundation`.
- Reset físico não entra no core antes dos contratos lógicos e adapters mínimos estarem definidos.
- Adapter Unity não deve virar gameplay consumer.
- Gameplay consumer não deve redefinir lifecycle, identity, ownership, policy ou diagnostics do core.
- Documento obsoleto/substituído não deve permanecer como arquivo separado.
- Toda fase fechada deve adicionar ou atualizar um `Usage` guide em `Documentation~/Guides/`.

## O que não mudar agora

- Não criar lifecycle novo por causa da consolidação documental.
- Não criar ADR separado, roadmap paralelo, tracker paralelo, closure por fase ou smoke documental separado.
- Não mover Camera, Audio, Actor, Pooling, Projectile, Damage, Attributes ou Powerups para F17-F24.
- Não criar visual Transition, Pause, Input ou gameplay antes das fases corretas.
- Não transformar F24 em gameplay adapter ou em "montar o jogo".
- Não criar Player/Actor/NPC/Door/Inventory/Combat adapters em F24.
- Não criar menu completo, sistema completo de UI ou sistema completo de save em F24.
- Não criar backend, PlayerPrefs, JSON, UI, scene object, ScriptableObject ou asmdef em F21A.
- Não tratar Snapshot como backend persistence.
- Não tratar Preferences como slot de progressão.
- Não tratar JSON futuro como contrato canônico.
- Não tratar Loading como fade, curtain, loading screen prefab ou substituto de SceneLifecycle.
- Não tratar Gate como UI, readiness ou input system.
- Não tratar fade/loading visual como substituto de Gate.
- Não tratar Pause como Activity, menu ou `Time.timeScale` canônico.
- Não copiar arquitetura Base 2.0 para o framework package.
- Não usar Cycle Reset como atalho para Object Reset ou Player Reset.

## Próximo corte

```text
F23C - Pause Overlay Adapter Boundary
```

F18B fechado: foram criadas primitivas passivas em `Runtime/Transition/` para operação, tipo, fase/status, step, plano, resultado e snapshot/diagnóstico. Também foi adicionado `FrameworkIdentityDomain.Transition` para manter operação como identidade tipada.

F18C fechado: foi criado `Run Transition Diagnostics Smoke`, um smoke sintético que valida shapes de `TransitionPlan`, `TransitionResult` e `TransitionSnapshot` sem trocar cenas e sem integrar Route/Activity.

F18D fechado: foi criado `TransitionGateBlockerPolicy`, que descreve uma operação ativa de Transition como `GateBlocker` passivo para `GameFlow/LifecycleRequest`, e `Run Transition Gate Blocker Smoke`, que valida criação do blocker, bloqueio sintético durante operação e liberação sintética em sucesso/falha.

F18E fechado: foi criado `TransitionOrchestrationObservationPolicy`, que descreve Route switch, Activity switch e Activity clear como observações passivas de Transition, e `Run Transition Orchestration Observation Smoke`, que valida planos/resultados/snapshot sintéticos sem executar requests reais.

F18F fechado: a fase foi marcada como `CLOSED / DOCS + USAGE + HANDOFF`, o guia `Documentation~/Guides/F18-Transition-Orchestration-Usage.md` foi criado e F19 foi marcado como próximo eixo. Ainda sem visual fade/loading, Pause menu, input real, gameplay object model, reset contextual de Player/Actor/NPC/Timer/Door/Pickup, lifecycle paralelo ou service locator. Fade/loading/curtain ficam para F19 como adapters/effects depois do contrato lógico.

F19A fechado: o ADR `F19-ADR-TRANSITION-002-Transition-Effects-Boundary.md` foi atualizado como Implementation Plan. F19A confirmou que Transition Effects são adapters/consumers de Transition Orchestration, não core Transition, não Gate e não SceneLifecycle. Também registrou que F19A não cria cenas, objetos nem ScriptableObjects. Setup manual começa apenas em cortes de adapter Unity concreto, provavelmente F19D+, e deve vir acompanhado de instruções explícitas de cena, GameObject, componente, campos, ScriptableObject se necessário, smoke e logs esperados.

F19B fechado: foram criadas primitivas passivas em `Runtime/TransitionEffects`: `TransitionEffectId`, `TransitionEffectKind`, `TransitionEffectRequiredness`, `TransitionEffectStatus`, `TransitionEffectRequest`, `TransitionEffectResult`, `TransitionEffectPlan` e `TransitionEffectSnapshot`. Também foi adicionado o domínio de identidade `FrameworkIdentityDomain.TransitionEffect`. Ainda sem scene object, ScriptableObject, adapter Unity, fade/loading visual, DOTween, runtime effect execution, Pause, Input ou UI.


F19C fechado: adicionado `TransitionEffectQaSmokeRunner` e botão `Run Transition Effect Diagnostics Smoke` no QA Canvas. O smoke valida `TransitionEffectRequest`, `TransitionEffectPlan`, `TransitionEffectResult` nos casos `Succeeded`, `Skipped` opcional e `MissingAdapter` required bloqueante, além de `TransitionEffectSnapshot`. F19C permanece sintético: sem scene object, Canvas, ScriptableObject, adapter Unity, fade/loading visual, DOTween, Pause, Input ou UI.

F19E fechado: adicionada policy de required/optional Transition Effects (`TransitionEffectAuthoringPolicy`), issue/evaluation primitives e smoke `Run Transition Effect Policy Guardrails Smoke`. A policy avalia um `TransitionEffectPlan` contra uma lista explícita de adapters fornecida pelo chamador. Required adapter ausente bloqueia, optional adapter ausente não bloqueia, duplicate effect id bloqueia como ambiguidade de authoring. Ainda sem ScriptableObject, registry, scene discovery, runtime effect owner, loading screen canônico, DOTween ou integração real com Route/Activity.

F22E result: Loading Screen Adapter Boundary applied. Added `ILoadingScreenAdapter`, `LoadingScreenPresentation`, `LoadingScreenAdapterAction`, `LoadingScreenAdapterStatus`, `LoadingScreenAdapterResult` and `Run Loading Screen Adapter Boundary Smoke` under QA Canvas `Show Loading diagnostics`. This is a visual adapter boundary only: it consumes canonical `LoadingOperation` data but does not create UI, scene objects, prefabs, ScriptableObjects, fade, curtain, TransitionEffect execution, SceneLifecycle execution, Transition execution, readiness mutation, backend, PlayerPrefs, JSON or asmdef changes.

F22F result: Loading Operation / Progress / Readiness Boundary closed with `Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. Closure is documentation-only and preserves the same boundaries: no runtime execution, UI, prefab, scene object, ScriptableObject, fade/curtain execution, SceneLifecycle replacement, Transition replacement, readiness mutation, backend, PlayerPrefs, JSON or asmdef changes. Pre-F23 audit reopened only framework-owned F22 debt before F23.

F22G result: Loading Readiness Observation Primitives applied. Added `LoadingReadinessObservationId`, `LoadingReadinessStatus`, `LoadingReadinessObservation` and `Run Loading Readiness Observation Smoke`. This resolves readiness observation as a canonical framework boundary without gameplay adapters, lifecycle execution, readiness mutation, UI, prefab, SceneLifecycle replacement or Transition replacement.

F22H result: Loading Result / Issue Primitive Closure applied. Added `LoadingIssueSeverity`, `LoadingIssue`, `LoadingResultStatus`, `LoadingResult` and `Run Loading Result and Issue Smoke`. This resolves Loading result/reporting as a canonical framework boundary without retry/fallback behavior, lifecycle execution, readiness mutation, UI, prefab, SceneLifecycle replacement, Transition replacement or gameplay adapters. F23B is now applied; next cut: F23D - Pause Input Boundary Contracts.


F23B result: Pause Content Anchor Consumer Contracts applied. Added passive request id, purpose, request/result/status records and `IPauseContentAnchorConsumer` under `Runtime/Pause`. The contracts can prepare canonical `ContentAnchorBindingRequest` data for future adapters, but do not create anchors, materialize UI, bind input, mutate Pause state, execute Transition Effects, change `Time.timeScale`, own Route/Activity lifecycle or add gameplay adapters. Next cut: F23D - Pause Input Boundary Contracts.


F23C result: Pause Overlay Adapter Boundary applied. Added `IPauseOverlayAdapter`, `PauseOverlayPresentation`, `PauseOverlayAdapterAction`, `PauseOverlayAdapterStatus` and `PauseOverlayAdapterResult` under `Runtime/Pause`. The contracts present canonical `PauseSnapshot` and optional prepared Pause Content Anchor data, but do not create UI, Canvas, prefab, ScriptableObject setup, input binding, `Time.timeScale` policy, TransitionEffect execution, anchor creation, scene object discovery, Route/Activity lifecycle ownership or gameplay adapters. Next cut: F23D - Pause Input Boundary Contracts.
