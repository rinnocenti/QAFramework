# F7 — Content Anchor Declaration Audit

Status: F7 closed / CONTENT ANCHOR DECLARATION BASELINE PASS  
Package: `com.immersive.framework`  
Scope: Content Anchor declaration baseline  
Runtime changes in F7A: none

---

## 1. Purpose

F7 defines `Content Anchor` as the framework contract for authored placement/reference points inside loaded content.

This phase must not implement materialization, prefab binding, Camera, Pause, UI, Actor, Save, Pooling or Addressables. It exists to create a stable authored vocabulary that future consumers can use without capturing the lifecycle core.

---

## 2. Current baseline before F7

F6 is closed.

Known validated baseline:

```text
Route Scene Composition PASS
Route Release PASS
Owned additive Route scene unload PASS
Primary Scene remains controlled by LoadSceneMode.Single
```

F6 gives F7 a reliable loaded-content boundary:

```text
RouteContentSet
  -> Primary Scene handle
  -> Additional owned scene handles
  -> release policy/result
```

F7 must work on top of this boundary instead of creating a second scene ownership model.

---

## 3. Naming decision

The old planned placement-point vocabulary is rejected.

Canonical term:

```text
Content Anchor
```

Reason:

- clearer in Unity;
- clearer in Portuguese as “Âncora de Conteúdo”;
- does not suggest mesh, material, physics, shader or floor semantics;
- does not suggest callback/event/plugin behavior like “hook”.

Prohibited names for the canonical concept:

```text
Hook
Content Hook
Hook Content
```

If a specific point type is needed, use:

```text
ContentAnchorPoint
```

---

## 4. Approved model for F7

| Model | Purpose | Runtime side effects? |
|---|---|---|
| `ContentAnchorId` | Stable authored id. | No |
| `ContentAnchorScope` | Route, Activity or Local. | No |
| `ContentAnchorKind` | Root, Slot or Point. | No |
| `ContentAnchorRequiredness` | Required or Optional. | No |
| `ContentAnchorRoot` | Semantic content root/container. | No |
| `ContentAnchorSlot` | Placement/mount slot for later runtime content. | No |
| `ContentAnchorPoint` | Semantic reference point/pose. | No |
| `ContentAnchorSet` | Scoped result of discovered anchors. | No |

F7 may add passive data, passive authoring components, scoped discovery, diagnostics and validators.

F7 must not add materialization or consumers.

---

## 5. Scope rules

Initial scopes authorized by F7:

| Scope | Meaning |
|---|---|
| Route | Anchor belongs to loaded Route content. |
| Activity | Anchor belongs to active Activity content. |
| Local | Anchor is reported as a local contribution when applicable. |

Do not add Session/global anchors in F7.

---

## 6. Public authoring UX guidance

Prefer explicit components by lifecycle scope:

```text
Route Content Anchor
Activity Content Anchor
```

Avoid starting public UX with a generic endpoint component if it makes the Inspector harder to understand. A shared internal contract is fine, but public authoring should make ownership obvious.

Recommended inspector language:

```text
Anchor Id
Kind
Requiredness
Description
```

Avoid exposing “pipeline”, “stage”, “consumer”, “materializer” or “binding” language in F7 authoring components.

---

## 7. Identity rules

Functional identity must be explicit.

Not allowed as functional identity:

- GameObject name;
- hierarchy path;
- scene name;
- scene path;
- instance id;
- global search result order.

Recommended id style:

```text
gameplay.world
player.spawn.primary
camera.default-target
ui.hud-root
pause.overlay-root
```

Initial duplicate policy recommendation:

```text
scope owner + anchor id + kind
```

---

## 8. Expected F7 cuts

| Cut | Name | Scope |
|---|---|---|
| F7A | Content Anchor ADR/detail audit | Accepted docs only. |
| F7B | ContentAnchor identity primitives | Closed/pass: `ContentAnchorId`, `ContentAnchorScope`, `ContentAnchorKind`, `ContentAnchorRequiredness`. |
| F7C | ContentAnchor declaration model | Closed/pass: `ContentAnchorDeclaration`, `ContentAnchorRoot`, `ContentAnchorSlot`, `ContentAnchorPoint`. |
| F7D | Route Content Anchor authoring | Applied: first passive public authoring component for Route scope. |
| F7E | ContentAnchorSet | Scoped result model and diagnostics. |
| F7F | Loaded Route Content Anchor discovery | Discover anchors from loaded Route content only. |
| F7G | Content Anchor diagnostics smoke | Manual QA smoke for discovered Route-scoped anchors. Closed/pass. |
| F7H | Content Anchor authoring validation | Closed/pass: missing Route/id, invalid kind, route-scene mismatch and duplicate ids are covered by loaded authoring validation. |
| F7I | F7 closure | Closed/pass: docs and guardrails only. |

Activity-scoped authoring may enter F7 only if Route-scoped authoring and set semantics remain stable. Otherwise, defer Activity anchors to a follow-up cut.

---

## 9. Non-goals

F7 does not authorize:

```text
RuntimeRootRegistry
Prefab materialization
RuntimeContentAnchorBinding
ContentAnchorBindingRequest
ContentAnchorBindingResult
Camera consumer
Pause consumer
UI consumer
Actor/Audio consumer
Input policy
Save/Snapshot integration
Pooling integration
Addressables backend
```

---

## 10. Acceptance criteria for F7A

F7A is complete when:

- ADR F7-01 is accepted and uses `Content Anchor` terminology;
- F7 plan avoids `duplicated anchor naming` naming;
- tracker points to F7B as next implementation cut;
- no runtime behavior changes are included;
- F6 remains closed and untouched.

---

## 11. Status after F7A

```text
F7A — APPLIED / DOC REVIEW ACCEPTED
```

---

## 12. F7B — ContentAnchor identity primitives

Status: Closed / PASS

F7B adds only passive runtime primitives:

| Primitive | Purpose |
|---|---|
| `ContentAnchorId` | Explicit stable authored id for a Content Anchor. |
| `ContentAnchorScope` | Route, Activity or Local lifecycle/authored scope. |
| `ContentAnchorKind` | Root, Slot or Point declaration intent. |
| `ContentAnchorRequiredness` | Optional or Required authoring validation policy. |

F7B intentionally does not add:

```text
RouteContentAnchor
ActivityContentAnchor
ContentAnchorRoot
ContentAnchorSlot
ContentAnchorPoint
ContentAnchorSet
Content Anchor discovery
Content Anchor validators
smoke buttons
RuntimeRootRegistry
Prefab materialization
runtime binding
consumer systems
```

F7B smoke validation passed without regression.

---

## 13. F7C — ContentAnchor declaration model

Status: Closed / PASS

F7C adds passive declaration models only:

| Type | Purpose |
|---|---|
| `ContentAnchorDeclaration` | Canonical passive authored anchor record. |
| `ContentAnchorRoot` | Typed wrapper for `Root` declarations. |
| `ContentAnchorSlot` | Typed wrapper for `Slot` declarations. |
| `ContentAnchorPoint` | Typed wrapper for `Point` declarations. |

F7C intentionally does not add:

```text
RouteContentAnchor
ActivityContentAnchor
ContentAnchorSet
Content Anchor discovery
Content Anchor validators
smoke buttons
runtime binding
consumer systems
```

Next authorized cut:

```text
F7D — Route Content Anchor authoring
```

---

## 14. F7D — Route Content Anchor authoring

Status: Closed / PASS

F7D adds the first public authoring component for Content Anchor declaration:

```text
RouteContentAnchor
```

The component is scene-authored and Route-scoped. It exposes:

```text
Route
Anchor Id
Kind
Requiredness
Display Name
Description
```

It can produce a local `ContentAnchorDeclaration` from its explicit owner and id, but it does not participate in lifecycle yet.

F7D intentionally does not add:

```text
ContentAnchorSet
ContentAnchorRegistry
Loaded scene discovery
FrameworkAuthoringValidator rules
QA smoke
Runtime binding
RuntimeRootRegistry
Prefab materialization
Camera/Pause/UI/Actor consumers
```

F7E followed this cut.

---

## 15. F7E — ContentAnchorSet

Status: Closed / pass

F7E adds the passive scoped set model:

```text
ContentAnchorSet
ContentAnchorSetIssue
ContentAnchorSetIssueKind
```

The set stores unique `ContentAnchorDeclaration` entries and records local diagnostic issues for:

```text
InvalidDeclaration
DuplicateIdentity
DuplicateAnchorId
```

It exposes count/query helpers by scope, kind and requiredness. It is still a local passive model.

F7E intentionally does not add:

```text
RouteContentAnchor discovery
ActivityContentAnchor
FrameworkAuthoringValidator rules
RouteLifecycleRuntime integration
ContentAnchorRegistry
Content Anchor smoke
Runtime binding
RuntimeRootRegistry
Prefab materialization
Camera/Pause/UI/Actor consumers
```

F7F followed this cut.

---

## 16. F7F — Route Content Anchor discovery

Status: Closed / PASS

F7F discovers authored `RouteContentAnchor` components from the scenes loaded by `RouteSceneCompositionResult`, converts valid anchors into `ContentAnchorDeclaration`, and builds a local diagnostic `ContentAnchorSet` for the active Route.

Discovery is attached to `RouteLifecycleStartResult` and `RouteRuntimeState` for diagnostics only. Boot and route request logs now include `contentAnchors`, `contentAnchorCandidates`, `contentAnchorIssues`, `contentAnchorInvalid` and `contentAnchorRouteMismatch`.

F7F intentionally does not add validators, required-anchor blocking, Activity anchors, runtime binding/placement, RuntimeRootRegistry, prefab materialization or gameplay consumers.

Smoke validation confirmed `contentAnchors='1'`, `contentAnchorCandidates='1'`, `contentAnchorIssues='0'`, `contentAnchorInvalid='0'` and `contentAnchorRouteMismatch='0'` for the QA Canonical Route.

---

## 17. F7G — Content Anchor diagnostics smoke + QA Canvas cleanup

Status: Closed / PASS

F7G adds a dedicated QA smoke for Content Anchor diagnostics and reduces the visible QA Canvas buttons to the current validation path.

The new smoke validates the active Route's `ContentAnchorDiscoveryResult` and `ContentAnchorSet` counts. It is still diagnostic-only: no validator rules, required-anchor blocking, Activity anchors, runtime binding/placement, RuntimeRootRegistry, prefab materialization or gameplay consumers are introduced.

Visible QA buttons after this cut:

```text
Run Standard Smoke
Run Activity Baseline Smoke
Run Local Contribution Smoke
Validate Loaded Authoring
Reset QA Scenario
Run Route Scene Composition Smoke
Run Route Release Smoke
Run Content Anchor Diagnostics Smoke
```

## 18. F7H — Content Anchor authoring validation

Status: Closed / PASS

F7H adds authoring-validation coverage for loaded `RouteContentAnchor` components. Validation reports missing Route, missing Anchor Id, `Kind = Unknown`, invalid Requiredness, scene/Route declaration mismatch and duplicate Content Anchor identities/ids.

The QA Canvas validation button is now:

```text
Validate Loaded Authoring
```

This cut does not enforce Required anchors in lifecycle and does not introduce Activity anchors, runtime binding/placement, RuntimeRootRegistry, prefab materialization or gameplay consumers.

F7H smoke validation passed with loaded authoring validation reporting one valid Route Content Anchor and zero issues.

```text
QA Authoring Validation completed. scope='Loaded Authoring' routeContentAnchors='1' issues='0' contentAnchors='1' contentAnchorIssues='0' contentAnchorDuplicateIdentity='0' contentAnchorDuplicateId='0'
```

---

## 19. F7I — F7 closure

Status: Closed / PASS

F7 is closed as the Content Anchor declaration baseline. It provides enough authored/diagnostic structure for later phases to consume anchors without inventing search conventions or hardcoded GameObject references.

Closed baseline:

```text
F7A — Content Anchor ADR/detail audit                  CLOSED / DOCS
F7B — ContentAnchor identity primitives                CLOSED / PASS
F7C — ContentAnchor declaration model                  CLOSED / PASS
F7D — Route Content Anchor authoring                   CLOSED / PASS
F7E — ContentAnchorSet                                 CLOSED / PASS
F7F — Route Content Anchor discovery                   CLOSED / PASS
F7G — Content Anchor diagnostics smoke + QA cleanup    CLOSED / PASS
F7H — Content Anchor authoring validation              CLOSED / PASS
F7I — F7 closure                                       CLOSED / DOCS
```

Validated evidence:

```text
Boot succeeded with contentAnchors='1' and contentAnchorIssues='0'
Validate Loaded Authoring PASS with routeContentAnchors='1' and issues='0'
Content Anchor Diagnostics Smoke PASS
Route Scene Composition Smoke PASS
Route Release Smoke PASS
Local Contribution Smoke PASS
```

F7 explicitly defers:

```text
ActivityContentAnchor
required-anchor lifecycle blocking
ContentAnchorRegistry as a global/service-like API
runtime placement/binding
RuntimeRootRegistry
prefab materialization
Camera/Pause/UI/Actor/Audio consumers
Input/Save/Pooling integration
Addressables backend
```

Next authorized phase:

```text
F8A — Runtime roots/materialization ADR-detail audit
```
