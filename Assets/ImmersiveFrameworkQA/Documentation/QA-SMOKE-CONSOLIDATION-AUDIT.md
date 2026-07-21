# QA-SMOKE-R1 — auditoria e mapa de consolidação

Data da auditoria: 2026-07-20  
Escopo: `Assets/ImmersiveFrameworkQA` de `QAFramework`.  
Natureza do corte: auditoria estática; nenhuma consolidação foi implementada.

## 1. Resumo executivo

- Foram encontradas **60 operações de smoke**: 50 classes `*Smoke`, 7 operações públicas no `AudioQaPanel` e 3 operações públicas no `PoolingQaPanel`.
- Há **1 classe de suite adicional**, `QaH2RegressionSuite`, com 2 comandos públicos. `QaP3CanonicalPreFirstGameSmoke` também opera como suite, mas já está incluído entre as 50 classes de smoke. `AudioQaPanel.RunAllSmokes` é um terceiro runner agregado.
- A superfície possui **60 comandos `MenuItem` distintos** (101 atributos contando os validadores duplicados) e **27 `ContextMenu` reais**. Dos `MenuItem`, 43 executam smokes, 2 executam a suite H2 e 15 são setup, repair, builder, configurador ou preflight.
- A distribuição proposta das 60 operações é: **22 FINAL**, **34 MERGE**, **1 REMOVE**, **1 LEGACY**, **1 TEMPORARY** e **1 DIAGNOSTIC**.
- O estado final proposto possui **24 regressões públicas**: as 22 bases `FINAL` atuais, mais `Audio Playback Regression` promovida a partir do runner `RunAllSmokes` e `Camera Override Authority Regression` promovida a partir do proof contextual C9R.
- As classes `QaH2RegressionSuite` e `QaP3CanonicalPreFirstGameSmoke` são mega-orquestradores por cortes históricos. Seus casos válidos pertencem aos smokes de domínio; suas superfícies públicas são candidatas a remoção.
- `Assets/ImmersiveFrameworkQA/README.md` está divergente: declara que Player QA expõe somente o menu P3 canônico, mas há 23 comandos `MenuItem` sob Player/Input/Pause.
- Nenhum teste NUnit foi incorporado. Não há fonte `[Test]`, `[TestCase]` ou `[UnityTest]` em `Assets`/`Packages` deste repositório; o projeto `Immersive.Framework.Pause.Tests.csproj` é superfície gerada do package vinculado e está fora deste inventário.

### Critério de contagem

Uma operação foi contada como smoke quando existe uma classe `*Smoke` executável ou um método público explicitamente chamado `Run*Smoke`. O runner `RunAllSmokes` não foi contado novamente como smoke individual. A classe H2 foi contada como suite, não como smoke. Métodos de UI sem semântica de smoke, builders, repairs e probes contextuais foram inventariados separadamente.

### Legenda de pré-condições

- `E`: Edit Mode e Unity sem compilação.
- `P`: Play Mode.
- `H`: `FrameworkRuntimeHost` único, inicializado e pronto.
- `F`: fixture persistida/configuração canônica aplicada.
- `T`: fixture temporária ou objetos criados pelo próprio smoke.
- `—`: sem cena/asset persistido.

## 2. Inventário completo de smokes

Cada linha registra arquivo, classe/método, menu/superfície, domínio, prova, pré-condições, cenas/assets, dependência de `FrameworkRuntimeHost`, cobertura equivalente e classificação.

### 2.1 Contratos, Activity Flow e Camera

| Arquivo / classe | Menu ou superfície | Domínio | O que prova | Pré-condições; cenas/assets | Host | Cobertura equivalente | Classe |
|---|---|---|---|---|---:|---|---|
| `Descriptors/Editor/QaA1ActivityIdSmoke.cs` / `QaA1ActivityIdSmoke` | `Immersive Framework QA/Contracts/A1 Run Activity ID Smoke` | Identidade de Activity | `ActivityId` é identidade funcional, rename não altera owner e IDs inválidos/distintos não colapsam. | E, T; sem asset persistido. | Não | Parte de identidade reaparece nos verticais P3, sem a matriz isolada. | **FINAL** |
| `Descriptors/Editor/QaB1DescriptorEqualitySmoke.cs` / `QaB1DescriptorEqualitySmoke` | `Immersive Framework QA/Contracts/B1 Run Descriptor Equality Smoke` | Descriptors | Igualdade funcional de Actor, PlayerActor, UnityInputTarget e ContentAnchor, independente de caminho/reparent. | E, T; paths sintéticos, nenhum asset persistido. | Não | Nenhum equivalente completo. | **FINAL** |
| `ActivityFlow/Editor/QaArchA2ActivityTransitionTransactionSmoke.cs` / `QaArchA2ActivityTransitionTransactionSmoke` | `Immersive Framework/QA/Activity Flow/ARCH-A2 Run Activity Transition Transaction Smoke` | Activity lifecycle | Transação integrada de clear/restart, ordem de fases, terminalidade, falha antes/depois de commit e single-flight. | P+H; usa Activity atual, sem fixture própria persistida. | Sim | H2.2.5/H2.2.6 cobrem requests/reset, não a máquina transacional completa. | **FINAL** |
| `Editor/CameraAuthoring/QaC9MFollowPipelineSmoke.cs` / `QaC9MFollowPipelineSmoke` | `Immersive Framework QA/Camera/C9M Run Follow Pipeline Smoke` | Camera authoring | Apply/Rebuild materializa `CinemachineCamera`/`CinemachineFollow`, alvo/offset corretos e idempotência sem duplicação. | E, T; sem asset persistido. | Não | Nenhum equivalente. | **FINAL** |
| `Camera/Scripts/Editor/QaCut4LocalPlayerCameraPublicationOwnershipAuthoringSmoke.cs` / `QaCut4...AuthoringSmoke` | `Immersive Framework/QA/Camera/Cut 4 Run ... Authoring Smoke` | Camera publication | Default sem auto-publicação, opt-in explícito, duplicate publisher e evidência por player. | E, T. | Não | Complementa o runtime Cut 4. | **MERGE** → Local Player Camera Publication |
| `Camera/Scripts/Editor/QaCut4LocalPlayerCameraPublicationOwnershipRuntimeSmoke.cs` / `QaCut4...RuntimeSmoke` | `Immersive Framework/QA/Camera/Cut 4 Run ... Runtime Smoke` | Camera publication | Lane P3 real é publicador único; identidade explícita; request é removido junto da admission. | P+H+F; chama `QaP3K7H...`; usa Lifecycle Route B. | Sim | K7H cobre a cadeia geral, mas não exclusividade/release de publicação. | **FINAL** |

### 2.2 Game Flow, reset e materialização

| Arquivo / classe | Menu | Domínio | O que prova | Pré-condições; cenas/assets | Host | Equivalência | Classe |
|---|---|---|---|---|---:|---|---|
| `GameFlow/InternalEditor/QaH222RouteRequestTriggerCompositionSmoke.cs` / `QaH222...CompositionSmoke` | `.../Game Flow/H2.2.2 Run Route Request Trigger Composition Smoke` | Route request | Zero/um/múltiplos triggers, binding idempotente e binding incompatível. | E, T. | Não | Binding H2.2.2. | **MERGE** |
| `GameFlow/InternalEditor/QaH222RouteRequestTriggerBindingSmoke.cs` / `QaH222...BindingSmoke` | `.../Game Flow/H2.2.2 Run Route Request Trigger Binding Smoke` | Route request | Binding explícito, rebind idempotente/incompatível, forwarding e ausência de fallback global. | P+H, T. | Sim | Composition H2.2.2. | **FINAL** |
| `GameFlow/InternalEditor/QaH223ActivityRequestTriggerCompositionSmoke.cs` / `QaH223...CompositionSmoke` | `.../Game Flow/H2.2.3 Run Activity Request Trigger Composition Smoke` | Activity request | Zero/um/múltiplos triggers e idempotência/incompatibilidade. | E, T. | Não | Binding H2.2.3. | **MERGE** |
| `GameFlow/InternalEditor/QaH223ActivityRequestTriggerBindingSmoke.cs` / `QaH223...BindingSmoke` | `.../Game Flow/H2.2.3 Run Activity Request Trigger Binding Smoke` | Activity request | Request/clear encaminhados ao port explícito, rebind e ausência de fallback. | P+H, T. | Sim | Composition H2.2.3. | **FINAL** |
| `GameFlow/InternalEditor/QaH224RouteCycleResetTriggerCompositionSmoke.cs` / `QaH224...CompositionSmoke` | `.../Game Flow/H2.2.4 Run Route Cycle Reset Trigger Composition Smoke` | Route cycle reset | Composição opcional, múltiplos triggers, idempotência e conflito. | E, T. | Não | Binding/Vertical H2.2.4. | **MERGE** |
| `GameFlow/InternalEditor/QaH224RouteCycleResetTriggerBindingSmoke.cs` / `QaH224...BindingSmoke` | `.../Game Flow/H2.2.4 Run Route Cycle Reset Trigger Binding Smoke` | Route cycle reset | Binding/forwarding de source/reason e mapeamento de success/warning/failure. | P+H, T. | Sim | Vertical H2.2.4. | **MERGE** |
| `GameFlow/InternalEditor/QaH224RouteCycleResetVerticalSmoke.cs` / `QaH224...VerticalSmoke` | `.../Game Flow/H2.2.4 Run Route Cycle Reset Vertical Smoke` | Route cycle reset | Sem rota, execução route+activity, zero participantes, optional/required failure e limpeza de in-flight. | P+H; contexto runtime atual. | Sim | Os dois H2.2.4 acima. | **FINAL** |
| `GameFlow/InternalEditor/QaH225ActivityCycleResetTriggerCompositionSmoke.cs` / `QaH225...CompositionSmoke` | `.../Game Flow/H2.2.5 Run Activity Cycle Reset Trigger Composition Smoke` | Activity cycle reset | Composição opcional/múltipla e rebind. | E, T. | Não | Binding/Vertical H2.2.5. | **MERGE** |
| `GameFlow/InternalEditor/QaH225ActivityCycleResetTriggerBindingSmoke.cs` / `QaH225...BindingSmoke` | `.../Game Flow/H2.2.5 Run Activity Cycle Reset Trigger Binding Smoke` | Activity cycle reset | Binding, source/reason, resultados e no-fallback. | P+H, T. | Sim | Vertical H2.2.5. | **MERGE** |
| `GameFlow/InternalEditor/QaH225ActivityCycleResetVerticalSmoke.cs` / `QaH225...VerticalSmoke` | `.../Game Flow/H2.2.5 Run Activity Cycle Reset Vertical Smoke` | Activity cycle reset | Ausência de route/activity, activity-only, scope incompatível, zero/optional/required e limpeza. | P+H; cria assets runtime temporários. | Sim | Os dois H2.2.5 acima. | **FINAL** |
| `GameFlow/InternalEditor/QaH226ActivityRestartTriggerCompositionSmoke.cs` / `QaH226...CompositionSmoke` | `.../Game Flow/H2.2.6 Run Activity Restart Trigger Composition Smoke` | Activity restart | Composição opcional/múltipla e rebind. | E, T. | Não | Binding/Vertical H2.2.6. | **MERGE** |
| `GameFlow/InternalEditor/QaH226ActivityRestartTriggerBindingSmoke.cs` / `QaH226...BindingSmoke` | `.../Game Flow/H2.2.6 Run Activity Restart Trigger Binding Smoke` | Activity restart | Binding, seleção/source/reason exatos, resultados e no-fallback. | P+H, T. | Sim | Vertical H2.2.6. | **MERGE** |
| `GameFlow/InternalEditor/QaH226ActivityRestartVerticalSmoke.cs` / `QaH226...VerticalSmoke` | `.../Game Flow/H2.2.6 Run Activity Restart Vertical Smoke` | Activity restart | Sem activity, target mismatch, seleção inválida, ordem reset-clear-reentry, single-flight e falhas. | P+H, T. | Sim | Os dois H2.2.6 acima. | **FINAL** |
| `GameFlow/InternalEditor/QaH227ObjectResetVerticalSmoke.cs` / `QaH227ObjectResetVerticalSmoke` | `.../Game Flow/H2.2.7 Run Object Reset Vertical Smoke` | Object reset | Reset unitário, subject ausente, zero participantes, optional/required, single-flight e limpeza. | P+H, T. | Sim | H2.2.8 e H2.2.10. | **MERGE** → Object Reset |
| `GameFlow/InternalEditor/QaH228ObjectResetGroupVerticalSmoke.cs` / `QaH228ObjectResetGroupVerticalSmoke` | `.../Game Flow/H2.2.8 Run Object Reset Group Vertical Smoke` | Object reset | Seleção múltipla normalizada, vazia/inválida/unregistered, failures, single-flight e binding explícito. | P+H, T. | Sim | H2.2.7/H2.2.10. | **FINAL** |
| `GameFlow/InternalEditor/QaH2210ResetRegistrationRuntimeBindingSmoke.cs` / `QaH2210...Smoke` | `.../Game Flow/H2.2.10 Run Reset Registration Runtime Binding Smoke` | Object reset registration | Owner obrigatório, subject/participant register/unregister, IDs monotônicos, duplicatas, rebind e cleanup. | P+H, T. | Sim | H2.2.7/H2.2.8. | **MERGE** → Object Reset |
| `GameFlow/InternalEditor/QaH229InputGateRuntimeBindingSmoke.cs` / `QaH229...Smoke` | `.../Game Flow/H2.2.9 Run Input Gate Runtime Binding Smoke` | UI/Input gate | Bloqueio/restauração de map, domínios independentes, baseline disabled, missing map, rebind e cleanup. | P+H, T `PlayerInput`. | Sim | IC1/IC2 cobrem authority, não o contrato de gate Game Flow. | **FINAL** |
| `GameFlow/InternalEditor/QaH2211ContentAnchorMaterializationRuntimeBindingSmoke.cs` / `QaH2211...Smoke` | `.../Game Flow/H2.2.11 Run Content Anchor Materialization Runtime Binding Smoke` | Scene composition/release | Preflight sem efeito, materialização física/lógica, placement/binding, release/idempotência e cleanup. | P+H, T; prefab runtime temporário. | Sim | Nenhum equivalente completo. | **FINAL** |
| `GameFlow/InternalEditor/QaH2212PlayerActorSelectionRuntimeBindingSmoke.cs` / `QaH2212...Smoke` | `.../Game Flow/H2.2.12 Run Player Actor Selection Runtime Binding Smoke` | Player actor selection | Readiness, join, seleção default, snapshot, rebind, diagnostics e close joining. | P+H+F. | Sim | P3K7H cobre a mesma autoridade na vertical real. | **MERGE** → Player Gameplay Admission |
| `GameFlow/InternalEditor/QaH2213DiagnosticsQaBoundarySmoke.cs` / `QaH2213...Smoke` | `.../Game Flow/H2.2.13 Run Diagnostics QA Boundary Smoke` | Diagnostics | Snapshot canônico, canvas sem authority estática, probe retirado e binding QA explícito. | P+H, T. | Sim | H2.4 e demais smokes já provam no-fallback; valor principal é diagnóstico estrutural. | **DIAGNOSTIC** |
| `GameFlow/InternalEditor/QaH24StaticHostAuthorityRemovalSmoke.cs` / `QaH24...Smoke` | `.../Game Flow/H2.4 Run Static Host Authority Removal Smoke` | Bootstrap/runtime host authority | Host único/pronto, ausência de `Current`/lookup estático no package/QA e resolver explícito ambiguity-rejecting. | P+H; lê fontes do package/QA. | Sim | H2.2.* prova no-fallback por adapter, mas não a varredura de authority estática. | **FINAL** |

### 2.3 Player participation, provisioning e gameplay

| Arquivo / classe | Menu | Domínio | O que prova | Pré-condições; cenas/assets | Host | Equivalência | Classe |
|---|---|---|---|---|---:|---|---|
| `Player/Editor/QaP3CPlayerProfileAuthoringSmoke.cs` / `QaP3C...Smoke` | Sem menu próprio; chamado pelo P3 canônico | Player participation authoring | IDs/ordem, selection policy, validação positiva/negativa, não mutação e template completo. | E, T; `__P3C..._Temp` autoclean. | Não | P3D cobre projeção de Activity. | **FINAL** |
| `Player/Editor/QaP3DActivityParticipationAuthoringSmoke.cs` / `QaP3D...Smoke` | Sem menu próprio | Player participation authoring | Projeções no/all/explicit, zero policy, requirements, duplicatas, project scan e template. | E, T; `__P3D..._Temp` autoclean. | Não | P3C. | **MERGE** → Player Participation Authoring |
| `Player/Editor/QaP3FSessionSlotRuntimeSmoke.cs` / `QaP3F...Smoke` | Sem menu próprio | Session slots | Roster ordenado, open/close, reserve/release/join, capacity, stale/foreign token, duplicatas e imutabilidade. | E, T; sem cenas. | Não | Provisioning P3G3 usa o contrato, mas não toda a máquina. | **FINAL** |
| `Player/Editor/QaP3G2JoinContractAuthoringSmoke.cs` / `QaP3G2...Smoke` | Sem menu próprio | Local player provisioning | Request/operation ID, authoring manual, campos obrigatórios, host técnico e não mutação. | E, T. | Não | P3G3 e validation smoke. | **MERGE** → Local Player Provisioning |
| `Player/Editor/QaP3LocalPlayerProvisioningValidationSmoke.cs` / `QaP3...ValidationSmoke` | Sem menu próprio | Local player provisioning | Surface obrigatória, válida, duplicada e manager divergente. | E, T. | Não | P3G2/Cut5. | **MERGE** |
| `Player/Editor/QaCut5ProvisioningCompositionRootSmoke.cs` / `QaCut5...Smoke` | `.../Player/Cut 5 Run Provisioning Composition Root Smoke` | Local player provisioning | Registration explícita é authority única; global unregistered ignorado; missing/duplicate/divergent falham. | E, T; path sintético `Cut5_UIGlobal.unity`, não persistido. | Não | P3 validation/G2. | **MERGE** |
| `Player/Editor/QaP3G3ProvisioningBridgeSyntheticSmoke.cs` / `QaP3G3...Smoke` | Sem menu próprio | Local player provisioning | Reserva antes de provisionar, callback/late callback, commit, rollbacks, policy, reentrância e host parenting. | E, T. | Não | P3G2/validation/Cut5/M4B1. | **FINAL** |
| `Player/Editor/QaP3M4ASceneLocalPlayerAdmissionAuthoringSmoke.cs` / `QaP3M4A...Smoke` | `.../Player/P3M4A Scene Local Player Admission Authoring Smoke` | Scene player authoring | Evidência nominal, timing, missing/outside/duplicate/input negativo e host manual. | E, T. | Não | P3G2/G3 e M5B. | **MERGE** → Local Player Provisioning |
| `Player/Editor/QaP3M4B1SceneLocalPlayerAdmissionTransactionSmoke.cs` / `QaP3M4B1...Smoke` | `.../Player/P3M4B1 Scene Local Player Admission Transaction Smoke` | Scene player admission | Slot ordenado, admission com joining fechado, idempotência, foreign token, release e ownership externo. | E, T. | Não | P3G3 e M5B. | **MERGE** → Local Player Provisioning |
| `Player/Editor/QaP3M4B2ASceneLocalPlayerActivityLifecycleSmoke.cs` / `QaP3M4B2A...Smoke` | `.../Player/P3M4B2A Scene Local Player Activity Lifecycle Smoke` | Scene player lifecycle | Enter/exit ordenado, selection, joining, idempotência, release e actor adoption requerido. | E, T; cena temporária `Assets/QA_P3M4B2A_Temp_*.unity`. | Sim por reflexão/fixture | M4B2B/M5A/M5B. | **MERGE** → Scene Player Route Lifecycle |
| `Player/Editor/QaP3M4B2BScenePlayerActorAdoptionSmoke.cs` / `QaP3M4B2B...Smoke` | `.../Player/P3M4B2B Scene Player Actor Adoption Smoke` | Player actor lifecycle | Actor preparado/adotado, ownership, no-duplication, token/idempotência, exit e gameplay-ready. | E, T; cena temporária `Assets/QA_P3M4B2B_Temp_*.unity`. | Sim por reflexão/fixture | M5A/M5B/K7H. | **MERGE** |
| `Player/Editor/QaP3M5ASceneLocalPlayerIntegratedLifecycleSmoke.cs` / `QaP3M5A...Smoke` | `.../Player/P3M5A Run Scene Local Player Integrated Lifecycle Smoke` | Scene player lifecycle | Activity real, admission/adoption/owner, release/reentry, stale token e negativos gameplay-ready/no-residue. | P+H+F; pasta `Player/P3M5A`. | Sim | P3M5B amplia para Route transitions/negative matrix. | **MERGE** |
| `Player/Editor/QaP3M5BRouteTransitionAndNegativeMatrixSmoke.cs` / `QaP3M5B...Smoke` | `.../Player/P3M5B Run Route Transition and Negative Matrix Smoke` | Route/Scene player lifecycle | A→B→A, startup Activity, fresh identities, release, duplicate/missing/mismatch/reused/undeclared e limpeza final. | P+H+F; pasta `Player/P3M5B`. | Sim | M4*/M5A. | **FINAL** |
| `Player/Editor/QaP3K7HRouteStartupActivityPlayerAdmissionSmoke.cs` / `QaP3K7H...Smoke` | Sem menu próprio; chamado pelo P3 canônico/Cut4 | Player gameplay | Join real, preparação/admission, route startup, handoff, gameplay-ready, camera output, clear/release e contratos puros. | P+H+F; `Lifecycle/Scenes/QA_LifecycleRouteB.unity` e fixtures P3G4/H4/J5/J6. | Sim | H2.2.12 e partes de M5B/Cut4. | **FINAL** |
| `Player/Editor/QaP3CanonicalPreFirstGameSmoke.cs` / `QaP3CanonicalPreFirstGameSmoke` | `.../Player/P3 Run Canonical Pre-FIRSTGAME Smoke` | Suite Player multi-domínio | Orquestra P3C, validation, P3D, P3F, P3G2, P3G3, setup J6 e K7H. | E→P+H+F; altera/restaura cenas e aplica fixture. | Indireto | É soma de 7 smokes e setup; mega-smoke. | **REMOVE** após runners finais independentes |

### 2.4 Pause e UI/Input

| Arquivo / classe | Menu | Domínio | O que prova | Pré-condições; cenas/assets | Host | Equivalência | Classe |
|---|---|---|---|---|---:|---|---|
| `Player/InternalEditor/PauseP1/QaPauseP1ConsumerSmoke.cs` / `QaPauseP1ConsumerSmoke` | `.../Player/Pause P1/Run Consumer Smoke` | Pause product binding | Binding admitido, UI/input toggle, maps Global/UI/Player, release, missing/duplicate/rebind e cleanup. | P+F; `PauseP1/Scenes/QA_PauseProductBinding.unity`, action reference. | Não; usa port explícito | Pause scene lifecycle/H2.2.1/IC2. | **FINAL** |
| `Player/InternalEditor/PauseP1/QaPauseP1SceneLifecycleCompositionSmoke.cs` / `QaPauseP1...CompositionSmoke` | `.../Player/Pause P1/Run Scene Lifecycle Composition Smoke` | Pause product binding | Roots exatos da cena, token, foreign/duplicate, release-before-exit e stale cleanup. | P+F; mesma cena Pause P1. | Não | Consumer smoke. | **MERGE** |
| `Player/InternalEditor/PauseP1/QaPauseP1LegacyBoundaryStaticSmoke.cs` / `QaPauseP1LegacyBoundaryStaticSmoke` | `.../Player/Pause P1/Run Legacy Boundary Static Smoke` | Pause legacy boundary | Ausência de APIs/bridges/context menus substituídos por P1. | E; lê fontes do package/QA. | Não | IC2 authority e Pause Consumer provam o caminho atual. | **LEGACY** |
| `Player/InternalEditor/QaH221PauseRequestTriggerCompositionSmoke.cs` / `QaH221...CompositionSmoke` | `.../Pause/H2.2.1 Run Pause Request Trigger Composition Smoke` | Pause request | Zero/um/múltiplos triggers, idempotência e binding incompatível. | E, T. | Não | H2.2.1 Binding/Pause Consumer. | **MERGE** |
| `Player/InternalEditor/QaH221PauseRequestTriggerBindingSmoke.cs` / `QaH221...BindingSmoke` | `.../Pause/H2.2.1 Run Pause Request Trigger Binding Smoke` | Pause request | Port explícito, rebind, snapshot/request forwarding e no-fallback. | P+H, T. | Sim | Pause Consumer. | **MERGE** |
| `Player/Editor/QaIc1PlayerInputSingleWriterSmoke.cs` / `QaIc1...Smoke` | `.../Unity Input/IC1 Run PlayerInput Single Writer Smoke` | Player input authority | Único writer físico, map/set select/restore/idempotência, rollback, lifecycle e source scan. | P, T `PlayerInput`; lê package. | Não | IC2 authority/H2.2.9. | **MERGE** → Player Input Mode Authority |
| `Player/InternalEditor/QaIc2InputModeRuntimeAuthoritySmoke.cs` / `QaIc2...AuthoritySmoke` | `.../Input Mode/IC2 Run Runtime Authority Smoke` | Input mode authority | Context/transactions, concurrency/rollback/stale/idempotência, bridge explícita, map policy e source ownership. | P+H, T; lê assets/package para legado. | Sim | IC1 e IC2 pause regression. | **FINAL** |
| `Player/InternalEditor/QaIc2PauseInputModeRuntimeRegressionSmoke.cs` / `QaIc2...RegressionSmoke` | `.../Input Mode/IC2 Run Pause Runtime Regression Smoke` | Input mode experimental | Integra Pause/InputMode em fixture sintética, preflight failures e cleanup. | P+H, T. | Sim | IC2 authority + Pause Consumer cobrem contratos atuais. | **TEMPORARY** |

### 2.5 Audio e Pooling

| Arquivo / classe.método | Superfície | Domínio | O que prova | Pré-condições; cenas/assets | Host | Equivalência | Classe |
|---|---|---|---|---|---:|---|---|
| `Audio/Scripts/Runtime/AudioQaPanel.cs` / `RunDirectSfxSmoke` | Botão `Run Direct SFX Smoke` | Audio playback | SFX direto retorna handle válido. | P+F; `QA_Audio.unity`, cue/clip/defaults. | Não; `AudioRuntimeHost` | Runner Audio. | **MERGE** |
| mesmo / `RunMissingClipSmoke` | Botão `Run Missing Clip Smoke` | Audio negative | `FailedMissingClip`. | P+F; cue sem clip. | Não | Runner Audio. | **MERGE** |
| mesmo / `RunMissingDefaultsSmoke` | Botão `Run Missing Defaults Smoke` | Audio negative | `FailedMissingDefaults`. | P+F; host sem defaults. | Não | Runner Audio. | **MERGE** |
| mesmo / `RunPooledSfxSmoke` | Botão `Run Pooled SFX Smoke` | Audio/pooling | SFX pooled retorna handle válido. | P+F; pooled cue/prefab/pool. | Não | Runner Audio. | **MERGE** |
| mesmo / `RunMissingPoolSmoke` | Botão `Run Missing Pool Smoke` | Audio negative | `FailedMissingPoolService`. | P+F; host sem pool. | Não | Runner Audio. | **MERGE** |
| mesmo / `RunListenerSmoke` | Botão `Run Listener Smoke` | Audio listener | Duplicata é reportada, policy `ReportOnly` não a desabilita. | P+F; listener host e objeto temporário. | Não | Runner Audio. | **MERGE** |
| mesmo / `RunBgmSmoke` | Botão `Run BGM Smoke` | BGM playback | Play sucede e stop retorna `Stopped`. | P+F; BGM cue/clip. | Não | Runner Audio. | **MERGE** |
| `Pooling/Scripts/Runtime/PoolingQaPanel.cs` / `RunBasicSmoke` | Context `Pooling QA/Run Basic Smoke` + botão | Pooling | Prewarm, 3 rents, return/reuse e zero active no fim. | P+F; `QA_Pooling.unity`, cube definition/prefab. | Não; `PoolRuntimeHost` | Max/Auto Return. | **FINAL** |
| mesmo / `RunMaxLimitSmoke` | Context `Pooling QA/Run Max Limit Smoke` + botão | Pooling negative | Terceiro rent falha com `maxSize=2`, sem expansão, total permanece 2. | P+F; limited definition. | Não | Basic. | **MERGE** |
| mesmo / `RunAutoReturnSmoke` | Context `Pooling QA/Run Auto Return Smoke` + botão | Pooling lifecycle | Auto-return deixa zero active e ao menos um inactive; rejeita execução concorrente/config inválida. | P+F; auto-return definition. | Não | Basic. | **MERGE** |

## 3. Suites, setup, validation e menus não-smoke

### 3.1 Suites e runners agregados

| Arquivo / classe/método | Superfície | Conteúdo | Classificação / decisão |
|---|---|---|---|
| `Editor/QaH2RegressionSuite.cs` / `QaH2RegressionSuite.RunFull` | `Immersive Framework/QA/H2 Run Full Regression Suite` | H2.2.1–H2.2.6 + P3 canônico, atravessando Pause, Game Flow e Player. | **REMOVE**: mega-suite e nomenclatura histórica. |
| mesmo / `RunH226` | `Immersive Framework/QA/H2 Run H2.2.6 Suite` | Composition + binding + vertical de Activity Restart. | **MERGE** no smoke final Activity Restart; remover menu. |
| `Player/Editor/QaP3CanonicalPreFirstGameSmoke.cs` | menu P3 canônico | Runner multi-domínio P3. | **REMOVE** após separação; já contado no inventário de smokes. |
| `Audio/Scripts/Runtime/AudioQaPanel.cs` / `RunAllSmokes` | botão `Run All Audio Smokes` | Executa os 7 casos Audio e exige zero failure. | Evoluir para **FINAL** `Audio Playback Regression`, com um único menu público. |

### 3.2 Setup, rebuild, repair, validation e builders

| Arquivo / classe | Menu | Papel | Classificação |
|---|---|---|---|
| `Audio/Scripts/Editor/AudioQaGeneratedClipRepair.cs` / `AudioQaGeneratedClipRepair` | `Immersive Framework QA/Audio/Repair Generated Audio Clips` | Repara clips gerados. | **SETUP** |
| `Audio/Scripts/Editor/AudioQaSceneBuilder.cs` / `AudioQaSceneBuilder` | `.../Audio/Create or Refresh Audio QA Scene` | Cria cena, cues, configs, pools e prefab Audio. | **SETUP** |
| `Audio/Scripts/Editor/FrameworkBgmQaSceneBuilder.cs` / `FrameworkBgmQaSceneBuilder` | `.../Audio/Configure Framework BGM Route-Activity QA` | Configura duas Routes/Activities e BGM lifecycle manual. | **SETUP** |
| `Camera/Scripts/Editor/QaC9RCameraOverrideAuthorityInstaller.cs` / `QaC9R...Installer` | `.../Camera/C9R Install Camera Override Authority QA` | Instala assets/cenas e chama scene installer. | **SETUP** |
| `Camera/Scripts/Editor/QaC9RCameraOverrideAuthoritySceneInstaller.cs` / `QaC9R...SceneInstaller` | Sem menu próprio | Cria/repara scene/route/hub/build settings C9R. | **SETUP** |
| `Hub/Scripts/Editor/QaHubSceneBuilder.cs` / `QaHubSceneBuilder` | `.../Hub/Create or Refresh QA Hub` | Cria Hub e rota de navegação. | **SETUP** |
| `Lifecycle/Scripts/Editor/QaLifecycleSceneBuilder.cs` / `QaLifecycleSceneBuilder` | `.../Lifecycle/Create or Refresh Lifecycle QA Scenes` | Cria Route A/B, additional scene e Activities. | **SETUP** |
| `Player/Editor/QaP3G4RuntimeIntegrationSetup.cs` / `QaP3G4RuntimeIntegrationSetup` | Sem menu; chamado por setup canônico | Prefab/scene local-player runtime. | **SETUP** |
| `Player/Editor/QaP3H4RuntimeHostActorSelectionSetup.cs` / `QaP3H4...Setup` | Sem menu | Actor selection no host. | **SETUP** |
| `Player/Editor/QaP3J5RuntimeHostPreparationSetup.cs` / `QaP3J5...Setup` | Sem menu | Player actor preparation. | **SETUP** |
| `Player/Editor/QaP3J6ActivityPlayerActorLifecycleSetup.cs` / `QaP3J6...Setup` | Sem menu | Fixture Activity player/actor lifecycle usada por K7H. | **SETUP** |
| `Player/Editor/QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.cs` | `.../P3M5A Apply Scene Local Player Integrated Lifecycle Fixture` | Materializa a fixture M5A. | **SETUP**; removível após migração para M5B. |
| `Player/Editor/QaP3M5BRouteTransitionAndNegativeMatrixSetup.cs` | `.../P3M5B Apply Route Transition and Negative Matrix Fixture` | Materializa Route A/B e matriz negativa. | **SETUP** preservado para final. |
| `Player/Editor/QaP3M5BReconciledFixtureSetup.cs` | `.../P3M5B Apply Reconciled Route Transition Fixture` | Wrapper/reconciliação adicional da mesma fixture. | **MERGE** no setup M5B único. |
| `Player/Editor/QaP3M5BPersistedSceneReferenceRepair.cs` | `.../P3M5B Repair Persisted Scene References` | Repair de referências persistidas. | **SETUP** interno; retirar menu público. |
| `Player/Editor/QaP3M5BPersistedFixturePreflight.cs` | `.../P3M5B Validate Persisted Fixture` | Preflight manual das cenas/referências. | **DIAGNOSTIC**; incorporar preflight ao final M5B. |
| `Player/InternalEditor/PauseP1/QaPauseP1Setup.cs` / `QaPauseP1Setup` | `.../Pause P1/Setup or Rebuild Consumer Scene`; `Open Consumer Scene` | Cria/abre cena e action reference Pause. | **SETUP**; manter um menu de setup, open vira diagnóstico. |
| `Pooling/Scripts/Editor/PoolingQaSceneBuilder.cs` / `PoolingQaSceneBuilder` | `.../Pooling/Create or Refresh Pooling QA Scene` | Cria cena, prefabs e definitions. | **SETUP** |
| `UnityBuildSurface/Scripts/Editor/CanonicalUIGlobalQaConfigurator.cs` | `.../Unity Build Surface/Configure Canonical UIGlobal QA Scene` | Configura UIGlobal/GameApplication/build settings. | **SETUP** |

### 3.3 Inventário completo de `ContextMenu`

- `FrameworkBgmQaPanel`: `Request Other Route`, `Request Startup Activity`, `Request Own Activity`, `Request Retain Previous Activity`, `Request Route Fallback Activity`, `Request Silence Activity`, `Clear Activity` — **DIAGNOSTIC**, sete comandos manuais.
- `QaC9RCameraOverrideAuthorityFixture`: `Run C9R Camera Override Authority Proof` — proof válido, mas a superfície contextual é **DIAGNOSTIC**; promover para o menu final de regressão Camera Override.
- `PoolingQaPanel`: `Prewarm Active`, `Rent One`, `Rent Three`, `Return Last`, `Return All`, `Clear Active`, `Reset Panel` — **DIAGNOSTIC**; os três `Run * Smoke` constam no inventário e devem virar casos internos do final Pooling.
- `QaLoadingSurfaceVisibilityHoldAdapter`: visible, hidden, 50%, complete e indeterminate — **DIAGNOSTIC**, cinco comandos manuais.
- `TransitionQaActivitySwitchPanel`: primary, alternate e clear — **DIAGNOSTIC**, três comandos.
- `TransitionQaRouteSwitchPanel`: target route — **DIAGNOSTIC**, um comando.

Total real: **27**. As duas strings `"[ContextMenu("` no smoke de boundary Pause são dados pesquisados em fonte, não atributos e não entram na contagem.

## 4. Grupos de cobertura redundante

| Grupo | Itens sobrepostos | Owner final | Decisão |
|---|---|---|---|
| Route Request | H2.2.2 Composition + Binding | Route Request Regression | Um runner; composition vira casos internos de binding/vertical. |
| Activity Request | H2.2.3 Composition + Binding | Activity Request Regression | Mesmo corte. |
| Route Cycle Reset | H2.2.4 Composition + Binding + Vertical | Route Cycle Reset Regression | Vertical é base; absorve positivos, negativos e idempotência. |
| Activity Cycle Reset | H2.2.5 Composition + Binding + Vertical | Activity Cycle Reset Regression | Vertical é base. |
| Activity Restart | H2.2.6 Composition + Binding + Vertical + suite H2.2.6 | Activity Restart Regression | Vertical é base; suite/menu específico desaparece. |
| Object Reset | H2.2.7 + H2.2.8 + H2.2.10 | Object Reset Regression | Group smoke é base; unitário e registration viram casos internos. |
| Pause | Pause Consumer + Scene Lifecycle + H2.2.1 Composition/Binding + legacy boundary | Pause Product Binding Regression | Consumer é base; legacy não vira caso permanente salvo boundary atual explícito. |
| Input authority | IC1 + IC2 authority + IC2 Pause experimental + H2.2.9 | Player Input Mode Authority e Input Gate Regression | Separar authority de InputMode do gate de Game Flow; remover experimental após cobertura. |
| Player authoring | P3C + P3D | Player Participation Authoring Regression | P3C é base e absorve Activity projection. |
| Provisioning | P3G2 + validation + Cut5 + P3G3 + M4A + M4B1 | Local Player Provisioning Regression | P3G3 é base; manter todos os negativos. |
| Scene player lifecycle | M4B2A + M4B2B + M5A + M5B | Scene Player Route Lifecycle Regression | M5B é base; absorve no-player/gameplay-ready/idempotência/stale token. |
| Player gameplay | H2.2.12 + K7H | Player Gameplay Admission Regression | K7H é base vertical. |
| Camera publication | Cut4 authoring + runtime + trecho K7H | Local Player Camera Publication Regression | Runtime é base e executa fase authoring antes do Play Mode. |
| Audio | 7 botões + `RunAllSmokes` | Audio Playback Regression | Sete helpers/casos, um menu público. |
| Pooling | Basic + Max Limit + Auto Return | Pooling Runtime Regression | Basic é base, os demais viram casos internos. |
| Mega-runners | H2 Full + P3 Canonical | Nenhum | Remover; não criar substituto global. |

## 5. Regressões finais propostas

Os nomes abaixo são comportamentais e não carregam códigos de cortes. Cada entrada tem um único menu público. Helpers podem permanecer privados ou compartilhados sem menu.

### 5.1 Activity Identity Regression

- **Nome final:** `Activity Identity Regression`
- **Domínio:** Contracts / Activity identity
- **Contrato provado:** identidade funcional é `ActivityId`; rename não altera owner; IDs inválidos/distintos não colapsam.
- **Casos internos:** rename-stable, distinct-id, whitespace-invalid, lifecycle owner canonical.
- **Smoke final escolhido:** `QaA1ActivityIdSmoke`.
- **Smokes substituídos:** nenhum.
- **Menus removidos:** menu A1, substituído por `Immersive Framework/QA/Contracts/Run Activity Identity Regression`.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit Mode.
- **Resultado PASS esperado:** todos os owners usam o ID estável e permanecem distintos após rename.

### 5.2 Descriptor Equality Regression

- **Nome final:** `Descriptor Equality Regression`
- **Domínio:** Contracts
- **Contrato provado:** igualdade/hash funcionais de Actor, PlayerActor, UnityInputTarget e ContentAnchor.
- **Casos internos:** igualdade positiva, diferença semântica, path/reparent irrelevante.
- **Smoke final escolhido:** `QaB1DescriptorEqualitySmoke`.
- **Smokes substituídos:** nenhum.
- **Menus removidos:** B1; novo menu comportamental.
- **Assets preservados:** nenhum.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit Mode.
- **Resultado PASS esperado:** igualdade e hash seguem os campos funcionais.

### 5.3 Runtime Host Authority Regression

- **Nome final:** `Runtime Host Authority Regression`
- **Domínio:** Bootstrap
- **Contrato provado:** um host explícito/pronto, sem authority ou lookup estático, com resolução ambiguity-rejecting somente no harness QA.
- **Casos internos:** unique ready host, no static field/method/source lookup, ambiguous resolver rejected, adapters sem fallback.
- **Smoke final escolhido:** `QaH24StaticHostAuthorityRemovalSmoke`.
- **Smokes substituídos:** boundary portions de H2.2.* e H2.2.13.
- **Menus removidos:** H2.4 e menu H2.2.13.
- **Assets preservados:** configuração canônica atual.
- **Assets removíveis:** probe diagnostics retirado.
- **Pré-condições:** Play Mode, host pronto.
- **Resultado PASS esperado:** exatamente um host resolvido pelo harness e zero authority estática no package/QA.

### 5.4 Activity Transition Transaction Regression

- **Nome final:** `Activity Transition Transaction Regression`
- **Domínio:** Route/Activity lifecycle
- **Contrato provado:** ordem, commit, readiness, terminalidade, falha e single-flight da transação Activity.
- **Casos internos:** clear, restart, sequence monotonic, invalid order, failed-before-commit, committed ready/not-ready/finalization-failed, post-terminal, concurrent.
- **Smoke final escolhido:** `QaArchA2ActivityTransitionTransactionSmoke`.
- **Smokes substituídos:** nenhum; reutiliza authorities atuais.
- **Menus removidos:** ARCH-A2; novo menu comportamental.
- **Assets preservados:** fixture canônica mínima usada no Play Mode.
- **Assets removíveis:** nenhum específico.
- **Pré-condições:** Play Mode e host pronto.
- **Resultado PASS esperado:** cada transação termina uma vez, na ordem válida, sem estado in-flight residual.

### 5.5 Route Request Regression

- **Nome final:** `Route Request Regression`
- **Domínio:** Route lifecycle
- **Contrato provado:** composição e binding explícitos de triggers Route, forwarding, idempotência e no-fallback.
- **Casos internos:** zero/one/multiple, same-port, different-port, missing target, unbound, exact forwarding.
- **Smoke final escolhido:** `QaH222RouteRequestTriggerBindingSmoke`.
- **Smokes substituídos:** `QaH222...CompositionSmoke`.
- **Menus removidos:** os 2 menus H2.2.2.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** fase Edit para composition; fase Play com host para binding.
- **Resultado PASS esperado:** apenas ports explícitos recebem requests e recomposição é idempotente.

### 5.6 Activity Request Regression

- **Nome final:** `Activity Request Regression`
- **Domínio:** Activity lifecycle
- **Contrato provado:** composição/binding explícitos de request e clear Activity.
- **Casos internos:** zero/one/multiple, same/different port, request, clear, unbound e missing target.
- **Smoke final escolhido:** `QaH223ActivityRequestTriggerBindingSmoke`.
- **Smokes substituídos:** `QaH223...CompositionSmoke`.
- **Menus removidos:** os 2 menus H2.2.3.
- **Assets preservados:** nenhum.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit + Play/host.
- **Resultado PASS esperado:** request/clear chegam ao port exato sem fallback.

### 5.7 Route Cycle Reset Regression

- **Nome final:** `Route Cycle Reset Regression`
- **Domínio:** Game Flow / Route reset
- **Contrato provado:** trigger, binding e execução vertical do reset do ciclo Route.
- **Casos internos:** composition optional/multiple, bind/rebind, no route, route+activity participants, zero, warning, required failure, cleanup.
- **Smoke final escolhido:** `QaH224RouteCycleResetVerticalSmoke`.
- **Smokes substituídos:** H2.2.4 Composition e Binding.
- **Menus removidos:** os 3 menus H2.2.4.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** helpers sintéticos depois da migração.
- **Pré-condições:** Edit + Play/host.
- **Resultado PASS esperado:** resultado estruturado correto e nenhuma request in-flight.

### 5.8 Activity Cycle Reset Regression

- **Nome final:** `Activity Cycle Reset Regression`
- **Domínio:** Game Flow / Activity reset
- **Contrato provado:** reset Activity respeita scope, participantes e falhas.
- **Casos internos:** composition/binding, no route/activity, activity-only, unsupported route-only, zero, warning, required failure, cleanup.
- **Smoke final escolhido:** `QaH225ActivityCycleResetVerticalSmoke`.
- **Smokes substituídos:** H2.2.5 Composition e Binding.
- **Menus removidos:** os 3 menus H2.2.5.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit + Play/host.
- **Resultado PASS esperado:** apenas Activity scope executa e o estado final fica limpo.

### 5.9 Activity Restart Regression

- **Nome final:** `Activity Restart Regression`
- **Domínio:** Game Flow / Activity restart
- **Contrato provado:** reset-clear-reentry atômico, seleção e single-flight.
- **Casos internos:** composition/binding, no activity, mismatch, invalid selection, nominal order, warning, blocking failure, concurrent, cleanup.
- **Smoke final escolhido:** `QaH226ActivityRestartVerticalSmoke`.
- **Smokes substituídos:** H2.2.6 Composition, Binding e `RunH226` suite.
- **Menus removidos:** 3 H2.2.6 + `H2 Run H2.2.6 Suite`.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit + Play/host.
- **Resultado PASS esperado:** restart completa na ordem correta ou falha sem clear/reentry parcial.

### 5.10 Object Reset Regression

- **Nome final:** `Object Reset Regression`
- **Domínio:** Game Flow / Object reset
- **Contrato provado:** registration, seleção unitária/múltipla, execução, failures, single-flight e cleanup.
- **Casos internos:** owner obrigatório, register/unregister/cascade, monotonic ID, duplicate, single/group/empty/missing/unregistered, optional/required, rebind, idempotência.
- **Smoke final escolhido:** `QaH228ObjectResetGroupVerticalSmoke`.
- **Smokes substituídos:** `QaH227ObjectResetVerticalSmoke`, `QaH2210ResetRegistrationRuntimeBindingSmoke`.
- **Menus removidos:** H2.2.7, H2.2.8 e H2.2.10.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** fixtures duplicadas unit/group depois da migração.
- **Pré-condições:** Play Mode e host pronto.
- **Resultado PASS esperado:** seleção executa exatamente uma vez e nenhuma registration/in-flight fica retida.

### 5.11 Input Gate Regression

- **Nome final:** `Input Gate Regression`
- **Domínio:** UI/Input
- **Contrato provado:** gate bloqueia/restaura maps respeitando baseline e domínio.
- **Casos internos:** gameplay/input-acceptance/unrelated domain, enabled/disabled baseline, missing map, no-fallback, rebind e cleanup.
- **Smoke final escolhido:** `QaH229InputGateRuntimeBindingSmoke`.
- **Smokes substituídos:** somente casos de gate hoje duplicados em IC1.
- **Menus removidos:** H2.2.9.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Play Mode, host e `PlayerInput` sintético.
- **Resultado PASS esperado:** maps retornam ao baseline exato e nenhum adapter fica bloqueado.

### 5.12 Content Anchor Materialization Regression

- **Nome final:** `Content Anchor Materialization Regression`
- **Domínio:** Scene composition/release
- **Contrato provado:** materialização/release físicos e lógicos com binding explícito.
- **Casos internos:** unbound, preflight side-effect-free, create, placement, bind, divergent rebind, scope release, repeated release, cleanup.
- **Smoke final escolhido:** `QaH2211ContentAnchorMaterializationRuntimeBindingSmoke`.
- **Smokes substituídos:** nenhum.
- **Menus removidos:** H2.2.11.
- **Assets preservados:** nenhum persistido; o prefab é temporário.
- **Assets removíveis:** nenhum asset persistido.
- **Pré-condições:** Play Mode e host pronto.
- **Resultado PASS esperado:** uma instância/handle durante o scope e zero estado após release.

### 5.13 Player Participation Authoring Regression

- **Nome final:** `Player Participation Authoring Regression`
- **Domínio:** Player participation
- **Contrato provado:** Player slots, requirements, actor selection e Activity projections válidos/invalidos; templates idempotentes.
- **Casos internos:** todos os casos P3C/P3D, incluindo missing/duplicate/empty/contradictory/project scan e templates.
- **Smoke final escolhido:** `QaP3CPlayerProfileAuthoringSmoke`.
- **Smokes substituídos:** `QaP3DActivityParticipationAuthoringSmoke`.
- **Menus removidos:** nenhum próprio hoje; remover dependência do menu P3 canônico.
- **Assets preservados:** templates oficiais do package consumidos pelo caso.
- **Assets removíveis:** temp folders continuam autoclean e não devem persistir.
- **Pré-condições:** Edit Mode.
- **Resultado PASS esperado:** válidos sem erro, negativos com issue exata, nenhuma mutação e segundo Apply sem duplicação.

### 5.14 Session Player Slots Regression

- **Nome final:** `Session Player Slots Regression`
- **Domínio:** Player participation/runtime
- **Contrato provado:** estado Session-scoped de slots, reservations, join e capacity.
- **Casos internos:** roster/order, open/close, reserve/release/join, stale/foreign, capacity, duplicate identity/reference, immutability, idempotência.
- **Smoke final escolhido:** `QaP3FSessionSlotRuntimeSmoke`.
- **Smokes substituídos:** nenhum.
- **Menus removidos:** dependência do P3 canônico.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit Mode.
- **Resultado PASS esperado:** transições/status exatos e profiles inalterados.

### 5.15 Local Player Provisioning Regression

- **Nome final:** `Local Player Provisioning Regression`
- **Domínio:** Player provisioning
- **Contrato provado:** configuração, reservation-before-provisioning, host técnico, callbacks, commit/rollback e Scene admission transaction.
- **Casos internos:** P3G2, validation, Cut5, P3G3, M4A e M4B1 completos, com positivos, negativos, idempotência/reentrância.
- **Smoke final escolhido:** `QaP3G3ProvisioningBridgeSyntheticSmoke`.
- **Smokes substituídos:** `QaP3G2...`, `QaP3Local...`, `QaCut5...`, `QaP3M4A...`, `QaP3M4B1...`.
- **Menus removidos:** Cut 5, P3M4A, P3M4B1 e P3 canônico.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** path sintético Cut5 depois da migração.
- **Pré-condições:** Edit Mode; fixture in-memory.
- **Resultado PASS esperado:** admission só após evidência válida; toda falha restaura slot/host sem parcial.

### 5.16 Scene Player Route Lifecycle Regression

- **Nome final:** `Scene Player Route Lifecycle Regression`
- **Domínio:** Route/Activity lifecycle + Player
- **Contrato provado:** Scene Local Player entra/sai/adota em transições Route reais, com identities frescas, release e matriz negativa.
- **Casos internos:** M4B2A/B, M5A e M5B; A→B→A, reentry/idempotência/stale token, no-player/gameplay-ready, duplicate/missing/mismatch/reused/undeclared, final cleanup.
- **Smoke final escolhido:** `QaP3M5BRouteTransitionAndNegativeMatrixSmoke`.
- **Smokes substituídos:** M4B2A, M4B2B, M5A.
- **Menus removidos:** M4B2A/B, M5A e os menus auxiliares M5B de repair/preflight/reconciled.
- **Assets preservados:** toda pasta `Player/P3M5B` durante a regressão final.
- **Assets removíveis:** pasta `Player/P3M5A` após migração dos casos e confirmação de zero referência.
- **Pré-condições:** setup M5B idempotente, cenas no Build Settings, Play Mode e host pronto.
- **Resultado PASS esperado:** somente uma admission ativa por vez e zero resíduo após negativos/restauração.

### 5.17 Player Gameplay Admission Regression

- **Nome final:** `Player Gameplay Admission Regression`
- **Domínio:** Player gameplay
- **Contrato provado:** join real, actor preparation, Route startup handoff, gameplay-ready, camera output e reverse release.
- **Casos internos:** K7H completo + binding/negative de H2.2.12.
- **Smoke final escolhido:** `QaP3K7HRouteStartupActivityPlayerAdmissionSmoke`.
- **Smokes substituídos:** `QaH2212PlayerActorSelectionRuntimeBindingSmoke` e dependência do P3 canônico.
- **Menus removidos:** H2.2.12 e P3 canônico.
- **Assets preservados:** Lifecycle Route B e fixtures `Player/P3G4`, `P3H4`, `P3J6`, profiles compartilhados.
- **Assets removíveis:** apenas duplicatas que o setup canônico deixar sem referência após unificação.
- **Pré-condições:** fixture J6 aplicada, Play Mode, host pronto, provisioning runtime-ready.
- **Resultado PASS esperado:** handoff/adoption/gameplay/release seguem ordem e owners exatos, sem candidato/grupo residual.

### 5.18 Player Input Mode Authority Regression

- **Nome final:** `Player Input Mode Authority Regression`
- **Domínio:** UI/Input
- **Contrato provado:** context/transaction de InputMode e único writer físico de `PlayerInput`.
- **Casos internos:** IC2 authority + IC1: prepare/commit/rollback/stale/concurrent/idempotent, set/map select/restore/rollback, single writer e no legacy bridge.
- **Smoke final escolhido:** `QaIc2InputModeRuntimeAuthoritySmoke`.
- **Smokes substituídos:** `QaIc1PlayerInputSingleWriterSmoke`; casos atuais úteis de `QaIc2PauseInputModeRuntimeRegressionSmoke`.
- **Menus removidos:** IC1 e os 2 menus IC2 atuais.
- **Assets preservados:** `Assets/InputSystem_Actions.inputactions` enquanto fixture Pause/Player usar.
- **Assets removíveis:** GUID scans e fixture experimental IC2 após migração.
- **Pré-condições:** Play Mode, host pronto e provisioning/input configurados.
- **Resultado PASS esperado:** um writer aplica estado residente de forma transacional e restaura baseline exato.

### 5.19 Pause Product Binding Regression

- **Nome final:** `Pause Product Binding Regression`
- **Domínio:** Pause
- **Contrato provado:** binding produto→input/UI, lifecycle de cena, toggle, release e no-fallback.
- **Casos internos:** Consumer + Scene Lifecycle + H2.2.1 Composition/Binding; positivos, missing/duplicate/foreign/different-port, idempotência, release-before-exit.
- **Smoke final escolhido:** `QaPauseP1ConsumerSmoke`.
- **Smokes substituídos:** Pause Scene Lifecycle e H2.2.1 pair; boundary legacy não é preservado como regressão.
- **Menus removidos:** 5 menus atuais de smoke/boundary; manter um setup e um menu final.
- **Assets preservados:** `Player/PauseP1/Scenes/QA_PauseProductBinding.unity`, `QA_PauseToggle.inputactionreference.asset`, input actions.
- **Assets removíveis:** nenhum antes da migração; código/fixture experimental IC2 não pertence a esta regressão.
- **Pré-condições:** setup idempotente aplicado, Play Mode.
- **Resultado PASS esperado:** Pause/Input maps alternam e liberam sem callback/binding/token residual.

### 5.20 Local Player Camera Publication Regression

- **Nome final:** `Local Player Camera Publication Regression`
- **Domínio:** Camera
- **Contrato provado:** authoring permite um publicador explícito e a lane real publica/revoga exatamente um request.
- **Casos internos:** todos os casos Cut4 authoring e runtime, incluindo duplicate, different-player, exact identity e release.
- **Smoke final escolhido:** `QaCut4LocalPlayerCameraPublicationOwnershipRuntimeSmoke`.
- **Smokes substituídos:** Cut4 Authoring.
- **Menus removidos:** os 2 menus Cut 4.
- **Assets preservados:** fixture K7H/Lifecycle Route B.
- **Assets removíveis:** nenhum próprio.
- **Pré-condições:** fase Edit seguida de Play fresco, host/fixture K7H.
- **Resultado PASS esperado:** exatamente um request por Local Player admitido e zero após release.

### 5.21 Camera Follow Authoring Regression

- **Nome final:** `Camera Follow Authoring Regression`
- **Domínio:** Camera authoring
- **Contrato provado:** materialização e rebuild idempotente do pipeline Follow.
- **Casos internos:** target source, camera/follow materializados, target/offset, segundo apply sem criação/duplicação.
- **Smoke final escolhido:** `QaC9MFollowPipelineSmoke`.
- **Smokes substituídos:** nenhum.
- **Menus removidos:** C9M; novo menu comportamental.
- **Assets preservados:** nenhum persistido.
- **Assets removíveis:** nenhum.
- **Pré-condições:** Edit Mode, Cinemachine disponível.
- **Resultado PASS esperado:** uma camera/um follow com configuração exata após dois Apply/Rebuild.

### 5.22 Camera Override Authority Regression

- **Nome final:** `Camera Override Authority Regression`
- **Domínio:** Camera
- **Contrato provado:** override tem priority/owner/identity exatos, ganha/perde authority e libera saída após Route transition.
- **Casos internos:** baseline, request override, arbitration, route completion, release e fallback.
- **Smoke final escolhido:** promover `QaC9RCameraOverrideAuthorityFixture.Run` para runner editor público; manter fixture runtime sem `ContextMenu` público.
- **Smokes substituídos:** proof contextual C9R.
- **Menus removidos:** `Run C9R Camera Override Authority Proof`; installer continua SETUP.
- **Assets preservados:** `Camera/Scenes/QA_PlayerCameraArbitration.unity`, route/activity/prefab C9R e Hub apenas se ainda necessário ao runner.
- **Assets removíveis:** integração Hub C9R se o runner final abrir a cena diretamente.
- **Pré-condições:** installer idempotente aplicado, Play Mode e host pronto.
- **Resultado PASS esperado:** override assume e devolve authority sem request residual.

### 5.23 Audio Playback Regression

- **Nome final:** `Audio Playback Regression`
- **Domínio:** Audio
- **Contrato provado:** SFX direct/pooled, BGM play/stop, listener policy e falhas de clip/defaults/pool.
- **Casos internos:** os 7 métodos atuais; reset/contagem privados; repetir a execução para idempotência/cleanup.
- **Smoke final escolhido:** `AudioQaPanel.RunAllSmokes`, promovido a runner com um menu público.
- **Smokes substituídos:** 7 botões públicos individuais como regressões.
- **Menus removidos:** botões individuais da superfície de regressão; controles manuais podem ficar em foldout Diagnostic.
- **Assets preservados:** `Audio/Scenes/QA_Audio.unity`, route, cues, clips, configs e `QA_PooledAudioSource.prefab`.
- **Assets removíveis:** cenas/assets `QA_FrameworkBgm*` somente se o diagnóstico Framework BGM for aposentado e não houver referência.
- **Pré-condições:** setup Audio aplicado, Play Mode, `AudioRuntimeHost`, listener e pool configurados.
- **Resultado PASS esperado:** 7 casos passam, zero failure, handles/serviços liberados no fim.

### 5.24 Pooling Runtime Regression

- **Nome final:** `Pooling Runtime Regression`
- **Domínio:** Pooling
- **Contrato provado:** prewarm/rent/reuse/return, limite sem expansão e auto-return.
- **Casos internos:** Basic, Max Limit, Auto Return, missing/invalid definition e segunda execução idempotente.
- **Smoke final escolhido:** evoluir `PoolingQaPanel.RunBasicSmoke` para orquestrar os três casos.
- **Smokes substituídos:** `RunMaxLimitSmoke`, `RunAutoReturnSmoke` como superfícies públicas.
- **Menus removidos:** três `ContextMenu` de smoke; criar um menu editor público.
- **Assets preservados:** `Pooling/Scenes/QA_Pooling.unity`, route, definitions e prefabs.
- **Assets removíveis:** nenhum antes da migração.
- **Pré-condições:** setup Pooling aplicado, Play Mode, `PoolRuntimeHost` configurado.
- **Resultado PASS esperado:** todos os objetos retornam, limite não cresce e auto-return deixa zero active.

## 6. Arquivos, classes e menus candidatos à remoção

### Remoção direta após runners finais existirem

- `Editor/QaH2RegressionSuite.cs` / `QaH2RegressionSuite`; remover os 2 menus H2.
- `Player/Editor/QaP3CanonicalPreFirstGameSmoke.cs` / `QaP3CanonicalPreFirstGameSmoke`; remover o menu P3 canônico e sua máquina `SessionState` multi-domínio.
- `Player/InternalEditor/PauseP1/QaPauseP1LegacyBoundaryStaticSmoke.cs`; manter a evidência histórica apenas em documentação/commit history, não como regressão final.
- `Player/InternalEditor/QaIc2PauseInputModeRuntimeRegressionSmoke.cs` e `QaIc2PauseInputModeBridgeFixture.cs`, depois que os casos atuais forem absorvidos por InputMode/Pause.
- `GameFlow/InternalEditor/QaH2213DiagnosticsQaBoundarySmoke.cs`, se os checks estruturais forem incorporados no Host Authority final; alternativamente manter helper sem menu.

### Arquivos/classes `MERGE` removíveis depois da migração de casos

- Game Flow: todas as classes `*CompositionSmoke` e `*BindingSmoke` H2.2.2–H2.2.6; H2.2.7; H2.2.10; H2.2.12.
- Pause: H2.2.1 pair e `QaPauseP1SceneLifecycleCompositionSmoke`.
- Player: P3D, P3G2, provisioning validation, Cut5, M4A, M4B1, M4B2A, M4B2B e M5A.
- Camera: Cut4 Authoring.
- UI/Input: IC1.
- Audio/Pooling: não é obrigatório remover os panels; tornar casos/helpers privados e retirar superfícies individuais.

Nenhum arquivo `MERGE` deve ser removido antes de: (1) migrar seus casos, (2) confirmar que o final executa positivos/negativos/idempotência, (3) verificar referências por classe/GUID e (4) validar no Unity pelo usuário.

## 7. Assets e cenas que podem ficar órfãos

### Candidatos fortes após consolidação

- Toda a pasta `Assets/ImmersiveFrameworkQA/Player/P3M5A` e seus `.meta`, depois que os casos exclusivos forem absorvidos pelo final M5B.
- Cenas temporárias `Assets/QA_P3M4B2A_Temp_*.unity` e `Assets/QA_P3M4B2B_Temp_*.unity` não devem existir persistidas; qualquer ocorrência após smoke indica resíduo.
- Assets/integração de Hub criados exclusivamente pelo C9R, se o final Camera Override abrir sua cena diretamente.
- `Audio/Scenes/QA_FrameworkBgm.unity`, `QA_FrameworkBgmRouteB.unity`, routes/activities/cues associados, caso o painel Framework BGM permaneça apenas diagnóstico e seja aposentado.
- `Hub/Scenes/QA_Hub.unity` e `Hub/Routes/QA_HubRoute.asset`, se nenhum diagnóstico/manual navigation restante os referenciar.
- Superfícies `UnityBuildSurface` (`UnityBuildSurfaceQA`, `TransitionRouteA/B`, `ActivityAdditionalContent`, `QA_UIGlobal`, routes/activities/game application) somente se, após a consolidação, não forem usadas pelo bootstrap/host, Pause, Lifecycle, Audio ou FIRSTGAME — esta auditoria **não autoriza** removê-las.

### Preservar

- `Player/P3M5B` completo enquanto existir Scene Player Route Lifecycle Regression, inclusive todas as cenas/assets negativas.
- `Lifecycle/Scenes/QA_LifecycleRouteB.unity` e fixtures P3G4/H4/J5/J6 enquanto K7H/Camera Publication dependerem delas.
- `Player/PauseP1` e action assets enquanto Pause Product Binding existir.
- `Audio/Scenes/QA_Audio.unity`, cues/configs/prefab; `Pooling/Scenes/QA_Pooling.unity`, definitions/prefabs.
- Camera C9R scene/route/activity enquanto Camera Override final existir.

Antes de qualquer remoção futura, pesquisar GUIDs dos `.meta`, referências YAML e `AssetDatabase` paths; esta auditoria não inferiu ausência apenas pelo nome.

## 8. Ordem segura de consolidação

1. Criar nomenclatura/menu final comum e helpers de execução sem alterar casos.
2. Consolidar famílias locais e sem assets: Route Request, Activity Request, Route Reset, Activity Reset e Activity Restart.
3. Consolidar Object Reset (registration + unit + group) e Content Anchor.
4. Consolidar Player Participation Authoring e Session Slots, ainda em Edit Mode/sintético.
5. Consolidar Local Player Provisioning, preservando todos os negativos/rollbacks.
6. Consolidar InputMode e Pause; só então retirar IC2 experimental e boundary legacy.
7. Consolidar Scene Player Route Lifecycle sobre M5B; migrar M5A/M4* antes de tocar em assets.
8. Consolidar K7H + H2.2.12 e depois Camera Publication, porque ambos compartilham a vertical Player gameplay.
9. Automatizar Camera Override, Audio e Pooling em um menu público por regressão.
10. Remover menus por código histórico e, por último, `QaP3CanonicalPreFirstGameSmoke` e `QaH2RegressionSuite`.
11. Auditar GUIDs e somente então remover arquivos/assets órfãos.

## 9. Cortes de implementação pequenos

| Corte | Escopo | Arquivos principais | Risco |
|---|---|---|---|
| R2 | Route/Activity request | H2.2.2/H2.2.3 pairs | Baixo; fixtures in-memory. |
| R3 | Route/Activity reset | H2.2.4/H2.2.5 families | Médio; Edit→Play orchestration. |
| R4 | Activity restart | H2.2.6 family + remover suite específica | Médio. |
| R5 | Object reset | H2.2.7/H2.2.8/H2.2.10 | Médio; registration cleanup. |
| R6 | Host/diagnostics | H2.4 + H2.2.13 | Baixo-médio; source scans. |
| R7 | Participation/slots | P3C/P3D/P3F | Baixo; temporários. |
| R8 | Provisioning | P3G2/G3, validation, Cut5, M4A/B1 | Médio-alto; muitos negativos. |
| R9 | Input/Pause | IC1/IC2, Pause Consumer/Scene/H2.2.1 | Alto; maps, callbacks e Play Mode. |
| R10 | Scene player lifecycle | M4B2A/B, M5A/B | Alto; scenes/GUIDs/release. |
| R11 | Player gameplay/camera publication | K7H/H2.2.12/Cut4 | Alto; vertical host-scoped. |
| R12 | Camera override/follow | C9R/C9M | Médio; installer + Cinemachine. |
| R13 | Audio/Pooling | panels e builders | Médio; coroutines e serviços técnicos. |
| R14 | Remoção de mega-runners/menus | P3 canonical + H2 suite + README | Baixo após todos os finais passarem. |
| R15 | Prune de assets | M5A/Hub/FrameworkBgm candidatos | Alto; somente após audit GUID/YAML e validação Unity. |

## 10. Validação realizada e checklist manual futuro

### Validação estática realizada neste corte

- Busca por nomes e conteúdo: `Smoke`, `Regression`, `Suite`, `Run All`, `ContextMenu`, `MenuItem`, `Setup`, `Rebuild`, `Validation`, `Negative`.
- Leitura dos 50 arquivos `*Smoke`, runners agregados, builders/setups/repairs, `README.md`, `Packages/manifest.json` e asmdefs QA relevantes.
- Contagem distinta de `MenuItem` e validação de atributos `ContextMenu` reais.
- Mapeamento estático de `FrameworkRuntimeHost`, métodos/casos (`completed.Add`) e paths de cenas/assets.
- Busca por NUnit em `Assets`/`Packages`; nenhum teste fonte foi incluído.
- Nenhum Unity build, import, test, Play Mode, batchmode ou smoke foi executado.

### Checklist manual para cada corte de implementação

1. Abrir Unity e confirmar import/compile sem erro.
2. Confirmar que existe exatamente um menu público por regressão final e nenhum menu por helper/caso.
3. Rodar o setup idempotente duas vezes quando houver assets e confirmar ausência de duplicatas/diffs inesperados.
4. Rodar o smoke final em sessão limpa nas fases Edit/Play exigidas.
5. Confirmar no log um relatório único com casos positivos, negativos e idempotência, incluindo nome do caso que falhou.
6. Repetir o smoke na mesma sessão quando o contrato permitir; quando one-shot for requisito real, confirmar rejeição explícita e cleanup.
7. Confirmar zero `FrameworkRuntimeHost` ambíguo, zero request/transaction/token/in-flight residual e restauração das cenas abertas.
8. Antes de remover assets, pesquisar GUID/YAML/path e abrir todas as cenas finais para verificar Missing Script/reference.
9. Atualizar `Assets/ImmersiveFrameworkQA/README.md` somente no corte final de menus.

## 11. O que não mudar agora

- Não alterar `com.immersive.framework`, `FIRSTGAME` ou packages técnicos congelados para acomodar o QA.
- Não consolidar testes NUnit do package dentro dos smokes.
- Não remover cenas/assets negativos de P3M5B: eles são casos válidos do final.
- Não apagar classes `MERGE` antes de migrar seus casos e validar no Unity.
- Não criar uma nova suite global para substituir H2/P3; a navegação final deve permanecer por domínio real.
- Não interpretar smoke que atualmente passa como justificativa suficiente para preservação.

## 12. Totais para o próximo corte

- **Quantidade encontrada:** 60 operações de smoke, 1 classe de suite adicional, 60 `MenuItem` distintos e 27 `ContextMenu`.
- **Quantidade final proposta:** 24 regressões públicas.
- **Quantidade a mesclar:** 34 operações de smoke classificadas `MERGE`.
- **Quantidade a remover:** 2 mega-orquestradores públicos (`QaP3CanonicalPreFirstGameSmoke` e `QaH2RegressionSuite`); adicionalmente, 1 legacy, 1 temporary e 1 diagnostic perdem menu/arquivo após absorção ou privatização.
- **Ordem dos próximos cortes:** R2 requests → R3 resets → R4 restart → R5 object reset → R6 host/diagnostics → R7 participation → R8 provisioning → R9 input/pause → R10 scene player → R11 gameplay/camera → R12 camera authoring/override → R13 audio/pooling → R14 menus/runners → R15 assets.
