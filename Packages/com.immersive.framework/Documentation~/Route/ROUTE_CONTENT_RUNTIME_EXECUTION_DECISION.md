# F3D — RouteContentRuntime execution decision

Status: APPLIED / PENDING COMPILE-SMOKE  
Roadmap: IF-FW-ROAD-3C — RouteContentRuntime execution decision  
Fase: F3

---

## Decisão aplicada

`RouteContentRuntime` passa a ser ativo no baseline da F3.

A integração é limitada a callbacks locais de conteúdo authored de Route em Primary Scene carregada.

## Ordem canônica aplicada

A ordem agora é:

```text
1. Route Content exit da Route anterior antes do carregamento Single da próxima Primary Scene.
2. Load/ativação da Primary Scene da próxima Route.
3. Route Content enter da nova Route.
4. Startup Activity da nova Route.
```

Isso evita que callbacks de saída dependam de objetos que já foram destruídos pelo carregamento `Single`.

## Arquivos principais

```text
Runtime/RouteLifecycle/RouteContentRuntime.cs
Runtime/RouteLifecycle/RouteContentLifecycleDispatchResult.cs
Runtime/RouteLifecycle/RouteLifecycleRuntime.cs
Runtime/RouteLifecycle/RouteLifecycleStartResult.cs
```

## Superfícies reclassificadas

Estas superfícies deixam de ser `Deferred` e passam a ser `Experimental` no baseline da F3:

```text
RouteContentBinding
RouteContentBehaviour
IRouteContentLifecycleReceiver
RouteContentLifecycleContext
RouteContentLifecycleEvents
RouteContentLifecyclePhase
```

`RouteContentRuntime` continua `Internal` porque é owner técnico, não API pública de gameplay.

## O que a integração registra

O resultado de lifecycle local registra:

```text
phase
route
status
binding count
receiver count
receiver failure count
source
reason
```

Os logs de Route Lifecycle passam a expor:

```text
routeContentExit='Executed'
routeContentEnter='Executed'
```

com contadores de bindings e receivers.

## O que não entra

F3D não implementa:

```text
additive scene loading
RouteContentSet ownership final
release policy
Content Anchor
RuntimeMaterialization
LocalContributionSet
consumers
prioridades configuráveis de callback
pipeline novo
```

## Validação esperada

Rodar o smoke padrão:

```text
Boot
Route Smoke
Activity Smoke
Clear Activity Smoke
```

Critérios adicionais:

```text
routeContentEnter='Executed'
routeContentExit='Executed'
```

`routeContentExit='Executed'` deve aparecer nas trocas de Route, não no boot inicial.
