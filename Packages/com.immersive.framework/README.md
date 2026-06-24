# Immersive Framework

Development package for game lifecycle architecture in Unity 6.5.

Package name:

```text
com.immersive.framework
```

## Current status

Use `Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md` as the single authoritative plan.

Short status:

```text
F0-F8 CLOSED
F9    CLOSED / LOGICAL CONTENT ANCHOR BINDING PASS
F10   NOT STARTED
F10+  PROPOSED / PENDING HUMAN APPROVAL
```

`Documentation~/COMPLETENESS_TRACKER.md` is intentionally short and only mirrors phase state.

## Documentation entry points

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Documentation~/COMPLETENESS_TRACKER.md
Documentation~/Planning/Foundation-Hardening-Backlog.md
Documentation~/RuntimeContent/RUNTIME_CONTENT_HANDLE.md
Documentation~/RuntimeContent/RUNTIME_SCOPE_ROOT_REGISTRY.md
Documentation~/RuntimeContent/RUNTIME_CONTENT_RUNTIME.md
Documentation~/RuntimeContent/RUNTIME_ROOT_LIFECYCLE_INTEGRATION.md
Documentation~/RuntimeContent/RUNTIME_MATERIALIZATION_REQUEST_RESULT.md
Documentation~/RuntimeContent/RUNTIME_RELEASE_POLICY_LOGICAL_EXECUTION.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_BINDING_CONTRACTS.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_BINDING_RUNTIME.md
Documentation~/ContentAnchor/ACTIVITY_CONTENT_ANCHOR_AUTHORING.md
Documentation~/ContentAnchor/ACTIVITY_CONTENT_ANCHOR_DISCOVERY.md
Documentation~/Route/ROUTE_CONTENT_PROFILE_USAGE.md
Documentation~/Route/ROUTE_SCENE_COMPOSITION_SMOKE.md
Documentation~/Route/ROUTE_RELEASE_SMOKE.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_SET.md
Documentation~/ContentAnchor/CONTENT_ANCHOR_AUTHORING_VALIDATION.md
Documentation~/ADRs/
```

## Current hard boundary

F9 is closed as a logical binding layer. F10 is not started.

Framework Core may define lifecycle, identity, ownership, request/result, policy, readiness, diagnostics, logical binding/release and future Activity entry/exit/reset/participation contracts.

Framework Core must not execute `Instantiate`, `Destroy`, `Addressables.Load`, `Addressables.Release`, pool rent/return, `Animator` reset, camera blend, UI concrete show/hide, player/actor mutation or gameplay state mutation.

Future Unity adapters own concrete scene, prefab, Addressables, Transform placement, hierarchy and physical release operations. Future gameplay consumers own Presentation, Actor, Player, NPC, Camera, Pause, Input, Save, Audio and gameplay Pooling behavior.
