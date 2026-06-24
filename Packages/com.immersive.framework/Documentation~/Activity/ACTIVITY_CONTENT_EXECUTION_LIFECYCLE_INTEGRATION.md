# Activity Content Execution Lifecycle Integration

Status: APPLIED / LIFECYCLE DIAGNOSTIC INTEGRATION / PENDING SMOKE  
Fase: F10I  
Escopo: Framework Core

---

## Contexto

F10B-F10H criaram contratos, aggregate result, participant contract, participant collection, phase plan/request factory, runtime executor e smoke sintetico.

F10I conecta essa cadeia ao `ActivityFlowRuntime` de forma diagnostica e vazia por padrao. O objetivo e validar o ponto de encaixe no lifecycle sem descobrir participants automaticamente e sem executar gameplay, adapter Unity ou materializacao fisica.

---

## Tipos adicionados

```text
ActivityContentExecutionLifecycleStatus
ActivityContentExecutionLifecycleResult
```

Fluxo diagnostico:

```text
ActivityFlowRuntime
  -> ResolveActivityContentExecutionParticipants(...)
      -> empty collection em F10I
  -> ActivityContentExecutionRequestFactory
      -> enter/exit phase plans
  -> ActivityContentExecutionRuntime
      -> aggregate enter/exit results
  -> ActivityContentExecutionLifecycleResult
```

---

## Integração no ActivityFlow

F10I adiciona campos de diagnostics em `ActivityFlowStartResult`:

```text
ActivityContentExecutionResult
```

E logs com:

```text
activityContentExecution
activityContentExecutionParticipants
activityContentExecutionEnter
activityContentExecutionEnterRequests
activityContentExecutionExit
activityContentExecutionExitRequests
activityContentExecutionBlockingIssues
activityContentExecutionBlocksReadiness
```

---

## Comportamento esperado em F10I

Como discovery de participants ainda nao existe, a collection padrao e vazia.

Assim, o resultado esperado em enter/exit e:

```text
activityContentExecution='SucceededNoContent'
activityContentExecutionParticipants='0'
activityContentExecutionEnter='SkippedNoContent'
activityContentExecutionExit='SkippedNoContent'
activityContentExecutionBlocksReadiness='False'
```

Dependendo da operacao, apenas enter ou apenas exit pode aparecer. Quando nao ha Activity anterior nem proxima, o lifecycle result fica `None`.

---

## Ordem preservada

F10I nao substitui o fluxo legado de `ActivityContentRuntime` / `ActivityLocalVisibilityAdapter`.

A integração nova fica diagnostica e separada:

```text
Activity scope root/context
Activity Content Anchor discovery
Activity Content Execution diagnostic lifecycle
Content Anchor binding cleanup
Activity runtime root removal
```

---

## Não responsabilidade

F10I nao adiciona:

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

---

## Smoke esperado

F10I deve passar por compile/import smoke e regressao dos smokes existentes.

O sinal novo deve aparecer nos logs de boot, route request e activity request como diagnostics de Activity Content Execution com zero participants.
