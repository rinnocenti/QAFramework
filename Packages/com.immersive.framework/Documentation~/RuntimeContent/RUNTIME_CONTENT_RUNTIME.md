# RuntimeContentRuntime and RuntimeScopeContext

Status: `F8J UPDATED / LOGICAL RELEASE`

F8E introduced the internal runtime owner for runtime-created content state and the explicit scope context used by future materialization cuts. F8F connects that owner to Session, Route and Activity lifecycle root/context creation. F8G/F8H add request/result and scoped cancellation. F8I adds the materialization adapter boundary without physical implementation in core. F8J adds release request/result/policy and logical release helpers.

This cut is still not materialization. It creates the coordination boundary that sits above the F8D registry, but it does not create hierarchy GameObjects, instantiate prefabs, destroy objects, unload scenes, return pools, release Addressables handles, bind Content Anchors or serve gameplay consumers.

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

- implementação de adapter físico;
- runtime root GameObjects;
- `Transform` parent assignment;
- `GameObject.Find`;
- `Instantiate`;
- `Destroy`;
- physical release execution;
- Content Anchor binding;
- Activity Content Anchor;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8J — Runtime release policy / logical release execution [APPLIED / PENDING COMPILE-SMOKE]
F8K — Runtime request/guard/release-policy smoke and F8 closure
```

## F8J logical release

`RuntimeContentRuntime` now creates `RuntimeReleaseRequest` and applies logical release by handle/scope through `ReleaseHandleLogically`, `ReleaseScopeLogically` and `ApplyReleaseResult`. These methods change `RuntimeContentHandle` state and optionally unregister handles from the logical root only. Physical cleanup remains outside RuntimeContent core.
