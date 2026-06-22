# F3B — RouteRuntimeState tipado

Status: APPLIED / PENDING COMPILE-SMOKE  
Roadmap: IF-FW-ROAD-3A — RouteRuntimeState tipado  
Fase: F3 — Route baseline

---

## Objetivo

Criar um snapshot tipado para a Route ativa, separado de `RouteAsset` e do resultado textual de startup/switch.

O corte implementa apenas o item do roadmap:

```text
IF-FW-ROAD-3A — RouteRuntimeState tipado
```

---

## Arquivos criados

```text
Runtime/RouteLifecycle/RouteRuntimeState.cs
```

---

## Arquivos alterados

```text
Runtime/RouteLifecycle/RouteLifecycleRuntime.cs
Runtime/RouteLifecycle/RouteLifecycleStartResult.cs
Runtime/SessionLifecycle/SessionRuntimeState.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeState.cs
README.md
Documentation~/README.md
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Documentation~/Planning/Capability-Traceability-Matrix.md
```

---

## O que mudou

`RouteLifecycleRuntime` agora mantém a Route ativa por meio de:

```text
RouteRuntimeState
```

em vez de manter somente campos soltos como `RouteAsset` e `RouteContentSet`.

O snapshot guarda:

```text
Route
RouteIdentity
SceneLifecycleResult
RouteContentSet
ActivityFlowResult
Source
Reason
Entered
```

A identidade da Route é tipada com:

```text
FrameworkIdentityKey
FrameworkIdentityDomain.Route
```

No baseline atual, o valor funcional vem da `PrimaryScenePath` declarada no `RouteAsset`, porque a F3 ainda não criou um `RouteId` authored final.

---

## Observabilidade

`RouteLifecycleStartResult` passa a carregar:

```text
RouteState
```

O log de Route Lifecycle passa a incluir:

```text
routeIdentity='Route:<primary-scene-path>'
```

Isso permite validar no smoke que o caminho de `RouteRuntimeState` foi exercitado.

---

## O que não foi feito

F3B não implementa:

```text
RouteExitResult
RouteContentRuntime active integration
RouteContentOwnership
Route local callbacks
Route validator expansion
additive scene loading
Content Anchor
RuntimeMaterialization
consumers
release policy
```

Esses itens pertencem aos próximos itens do roadmap da F3.

---

## Validação esperada

Rodar o smoke padrão:

```text
Boot
Route Smoke
Activity Smoke
Clear Activity Smoke
```

Critérios:

```text
- Unity compila sem erro CS.
- Boot succeeded aparece.
- Route Smoke completed aparece.
- Activity Smoke completed aparece.
- Clear Activity Smoke completed aparece.
- O log de Route Lifecycle mostra routeIdentity='Route:...'.
- Não há Exception, FATAL, error CS, failed ou Failed.
```

---

## Próximo corte conforme roadmap

```text
F3C — IF-FW-ROAD-3B — RouteExitResult mínimo
```
