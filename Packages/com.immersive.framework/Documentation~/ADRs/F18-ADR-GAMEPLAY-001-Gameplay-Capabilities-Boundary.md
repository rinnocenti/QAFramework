# F18-ADR-GAMEPLAY-001 - Gameplay Capabilities Boundary

Status: Deferred / Superseded phase number  
Original phase: F18 - Gameplay Capabilities  
Current phase: F22+ / Future  
Type: Gameplay / Capabilities / Boundary  
Last updated: 2026-06-26

---

## 1. Context

This ADR preserved the boundary for future gameplay capabilities such as Projectile, Damage, Attributes, Powerups and advanced Actor capabilities.

The original phase number is superseded. F18 is now `Transition Orchestration Foundation`. Gameplay Capabilities must not be treated as the current next cut.

---

## 2. Current Decision

Gameplay Capabilities are deferred to F22+.

They can be reconsidered only after:

```text
F17 - Gate Foundation
F18 - Transition Orchestration Foundation
F19 - Transition Effects / Loading and Fade Adapters
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
```

They also depend on a mature gameplay object/actor/player model.

---

## 3. Deferred Capabilities

Future gameplay capabilities may include:

```text
Projectile as runtime-spawned content
Impact as capability
Damage as actor/object capability
Attributes as snapshot-capable capability
Powerups as gameplay capability
Advanced Actor capabilities
```

They must consume contracts instead of redefining core:

```text
Gate
RuntimeContentHandle
Object Entry
Actor/Player participation
Pooling boundary
Reset participants
Snapshot participants
Release policy
Input contracts
Diagnostics
```

---

## 4. Still Excluded From Current Plan

F17-F21 must not implement:

```text
Projectile gameplay
Damage system
Attributes system
Powerups
Combat rules
Inventory
Advanced Actor capabilities
contextual Player/Actor/NPC/Timer/Door/Pickup reset
```

---

## 5. Guardrails

- Gameplay cannot alter Route/Activity ownership.
- Gameplay cannot create a parallel pipeline.
- Gameplay cannot use a public service locator.
- Gameplay cannot use name/path as functional identity.
- Gameplay cannot transform pool return into reset.
- Gameplay cannot transform snapshot restore into reset.
- Gameplay cannot ignore required/optional policy.
- Gameplay cannot bypass Gate when flow admission is required.

---

## 6. What Not To Change Now

Do not implement gameplay capabilities before Gate, Transition and Pause have a stable plan and the gameplay object model is mature enough.
