# IF-READY-04 QA-03 — Direct Activity Readiness Policies

## Objetivo

Validar diretamente as políticas `WaitVisible` e `WaitCovered` em uma Activity runtime temporária, usando somente as superfícies de apresentação já pertencentes ao `FrameworkRuntimeHost` canônico do QA Hub.

## Preparação

No Edit Mode, execute:

`Immersive Framework/QA/Setup/Activity Entry Readiness/Prepare Direct Readiness Policies Regression`

O setup seleciona o `Game Application` canônico, exige `FrameworkStartup`, confirma a cena `QA_Hub` e exige que `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/ActivityAdditionalContent.unity` já esteja habilitada em Build Settings. Ele não altera Build Settings.

## Execução

Em uma nova sessão de Play Mode, execute:

`Immersive Framework/QA/Regressions/Game Flow/Run Direct Activity Readiness Policies Regression`

A regressão cria uma `ActivityAsset` e um `ActivityContentProfileAsset` apenas em memória. O perfil declara uma única cena requerida, aditiva e com `ReleaseOnActivityChange`: `Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity`.

`ActivityAdditionalContent.unity` não é usado: ele pertence a outro contrato de visibilidade de Activity. QA-03 possui uma cena neutra, com uma única raiz `QA_IF_READY_04_DirectPoliciesContent` e somente `Transform`. Ela deve estar descarregada antes de cada política, ser carregada pela composição real da Activity, permanecer carregada enquanto a Activity alvo é autoridade e ser descarregada pela limpeza em estágios da fixture.

`QA_UIGlobal` é a cena-fonte de authoring do Persistent Content. O package a carrega, retém as hierarquias raiz completas com `DontDestroyOnLoad` e descarrega a cena-fonte; as raízes retidas não se tornam filhas do `FrameworkRuntimeHost`. Por isso `HostOwned` significa que os adaptadores foram coletados e são operados pelo runtime escopado do host, não que pertencem à sua hierarquia de `Transform`.

A regressão resolve Transition e Loading exclusivamente pelas raízes da cena runtime persistente obtida de `host.gameObject.scene`. Não usa busca global, enumeração de todas as cenas ou suposição de parenthood. A configuração authoring ainda exige `QA_UIGlobal` como Container Scene e confirma que a cena-fonte não permaneceu carregada após o boot.

## Evidência esperada

Para `WaitVisible`, a apresentação é revelada antes de `CompletePreparation`, mas a requisição e o gate permanecem ativos até `Ready`. Para `WaitCovered`, Transition e Loading permanecem ativos até `CompletePreparation`; a revelação ocorre somente depois.

O observador passivo de Transition emite um evento imutável por evidência registrada. A evidência de Loading deve conter, em cada política, a sequência `RequestReceived`, aplicação visual e `ResultRecorded` para Show e Hide.

O gate validado é exclusivamente `FrameworkRuntimeHost.TransitionGateSnapshot`; Pause e o snapshot combinado não participam desta prova. Enquanto a readiness está pendente, a regressão exige blockers de `GameFlow/LifecycleRequest`, `Input/InputAcceptance`, `Interaction/InteractionAcceptance` e `Gameplay/GameplayAction`. Depois da conclusão, o snapshot de Transition deve estar vazio.

Cada política exige exatamente seis entradas de Loading, nesta ordem: `RequestReceived/Show`, `VisibleApplied/Show`, `ResultRecorded/Show`, `RequestReceived/Hide`, `HiddenApplied/Hide` e `ResultRecorded/Hide`. As sequências são crescentes, as visibilidades solicitada/aplicada são verificadas e os resultados finais de Show/Hide devem ser bem-sucedidos.

Para `WaitVisible`, a evidência `Hidden` ocorre antes do checkpoint `wait-visible-before-readiness-complete`. Para `WaitCovered`, nenhuma evidência `Hidden` pode ocorrer antes de `wait-covered-before-readiness-complete`; a primeira deve ocorrer depois desse checkpoint. As evidências imutáveis por política são capturadas antes de reset/destruição e alimentam o log final.

`WaitVisible` usa somente o evento passivo `StateChanged/Hidden` de Transition como sinal de revelação: ele exige que o participante ainda esteja `Preparing` e que já exista um `Visible` posterior ao checkpoint da política. A ordem oficial do framework já conclui Loading Hide antes do Transition After; por isso Loading e sua evidência exata são validados imediatamente depois do sinal de Transition, não como parte do predicado do evento. A combinação anterior dos dois subsistemas no mesmo predicado podia perder o único evento Hidden e causar espera indefinida.

O evento passivo Hidden e a projeção cacheada `IsVisible` são camadas diferentes. A entrada tipada `StateChanged/Hidden`, seu alpha e o alpha atual são a fronteira causal pré-readiness; nenhuma política usa frame arbitrário, `IsVisible` ou `LastStatus` nesse ponto. `IsVisible`, `LastStatus == Succeeded` e a projeção final de Loading só são validados depois que o request público termina. A composição real pode emitir zero ou mais triplets de Loading `Update`; a gramática é `Show + Update* + Hide`, onde seis é apenas a evidência fixa do ciclo Show/Hide.

QA-03 usa `QaOwnedAsyncOperation<FrameworkActivityRequestResult>` desde o início da requisição até o terminal. Se uma asserção falha depois do início, o runner completa readiness somente pelo callback de unwind explícito para encerrar a própria requisição antes de descartar a fixture. Execution, unwind, cleanup e authority verification permanecem separados; unwind não completa casos de sucesso e `GameFlowRuntimeDisposed` não é um mecanismo de conclusão válido.

O runner emite logs compactos `status='Running'` para as fases intencionais `Preparing`, `RevealObservedBeforeReady` e `ReadinessCompleted`. Interromper Play Mode durante a espera de readiness produz cancelamento `GameFlowRuntimeDisposed`; não é resultado válido de QA.

O contrato de PASS é exatamente:

`[IF_READY_04_QA_DIRECT_POLICIES] status='Passed' cases='42' waitVisible='Passed' waitCovered='Passed' presentationSource='HostOwned'`

O log também inclui diagnósticos imutáveis de Loading, Transition, amostras de estado e máximo de blockers para `waitVisible` e `waitCovered`. Falhas de limpeza dessas políticas são reportadas separadamente como `waitVisibleCleanup` e `waitCoveredCleanup`.

A limpeza sempre tenta destruir o observer, ocultar as superfícies do host, descarregar o conteúdo temporário, resetar evidências e restaurar autoridade. A liberação do gate só é exigida quando uma requisição de política foi efetivamente iniciada por QA-03; blockers anteriores de startup não são atribuídos a este teste. Os casos compartilhados de limpeza só são registrados quando são o próximo caso esperado, preservando a falha primária sem mascará-la por erro de ordem do registry.

A preparação QA-02 é um fluxo manual separado: após restaurar o QA Hub, execute também `Prepare Presentation Evidence Regression` antes de executar sua regressão.

## Limpeza e limites

## HARDENING-01 — causalidade de evidência

O evento passivo Hidden e a projeção cacheada IsVisible são camadas diferentes. A entrada tipada StateChanged/Hidden, seu alpha e o alpha atual são a fronteira causal pré-readiness; nenhuma política usa frame arbitrário, IsVisible ou LastStatus nesse ponto. IsVisible, LastStatus Succeeded e a projeção final de Loading só são validados depois que o request público termina. Loading usa a gramática Show + Update* + Hide.

QA-03 usa QaOwnedAsyncOperation<FrameworkActivityRequestResult> desde o início da requisição até o terminal. Se uma asserção falha, o runner completa readiness somente pelo callback de unwind explícito para encerrar a própria requisição antes de descartar a fixture. Execution, unwind, cleanup e authority verification permanecem separados; unwind não completa casos de sucesso. A fixture só inicia disposal após a operação owned alcançar terminal. QaCaseRegistry preserva os 42 casos fixos, e a foundation regression de 20 casos é o guard obrigatório do contrato compartilhado.

Após cada política, a regressão limpa a Activity temporária, descarrega a cena adicional, destrói o profile em memória e restaura a autoridade inicial. O observer temporário é destruído; os adaptadores pertencentes ao host permanecem ocultos.

O escopo não altera `com.immersive.framework`, packages, cenas, prefabs, settings ou Build Settings.

## Próximo passo

IF-READY-04-QA-04 — Committed Readiness Failure and Recovery Ownership — deve consumir apenas estas evidências determinísticas para cobrir cenários de falha/cancelamento de readiness, sem reintroduzir fixtures sintéticas nem fallback de apresentação.
### FIX1 — observação terminal idempotente

Observar uma operação terminal é idempotente. A limpeza normal pode observar novamente uma operação owned que já terminou; isso não muda a fase, não invoca o callback de completion e preserva resultado, fault ou cancellation originais. Um request terminal bem-sucedido é reportado pelo unwind como concluído, com completion callback não emitido.
### FIX2 — join causal WaitCovered

WaitCovered une Visible e Preparing como sinais monotônicos independentes. As ordens Visible → Preparing e Preparing → Visible são válidas: o evento visual passivo não é descartado porque readiness ainda não propagou. O join conclui uma única vez. A saída manual de Play Mode produz disposal do runtime e não é classificada como conclusão antecipada natural da política.
