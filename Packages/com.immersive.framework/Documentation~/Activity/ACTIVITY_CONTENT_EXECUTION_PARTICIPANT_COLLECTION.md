# Activity Content Execution Participant Collection

Status: APPLIED / PARTICIPANT COLLECTION + ORDERING MODEL ONLY  
Fase: F10E  
Escopo: Framework Core

---

## Contexto

F10D adicionou o contrato passivo de participant. F10E adiciona a colecao passiva e ordenavel desses participants, ainda sem discovery, sem executor runtime e sem integracao no lifecycle.

## Contratos adicionados

```text
ActivityContentExecutionParticipantEntry
ActivityContentExecutionParticipantCollection
ActivityContentExecutionParticipantCollectionIssue
ActivityContentExecutionParticipantCollectionIssueKind
```

## Entry

`ActivityContentExecutionParticipantEntry` pareia:

```text
IActivityContentExecutionParticipant
ActivityContentExecutionParticipantDescriptor
sourceIndex
```

Ela expõe `RuntimeContentId`, `requiredness`, `supportsEnter`, `supportsExit` e `order` para diagnostics e ordering futuro.

## Collection

`ActivityContentExecutionParticipantCollection` recebe uma lista de participants e produz uma snapshot imutavel:

```text
entries ordenadas por order + sourceIndex
issues diagnosticas
contagens required/optional
contagens enter/exit
lookup por RuntimeContentId
snapshots por phase Enter/Exit
```

A collection remove participants invalidos ou duplicados da lista aceita e registra issues diagnosticas. Ela nao executa participant, nao descobre componentes, nao cria requests, nao agrega resultados e nao toca lifecycle.

## Issues

Issues possiveis:

```text
NullParticipant
InvalidDescriptor
DuplicateContentId
```

Duplicates usam `RuntimeContentId.StableText` como chave canonica. O primeiro participant valido e preservado; duplicados sao rejeitados na collection.

## Fronteiras

F10E nao adiciona:

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

Nao ha Play Mode behavior novo em F10E.
