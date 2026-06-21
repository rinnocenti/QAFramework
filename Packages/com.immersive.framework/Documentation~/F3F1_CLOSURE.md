# F3F1 — QA panel simplification — Closure

Status: CLOSED / COMPILE-SMOKE PASS

## Purpose

F3F1 reduced the default QA panel surface before closing F3F.

## Validated behavior

The simplified panel exposes `Run Standard Smoke` as the primary smoke path and keeps specialized Route Content callback validation in a separate section.

Validated smoke evidence:

```text
QA Smoke completed. name='Standard Smoke'
```

## Result

The default QA panel is smaller and separates canonical smoke from specialized callback smoke.

## Non-goals

F3F1 did not remove advanced/manual controls from code. It only moved them behind an advanced foldout.
