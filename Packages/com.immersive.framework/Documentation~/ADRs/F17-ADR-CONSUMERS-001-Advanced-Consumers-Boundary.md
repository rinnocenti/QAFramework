# F17-ADR-CONSUMERS-001 - Advanced Consumers Boundary

Status: Deferred / Superseded phase number  
Original phase: F17 - Advanced Consumers  
Current phase: F22+ / Future  
Type: Consumers / Boundary / Integration  
Last updated: 2026-06-26

---

## 1. Context

This ADR preserved the boundary for future advanced consumers such as Camera, Audio, Actor and Pooling.

The original phase number is superseded. F17 is now `Gate Foundation`, with F17A as documentation/ADR-only plan realignment. Advanced Consumers must not be treated as the current next cut.

---

## 2. Current Decision

Advanced Consumers are deferred to F22+.

They can be reconsidered only after:

```text
F17 - Gate Foundation
F18 - Transition Orchestration Foundation
F19 - Transition Effects / Loading and Fade Adapters
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
```

They also depend on a mature enough model for gameplay objects/actors where relevant.

---

## 3. Deferred Consumers

Future advanced consumers may include:

```text
Camera
Audio
Actor
Pooling integration
pooled materializer
```

They must consume existing core contracts instead of defining them:

```text
Gate
Lifecycle
Content Anchor
Runtime Materialization
RuntimeContentHandle
Object Entry
Reset
Contribution
Release
Diagnostics
```

---

## 4. Still Excluded

This deferred ADR still excludes gameplay-heavy systems:

```text
Projectile gameplay
Damage system
Attributes system
Powerups
Combat rules
Inventory
Enemy AI framework
contextual Player/Actor/NPC/Timer/Door/Pickup reset
```

---

## 5. Guardrails

- No advanced consumer can create a parallel lifecycle.
- No advanced consumer can discover the world globally as source of truth.
- No advanced consumer can use name/path as functional identity.
- No advanced consumer can create a silent fallback when a required dependency is missing.
- No advanced consumer can transform reset into release or pool return.
- No advanced consumer should bypass Gate when flow admission is required.

---

## 6. What Not To Change Now

Do not implement Camera, Audio, Actor, gameplay Pooling, Projectile, Damage, Attributes or Powerups in F17-F21.
