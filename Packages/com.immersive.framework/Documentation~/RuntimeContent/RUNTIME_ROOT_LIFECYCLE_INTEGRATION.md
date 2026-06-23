# Runtime root lifecycle integration

Status: `F8F APPLIED`

F8F connects the F8 runtime content owner to the existing Session, Route and Activity lifecycles.

This is still logical lifecycle integration only. It does not create hierarchy roots, instantiate prefabs, destroy objects, bind Content Anchors or execute runtime release.

## Runtime shape after F8F

```text
FrameworkRuntimeHost
  -> RuntimeContentRuntime
      -> RuntimeRootRegistry
          -> RuntimeScopeRoot(Session owner)
          -> RuntimeScopeRoot(Route owner)
          -> RuntimeScopeRoot(Activity owner)
```

## What is integrated

| Lifecycle | Integration |
|---|---|
| Session | `FrameworkRuntimeHost` creates the Session runtime scope root when the application runtime is initialized. |
| Route enter | `RouteLifecycleRuntime` creates the Route runtime scope root after route scene composition succeeds and before route content enter/startup activity. |
| Route exit | `RouteLifecycleRuntime` removes the previous Route runtime scope root after the startup activity transition has completed for the new route. |
| Activity enter | `ActivityFlowRuntime` creates the Activity runtime scope root before Activity local lifecycle events are published. |
| Activity exit/clear | `ActivityFlowRuntime` removes the previous Activity runtime scope root after Activity local lifecycle events are published. |

## Diagnostics added

F8F adds `RuntimeScopeLifecycleResult` as an internal diagnostic snapshot for lifecycle-driven root/context operations.

Route diagnostics now expose:

```text
runtimeRouteScope
runtimeRouteRootEnter
runtimeRouteRootExit
runtimeRouteContext
runtimeRootCount
```

Activity diagnostics now expose:

```text
runtimeActivityScope
runtimeActivityRootEnter
runtimeActivityRootExit
runtimeActivityContext
runtimeRootCount
```

## Registry semantics added in F8F

`RuntimeRootRegistry` now supports logical root removal.

A root can be removed only when it has no registered handles. If handles exist, removal is rejected with a diagnostic result. F8F does not release those handles because runtime release execution belongs to a later F8 cut.

## Non-goals

F8F does not add:

- `RuntimeMaterializationRequest`;
- `RuntimeMaterializationResult`;
- transition guard or scoped cancellation;
- prefab materializer;
- physical release execution;
- hierarchy root GameObjects;
- `GameObject.Find`;
- `Instantiate`;
- `Destroy`;
- Content Anchor binding;
- Activity anchors;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

## Next cut

```text
F8I — PrefabContentMaterializer
```
