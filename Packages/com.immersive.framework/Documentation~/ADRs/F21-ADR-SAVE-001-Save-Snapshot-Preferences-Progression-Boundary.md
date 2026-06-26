# F21-ADR-SAVE-001 - Save Snapshot Preferences Progression Boundary

Status: Accepted / F21A ADR Plan  
Phase: F21 - Save / Snapshot / Preferences / Progression Save Foundation  
Type: Framework Core + Save Module Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as logical core. The next framework cut must open Save before Pause visual/gameplay work so state capture, preferences and progression persistence have explicit boundaries before overlay/input/gameplay adapters start depending on them.

F10 already decided that Snapshot is different from Reset, and that Reset Baseline is not Save Snapshot.

NewScripts is reference-only for concepts. F21 does not copy code, assets, configs or runtime architecture from the old project.

---

## 2. Decision

F21 is renamed to:

```text
Save / Snapshot / Preferences / Progression Save Foundation
```

F21 defines three separate ownership areas:

```text
Snapshot
Preferences
Progression Save
```

Snapshot is an envelope and participant contract boundary. It does not know or select a persistence backend.

Preferences is for user/application settings. Preferences does not use progression slots.

Progression Save owns progression save requests, slots, manifests and backend port contracts. Progression Save uses a replaceable backend port.

The future JSON backend is the initial adapter only, not the canonical save contract. A future premium backend must replace JSON behind the same interface.

---

## 3. Boundary

Snapshot may define:

```text
snapshot envelope identity
snapshot owner
schema/version metadata
payload shape
participant contracts
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
```

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
```

Progression Save may define:

```text
save slot identity
save manifest
save request/result
backend port
autosave moment contracts
diagnostic facts
```

Progression Save must not define a concrete backend as the canonical contract.

---

## 4. F21 Plan

| Cut | Status | Objective |
|---|---|---|
| F21A | `CURRENT / ADR PLAN` | Save / Snapshot / Preferences / Progression ADR Plan. |
| F21B | `PLANNED` | Snapshot Envelope Primitives. |
| F21C | `PLANNED` | Snapshot Participant Contracts + Diagnostics Smoke. |
| F21D | `PLANNED` | Preferences Store Contracts + PlayerPrefs Backend. |
| F21E | `PLANNED` | Progression Save Port + Slot/Manifest Primitives. |
| F21F | `PLANNED` | JSON Progression Backend + Diagnostics Smoke. |
| F21G | `PLANNED` | Progression Save Runtime Request Path + Autosave Moment Contracts. |
| F21H | `PLANNED` | Closure + Usage Guide. |

---

## 5. Excluded Now

F21A does not implement:

```text
runtime code
backend
PlayerPrefs
JSON
UI
scene object
ScriptableObject
asmdef changes
Unity object setup
save runtime request path
autosave execution
```

---

## 6. Consequences

Save work can evolve without binding Snapshot to JSON, PlayerPrefs or premium storage.

Preferences can remain a lightweight settings boundary and cannot accidentally consume progression slots.

Progression Save can start with JSON later while preserving the ability to replace the backend behind the same port.

Pause visual/content/input moves to F23. Gameplay Adapter Foundation moves to F24.

