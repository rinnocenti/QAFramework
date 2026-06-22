# ActivityReadinessState mínimo

Status: CLOSED / COMPILE-SMOKE PASS  
Fase: F4  
Corte: F4D  
Roadmap: IF-FW-ROAD-4D — ActivityReadinessState mínimo

---

## Contexto

F4A criou `ActivityRuntimeState`, F4B criou `ActivityContentSet` e F4C criou `ActivityContentLifecycleResult`.

Ainda faltava uma fronteira explícita dizendo se a Activity atual está pronta para seguir o fluxo de jogo.

## Decisão

F4D adiciona `ActivityReadinessState` mínimo.

Readiness inicial:

```text
Ready = Activity ativa + Activity Content baseline aplicado + sem falha de lifecycle local + sem referência de Activity ausente em binding carregado.
```

Quando não há Activity ativa, o estado é:

```text
activityReadiness='None'
```

## Arquivos principais

```text
Runtime/ActivityFlow/ActivityReadinessStatus.cs
Runtime/ActivityFlow/ActivityReadinessState.cs
Runtime/ActivityFlow/ActivityFlowStartResult.cs
```

## Diagnóstico esperado

Activity ativa pronta:

```text
activityReadiness='Ready' activityReadinessReason='BaselineReady' activityReadinessIssues='0'
```

Activity limpa:

```text
activityReadiness='None' activityReadinessReason='NoActiveActivity' activityReadinessIssues='0'
```

Se houver problema bloqueante:

```text
activityReadiness='NotReady' activityReadinessReason='...' activityReadinessIssues='N'
```

## Propagação de estado

O readiness fica disponível em:

```text
ActivityFlowStartResult.ActivityReadinessState
RouteRuntimeState.ActivityReadinessState
SessionRuntimeState.ActivityReadinessState
FrameworkRuntimeState.ActivityReadinessState
```

## Fora do escopo

F4D não implementa:

```text
readiness de actors
readiness de input
readiness de camera
readiness visual
gates assíncronos
profile loading
release/unload
snapshot/reset
Content Anchor
RuntimeMaterialization
LocalContributionSet
```

## Validação esperada

Rodar:

```text
Run Standard Smoke
Run Route Callback Smoke
Validate Loaded Route Content
```

Critérios novos:

```text
activityReadiness='Ready'
activityReadinessReason='BaselineReady'
activityReadinessIssues='0'
activityReadiness='None'
activityReadinessReason='NoActiveActivity'
```


---

## Fechamento

F4D foi fechado após smoke com:

```text
activityReadiness='Ready'
activityReadinessReason='BaselineReady'
activityReadinessIssues='0'
activityReadiness='None'
activityReadinessReason='NoActiveActivity'
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content'
```
