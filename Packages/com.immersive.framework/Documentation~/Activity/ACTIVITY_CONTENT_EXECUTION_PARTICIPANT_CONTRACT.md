# Activity Content Execution Participant Contract

Status: CLOSED / COVERED BY F10 ACTIVITY CONTENT EXECUTION CORE PASS  
Fase: F10D-F10M  
Escopo: Framework Core

---

## Contexto

F10B/F10C definiram request/result e aggregate result para Activity Content Execution. F10D adiciona a fronteira passiva para um participante de execucao. F10E adiciona a colecao passiva/ordenavel desses participants. F10F adiciona request factory e phase plan. F10G-F10L adicionam executor, lifecycle integration, source boundary e smokes diagnosticos.

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

F10D-F10G nao adicionam:

```text
participant discovery
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

Nao ha Play Mode behavior novo integrado em F10D-F10G.


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

Ela nao descobre nada. O objetivo e alimentar phase plans e o executor F10G com ordering deterministico e diagnostico de entradas invalidas.


---

## F10K participant source boundary

F10K adiciona `IActivityContentExecutionParticipantSource` como fronteira explicita para fornecer participants conhecidos ao lifecycle. O source padrao permanece vazio e nao faz discovery fisico, busca global, placement ou gameplay mutation.
