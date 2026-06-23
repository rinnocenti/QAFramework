# RuntimeContentRuntime and RuntimeScopeContext

Status: `F8F UPDATED`

F8E introduced the internal runtime owner for runtime-created content state and the explicit scope context used by future materialization cuts. F8F connects that owner to Session, Route and Activity lifecycle root/context creation.

This cut is still not materialization. It creates the coordination boundary that sits above the F8D registry, but it does not create hierarchy GameObjects, instantiate prefabs, destroy objects, execute release, bind Content Anchors or serve gameplay consumers.

## Added runtime types

| Type | Role |
|---|---|
| `RuntimeContentRuntime` | Internal owner for runtime-created content state in the current framework runtime. It owns a `RuntimeRootRegistry` instance and coordinates explicit root/context/handle operations. |
| `RuntimeScopeContext` | Public experimental passive context for one `RuntimeContentOwner`. It carries owner/source/reason and can create identities for that owner. |

## RuntimeContentRuntime semantics

`RuntimeContentRuntime` is an internal owner, not a global provider.

It supports:

- explicit scope root creation;
- scope context creation only when the root already exists;
- passive handle declaration and registration;
- handle lookup through an explicit `RuntimeScopeContext`;
- handle snapshots through an explicit `RuntimeScopeContext`;
- handle unregistering through an explicit `RuntimeScopeContext`.

It deliberately does not auto-create a missing root when a context or handle is requested.

## RuntimeScopeContext semantics

`RuntimeScopeContext` represents:

```text
RuntimeContentOwner + source + reason
```

It is valid only when its owner is valid. It does not store a `RuntimeScopeRoot` reference and does not expose the registry.

The context exists so future materialization requests can be scoped explicitly without reaching for a static provider or a scene lookup.

## Operation boundary

F8E establishes this internal shape:

```text
RuntimeContentRuntime
  -> RuntimeRootRegistry
      -> RuntimeScopeRoot(owner)
          -> RuntimeContentHandle
```

The only way to operate on handles through `RuntimeContentRuntime` is by passing an explicit `RuntimeScopeContext`.

## Explicit non-goals

F8E does not add:

- `RuntimeMaterializationRequest`;
- `RuntimeMaterializationResult`;
- prefab materializer;
- runtime root GameObjects;
- `Transform` parent assignment;
- `GameObject.Find`;
- `Instantiate`;
- `Destroy`;
- release execution;
- Content Anchor binding;
- Activity Content Anchor;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8I — PrefabContentMaterializer
```
