# F4C — ActivityContentLifecycleResult

Status: **APPLIED / PENDING COMPILE-SMOKE**  
Roadmap item: `IF-FW-ROAD-4C — ActivityContentLifecycleResult`

## Objetivo

F4C torna explícito o resultado dos callbacks locais de Activity Content.

Antes do corte, `ActivityContentRuntime` já chamava `IActivityContentLifecycleReceiver`, mas esse despacho era efeito colateral interno. Depois do corte, cada aplicação de Activity Content carrega um resultado agregado de lifecycle.

## Implementação

Novo arquivo:

```text
Runtime/ActivityFlow/ActivityContentLifecycleResult.cs
```

Atualizados:

```text
Runtime/ActivityFlow/ActivityContentRuntime.cs
Runtime/ActivityFlow/ActivityContentApplyResult.cs
Runtime/ActivityFlow/ActivityFlowStartResult.cs
Runtime/RouteLifecycle/RouteRuntimeState.cs
Runtime/SessionLifecycle/SessionRuntimeState.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeState.cs
```

## Semântica

`ActivityContentLifecycleResult` registra:

```text
previousActivity
activeActivity
activityContentLifecycle status
enter binding/receiver/failure counts
exit binding/receiver/failure counts
source
reason
```

O resultado é agregado por transição de Activity. Ele não é um plano de release e não executa carregamento.

## Diagnostics

Quando callbacks locais são avaliados, a mensagem de Activity Content inclui:

```text
activityContentLifecycle='Executed'
activityContentEnterBindings='N'
activityContentEnterReceivers='N'
activityContentEnterFailed='0'
activityContentExitBindings='N'
activityContentExitReceivers='N'
activityContentExitFailed='0'
```

Em uma Activity sem binding local próprio, ou em um clear sem binding local de saída, o lifecycle pode ficar `Skipped` e não precisa aparecer na mensagem principal.

## Propagação

O resultado fica acessível como state/result data em:

```text
ActivityContentApplyResult.LifecycleResult
ActivityFlowStartResult.ActivityContentLifecycleResult
RouteRuntimeState.ActivityContentLifecycleResult
SessionRuntimeState.ActivityContentLifecycleResult
FrameworkRuntimeState.ActivityContentLifecycleResult
```

## Não entra

F4C não implementa:

```text
ActivityContentProfile loading
ActivityReadinessState
ActivityExitPlan real
release/unload
actors
input
camera
reset
snapshot
Content Anchor
RuntimeMaterialization
additive scene loading
LocalContributionSet
```

## Critério de validação

```text
Unity compila sem erro CS.
Boot succeeded.
QA Smoke completed. name='Standard Smoke'.
QA Smoke completed. name='Route Callback Smoke'.
QA Authoring Validation completed. scope='Loaded Route Content'.
Ao ativar Activity com binding local, log inclui activityContentLifecycle='Executed'.
activityContentEnterBindings='1'.
activityContentEnterFailed='0'.
activityContentExitFailed='0'.
```

## Próximo corte

```text
F4D — IF-FW-ROAD-4D — ActivityReadinessState mínimo (applied/pending smoke)
```
