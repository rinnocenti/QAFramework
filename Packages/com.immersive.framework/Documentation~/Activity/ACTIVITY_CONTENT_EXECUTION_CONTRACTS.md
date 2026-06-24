# Activity Content Execution Contracts

Status: APPLIED / CONTRACTS ONLY  
Fase: F10  
Corte: F10B  
Escopo: Framework Core

---

## Contexto

F10 reintroduz entrada/saida de conteudo de Activity como conceito de framework core, sem capturar `Presentation`, gameplay, prefab, scene adapter ou placement fisico.

Este corte adiciona apenas contratos passivos para descrever uma execucao logica de conteudo de Activity.

## Contratos adicionados

```text
ActivityContentExecutionPhase
ActivityContentExecutionRequiredness
ActivityContentExecutionStatus
ActivityContentExecutionRequest
ActivityContentExecutionResult
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

## Fronteiras

Este corte nao adiciona:

```text
execution runtime
participant discovery
ActivityFlow integration
readiness aggregation
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

Nao ha Play Mode behavior novo neste corte.
