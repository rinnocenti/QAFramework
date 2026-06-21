# F4A — ActivityRuntimeState refinado

Status: **APPLIED / PENDING COMPILE-SMOKE**  
Roadmap item: `IF-FW-ROAD-4A — ActivityRuntimeState refinado`

## Objetivo

F4A introduz um estado tipado mínimo para Activity sem copiar a `ActivityEntryPipeline` antiga e sem antecipar materialização, readiness real, actors, input, camera, reset, snapshot ou release.

O corte estabiliza a fronteira de estado da Activity ativa para que os próximos cortes possam adicionar `ActivityContentSet`, resultado de lifecycle e readiness sem depender apenas de referência direta para `ActivityAsset`.

## Implementação

Novos arquivos:

```text
Runtime/ActivityFlow/ActivityRuntimeStatus.cs
Runtime/ActivityFlow/ActivityRuntimeState.cs
```

`ActivityFlowRuntime` agora mantém:

```text
ActivityRuntimeState _currentActivityState
```

em vez de manter apenas:

```text
ActivityAsset _currentActivity
```

`ActivityRuntimeState` carrega:

```text
status
activity
activityIdentity
previousActivity
source
reason
```

## Semântica

| Status | Uso em F4A | Observação |
|---|---|---|
| `None` | Sem Activity ativa ou Activity limpa | Não implica unload/release. |
| `Active` | Activity ativa atual | Carrega `FrameworkIdentityKey` no domínio `Activity`. |
| `Transitioning` | Reservado | Não é emitido ainda neste corte. |

A identidade diagnóstica segue o domínio tipado do framework:

```text
Activity:<ActivityName>
```

Essa identidade é um baseline interno para F4. Ela não substitui uma política futura de GUID/asset-id se a fase de authoring exigir isso.

## Propagação

O estado passa a aparecer em:

```text
ActivityFlowStartResult.ActivityState
RouteRuntimeState.ActivityState
SessionRuntimeState.ActivityState
FrameworkRuntimeState.ActivityState
```

As propriedades antigas continuam disponíveis como conveniência:

```text
CurrentActivity
CurrentActivityName
HasActiveActivity
```

## Diagnostics

As mensagens de Activity Flow agora incluem estado e identidade quando há Activity ativa:

```text
activityState='Active' activityIdentity='Activity:QA Primary Content Activity'
```

Quando a Activity é limpa ou a Route não tem Startup Activity, o estado esperado é:

```text
activityState='None'
```

## Não entra

F4A não implementa:

```text
ActivityContentSet
ActivityContentProfile loading
ActivityContentLifecycleResult
ActivityReadinessState
actors
input
camera
reset
snapshot
release
Surface
RuntimeMaterialization
additive scene loading
```

## Critério de validação

```text
Unity compila sem erro CS.
Boot succeeded.
QA Smoke completed. name='Standard Smoke'.
QA Smoke completed. name='Route Callback Smoke'.
Activity Flow logs incluem activityState='Active' para Activity ativa.
Clear Activity logs incluem activityState='None'.
```

## Próximo corte

```text
F4B — IF-FW-ROAD-4B — ActivityContentSet mínimo
```
