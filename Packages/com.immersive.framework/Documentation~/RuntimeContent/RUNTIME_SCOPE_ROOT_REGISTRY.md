# RuntimeScopeRoot and RuntimeRootRegistry

Status: `F8D APPLIED`

F8D introduces the first internal root registry boundary for runtime-created content.

This cut is deliberately logical/passive. It creates root ownership records and handle registration rules, but it does not create hierarchy GameObjects, instantiate prefabs, destroy objects, release scopes or bind Content Anchors.

## Added runtime internals

| Type | Role |
|---|---|
| `RuntimeScopeRoot` | Internal logical root for one `RuntimeContentOwner`. Stores handles owned by that scope/owner. |
| `RuntimeRootRegistry` | Internal registry for logical runtime roots in the current framework runtime. |
| `RuntimeRootRegistryOperationStatus` | Internal result vocabulary for root/handle registry operations. |
| `RuntimeRootRegistryOperationResult` | Internal immutable diagnostic result for registry operations. |

## Root semantics

A `RuntimeScopeRoot` is keyed by `RuntimeContentOwner`.

That means roots are not keyed by GameObject name, scene object name, prefab name or Content Anchor id.

Valid owners remain the F8B ownership primitives:

| Scope | Owner identity domain |
|---|---|
| `Session` | `FrameworkIdentityDomain.Session` |
| `Route` | `FrameworkIdentityDomain.Route` |
| `Activity` | `FrameworkIdentityDomain.Activity` |
| `Transient` | `FrameworkIdentityDomain.Runtime` |

## Registry semantics

`RuntimeRootRegistry` supports:

- explicit root creation;
- lookup by `RuntimeContentOwner`;
- snapshot by scope;
- registering a `RuntimeContentHandle` into its owner root;
- unregistering a handle by `RuntimeContentIdentity`;
- handle lookup by identity.

Registering a handle does not auto-create a missing root. Missing root produces `RejectedMissingRoot`.

This keeps root creation explicit and avoids silent fallback behavior.

## Handle registration rules

| Case | Result |
|---|---|
| Root is explicitly created | `RootCreated` |
| Root already exists | `RootAlreadyExists` |
| Handle owner matches root owner and content id is free | `HandleRegistered` |
| Same handle is registered again | `HandleAlreadyRegistered` |
| Another handle with same identity exists | `RejectedDuplicateHandle` |
| Handle owner does not match root owner | `RejectedMismatchedOwner` |
| Root does not exist | `RejectedMissingRoot` |
| Unregister missing handle | `HandleMissing` |

## F8D1 plan realignment

F8D1 changes the next gate after this document. The next authorized technical cut is not materialization request/result. The next gate is:

```text
F8E — RuntimeContentRuntime + RuntimeScopeContext
```

Request/result, prefab materialization and release execution remain in F8, but only after the internal runtime content owner, scope context and lifecycle root integration exist.

## Explicit non-goals

F8D does not add:

- runtime root GameObjects;
- `Transform` parent assignment;
- `GameObject.Find`;
- `Instantiate`;
- `Destroy`;
- materialization request/result;
- prefab materializer;
- runtime release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8E — RuntimeContentRuntime + RuntimeScopeContext
```
