# F10F — Pause Presentation Model Decision

Status: Accepted / Decision / docs-only

## Purpose

F10F resolves the design question raised after F10E:

```text
Does Pause need to be runtime-materialized,
or can the visual Pause menu live directly in UIGlobal and be shown from there?
```

## Decision

The canonical product path for the normal Pause menu should be:

```text
Resident UIGlobal Pause Surface
```

That means:

```text
UIGlobal scene owns the concrete Pause visual hierarchy.
Pause logic requests presentation changes.
The visual surface shows/hides the resident UI.
```

F10E remains accepted as a valid capability proof and optional path, but it should not be treated as the mandatory product implementation for Pause.

## Why resident UIGlobal is preferred for Pause

Pause is a global presentation concern. It is expected to be immediately available, independent of Route or Activity content. A resident UIGlobal surface is simpler for designers and safer for product authoring:

```text
- the Pause panel is visible in the scene/prefab hierarchy;
- designers can edit the menu directly;
- no runtime spawn is required just to open the menu;
- no prefab loading/materialization is needed for the common case;
- visual state can be handled by show/hide animation or CanvasGroup state;
- the logical Pause system remains independent from Route/Activity lifecycle.
```

## What F10E still provides

F10E is not wasted.

It proves the reusable materialized-consumer path:

```text
Pause visual contract
  -> ContentAnchor binding
  -> RuntimeContent materialization
  -> physical placement
  -> explicit cleanup
```

That path remains useful for:

```text
- non-resident optional Pause skins;
- route/activity-specific Pause overlays;
- modular UI packages;
- DLC/streamed UI;
- QA validation of ContentAnchor materialization;
- future consumers that truly need spawned presentation.
```

## Consequence for the next implementation

The next technical cut should not continue directly into a materialized Pause release proof as the canonical Pause product path.

Instead, the next implementation should define the resident surface contract:

```text
F10G — Pause UIGlobal Resident Surface Contract Proof
```

F10G should prove:

```text
- a resident Pause visual surface can be declared in UIGlobal;
- logical Pause presentation state can be applied to that surface;
- the surface can show/hide without runtime materialization;
- no InputMode, PlayerInput or Time.timeScale mutation is introduced yet;
- no Route/Activity auto-materialization or auto-release is introduced.
```

## Accepted presentation modes

| Mode | Status | Use case |
|---|---|---|
| Resident UIGlobal Pause Surface | Canonical default | Standard Pause menu / designer-authored UI. |
| Runtime-materialized Pause Visual Surface | Supported optional path | Modular, streamed, route-specific or variant Pause visuals. |

## Guardrails

F10F is docs-only and does not implement:

- new runtime code;
- editor code;
- scene changes;
- prefab changes;
- QA buttons;
- Pause toggle integration;
- InputMode changes;
- PlayerInput changes;
- `Time.timeScale` policy;
- Route/Activity auto-materialization;
- Route/Activity auto-release;
- camera, audio, save, actor, pooling, PlayerJoin or gameplay/F34 consumers.

## Designer-facing rule

For the common game menu, think:

```text
Pause menu lives in UIGlobal.
Pause state tells it when to appear.
```

Think of materialization as the advanced/optional path:

```text
Pause menu does not exist yet,
so the framework creates it at a declared ContentAnchor.
```
