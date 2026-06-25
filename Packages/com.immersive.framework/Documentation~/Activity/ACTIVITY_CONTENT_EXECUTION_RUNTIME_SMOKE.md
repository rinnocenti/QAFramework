# Activity Content Execution Runtime Smoke

Status: PASS / COVERED BY F10 ACTIVITY CONTENT EXECUTION CORE PASS  
Fase: F10H  
Escopo: Framework Core diagnostics

---

## Contexto

F10G adicionou `ActivityContentExecutionRuntime`, mas ainda sem discovery automatico e sem integracao no `ActivityFlowRuntime`.

F10H adiciona um smoke diagnostico para validar o executor usando participants sinteticos controlados pelo QA Canvas.

---

## Smoke adicionado

```text
Run Activity Content Execution Runtime Smoke
```

Fluxo validado:

```text
Activity ativa
  -> RuntimeScopeContext Activity-scoped disponivel
  -> synthetic participants
  -> ActivityContentExecutionParticipantCollection
  -> Enter phase plan
  -> ActivityContentExecutionRuntime.ExecutePhasePlan
  -> Exit phase plan
  -> ActivityContentExecutionRuntime.ExecutePhasePlan
  -> ActivityContentExecutionAggregateResult
```

---

## Critérios de sucesso

O smoke espera:

```text
participants = 2
collectionIssues = 0
enterPlan = Planned
enterRequests = 2
enterStatus = Succeeded
enterResults = 2
enterRequired = 1
enterOptional = 1
enterBlockingIssues = 0
enterBlocksReadiness = False
exitPlan = Planned
exitRequests = 1
exitStatus = Succeeded
exitResults = 1
exitRequired = 1
exitOptional = 0
exitBlockingIssues = 0
exitBlocksReadiness = False
```

---

## Não responsabilidade

F10H não adiciona:

```text
participant discovery
ActivityFlow integration
readiness aggregation integrada ao lifecycle
Transform placement
GameObject hierarchy root
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
```

O smoke usa participants sinteticos locais ao QA Canvas. Eles validam o contrato e o executor, mas nao representam gameplay consumer e nao executam comportamento Unity concreto.
