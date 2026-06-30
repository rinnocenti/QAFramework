# IF-FW-F10A — Pause ContentAnchor Consumer Re-entry Plan

Status: Accepted / Plan / docs-only.

## Intent

Select Pause as the next explicit axis after the F9R materialization/release closure.

F10A re-enters the existing Pause work through the new RuntimeContent + ContentAnchor materialization foundation. It defines Pause as a first consumer of the materialization chain, but does not implement the visual Pause surface yet.

## Starting point

The framework already has logical Pause language and runtime NoOp behavior when no visual surface is configured:

```text
Pause surface is not configured.
Logical Pause will remain available without visual Pause presentation.
```

F9R now provides the missing foundation for a future Pause visual surface:

```text
ContentAnchor declaration/discovery
+ explicit materialization
+ explicit bridge / bridge set
+ preflight and rollback
+ lifecycle-owned registry
+ release plan / release execution
+ composite physical/logical/binding cleanup
```

## Accepted consumer boundary

Pause is accepted as a **consumer** of ContentAnchor and RuntimeContent.

Pause is not accepted as:

- owner of Route lifecycle;
- owner of Activity lifecycle;
- owner of generic materialization;
- owner of generic auto-release;
- owner of player join or input manager behavior;
- replacement for the existing logical Pause runtime.

## Game design reading

Pause should behave like a presentation layer that appears over the current game state.

In game-design terms:

```text
The game enters Pause.
The logical state says gameplay is paused.
The visual Pause layer asks for an authorized place to appear.
The Pause visual content is materialized there.
When Pause ends, that visual content is released cleanly.
```

Pause should not decide which Route is active, which Activity is active, or whether Route/Activity content should be unloaded.

## Initial target model

The first Pause visual model should be explicit and opt-in:

```text
Pause logical state
  -> Pause visual consumer request
  -> ContentAnchor binding request for a Pause overlay anchor
  -> explicit materialization through the proven F9R chain
  -> explicit composite release when Pause visual content is dismissed
```

The preferred authoring target is a dedicated Pause overlay anchor/surface, normally in a persistent UI/global presentation context when available. Route/Activity anchors can be considered later only if a cut explicitly defines the selection and priority policy.

## F10A does not implement

F10A does not add runtime code, editor code, scenes, prefabs, asmdefs, package metadata or QA buttons.

It does not implement:

- Pause visual materialization;
- Pause visual release;
- Pause ContentAnchor authoring component;
- Pause ContentAnchor binding request execution;
- Pause toggle integration with visual materialization;
- Route/Activity auto-materialization;
- Route/Activity auto-release;
- lifecycle exit wiring;
- InputMode changes;
- PlayerInput changes;
- Time.timeScale policy;
- gameplay/F34;
- camera, audio, save/progression, actor, pooling or PlayerJoin consumers.

## Proposed F10 sequence

The accepted next sequence is:

| Cut | Purpose | Type |
|---|---|---|
| F10A | Pause ContentAnchor Consumer Re-entry Plan | Docs-only |
| F10B | Pause Visual Surface Authoring Contract | Contract / authoring plan or proof |
| F10C | Pause ContentAnchor Binding Request Proof | Runtime proof, explicit only |
| F10D | Pause Visual Materialization Smoke | Explicit materialization proof |
| F10E | Pause Visual Release Smoke | Explicit release proof |
| F10F | Pause Toggle to Visual Pause Integration Decision/Proof | Only after F10B-F10E evidence |

This sequence may be adjusted only by explicit user selection.

## Acceptance criteria for leaving F10A

Before implementing Pause visual code, the next cut must answer:

1. Which authored surface/anchor represents Pause visual placement?
2. Is the first proof UIGlobal/persistent-surface scoped, Route-scoped, Activity-scoped or QA-fixture scoped?
3. What request/result names represent Pause visual materialization?
4. What is required vs optional when the Pause visual anchor is missing?
5. Which smoke proves visual materialization without changing Route/Activity lifecycle?

## Decision

F10 Pause ContentAnchor consumer is selected as the next axis after F9R.

Only F10A is accepted by this document. Implementation requires the next explicit cut.
