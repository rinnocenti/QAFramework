# Runtime Ownership Primitives

Status: `F8B APPLIED`

F8B introduces passive primitives for runtime-created content ownership. These primitives define identity, owner scope and state vocabulary only.

## Added runtime primitives

| Primitive | Role |
|---|---|
| `RuntimeContentScope` | Declares the owner lifetime: `Session`, `Route`, `Activity` or `Transient`. |
| `RuntimeContentState` | Defines passive state vocabulary for future handles and release diagnostics. |
| `RuntimeContentId` | Typed id for one runtime-created content record. It is not a GameObject name or prefab path. |
| `RuntimeContentOwner` | Couples a runtime content scope with a typed owner identity. |
| `RuntimeContentIdentity` | Combines owner and content id into a stable runtime content identity. |

## Scope to owner domain

F8B keeps ownership strict:

| Runtime scope | Expected owner identity domain |
|---|---|
| `Session` | `FrameworkIdentityDomain.Session` |
| `Route` | `FrameworkIdentityDomain.Route` |
| `Activity` | `FrameworkIdentityDomain.Activity` |
| `Transient` | `FrameworkIdentityDomain.Runtime` |

A mismatched owner domain throws at construction time. This prevents silent fallback roots and avoids using GameObject names as ownership identity.

## Explicit non-goals

F8B does not add:

- runtime scope root GameObjects;
- root registry;
- `RuntimeContentHandle`;
- materialization request/result;
- prefab materializer;
- release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8C — RuntimeContentHandle passive and release state
```
