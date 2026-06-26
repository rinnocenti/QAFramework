# F21-ADR-SAVE-001 - Save Snapshot Preferences Progression Boundary

Status: Accepted / F21B Primitives Applied / F21C Next  
Phase: F21 - Save / Snapshot / Preferences / Progression Save Foundation  
Type: Framework Core + Save Module Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as logical core. The next framework axis opens Save before Pause visual/gameplay work so state capture, player preferences and progression persistence have explicit contracts before overlay/input/gameplay adapters start depending on them.

F10 already decided that Snapshot is different from Reset and that Reset Baseline is not Save Snapshot. F21 keeps that separation and adds the missing persistence-side boundaries.

`NewScripts` is reference-only for concepts. F21 does not copy code, assets, configs, ProjectSettings or runtime architecture from the old project.

---

## 2. Decision

F21 is:

```text
Save / Snapshot / Preferences / Progression Save Foundation
```

F21 defines three separate ownership areas:

```text
Snapshot
Preferences
Progression Save
```

Snapshot is an envelope and participant contract boundary. It captures/restores runtime state and does not know or select a persistence backend.

Preferences is the user/application settings boundary. It is independent from progression slots and independent from gameplay snapshot capture.

Progression Save owns progression save requests, save slots, slot manifests, autosave/load moment contracts and backend port contracts. Progression Save depends on a replaceable backend port, not on a concrete storage engine.

The future JSON backend is the initial adapter only, not the canonical save contract. A future premium backend must replace JSON behind the same interface/port without changing framework-level request/adaptation code.

---

## 3. Snapshot Boundary

Snapshot may define:

```text
snapshot envelope identity
snapshot owner/scope
schema/version metadata
payload records
participant capture/restore contracts
snapshot set/collection contracts
diagnostic facts
```

Snapshot must not define:

```text
file paths
PlayerPrefs keys
JSON format as canonical contract
cloud/premium backend details
progression slot selection
autosave policy
menu/UI flow
```

---

## 4. Preferences Boundary

Preferences may define:

```text
preference keys/records
load/save result contracts
store port
PlayerPrefs adapter boundary in a future cut
diagnostic facts
```

Preferences must not define:

```text
progression slots
save manifests
checkpoint/progression identity
snapshot participant lifecycle
gameplay progression payloads
```

---

## 5. Progression Save Boundary

Progression Save may define:

```text
save slot identity
save snapshot identity
save slot manifest
progression save request/result
progression load request/result
autosave/load moment contracts
backend port
diagnostic facts
```

Progression Save must not define a concrete backend as the canonical contract. JSON and premium storage are adapters behind the same port.

---

## 6. F21 Plan

| Cut | Status | Objective |
|---|---|---|
| F21A | `APPLIED / DOCS ONLY` | Save / Snapshot / Preferences / Progression ADR Plan and roadmap realignment. |
| F21B | `APPLIED / PRIMITIVES` | Snapshot Envelope Primitives. |
| F21C | `NEXT / PLANNED` | Snapshot Participant Contracts + Diagnostics Smoke. |
| F21D | `PLANNED` | Preferences Store Contracts + PlayerPrefs Backend. |
| F21E | `PLANNED` | Progression Save Port + Slot/Manifest Primitives. |
| F21F | `PLANNED` | JSON Progression Backend + Diagnostics Smoke. |
| F21G | `PLANNED` | Progression Save Runtime Request Path + Autosave Moment Contracts. |
| F21H | `PLANNED` | Closure + Usage Guide. |

---

## 7. F21B Applied Shape

F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`:

```text
SnapshotEnvelopeId
SnapshotScope
SnapshotSchemaId
SnapshotSchemaVersion
SnapshotPayloadFormat
SnapshotPayload
SnapshotEnvelope
```

The envelope shape records:

```text
envelope identity
scope
owner identity
schema identity
schema version
payload format
payload bytes or explicit empty payload
captured UTC ticks
source/reason diagnostics
```

F21B does not add participant contracts, capture execution, restore execution, backend ports, PlayerPrefs, JSON, progression slots, save manifests, autosave moments, UI, scene objects, prefabs, ScriptableObjects or asmdef changes.

---

## 8. Excluded in F21A

F21A does not implement:

```text
runtime code
backend
PlayerPrefs
JSON
UI
scene object
prefab
ScriptableObject
asmdef changes
Unity object setup
save runtime request path
autosave execution
```

---

## 9. Consequences

Save work can evolve without binding Snapshot to JSON, PlayerPrefs or premium storage.

Preferences can remain a lightweight settings boundary and cannot accidentally consume progression slots.

Progression Save can start with JSON later while preserving the ability to replace the backend behind the same port.

Pause visual/content/input moves to F23. Gameplay Adapter Foundation moves to F24.

---

## 10. Next Cut

```text
F21C - Snapshot Participant Contracts + Diagnostics Smoke
```
