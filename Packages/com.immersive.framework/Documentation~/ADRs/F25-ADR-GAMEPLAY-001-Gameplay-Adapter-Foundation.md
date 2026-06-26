# F25-ADR-GAMEPLAY-001 - Gameplay Adapter Foundation

Status: Deferred / Planned after F24  
Phase: F25 - Gameplay Adapter Foundation  
Type: Gameplay Adapter / Consumer Boundary  
Last updated: 2026-06-26

---

## 1. Context

Gameplay Adapter Foundation was previously tracked by a superseded Gameplay Capabilities ADR with an old phase number. The official roadmap now inserts F24 Unity Build Surface / Lifecycle Wiring before gameplay adapters.

F25 can only start after:

```text
F23 - Pause Content / Overlay / Input Boundary
F24 - Unity Build Surface / Lifecycle Wiring
```

F24 exists so framework contracts that need real Unity surfaces are proven before gameplay adapters consume them.

---

## 2. Decision

F25 is:

```text
Gameplay Adapter Foundation
```

F25 is after F24.

F25 remains outside the current documentation-only cut.

Gameplay adapters must consume framework contracts instead of redefining them:

```text
Gate
Transition
Loading
Pause
Save / Snapshot / Preferences / Progression Save
Runtime Content
Content Anchor
Object Entry
Object Reset
Pooling boundary
Input contracts
Diagnostics
```

---

## 3. Deferred Scope

F25 may later plan gameplay-facing adapters such as:

```text
Player
Actor
Camera
Audio
gameplay Pooling
Projectile
Damage
Attributes
Powerups
contextual reset
```

These are not detailed further in this ADR because gameplay adapter design is intentionally deferred until F24 proves Unity build/lifecycle wiring.

---

## 4. Guardrails

- Gameplay cannot alter Route/Activity ownership.
- Gameplay cannot create a parallel lifecycle pipeline.
- Gameplay cannot use a public service locator.
- Gameplay cannot use name/path as functional identity.
- Gameplay cannot transform pool return into reset.
- Gameplay cannot transform snapshot restore into reset.
- Gameplay cannot ignore required/optional policy.
- Gameplay cannot bypass Gate when flow admission is required.
- Gameplay adapters must not replace F24 framework Unity build surfaces.

---

## 5. Excluded Now

This documentation cut does not implement:

```text
runtime code
asmdef changes
Player adapter
Actor adapter
NPC adapter
Door adapter
Inventory adapter
Combat adapter
Projectile/Damage/Attributes adapter
gameplay reset adapter
scene object
prefab
ScriptableObject
UI
smoke
```

