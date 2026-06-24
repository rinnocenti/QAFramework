# Activity Content Execution Contracts

Status: APPLIED / CONTRACTS + AGGREGATE + PARTICIPANT CONTRACT ONLY  
Fase: F10  
Cortes: F10B, F10C, F10D  
Escopo: Framework Core

---

## Contexto

F10 reintroduz entrada/saida de conteudo de Activity como conceito de framework core, sem capturar `Presentation`, gameplay, prefab, scene adapter ou placement fisico.

F10B adiciona contratos passivos para um item de execucao logica de conteudo de Activity. F10C adiciona o resultado agregado passivo para uma fase de execucao. F10D adiciona o contrato passivo de participante, sem discovery ou executor runtime.

## Contratos adicionados

```text
ActivityContentExecutionPhase
ActivityContentExecutionRequiredness
ActivityContentExecutionStatus
ActivityContentExecutionRequest
ActivityContentExecutionResult
ActivityContentExecutionAggregateStatus
ActivityContentExecutionAggregateResult
ActivityContentExecutionParticipantDescriptor
IActivityContentExecutionParticipant
```

## O que o request carrega

`ActivityContentExecutionRequest` descreve:

```text
phase: Enter | Exit
activity
previousActivity
nextActivity
RuntimeScopeContext Activity-scoped
RuntimeContentId
requiredness: Required | Optional
source/reason
```

Ele usa `RuntimeContent` apenas como identidade/contexto logico. Nao cria root, nao materializa, nao faz binding e nao executa gameplay.

## O que o result carrega

`ActivityContentExecutionResult` descreve:

```text
status
blockingIssueCount
nonBlockingIssueCount
message
source/reason
```

E permite diagnosticar:

```text
succeeded
skipped
failed
blocksReadiness
```


## O que o aggregate carrega

`ActivityContentExecutionAggregateResult` descreve um conjunto de resultados de uma mesma fase:

```text
phase: Enter | Exit
activity / previousActivity / nextActivity
status agregado
resultCount
requiredCount / optionalCount
succeededCount / skippedCount / failedCount
blockingIssueCount / nonBlockingIssueCount
blocksReadiness
source/reason/message
```

Ele existe para preparar a futura agregacao de readiness sem executar participants, sem integrar no lifecycle e sem criar side effects Unity.

## O que o participant contract carrega

`ActivityContentExecutionParticipantDescriptor` descreve um participante futuro:

```text
RuntimeContentId
requiredness
supportsEnter / supportsExit
order
displayName
source/reason
```

`IActivityContentExecutionParticipant` define uma fronteira minima:

```text
GetActivityContentExecutionDescriptor()
ExecuteActivityContent(ActivityContentExecutionRequest request) -> ActivityContentExecutionResult
```

Essa fronteira ainda nao descobre participantes, nao ordena execucao, nao agrega readiness no lifecycle e nao executa side effects do framework.

## Fronteiras

Este corte nao adiciona:

```text
execution runtime
participant discovery
execution runtime
ordering runtime
ActivityFlow integration
readiness aggregation integrada ao lifecycle
execution runtime
smoke
Transform placement
GameObject hierarchy root
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
```

## Validacao

Smoke esperado: compile/import.

Nao ha Play Mode behavior novo em F10B/F10C.
