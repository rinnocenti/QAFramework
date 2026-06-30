# F9R-I — Unity ContentAnchor Materialization Runtime Authoring Gate Proof

Status: Implemented

## Purpose

Close the gap between authoring validation and runtime bridge-set submission. The authored bridge set now uses the same `UnityContentAnchorMaterializationAuthoringValidator` as a runtime gate before batch materialization.

## Runtime changes

- `UnityContentAnchorMaterializationBridgeSet` runs authoring validation before materialization preflight and before any materialization side effects.
- `UnityContentAnchorMaterializationBridgeSetStatus` adds `FailedAuthoringValidation`.

## QA

New QA smoke:

```text
Run Content Anchor Materialization Runtime Authoring Gate Smoke
```

Expected behavior:

- invalid bridge authoring is blocked by the runtime authoring gate;
- duplicate materialization key is blocked by the runtime authoring gate;
- valid bridge set still materializes and releases all content;
- invalid runtime gate attempts do not create registry entries, live objects or physical release requests.

## Non-goals

- No Route/Activity automatic lifecycle wiring.
- No Pause, Camera, Audio, Save, Actor or gameplay consumer.
- No Addressables.
- No pooling.
- No ContentAnchor-owned object creation or destruction.
