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
F10   CLOSED / ACTIVITY CONTENT EXECUTION CORE PASS
F11+  PROPOSED / PENDING HUMAN APPROVAL
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
Documentation~/ADRs/F10-activity-content-execution-core/
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_CONTRACTS.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_CONTRACT.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_COLLECTION.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_PHASE_PLAN.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_RUNTIME_SMOKE.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_INTEGRATION.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_LIFECYCLE_TRANSITION_SMOKE.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_PARTICIPANT_SOURCE_SMOKE.md
Documentation~/Activity/ACTIVITY_CONTENT_EXECUTION_CORE_CLOSURE.md
Documentation~/ADRs/
```

## Current hard boundary

F9 is closed as a logical Content Anchor binding layer. F10 is closed as the Framework Core layer for Activity Content Execution: contracts, aggregate result, participant contract, collection/ordering model, phase plan/request factory, runtime executor, lifecycle integration, participant source boundary and diagnostic smokes passed.

F10 does not add participant authoring/discovery, scene scan, physical placement, adapters or gameplay consumers.

Framework Core may define lifecycle, identity, ownership, request/result, policy, readiness, diagnostics, logical binding/release and future Activity entry/exit/reset/participation contracts.

Framework Core must not execute `Instantiate`, `Destroy`, `Addressables.Load`, `Addressables.Release`, pool rent/return, `Animator` reset, camera blend, UI concrete show/hide, player/actor mutation or gameplay state mutation.

Future Unity adapters own concrete scene, prefab, Addressables, Transform placement, hierarchy and physical release operations. Future gameplay consumers own Presentation, Actor, Player, NPC, Camera, Pause, Input, Save, Audio and gameplay Pooling behavior.
