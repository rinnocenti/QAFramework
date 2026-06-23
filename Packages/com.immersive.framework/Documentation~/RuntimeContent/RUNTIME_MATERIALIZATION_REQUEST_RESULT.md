# Runtime Materialization Request / Result

Status: `F8G APPLIED / CONTRACTS`

This document records the F8G runtime materialization contracts introduced after runtime ownership primitives, passive handles, logical roots, `RuntimeContentRuntime`, `RuntimeScopeContext` and lifecycle root integration.

F8G does not materialize anything by itself. It defines the request/result language that a later concrete materializer must use.

---

## Added runtime contracts

| Type | Role |
|---|---|
| `RuntimeMaterializationResource` | Explicit materializer-facing resource descriptor. |
| `RuntimeMaterializationRequest` | Explicit request to create runtime content in a known `RuntimeScopeContext`. |
| `RuntimeMaterializationResult` | Immutable result of one materialization attempt. |
| `RuntimeMaterializationStatus` | Result status vocabulary. |

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

`RuntimeMaterializationResource` carries the input that a concrete materializer can interpret later:

```text
resourceType
resourceKey
resourceName
resourcePath
```

`resourceKey` is materializer input. It is not the runtime content identity and must not replace `RuntimeContentId`.

`resourceName` and `resourcePath` are diagnostics. They are not functional ownership keys.

F8G includes a convenience descriptor for future prefab materialization:

```text
RuntimeMaterializationResource.Prefab(...)
```

It does not reference `UnityEngine.GameObject`.

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

Future chain after F8I:

```text
RuntimeMaterializationRequest
  -> PrefabContentMaterializer
      -> RuntimeMaterializationResult
          -> RuntimeContentHandle
```

---

## Non-goals in F8G

F8G does not add:

- concrete materializer;
- `PrefabContentMaterializer`;
- transition guard;
- scoped cancellation;
- hierarchy root real;
- `GameObject`;
- `Transform`;
- `Instantiate`;
- `Destroy`;
- `GameObject.Find`;
- release execution;
- Content Anchor binding;
- Activity anchors;
- Actor/Pause/Camera/UI/Input/Save/Pooling consumers.

---

## Next cut

```text
F8H — transition guard + scoped cancellation
```
