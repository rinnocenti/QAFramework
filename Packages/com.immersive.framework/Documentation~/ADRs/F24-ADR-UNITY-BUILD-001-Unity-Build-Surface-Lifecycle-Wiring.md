# F24-ADR-UNITY-BUILD-001 - Unity Build Surface Lifecycle Wiring

Status: Accepted / F24A Planned  
Phase: F24 - Unity Build Surface / Lifecycle Wiring  
Type: Framework Unity Build Surface / Lifecycle Wiring Boundary  
Last updated: 2026-06-26

---

## 1. Context

F21 closed Save / Snapshot / Preferences / Progression Save Foundation. F22 closed Loading Operation / Progress / Readiness Boundary. F23 owns Pause Content / Overlay / Input Boundary.

The roadmap needs an intermediate framework phase before Gameplay Adapter Foundation. The blind spot is:

```text
contract exists
synthetic smoke passes
adapter exists or is planned
but no real GameObject / minimal prefab / Unity wiring proves it works in play
```

This is not gameplay adapter work. It is a framework-owned Unity build surface and lifecycle wiring phase.

---

## 2. Decision

F24 is:

```text
Unity Build Surface / Lifecycle Wiring
```

F24 comes after F23 and before F25 Gameplay Adapter Foundation.

F24 exists to prove framework contracts through minimal real Unity surfaces where applicable:

```text
real object wiring
minimal prefab or scene surface
explicit lifecycle wiring
real smoke with object/prefab/surface when applicable
limited framework scope
```

F24 must follow the F19D pattern:

```text
minimal object
explicit wiring
real smoke
limited scope
no product gameplay
```

---

## 3. Critical Ordering

F24B must be the first technical cut of F24:

```text
F24B - Transition ↔ GameFlow Runtime Integration
```

Reason:

```text
Transition exists as language,
but RouteRequestTrigger / GameFlow still need to pass through a real TransitionPlan.
```

Without F24B, Transition Curtain and Loading Screen can become visual surfaces that are mounted but not integrated with the real lifecycle path.

---

## 4. F24 Plan

| Cut | Status | Objective |
|---|---|---|
| F24A | `PLANNED / ADR PLAN` | Unity Build / Lifecycle Wiring ADR Plan. |
| F24B | `PLANNED` | Transition ↔ GameFlow Runtime Integration. |
| F24C | `PLANNED` | Transition Curtain Unity Build. |
| F24D | `PLANNED` | Loading Screen Unity Adapter Build. |
| F24E | `PLANNED` | Pause Overlay Unity Build. |
| F24F | `PLANNED` | Save Moment Authoring Boundary. |
| F24G | `PLANNED` | Preferences Authoring Boundary. |
| F24H | `PLANNED` | Closure + Usage Guide. |

---

## 5. Recommended Smokes

F24 should register real-object smokes where applicable:

```text
F24B - real Route switch generates TransitionSnapshot and executes the full sequence.
F24C - real Route switch executes visual fade curtain through UnityFadeCurtainEffectAdapter.
F24D - real Route switch shows/updates/hides a concrete loading screen adapter.
F24E - RequestPause opens/closes a real overlay without gameplay adapter.
F24F - manual/checkpoint save authoring triggers ProgressionSaveRuntime without gameplay payload.
F24G - Preferences authoring writes/reads a key through authoring surface without silent fallback.
```

---

## 6. Exclusions

F24 does not create gameplay adapters.

F24 must not create:

```text
Player adapter
Actor adapter
NPC adapter
Door adapter
Inventory adapter
Combat adapter
Projectile/Damage/Attributes adapter
gameplay reset adapter
full game menu
full UI system
full save system
product gameplay
```

F24A does not implement:

```text
runtime code
asmdef changes
GameObject
prefab
scene object
ScriptableObject
asset
UI
Transition ↔ GameFlow integration
loading screen adapter concrete build
pause overlay concrete build
save authoring concrete build
preferences authoring concrete build
smoke execution
```

---

## 7. Consequences

F24 prevents synthetic-only framework contracts from being handed directly to gameplay adapter work.

F25 can start only after the framework has proven its Unity build surfaces and lifecycle wiring.

F24 must remain a framework integration phase. It must not become "build the game".

