# F9R-O — Bridge Lifecycle Registry Registration Proof

Status: Ready for smoke  
Type: Runtime QA proof / hardening  
Scope: RuntimeContent + ContentAnchor materialization evidence  
Package area: `Packages/com.immersive.framework`

## Purpose

F9R-O proves that content materialized explicitly through an authored `UnityContentAnchorMaterializationBridgeSet` can be registered into the lifecycle-owned `LifecycleMaterializationRegistry` introduced by F9R-N.

This cut does **not** make Route or Activity own materialization automatically. It only proves the manual handoff:

```text
authored bridge set explicit MaterializeAll
  -> Unity materialized object evidence
  -> RuntimeContentHandle evidence
  -> explicit registration into LifecycleMaterializationRegistry
```

## What changed

Runtime QA now includes:

```text
Run Bridge Lifecycle Registry Registration Smoke
```

The smoke creates two authored bridge fixtures, materializes them explicitly through a bridge set, registers the resulting materialized handles into a local lifecycle registry, validates duplicate registration idempotency, then releases the bridge set explicitly for cleanup.

## Validated behavior

The smoke is expected to validate:

- bridge set materialization remains explicit;
- lifecycle registry registration remains explicit;
- two materialized bridge handles can be registered;
- duplicate registration of the same handle is stable/idempotent;
- lifecycle registry stores typed owner/scope evidence;
- explicit bridge release cleans the bridge/runtime physical/logical state;
- lifecycle registry does not execute physical release;
- lifecycle registry does not execute logical RuntimeContent release;
- lifecycle registry does not remove ContentAnchor bindings;
- Route/Activity auto-materialization remains disabled;
- Route/Activity auto-release remains disabled.

Expected smoke shape:

```text
QA Bridge Lifecycle Registry Registration Smoke step completed.
step='unity-content-anchor-bridge-lifecycle-registry-registration'
passed='True'
materializeAll='SucceededMaterializedAll'
materialized='2'
lifecycleRegisterFirst='SucceededRegistered'
lifecycleRegisterSecond='SucceededRegistered'
duplicateRegister='SucceededAlreadyRegistered'
lifecycleEntries='2'
lifecycleActive='2'
bridgeReleaseAll='SucceededReleasedAll'
bridgeReleased='2'
bridgeRegistryActive='0'
contentHandles='0'
bridgeSetMaterializationExplicit='True'
lifecycleRegistrationExplicit='True'
duplicateRegistrationStable='True'
lifecycleRegistryEvidenceOnly='True'
lifecycleRegistryPhysicalRelease='False'
lifecycleRegistryLogicalRuntimeContentRelease='False'
lifecycleRegistryContentAnchorBindingCleanup='False'
bridgeExplicitRelease='True'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
```

## Explicit non-goals

F9R-O does not implement:

- Route/Activity lifecycle integration;
- Route/Activity auto-materialization;
- Route/Activity auto-release;
- lifecycle release planning;
- lifecycle release execution;
- physical release from lifecycle registry;
- logical RuntimeContent release from lifecycle registry;
- ContentAnchor binding cleanup from lifecycle registry;
- Pause, camera, audio, save/progression, actor, pooling, player join, F34 or gameplay consumers.

## Why this cut matters

F9R-N proved the registry contract using synthetic handles. F9R-O proves the same registry can accept real handles created by the current Unity ContentAnchor bridge set proof.

This is still not lifecycle ownership execution. It is the narrow proof that materialized bridge evidence can cross into the lifecycle-owned registry model without making lifecycle responsible for creation or cleanup yet.

## Next candidate

The next safe candidate after smoke PASS is:

```text
F9R-P — Lifecycle Registry Release Plan Proof
```

That future cut should produce a release plan from registered lifecycle entries. It still should not wire Route/Activity exit automatically unless a later cut explicitly selects it.
