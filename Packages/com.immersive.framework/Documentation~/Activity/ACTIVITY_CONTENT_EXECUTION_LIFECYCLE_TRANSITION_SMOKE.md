# Activity Content Execution Lifecycle Transition Smoke

Status: PASS / COVERED BY F10 ACTIVITY CONTENT EXECUTION CORE PASS  
Fase: F10J  
Escopo: Framework Core diagnostics

---

## Contexto

F10I conectou `ActivityContentExecutionLifecycleResult` ao `ActivityFlowRuntime` com uma collection vazia por padrao.

F10J adiciona um smoke diagnostico para validar a transicao real de Activity no lifecycle:

```text
active Activity
  -> clear Activity
  -> Activity Content Execution exit diagnostics
  -> restore Activity
  -> Activity Content Execution enter diagnostics
```

O objetivo e confirmar que o `Exit` real aparece como `SkippedNoContent` quando nao existem participants descobertos.

---

## Smoke adicionado

```text
Run Activity Content Execution Lifecycle Transition Smoke
```

Fluxo validado:

```text
Primary Activity ativa
  -> Clear Activity
      -> activityContentExecution = SucceededNoContent
      -> exit = SkippedNoContent
      -> exitRequests = 0
      -> blocksReadiness = False
  -> Restore Primary Activity
      -> activityContentExecution = SucceededNoContent
      -> enter = SkippedNoContent
      -> enterRequests = 0
      -> blocksReadiness = False
```

---

## Critérios de sucesso

O smoke espera:

```text
clearExecution = SucceededNoContent
clearExit = SkippedNoContent
clearExitRequests = 0
clearExitResults = 0
clearBlocksReadiness = False
restoreExecution = SucceededNoContent
restoreEnter = SkippedNoContent
restoreEnterRequests = 0
restoreEnterResults = 0
restoreBlocksReadiness = False
```

---

## Não responsabilidade

F10J nao adiciona:

```text
participant discovery
authoring de participants
readiness aggregation integrada ao lifecycle
adapters Unity
Transform placement
GameObject hierarchy root
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
```

O smoke valida apenas o encaixe diagnostico de enter/exit no lifecycle com zero participants.
