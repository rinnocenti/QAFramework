# Activity Content Execution Participant Contract

Status: APPLIED / PARTICIPANT CONTRACT + COLLECTION MODEL  
Fase: F10D-F10F  
Escopo: Framework Core

---

## Contexto

F10B/F10C definiram request/result e aggregate result para Activity Content Execution. F10D adiciona a fronteira passiva para um participante de execucao. F10E adiciona a colecao passiva/ordenavel desses participants. F10F adiciona request factory e phase plan, ainda sem discovery, sem executor runtime e sem integracao no lifecycle.

## Contratos adicionados

```text
ActivityContentExecutionParticipantDescriptor
IActivityContentExecutionParticipant
ActivityContentExecutionParticipantEntry
ActivityContentExecutionParticipantCollection
ActivityContentExecutionParticipantCollectionIssue
ActivityContentExecutionParticipantCollectionIssueKind
```

## Descriptor

`ActivityContentExecutionParticipantDescriptor` descreve um participante sem executar nada:

```text
RuntimeContentId
requiredness: Required | Optional
supportsEnter
supportsExit
order
displayName
source/reason
```

O descriptor existe para preparar ordering, request creation e diagnostics futuros. Ele nao descobre componentes, nao cria RuntimeContentHandle, nao faz binding, nao materializa e nao muta objetos Unity.

## Interface

`IActivityContentExecutionParticipant` define:

```text
GetActivityContentExecutionDescriptor()
ExecuteActivityContent(ActivityContentExecutionRequest request)
```

A interface retorna `ActivityContentExecutionResult` e preserva a fronteira:

```text
framework core consome request/result/diagnostics
participant/adapters/consumers futuros executam comportamento local
```

## Fronteiras

F10D-F10F nao adicionam:

```text
participant discovery
execution runtime
ActivityFlow integration
readiness aggregation integrada ao lifecycle
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

Nao ha Play Mode behavior novo em F10D-F10F.


## Collection

`ActivityContentExecutionParticipantCollection` recebe participants ja conhecidos pelo caller/futuro discovery e cria uma snapshot ordenada:

```text
order
sourceIndex
RuntimeContentId
requiredness
supportsEnter / supportsExit
issues diagnosticas
```

Ela nao executa nada. O objetivo e preparar o executor futuro com ordering deterministico e diagnostico de entradas invalidas.
