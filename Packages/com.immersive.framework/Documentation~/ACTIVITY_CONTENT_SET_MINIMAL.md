# F4B — ActivityContentSet mínimo

Status: **CLOSED / COMPILE-SMOKE PASS**  
Roadmap item: `IF-FW-ROAD-4B — ActivityContentSet mínimo`

## Objetivo

F4B introduz um `ActivityContentSet` mínimo para registrar conteúdo local conhecido da Activity ativa.

O corte não transforma `ActivityContentBinding` em materialização canônica. Em F4B, o binding continua sendo apenas um **Local Visibility Adapter** para conteúdo scene-authored já carregado.

## Implementação

Novos arquivos:

```text
Runtime/ActivityFlow/ActivityContentEntry.cs
Runtime/ActivityFlow/ActivityContentSet.cs
```

Atualizados:

```text
Runtime/ActivityFlow/ActivityContentRuntime.cs
Runtime/ActivityFlow/ActivityContentApplyResult.cs
Runtime/ActivityFlow/ActivityFlowStartResult.cs
Runtime/RouteLifecycle/RouteRuntimeState.cs
Runtime/SessionLifecycle/SessionRuntimeState.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeState.cs
Runtime/ContentFlow/FrameworkContentHandle.cs
```

## Semântica

`ActivityContentSet` é um snapshot imutável do conteúdo scene-authored local registrado para a Activity ativa.

Em F4B, um entry representa:

```text
ActivityAsset owner
ActivityContentBinding local
FrameworkContentScope.Activity
FrameworkContentKind.SceneAuthored
FrameworkContentRequiredness.Optional
```

Isso significa:

```text
registrado para diagnostics/state
não carregado por profile
não materializado
não owned para release
não descoberto como LocalContributionSet
```

## Diagnostics

`ActivityContentApplyResult` agora carrega:

```text
ActivityContentSet ActivityContentSet
ActivityContentCount
HasActivityContent
```

As mensagens de Activity Content passam a incluir:

```text
activityContentHandles='N'
```

Exemplo:

```text
Activity Content applied 1 binding(s) for Activity 'QA Secondary Content Activity'. activated='1' deactivated='0' unchanged='0' activityContentHandles='1'.
```

Quando a Activity ativa não possui conteúdo local registrado, o valor esperado é:

```text
activityContentHandles='0'
```

Isso é válido para Activities sem conteúdo local próprio ou quando a Activity foi limpa.

## Propagação

O set fica acessível como state data em:

```text
ActivityFlowStartResult.ActivityContentSet
RouteRuntimeState.ActivityContentSet
SessionRuntimeState.ActivityContentSet
FrameworkRuntimeState.ActivityContentSet
```

## Não entra

F4B não implementa:

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
Surface
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
Activity Content logs incluem activityContentHandles='N'.
Ao trocar para uma Activity com binding local, activityContentHandles='1'.
Ao limpar ou usar Activity sem binding local, activityContentHandles='0'.
```

## Próximo corte

```text
F4C — IF-FW-ROAD-4C — ActivityContentLifecycleResult
```
