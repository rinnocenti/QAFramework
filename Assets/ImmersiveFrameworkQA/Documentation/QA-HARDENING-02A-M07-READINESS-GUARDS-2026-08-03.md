# QA-HARDENING-02A — M07 Readiness Guards

Data: 2026-08-03  
Baseline QAFramework: `bbbe8753b73afc5f808844de70454faefd62ac48`  
Framework somente leitura: `f5620efa8ddd1046e6ecb7f3194a2ee562db6dd5`

## Relação com o M07 do FIRSTGAME

Este corte não implementa o M07 e não altera o FIRSTGAME. Ele fortalece os dois guards de Activity Entry Readiness que são mais próximos do fluxo de provisionamento do Player:

- QA-01 prova autoridade, readiness e cleanup de uma Activity `ObserveOnly`;
- QA-02 prova apresentação Transition/Loading sintética e preservação de autoridade;
- QA-03 permanece como guard real de `WaitVisible` e `WaitCovered`, já aprovado no HARDENING-01.

A migração ampla das famílias Player, Pause, Camera, Route, Reset e Restart não é pré-requisito do M07 e permanece fora deste pacote.

## Objetivo

Aplicar a fundação causal validada no HARDENING-01 aos guards QA-01 e QA-02 sem alterar seus contratos públicos:

- QA-01 continua com 18 casos;
- QA-02 continua com 26 casos e `fixtureMode='RuntimeSynthetic'`;
- QA-03 permanece inalterado com 42 casos;
- nenhum arquivo do package, FIRSTGAME, cena, prefab, asset ou setting é alterado.

## QA-01 — mudanças

Arquivo:

`Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/QaActivityEntryReadinessFoundationRegression.cs`

Mudanças:

- substituição da lista local por `QaCaseRegistry`;
- substituição da agregação ad hoc por `QaFailureCollector`;
- ownership explícito de `RequestActivityAsync` por `QaOwnedAsyncOperation<FrameworkActivityRequestResult>`;
- reobservação terminal idempotente durante cleanup;
- unwind explícito pela API pública `CompletePreparation()` somente quando o request ainda está pendente e o participant está `Preparing`;
- bloqueio de cleanup caso o request owned não alcance terminal;
- remoção do `NextFrameAsync` usado para esperar o evento `Ready`;
- captura do `ReadinessReady.Task` antes de `CompletePreparation()` e await direto do sinal causal;
- preservação dos 18 nomes e da ordem dos casos.

Nota: o request `ObserveOnly` termina antes da readiness. A conclusão posterior de readiness não tenta alterar a fase de uma operação já terminal.

## QA-02 — mudanças

Arquivo:

`Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/QaActivityEntryPresentationEvidenceRegression.cs`

Mudanças:

- substituição da lista local por `QaCaseRegistry`;
- substituição da agregação ad hoc por `QaFailureCollector`;
- remoção dos dois frames usados depois de `Transition.ExecuteAsync`;
- uso do resultado de `ExecuteAsync` como boundary autoritativo da operação;
- uso de `QaEvidenceCheckpoint` para ordenar baseline, pre-exercise, post-show e post-hide;
- validação do prefixo Show e da gramática completa `Show + Update* + Hide` por `QaLoadingPresentationEvidenceGrammar`;
- manutenção de um único `NextFrameAsync`, exclusivamente após `Object.Destroy`, para propagação Unity;
- preservação dos 26 nomes e da ordem dos casos.

`UnityFadeCurtainEffectAdapter.ExecuteAsync` retorna `Awaitable<TransitionEffectResult>`, não `Task<TransitionEffectResult>`. Por isso QA-02 mantém ownership lexical por `await` direto e não tenta adaptar o retorno para `QaOwnedAsyncOperation<T>`.

## Fora de escopo

- alterações no `com.immersive.framework`;
- alterações no `planet-devourer`;
- migração das famílias Player, Pause, Camera, Route, Reset ou Restart;
- `QaSetupSession`;
- fixture scoping por scene/root/asset;
- mudanças de Build Settings;
- cenas, prefabs, ScriptableObjects e ProjectSettings;
- alteração da foundation compartilhada já aprovada.

## Validação estática realizada no pacote

- QA-01: 18 casos únicos;
- QA-02: 26 casos únicos;
- chaves e parênteses balanceados nos dois arquivos;
- QA-01: zero `NextFrameAsync`;
- QA-02: um `NextFrameAsync`, documentado como propagação de `Destroy`;
- nenhuma ocorrência nova de timeout, polling, reflection, log parsing ou busca global;
- nenhum asmdef novo;
- nenhum arquivo removido.

## Validação Unity requerida após aplicação

1. Import/compile: zero erros.
2. `QA_CAUSAL_ASYNC_FOUNDATION`: 20 casos.
3. QA-01: 18 casos.
4. QA-02: 26 casos, `fixtureMode='RuntimeSynthetic'`.
5. QA-03: 42 casos, `WaitVisible` e `WaitCovered` Passed.
6. Confirmar `RestoredAfterPlay` após QA-02 e QA-03.

## Commit message sugerida

`test(qa): harden M07 readiness guards`
