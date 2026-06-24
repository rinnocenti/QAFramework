# Runtime Materialization Request / Result

Status: `F8K UPDATED / APPLY HANDOFF + SMOKE`

This document records the F8G runtime materialization contracts introduced after runtime ownership primitives, passive handles, logical roots, `RuntimeContentRuntime`, `RuntimeScopeContext` and lifecycle root integration. F8H extends the request with a scoped cancellation token. F8I adds the materialization adapter boundary. F8J adds the separate release request/result/policy boundary. F8K adds the explicit `ApplyMaterializationResult` handoff from adapter result back into the RuntimeContent registry.

These contracts do not materialize anything by themselves. They define the request/result language and the `IRuntimeMaterializationAdapter` boundary that a physical adapter outside the RuntimeContent core may implement.

---

## Added runtime contracts

| Type | Role |
|---|---|
| `RuntimeMaterializationResource` | Explicit materializer-facing resource descriptor. |
| `RuntimeMaterializationRequest` | Explicit request to create runtime content in a known `RuntimeScopeContext`, now carrying `RuntimeScopeCancellationToken`. |
| `RuntimeMaterializationResult` | Immutable result of one materialization attempt. |
| `RuntimeMaterializationStatus` | Result status vocabulary. |
| `IRuntimeMaterializationAdapter` | Public experimental boundary implemented by physical adapters outside the RuntimeContent core. |

---

## Request shape

A `RuntimeMaterializationRequest` contains:

```text
RuntimeScopeContext
RuntimeContentId
RuntimeContentIdentity
RuntimeMaterializationResource
source
reason
```

The request is scoped. It does not invent ownership and does not auto-create roots.

The identity is derived from:

```text
RuntimeScopeContext.Owner + RuntimeContentId
```

---

## Resource descriptor

`RuntimeMaterializationResource` carries the input that a adapter físico can interpret later:

```text
resourceType
resourceKey
resourceName
resourcePath
```

`resourceKey` is materializer input. It is not the runtime content identity and must not replace `RuntimeContentId`.

`resourceName` and `resourcePath` are diagnostics. They are not functional ownership keys.

F8G intentionally keeps `RuntimeMaterializationResource` generic. It has `resourceType`, `resourceKey`, `resourceName` and `resourcePath`, but no canonical prefab/cena/addressable factory. External adapters may interpret `resourceType` explicitly outside the core.

---

## Result shape

A `RuntimeMaterializationResult` contains:

```text
RuntimeMaterializationRequest
RuntimeMaterializationStatus
RuntimeContentHandle optional
source
reason
message
```

A successful result must include a handle whose identity matches the request identity.

Failures do not include a handle.

---

## RuntimeContentRuntime change

`RuntimeContentRuntime` can now create materialization requests from an existing `RuntimeScopeContext`.

It still does not execute materialization.

Current chain:

```text
RuntimeContentRuntime
  -> RuntimeRootRegistry
      -> RuntimeScopeRoot(owner)
          -> RuntimeScopeContext
              -> RuntimeMaterializationRequest
```

Adapter chain after F8K:

```text
RuntimeMaterializationRequest
  -> IRuntimeMaterializationAdapter.Materialize
      -> RuntimeMaterializationResult
          -> RuntimeContentRuntime.ApplyMaterializationResult
              -> RuntimeContentHandle
              -> RuntimeScopeRoot registration
```

`IRuntimeMaterializationAdapter` is only a boundary. `ApplyMaterializationResult` is the core handoff that validates the scoped guard, normalizes declared handles to `Materialized` and registers the handle. The framework core still does not ship a prefab, scene, Addressables or pool implementation.

---

## Non-goals in F8G

F8G/F8I do not add:

- implementação de adapter físico;
- hierarchy root real;
- `GameObject`;
- `Transform`;
- `Instantiate`;
- `Destroy`;
- `GameObject.Find`;
- physical release execution;
- Content Anchor binding;
- Activity anchors;
- Actor/Pause/Camera/UI/Input/Save/Pooling consumers.

---

## Next cut

```text
F8J — Runtime release policy / logical release execution [APPLIED / COMPILE-SMOKE PASS]
F8K — Runtime request/guard/release-policy smoke and F8 closure [PASS / F8 CLOSED]
```
