# Player QA certification — 2026-08-09

Status: **CERTIFIED**  
Scope: `QAFramework` Player QA canonical one-button flow  
Operational entrypoint: `Immersive Framework/QA/Player/Run Full Player QA`

## Final verdict

```text
[QA_PLAYER_FULL] status='Passed' verdict='PLAYER QA CERTIFIED' session='PASS' sceneProvided='PASS' managerProvisioned='PASS' actor='PASS' publicSurface='PASS' participation='PASS'.
```

This certification was produced by the canonical master orchestrator in Unity.
The normal operator flow is one button; individual menus are diagnostic tools.

## Certified phases

| Phase | Verdict | Representative evidence from the run |
|---|---|---|
| Session | PASS | Player Participation Authoring PASS — 7 cases |
| Scene Provided | PASS | P3M5B Route Transition / Negative Matrix PASS — 25 cases |
| Manager Provisioned | PASS | Public Contract PASS — 9 cases; Waiting Projection PASS — 14 cases |
| Actor | PASS | Actor Selection Runtime Binding PASS — 13 cases; Player Gameplay Admission PASS — 114 cases |
| Public Surface | PASS | Q1 PASS — 28 cases; Q2 PASS — 36 cases |
| Participation | PASS | Activity Session Projection PASS — 30 cases |

## Configuration evidence

The Manager-Provisioned fixture was prepared with the active canonical QA
application/session and a derived Input System bridge:

```text
application='GameApplication'
session='CanonicalPlayerSessionProfile'
supportedSlots='2'
maxPlayers='2'
```

The Scene-Provided fixture was prepared independently with:

```text
hostProvisioning='SceneProvided'
supportedSlots='2'
```

This confirms that Scene-Provided and Manager-Provisioned proofs ran with their
own provisioning configuration rather than sharing one immutable runtime
Session across phases.

## Public Surface negatives

Q2 intentionally exercises rejected, stale, wrong-scope, destroyed-binding and
unbound-trigger behavior. Framework `ERROR` logs emitted for those expected
negative operations are diagnostic evidence; Q2 completed all 36 cases and
returned PASS.

The certified Q2 contract uses current Session semantics, including Supported
Slots, first-available Slot ordering and explicit no-available-slot rejection.
It does not use the removed Player Session Capacity model.

## Operational rule

Normal validation:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Use focused Session, Scene Provided, Manager Provisioned, Actor, Public Surface
or Game Flow/Participation menus only to diagnose a failed phase. Do not require
manual P3/M07/Q1/Q2 preparation as the normal Player QA workflow.

## Scope note

This record certifies the QAFramework Player QA execution captured on
2026-08-09. It does not convert historical P3/M07 documents into current
operational instructions and does not change framework product contracts.
