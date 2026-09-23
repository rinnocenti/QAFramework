# QA-SYSTEMIC-AUDIT-01 — Async Operations, Evidence Semantics and Cleanup Ownership

Data: 2026-08-03 (America/Sao_Paulo)  
Tipo: auditoria técnica estática de todo o repositório; nenhuma execução Unity foi feita.

## 1. Baseline e cobertura

| Item | Evidência |
|---|---|
| QAFramework | `a2fbc5feb03ec5f158331ba7e10a4a13e79dbe29` — `fix(game-flow): align direct readiness evidence with real host progress` |
| Framework (somente leitura) | `f5620efa8ddd1046e6ecb7f3194a2ee562db6dd5` |
| Estado inicial | ambos os worktrees limpos |
| Unity | `6000.5.0f1 (88b47c5e7076)` |
| Escopo lido | `Assets/ImmersiveFrameworkQA/**`, `ProjectSettings/EditorBuildSettings.asset`, `EditorSettings.asset`, `Packages/manifest.json`, `packages-lock.json` e os runtime/editor sources do framework que determinam as asserções |
| C# / menus / prefixos | 128 arquivos C#, 113 `MenuItem`, 90 prefixos de resultado |
| Artefatos | 33 cenas, 11 prefabs, 93 assets, 17 asmdefs, 18 documentos |
| Indicadores de coordenação | 9 `TaskCompletionSource`, 3 `Task.WhenAny`, 47 `Awaitable.NextFrameAsync`, 25 `async void`, 3 `DisposeAsync`, 20 usos de `SessionState` |

As 33 cenas QA foram confrontadas com as 59 entradas ativas de Build Settings. A diferença é esperada: há cenas negativas, auxiliares e de conteúdo que são carregadas por contrato, não por abertura inicial. Os 93 assets são fixtures/configurações; não há evidência estática de que um asset runtime criado seja preservado depois do ciclo que o criou, mas esse contrato não é uniforme entre as famílias.

## 2. Conclusão executiva

**Status: Systemic hardening required before QA-03.**

QA-01 e QA-02 não têm uma falha runtime conhecida equivalente e são de risco global menor, mas a infraestrutura QA ainda permite que uma observação passiva, uma projeção cacheada e uma continuação de frame sejam tratadas como o mesmo limite causal. QA-03 já corrigiu o unwind do request pendente; o bloqueio remanescente é de semântica de evidência, e o mesmo padrão aparece em outras superfícies de apresentação e integração.

Não foi identificada justificativa para alterar o pacote: a ordem observada é compatível com a implementação do framework. A correção deve ser de contrato QA compartilhado.

## 3. Achado atual de QA-03

Em `QaDirectActivityReadinessPoliciesRegression.cs:366-378`, a sequência aceita `Hidden` do observer, aguarda um frame e exige simultaneamente `!transition.IsVisible` e alpha zero. A evidência runtime fornecida mostra `Hidden`, alpha `0`, status `Succeeded`, request ainda pendente em `WaitVisible`, mas `IsVisible=true` após esse frame.

`IsVisible` é **diagnostic-only at passive boundary**; torna-se uma projeção válida apenas após o **resultado da operação do adaptador**, nunca uma prova pré-readiness derivada de `LateUpdate` + frame arbitrário. O observer mede uma amostra visual; `IsVisible` é `lastVisibleState` cacheado. Portanto, para esta prova, `IsVisible` é inválido como asserção de passagem naquele ponto. Não se recomenda aumentar a contagem de frames.

O unwind em `QaDirectActivityReadinessPoliciesRegression.cs:1036-1065` é efetivo segundo a evidência fornecida: completa a preparação, aguarda o request, verifica autoridade/gate/superfícies e só então permite disposal. Ele não deve ser revertido.

## 4. Inventário operacional completo

Os caminhos abaixo são a indexação integral dos 128 arquivos C# operacionais. `Ativo` significa invocável por menu, componente de cena/prefab ou chamado por outra superfície; `Alcançável legado` é menu/fixture ainda compilado, sem prova estática de consumidor atual; `Desconhecido` requer inspeção da referência serializada. Todos os Edit Mode menus usam a própria função `Run/Setup/Validate` como rota; componentes Runtime são Play Mode e invocados pela cena/prefab que os referencia.

| Tipo QA | Arquivos/classes, rota, modo, autoridade e ownership | Coordenação/evidência, limpeza/restauração, alcance |
|---|---|---|
| Regression | `ActivityFlow/Editor/QaActivityLocalVisibilityRuleRegression`; `Camera/Scripts/Editor/QaCameraOutputSessionBindingAuthoringRegression`, `QaCameraRuntimeHostIntegrationRegression`, `QaPersistentCameraPresentationCompositionRegression`, `QaSessionCameraOverrideIdentityAuthoringRegression`; `Descriptors/Editor/QaRouteActivityIdentityValidationRegression`; `GameFlow/InternalEditor/ActivityRequestRegression`, `QaActivityEntryPresentationEvidenceRegression`, `QaActivityEntryReadinessFoundationRegression`, `QaActivityLocalVisibilityLifecycleRegression`, `QaBootGameFlowBaselineRegression`, `QaDirectActivityReadinessPoliciesRegression`, `QaGameFlowPlayerIndependentNavigationRegression`, `QaPlayerActorSelectionRuntimeBindingRegression`, `QaRouteActivityIdentityRegression`, `QaRouteOwnedSceneDiscoveryRegression`, `RouteRequestRegression`; `Player/Editor/QaP3M4BRouteSceneProvidedAdmissionRegression`, `QaP3M4CSceneProvidedAdmissionScopeRegression`, `QaP3M4DSceneProvidedExitReentryRegression`, `QaP3M4ESceneProvidedActivitySwitchRegression`, `QaPlayerGameplayAdmissionRegression`, `QaPlayerParticipationAuthoringRegression`. | Menus Edit Mode/Play Mode; tocam host, route/activity, player, câmera e cenas. Requests são retidos localmente; QA-01/02/03 têm contagem explícita 18/26/42. Cleanup é do próprio runner/fixture; restauração de hub é setup. Ativo, salvo rotas serializadas `Desconhecido`. |
| Smoke | `QaArchA2ActivityTransitionTransactionSmoke`; todos `QaA1*`, `QaB1*`, `QaC9M*`, `QaCore*`, `QaObjectReset*`, `QaActivity*VerticalSmoke`, `QaActivityReadinessPostTransitionSmoke`, `QaGameApplicationValidationScopeSmoke`, `QaGameFlowDiagnosticFaultLeaseSmoke`, `QaH24*`, `QaH2FrameworkReadiness`, `QaInputGateRuntimeBindingSmoke`, `QaPlayerHostEvidenceDiagnosticFormattingSmoke`, `QaZeroSlot*`; `Player/**/QaCpsa1*`, `QaP3G3*`, `QaP3M4B1*`, `QaCut5*`, `QaIc1*`, `QaP3C*`, `QaP3D*`, `QaP3F*`, `QaP3G2*`, `QaP3Local*`, `QaP3M4A*`, `QaP3M4B2A*`, `QaP3M4B2B*`, `QaP3M5A*`, `QaP3M5BRoute*`, `QaH221*`, `QaIc2*`, `QaPauseP1ConsumerSmoke`, `QaPauseP1LegacyBoundaryStaticSmoke`, `QaPauseP1SceneLifecycleCompositionSmoke`, `QaPauseInputActionMapReferenceSmoke`; `PoolingRuntimeRegressionMenu` smoke routes. | Principalmente menus Edit Mode; smoke runtime em Play Mode, tocando contracts de host/gate/pause/player/camera. Sem registry comum; prefixos próprios. Alcance Ativo quando menu, os demais Alcançável legado até uma matriz de execução unificada. |
| Validator | `QaPauseProductBindingStaticValidator`, validadores aninhados de `QaPersistentContentApplicationMigration`, `QaObjectResetTriggerAuthoringValidationSmoke`, `QaP3M5BPersistedFixturePreflight`. | Editor, AssetDatabase/cenas/configuração; sem request de runtime. Cleanup não aplicável; risco é escopo global de busca. Ativo. |
| Setup / Restore | `QaActivityEntryPresentationEvidenceSetup`, `QaLocalPlayerRuntimeIntegrationSetup`, `QaP3H4RuntimeHostActorSelectionSetup`, `QaP3J5RuntimeHostPreparationSetup`, `QaP3J6ActivityPlayerActorLifecycleSetup`, `QaP3M4BRouteSceneProvidedAdmissionSetup`, `QaP3M4CSceneProvidedAdmissionScopeSetup`, `QaP3M4DSceneProvidedExitReentrySetup`, `QaP3M4ESceneProvidedActivitySwitchSetup`, `QaP3M5A*Setup`, `QaP3M5BReconciledFixtureSetup`, `QaP3M5BRouteTransitionAndNegativeMatrixSetup`, `QaPauseP1Setup`, `QaPauseProductBindingSetup`, `QaPauseSceneRequestBindingSetup`; builders/configurators/migrations Audio, Hub, Lifecycle, Pooling, Canonical UI. | Edit Mode, alteram cenas/assets/Build Settings e alguns marcam `SessionState`; restauração é ad hoc por fluxo. Ativo; alto risco operacional se executados fora da sessão correspondente. |
| Fixture | `QaActivityEntryReadinessFixture`, `QaPlayerGameplayAdmissionFixture`, `QaC9RCameraOverrideAuthorityFixture`, `QaC9R*CompletionCoordinator`, `PlayerSceneShape`, `QaObjectResetTriggerAuthoringValidationSmoke.Fixture`. | QA-01/03 e player/câmera; criam objetos e ScriptableObjects, assinam eventos e chamam route/activity. `QaActivityEntryReadinessFixture.DisposeAsync` tem ordem explícita, mas pressupõe request já resolvido. Ativo. |
| Observer / Probe | `QaTransitionPresentationEvidenceObserver`, `QaRouteContentLifecycleProbe`, `PoolingQaCallbackProbe`, `PauseSceneRequestBindingProbe`, `ReflectionBinderProbe`, `QaPlayerJoinEvidence`, panels de evidence. | Runtime/Play Mode; amostram `LateUpdate`, callbacks e estado de componentes. Não são autoridade. Cleanup deve destruir/unsubscribir o dono de fixture. Ativo por cena; referências serializadas requerem confirmação. |
| Adapter / Fake / Synthetic Surface | `QaLoadingSurfaceVisibilityHoldAdapter`, `QaPauseSurfaceAdapter`, `QaFakeActivity*`, `QaFakeRoute*`, `QaFakePause*`, painéis Transition, Audio/Hub/Lifecycle/Pooling/Pause QA. | Fakes são Edit Mode; adapters/painéis Runtime. Loading produz protocolo Show/Update*/Hide e evidencia request/apply/result. Ativo; superfície sintética de QA-02 não prova por si só o host real. |
| Editor Menu / Wizard | `AudioQaGeneratedClipRepair`, `AudioQaSceneBuilder`, `FrameworkBgmQaSceneBuilder`, `QaCameraOverrideAuthorityInstaller`, `QaCameraOverrideAuthoritySceneInstaller`, `QaCut4*`, `QaPauseProductBindingMenu`, builders/migration/configurators já citados. | 113 menus; Edit Mode. Invocam descoberta de assets/cenas e mutação autoral. Sem cancelamento/rollback transacional comum. Ativo ou Alcançável legado. |
| Runtime Harness Component | `AudioQaPanel`, `FrameworkBgmQaPanel`, `QaHubPanel`, `QaHubReturnPanel`, `QaLifecyclePanel`, `QaC9RLocalPlayerCameraRequestBinding`, `PauseOfficialPlayerPreflightPanel`, `PauseQaIntentPanel`, `PauseRuntimeEvidencePanel`, `PoolingQaPanel`. | Play Mode, componentes de cena. Autoridade é a cena/host configurado; usam callbacks e UI. Cleanup pelo unload/destruição de cena. Alcance Desconhecido sem abrir todas as referências. |
| Shared Assertion / Diagnostic Helper | `QaGameFlowPlayerIndependentNavigationSupplementalCases`, `QaInputModeFrameworkRuntimeHostResolver`, `RegressionCompensationStack`, `QaH225Scenario/ParticipantSource/Participant`, helpers internos de fixture/evidence. | Não têm rota própria; compartilham asserções/diagnósticos. Não há biblioteca QA consolidada para case/failure/owned async. Ativo por referências ou Desconhecido. |
| Scene / Prefab / Asset Fixture | 33 `.unity` (incluindo `QA_IF_READY_04_DirectPoliciesContent`, `QA_UIGlobal`, `ActivityAdditionalContent`, rotas Transition, P3M4/5, Pause, Camera, Pooling, Audio, Hub/Lifecycle), 11 prefabs e 93 assets. | Autoridade declarada por rota/activity/global UI ou componente da cena; Build Settings só é requisito quando a rota usa índice/cena registrada. `QA_IF_READY_04_DirectPoliciesContent` é dedicada; `QA_UIGlobal` é fonte descarregada após persistência dos roots. Ativo/Desconhecido por referência serializada. |
| Documentation-only | 18 `.md` existentes em `Assets/ImmersiveFrameworkQA/Documentation`. | Sem execução; descrevem setup/evidência histórica. Não são prova de runtime. |

Namespaces seguem a família `ImmersiveFrameworkQA.*`; os arquivos internal editor sem namespace explícito foram tratados como assembly `ImmersiveFrameworkQA.GameFlow.Internal.Editor`/equivalente. PASS/FAIL são os prefixos definidos localmente (90 ocorrências), não um contrato unificado. A coluna de registro é `18` em QA-01, `26` em QA-02 e `42` em QA-03; os demais usam listas/asserções locais ou não possuem registry.

## 5. Achados

| ID / severidade / confiança | Arquivo e linha; superfície; classe | Trigger, falha e riscos | Impacto cleanup; correção; helper; pacote |
|---|---|---|---|
| QA-AUD-EVIDENCE-001 — **Critical**, alta | `GameFlow/InternalEditor/QaDirectActivityReadinessPoliciesRegression.cs:366-378`; QA-03; evidência passiva × projeção cacheada | `LateUpdate` registra Hidden/alpha 0; o cache do adaptador ainda pode ser true. **Observado**. FP: reprovação apesar de apresentação correta. FN: usar só cache depois esconderia alpha divergente. | Unwind já preserva request. Validar a ordem por entrada tipada + checkpoint e usar resultado do adaptador no ponto causal; não frame extra. `QaEvidenceCheckpoint`. Cross-check: `UnityFadeCurtainEffectAdapter.cs:232-278` atualiza alpha e `lastVisibleState` em continuidades distintas. |
| QA-AUD-ASYNC-001 — **High**, alta | `QaDirectActivityReadinessPoliciesRegression.cs:310-352`; QA-03; evento/TCS/WhenAny | A inscrição ocorre antes do request (bom), mas o `TaskCompletionSource` só fecha para predicado composto; se nenhuma continuação avança, ambos tasks ficam pendentes. É hipotético após a correção do sinal perdido. | Não iniciar disposal antes de `request` encerrar. Registrar dono/fase e unwind obrigatório, não timeout como solução. `QaOwnedAsyncOperation<T>`. Pacote cross-check: `GameFlowRuntime.cs:736+` mantém request até a readiness. |
| QA-AUD-FRAME-001 — **High**, alta | 47 ocorrências; exemplos QA-03 `:122,366,399,433,1059`, QA-02 `QaActivityEntryPresentationEvidenceRegression.cs:128,144`; frame como sincronização | Frames de Destroy são aceitáveis após ownership; os listados inferem ordem entre observer/adapter/request. QA-03 é observado; nos demais o risco é hipotético. FP recorrente; FN possível se amostra não ocorre. | Pode iniciar cleanup por estado aparente. Classificar cada frame por `Destroy`, scene operation, visual sample ou resultado; remover os de inferência. `QaEvidenceCheckpoint`. Pacote confirma que continuidades independentes não têm ordem de frame pública. |
| QA-AUD-EVIDENCE-002 — **High**, alta | `QaDirectActivityReadinessPoliciesRegression.cs:748-773`; `QaLoadingSurfaceVisibilityHoldAdapter.cs:368-465`; QA-03/loading | Contagem/ordem de Updates é variável com progresso real. FIX5 já usa gramática Show + Update* + Hide; outras superfícies ainda têm contagens exatas de adapter/samples. FP quando houver updates extras; FN se apenas contagem coincidir com razão errada. | Cleanup pode interpretar hold incorretamente. Exigir prefixo, grupos Update*, sufixo e terminal success com identidade. `QaEvidenceGrammar`. Cross-check: host cria reporter e pode emitir progress (`FrameworkRuntimeHost.cs:592-629`). |
| QA-AUD-CLEANUP-001 — **High**, alta | `QaActivityEntryReadinessFixture.cs:496-503`; QA-01/03 fixture | `DisposeAsync` remove listeners/destroi surface antes de restaurar autoridade; não possui tipo que declare “nenhum request pendente”. QA-03 compensa externamente; outro consumidor pode não fazê-lo. FP: cleanup mascara erro; FN: fixture desliga evidência enquanto request continua. | Crítico se pending request sobrevive; Play Mode não pode ser o cancelador. Exigir `QaOwnedAsyncOperation` resolvido/unwound como precondição de Dispose. Cross-check: request público só atualiza host state após await (`FrameworkRuntimeHost.cs:677-747`). |
| QA-AUD-CLEANUP-002 — **Medium**, alta | 25 `async void`, incluindo QA-01 `:25`, QA-02 `:28`, QA-03 `:65`; runners de menu | Exceção não observada fora do bloco local ou execução interrompida pode perder contexto do owner/fase. Hipotético; logs atuais ajudam, mas não são contrato. | Falha secundária pode substituir/duplicar a primária. Um runner deve capturar primária, cleanup e unwind separadamente, sem avançar cases no finally. `QaFailureCollector`. Pacote não envolvido. |
| QA-AUD-SETUP-001 — **Medium**, alta | `QaActivityEntryPresentationEvidenceSetup.cs:53-55,96-98,290-367`; setup QA-02/03 | Prepare QA-02 apaga QA-03 e vice-versa; ambos usam marker sem id de sessão Play Mode. Restore automático limpa ambos. Observado estaticamente. FP: menu bloqueia execução válida; FN: marker sobreviver a reload e validar sessão errada. | Restauração pode ser aplicada ao alvo errado/sem diagnóstico de invalidação. Criar sessão canônica com id, perfil, estado e resultado de restore. `QaSetupSession` Editor. Pacote não envolvido. |
| QA-AUD-FIXTURE-001 — **Medium**, alta | `QaDirectActivityReadinessPoliciesRegression.cs:271+`; `GlobalUiSceneRuntime.cs:340-447`; cenas QA-03 | A antiga hipótese “filho do host” não é válida: `QA_UIGlobal` transfere roots a `DontDestroyOnLoad` e descarrega a source scene. A resolução atual por `gameObject.scene` é a direção correta; buscas globais futuras voltariam a contaminar. | Não destruir roots persistentes cuja autoridade não foi adquirida. Registrar `QaSceneFixtureContract` com source/persistent roots/owner. Cross-check confirmado. |
| QA-AUD-FIXTURE-002 — **Medium**, média | 33 cenas/11 prefabs/93 assets; `GetRootGameObjects` 41, `GetComponentsInChildren` 61, `SceneManager.sceneCount` 7 | Fixtures P3M4/P3M5/Pause/Camera compartilham Build Settings e algumas rotas. Sem contrato declarativo por fixture, busca de todos os roots/cenas pode aceitar componente estrangeiro. Hipotético. FP e FN por contaminação. | Pode destruir conteúdo autorado ou restaurar autoridade indevida. Delimitar cena primária, raiz autorizada e assets temporários. `QaScopedRuntimeResolver` + `QaSceneFixtureContract`. Pacote cross-check por composição de cena, sem mudança recomendada. |
| QA-AUD-SCOPE-001 — **Medium**, alta | 12 `Resources.FindObjectsOfTypeAll`, 1 `FindAnyObjectByType`, 33 `GetSceneByPath`; validators/setup | Busca whole-project é válida apenas em validação de autoria; em runtime ela excede a autoridade da rota/host. Hipotético, especialmente em cenas additive/persistent. FP: encontra objeto correto porém estrangeiro; FN: ignora owner real. | Cleanup pode capturar autoridade que não adquiriu. Exigir escopo explícito (asset folder, primary scene ou persistent runtime scene). `QaScopedRuntimeResolver`. Pacote cross-check confirma múltiplos roots persistidos. |
| QA-AUD-REGISTRY-001 — **Medium**, alta | `QaActivityEntryReadinessFoundationRegression.cs:19,229`; `QaActivityEntryPresentationEvidenceRegression.cs:22,270`; `QaDirectActivityReadinessPoliciesRegression.cs:32,67`; `CaseRegistry` 4 usos | QA-01/02 usam listas locais; QA-03 usa `CaseRegistry`. Contagem fixa é válida para casos internos, mas não para eventos visuais/progress. Indexação em diagnóstico deve ser segura em zero/parcial/completo. Hipotético fora QA-03. | Caso de cleanup não pode avançar a sequência normal ou mascarar falha primária. Consolidar registry e failure snapshot. `QaCaseRegistry`. Pacote não envolvido. |
| QA-AUD-EVIDENCE-003 — **Medium**, alta | `FrameworkRuntimeHost.cs:677-747` e usos QA de `CurrentActivity/CurrentRoute` (229/144); integração host | Host público atualiza `_state` depois do await. Inspecioná-lo enquanto request está pendente produz estado anterior mesmo que GameFlow já tenha commit interno. Hipotético. FP de ordenação; FN se se confundir snapshot com conclusão. | Pode iniciar restore concorrente. Só usar snapshot público após task; antes disso usar entrada tipada/checkpoint. `QaOwnedAsyncOperation<T>`. Cross-check confirmado. |

## 6. Padrões recorrentes por causa raiz

| Causa | Onde reaparece | Regra consolidada |
|---|---|---|
| Evidência passiva versus cache | observer Transition, `IsVisible`, `CurrentAlpha`, `LastStatus` | Amostra visual, cache e resultado têm tipos semânticos distintos; nunca comparar dois como se fossem o mesmo evento. |
| Sincronização por frame | 47 `NextFrameAsync`, observers Update/LateUpdate | Frame só para propagação Unity documentada ou Destroy já causalmente decidido; jamais para ordenar tasks independentes. |
| Gramática variável | loading/progress, callbacks, amostras LateUpdate | Validar protocolo `prefix + repeat* + suffix + terminal result`, não contagem exata variável. |
| Ownership de request pendente | route/activity/pause/restart/player/camera | Quem inicia conserva task, logging de fase e unwind até terminal antes de disposal. |
| Autoridade de cleanup | fixture readiness, surfaces e roots persistentes | Desinscrever com segurança, resolver/unwind, liberar autoridade adquirida, destruir temporários, restaurar e verificar. |
| Ciclo de marker | QA-02/03 setup e post-play restore | Marker precisa id de sessão, perfil selecionado, transições declaradas e relatório de invalidação. |
| Contaminação de fixture | cenas additive, global UI persistente, P3/Pause/Camera | Toda fixture declara scene/root/assets que possui e o que é emprestado; não usar busca global como ownership. |
| Escopo de lookup | Resources, roots, cenas carregadas, AssetDatabase | Escopo deve ser parte do assertion: pasta específica, cena primária ou runtime persistent específico. |
| Segurança de registry | listas QA-01/02/03 e mensagens de falha | Registro fixa somente casos de teste; nunca eventos variáveis. Snapshot seguro em qualquer progresso. |

## 7. Matriz de risco

| QA | Hang | False-fail | False-pass | Leak cleanup | Uso indevido setup | Contaminação fixture | Geral |
|---|---|---|---|---|---|---|---|
| QA-01 Foundation | Médio | Médio | Baixo | Médio | Médio | Médio | Médio |
| QA-02 Presentation | Médio | Médio | Médio | Médio | Alto | Médio | Médio/alto |
| QA-03 Direct policies | Médio (com unwind) | **Alto/observado** | Médio | Baixo após unwind | Alto | Médio | **Alto** |
| Route/Activity/Reset smokes | Médio | Médio | Médio | Médio | Baixo | Médio | Médio |
| Player/Pause/Camera integration | Médio | Médio | Médio | Médio | Alto | Alto | Alto |
| Builders/validators authoring | Baixo | Médio | Médio | Baixo | Alto | Médio | Médio |

## 8. Infraestrutura QA compartilhada necessária

| Primitiva | Problema e consumidores | Deve fazer / não deve esconder | Local |
|---|---|---|---|
| `QaOwnedAsyncOperation<T>` + `QaOperationUnwindResult<T>` | ownership, fase, request pendente e unwind de QA-01/02/03, route/activity/pause/restart/player/camera | Reter task, registrar início/terminal, preservar exceção primária e exigir unwind antes de dispose; **não** cancelar/silenciar falhas. | QA Runtime/Common |
| `QaEvidenceCheckpoint` | ordem causal QA-02/03 e observers | anexar sequência, fonte e boundary causal; **não** promover cache/frame a autoridade. | QA Runtime/Common |
| `QaEvidenceGrammar` | Loading progress e callbacks variáveis | prefixo, grupos repetidos, sufixo, identidade e terminal; **não** normalizar perda de evento. | QA Runtime/Common |
| `QaFailureCollector` e `QaCaseRegistry` | 25 runners async void, registros 01/02/03 | separar falha principal, unwind e cleanup; casos únicos/ordem segura; **não** converter cleanup em PASS. | QA Editor/Common |
| `QaSetupSession` | markers QA-02/03 e demais setups | id de sessão, perfil, estado, invalidação explícita e restore diagnosticado; **não** restaurar silenciosamente. | QA Editor |
| `QaScopedRuntimeResolver` + `QaSceneFixtureContract` | global UI, P3/Pause/Camera, buscas globais | limitar scene/root/folder, registrar propriedades e temporários; **não** inferir ownership por Transform. | QA Runtime/Common + QA Editor |

## 9. Cortes consolidados de hardening

### Corte 1 — contratos causais de evidência e operação

Objetivo: eliminar o achado Critical e todos os riscos que compartilham a causa “amostra/cache/frame como boundary”. Escopo: QA-01/02/03, observer Transition, adapter QA Loading, `QaOwnedAsyncOperation`, checkpoints e grammar. Fora: alteração do framework e redesign de produto. Ordem: introduzir tipos; migrar QA-03; migrar QA-02/01; adicionar negativos de sinal ausente e request pendente. Aceite: nenhuma asserção pré-readiness depende de `IsVisible` após frame; loading aceita Update*; request tem terminal/unwind. Risco: migração revela assertions que eram temporalmente frágeis. Commit sugerido: `test(qa): add causal async evidence contracts`.

### Corte 2 — cleanup e agregação de falha

Objetivo: tornar ownership de task precondição verificável de fixture disposal. Escopo: `QaActivityEntryReadinessFixture`, fixtures Player/Camera e runners async. Fora: mudar lifecycle do pacote. Ordem: collector; operation owner; fixtures; negativos de cleanup após falha. Guard: falha primária preservada, casos de cleanup não avançam registry. Aceite: nenhum componente/listener/surface temporário é destruído antes do task iniciado pelo QA terminar ou sofrer unwind explícito. Risco: expõe requests sem completion pública. Commit: `test(qa): enforce request ownership before fixture cleanup`.

### Corte 3 — sessão canônica de setup/restauração

Objetivo: eliminar markers mutuamente implícitos. Escopo: `QaActivityEntryPresentationEvidenceSetup` e famílias Player/Pause que alteram cenários. Fora: alteração automática de Build Settings fora de menu explícito. Ordem: modelo de sessão; migrar QA-02/03; migrar demais setup; testes de Enter/Exit/domain reload/interrupção. Aceite: cada menu informa perfil invalidado, sessão e resultado de restore. Risco: primeiro uso requer re-preparo explícito. Commit: `test(qa): make setup and restore sessions explicit`.

### Corte 4 — contratos de fixture e resolução com escopo

Objetivo: impedir contaminação por cena/root/asset estrangeiro. Escopo: Global UI, DirectPolicies, P3M4/P3M5, Pause e Camera; resolver e contract. Fora: reestruturar cenas de produto. Ordem: documentar owner por fixture; substituir buscas globais runtime; negativos com roots persistentes/additive. Aceite: cada lookup declara a cena/root/pasta e cada temporário tem dono e release. Risco: referências serializadas históricas serão expostas. Commit: `test(qa): scope fixture authority and runtime discovery`.

### Corte 5 — matrizes e execução observável

Objetivo: uniformizar registry, prefixos, reachability e roteiro de execução. Escopo: 113 menus e famílias de smoke/regression; relatório de casos. Fora: converter todos os menus em testes Unity. Ordem: catalogar menu→pré-requisito→modo; adotar registry onde há suite; marcar legado/orphan. Aceite: cada entrada tem setup, owner, PASS/FAIL e recuperação conhecidos. Risco: trabalho editorial amplo, sem mudança de pacote. Commit: `docs(qa): catalog operational test entry points`.

## 10. Ordem de teste após hardening

1. Compilação estática/asmdef e `git diff --check`.
2. Guardas de setup em Edit Mode: Clean, Prepared, marker incompatível, DomainReloaded e Restored.
3. Smoke guards Play Mode isolados para adapter/observer/grammar e fixture contract.
4. Negativos: request pendente + falha de assertion + unwind; callback ausente; Update*; root persistido em source scene descarregada.
5. Integrações reais de host: QA-01, QA-02, depois QA-03 WaitVisible/WaitCovered; route/player/pause/camera por família.
6. Verificação de restauração: gates, presentation, autoridade route/activity, roots temporários, assets runtime e setup session.

## 11. Decisão QA-03

**STOP — do not rerun.**

O runtime já demonstrou que a limpeza funciona, mas o critério ativo de QA-03 ainda exige uma coincidência temporal que o pacote não promete. Reruns só alternariam a ordem das continuidades e não aumentariam a confiança. Executar primeiro os Cortes 1 e 2; então QA-03 poderá rodar com evidência causal, unwind obrigatório e diagnóstico preservado.

## Validação desta auditoria

Foram feitas somente leituras estáticas e inventário de arquivos/padrões. Não foi executado Unity, build, teste, Play Mode ou batchmode. A validação final exigida é: `git diff --check`, `git status --short`, `git diff --stat` e `git diff --name-status`, devendo apontar exclusivamente este documento.
