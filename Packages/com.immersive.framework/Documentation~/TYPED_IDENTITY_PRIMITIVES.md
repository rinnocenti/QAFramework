# F1E — Typed identity primitives

Status: `APPLIED / PENDING COMPILE-SMOKE`

## Purpose

F1E introduces the minimum primitives required by `ADR-ID-001 — Typed Identity Policy`.

The goal is not to migrate every existing `string` field immediately. The goal is to stop creating new functional identity surfaces as loose strings and to provide a small canonical vocabulary for future domain-specific identifiers.

## Added primitives

```text
Runtime/Identity/FrameworkIdentityDomain.cs
Runtime/Identity/FrameworkIdentityValue.cs
Runtime/Identity/FrameworkIdentityKey.cs
Runtime/Identity/IFrameworkIdentity.cs
```

## Primitive roles

| Primitive | Role |
|---|---|
| `FrameworkIdentityDomain` | Coarse domain of a framework identity, such as Route, Activity, Content or Surface. |
| `FrameworkIdentityValue` | Validated immutable identity payload. Rejects null, empty and whitespace values. |
| `FrameworkIdentityKey` | Domain-qualified key composed from `FrameworkIdentityDomain` + `FrameworkIdentityValue`. |
| `IFrameworkIdentity` | Minimal contract for future domain-specific typed identity wrappers. |

## Rules introduced by F1E

```text
1. New functional identities must have an explicit domain.
2. New functional identity values must reject null, empty and whitespace input.
3. Equality must be ordinal and deterministic.
4. Default enum value `Unspecified` is not valid for authored/runtime identity keys.
5. String remains valid for display labels, source, reason and human diagnostics.
6. This cut does not migrate existing lifecycle fields yet.
```

## Why not migrate everything now

A broad migration would touch Route, Activity, Content, Diagnostics, validators and authoring assets at the same time. That would make the cut too large and would blur the boundary between identity policy and content identity review.

F1E deliberately creates primitives only. Domain-specific adoption is reserved for later cuts, starting with the content identity review.

## Explicit non-goals

F1E does not create:

```text
RouteId
ActivityId
ContentIdentity final model
Session identity model
Surface identity model
migration of existing serialized string fields
asset GUID policy
validator enforcement
runtime registry
service locator
fallback identity generation
```

## Validation

Run the baseline smoke:

```text
1. Unity compiles without CS errors.
2. Boot succeeds.
3. Route Smoke completes.
4. Activity Smoke completes.
5. Clear Activity Smoke completes.
```

If validation passes, close F1E as:

```text
F1E — CLOSED / COMPILE-SMOKE PASS
```

## Next cut

The next expected cut is:

```text
F1F — Content identity / FrameworkContentHandle review
```

F1F may consume these primitives to normalize content identity, but should still avoid a broad framework-wide identity migration.
