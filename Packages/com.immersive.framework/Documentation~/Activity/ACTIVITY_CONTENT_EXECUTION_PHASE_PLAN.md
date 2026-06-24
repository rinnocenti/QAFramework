# Activity Content Execution Phase Plan

Status: APPLIED / REQUEST FACTORY + PHASE PLAN ONLY  
Fase: F10F  
Escopo: Framework Core

---

## Contexto

F10E adicionou a collection passiva e ordenavel de participants. F10F adiciona a etapa seguinte: transformar uma collection ja conhecida em um plano de fase com requests individuais.

F10G adiciona o executor runtime depois deste phase plan. Ainda nao existe discovery ou integracao no lifecycle.

## Contratos adicionados

```text
ActivityContentExecutionPhasePlanStatus
ActivityContentExecutionPhasePlan
ActivityContentExecutionRequestFactory
```

## Request factory

`ActivityContentExecutionRequestFactory` cria planos para fases explicitas:

```text
CreateEnterPlan(...)
CreateExitPlan(...)
CreatePlan(...)
```

A factory usa:

```text
ActivityContentExecutionParticipantCollection
RuntimeScopeContext Activity-scoped
ActivityContentExecutionPhase
ActivityAsset
```

E produz:

```text
ActivityContentExecutionPhasePlan
```

Ela nao descobre participants, nao agrega resultados por conta propria e nao toca lifecycle. F10G executa phase plans ja fornecidos.

## Phase plan

`ActivityContentExecutionPhasePlan` contem:

```text
phase
activity / previousActivity / nextActivity
runtime Activity context
ordered entries for the phase
generated ActivityContentExecutionRequest[]
collection issues copied from the participant collection
status
source / reason / message
```

O plano expoe contagens diagnosticas:

```text
requestCount
entryCount
requiredCount
optionalCount
collectionIssueCount
nullParticipantIssueCount
invalidDescriptorIssueCount
duplicateContentIdIssueCount
```

## Status

Status possiveis:

```text
Planned
SkippedNoParticipants
RejectedInvalidPhase
RejectedMissingActivity
RejectedInvalidContext
RejectedInvalidParticipants
```

## Fronteiras

F10F nao adiciona:

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

Nao ha Play Mode behavior novo em F10F.
