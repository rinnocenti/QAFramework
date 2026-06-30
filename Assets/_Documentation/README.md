# Immersive Framework — Project Documentation

Esta pasta contém a documentação viva da etapa Unity-facing do projeto.

A partir de F24, a fonte operacional deve ser lida por fronteira:

- `Assets/` é a fonte operacional de assets, cenas, QA, documentação viva e configurações do projeto consumidor.
- `Packages/com.immersive.framework/` é a fonte operacional do framework core quando o corte altera contratos, lifecycle, runtime, diagnostics ou authoring genérico do framework.
- Outros packages só entram quando o corte declarar explicitamente integração com adapter/package externo.

## Índice

### ADRs

- `ADRs/F24-ADR-UNITY-001-Implementation-Tracks.md`
- `ADRs/F24-ADR-UNITY-002-Implementation-Workflow-And-QA-Workspace.md`
- `ADRs/F24-ADR-UNITY-003-Project-And-Framework-Source-Boundary.md`

### Plans

- `Plans/F24-PLAN-Unity-Build-Surface.md`
- `Plans/F25-PLAN-Activity-Content-Scene-Composition.md`
- `Plans/F26-PLAN-Activity-Discovery-And-Loading-Progress.md`
- `Plans/F27-PLAN-Pause-UIGlobal-And-Input.md`
- `Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md` — Roadmap Reconciliation and Adapter Module Spine
- `Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md` — Unity Input Target Ownership Proof
- `Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md` — InputMode Identity and Request Result Model / closed
- `Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md` — PlayerActor Identity and Unity Input Evidence / closed
- `Plans/F32-PLAN-InputMode-Unity-Adapter-Application.md` — InputMode Unity Adapter Application / closed
- `Plans/F33-PLAN-Pause-Runtime-PlayerInput-Wiring.md` — Pause Runtime PlayerInput Wiring / closed
- `Plans/POST-F33-PLAN-Matrix-Reconciliation.md` — Matrix Reconciliation / accepted; supersedes any post-F33 reading that implied F34 or gameplay selection.
- `Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md` — Runtime Root / Handle / Release Policy planning; docs-only, no materializer implementation selected.
- `Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md` — Runtime Materialization Adapter Boundary planning; docs-only, no materializer implementation selected.
- `Plans/F8R-D-PLAN-Physical-Release-Adapter.md` — Physical Release Adapter planning; docs-only, no release adapter implementation selected.
- `Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md` — ContentAnchor Runtime Binding Re-entry draft plan/ADR; docs-only, no physical placement or implementation selected.
- `Plans/F9R-M-PLAN-Lifecycle-Owned-Materialization-Registry.md` — Lifecycle-Owned Materialization Registry planning baseline; docs-only, no lifecycle auto-materialization selected.
- `Plans/F9R-R-PLAN-Route-Activity-Exit-Auto-Release-Decision.md` — Route/Activity exit auto-release decision; immediate wiring rejected, composite release gap selected.
- `Plans/F10A-PLAN-Pause-ContentAnchor-Consumer-Reentry.md` — Pause ContentAnchor consumer re-entry plan; selected next axis after F9R, docs-only.
- `Notes/F8R-E-Unity-Prefab-Runtime-Materialization-Adapter-Proof.md` — Unity prefab RuntimeContent materialization adapter proof; first physical adapter implementation and QA smoke.
- `Notes/F9R-B-Unity-ContentAnchor-Physical-Placement-Adapter-Proof.md` — Unity ContentAnchor physical placement adapter proof; explicit Transform parenting after logical binding and materialization evidence.
- `Notes/F9R-C-Unity-ContentAnchor-Materialization-Pipeline-Proof.md` — Unity ContentAnchor materialization pipeline proof; composes materialization, logical binding and placement.
- `Notes/F9R-D-Unity-ContentAnchor-Materialization-Scope-Release-Proof.md` — Unity ContentAnchor scope release proof; explicit cleanup by runtime owner.
- `Notes/F9R-E-Unity-ContentAnchor-Materialization-Bridge-Proof.md` — authored opt-in ContentAnchor materialization bridge proof; explicit scene-facing submit/release surface.
- `Notes/F9R-F-Unity-ContentAnchor-Materialization-Bridge-Set-Proof.md` — authored opt-in ContentAnchor bridge set proof; explicit batch submit/release over multiple bridges.
- `Notes/F9R-G-Unity-ContentAnchor-Materialization-Bridge-Set-Preflight-Proof.md` — bridge set batch preflight proof; invalid bridge or duplicate materialization key blocks before partial side effects.
- `Notes/F9R-H-Unity-ContentAnchor-Materialization-Authoring-Validation-Proof.md` — authoring validation proof for bridge and bridge set configuration.
- `Notes/F9R-I-Unity-ContentAnchor-Materialization-Runtime-Authoring-Gate-Proof.md` — runtime authoring gate proof; invalid authored set is blocked at runtime before materialization.
- `Notes/F9R-J-Unity-ContentAnchor-Materialization-Diagnostics-Snapshot-Proof.md` — diagnostics snapshot proof; query-only snapshot with no runtime side effects.
- `Notes/F9R-K-F9R-Closeout-Documentation-Sync.md` — F9R closeout and documentation sync.
- `Notes/F9R-L-Unity-ContentAnchor-Materialization-Bridge-Set-Rollback-Proof.md` — bridge set partial materialization rollback proof; Closed / PASS.
- `Notes/F9R-N-Lifecycle-Owned-Materialization-Registry-Contract-Proof.md` — lifecycle-owned materialization registry contract proof; Closed / PASS.
- `Notes/F9R-O-Bridge-Lifecycle-Registry-Registration-Proof.md` — explicit bridge lifecycle registry registration proof; Closed / PASS.
- `Notes/F9R-P-Lifecycle-Materialization-Registry-Release-Plan-Proof.md` — lifecycle materialization registry release plan proof; Closed / PASS.
- `Notes/F9R-T-QA-Canvas-Smoke-Button-Cleanup.md` — QA Canvas smoke button cleanup; obsolete/intermediate buttons removed from visible panel.
- `Notes/F9R-U-F9R-Closure-Next-Axis-Decision.md` — F9R closure and next-axis decision boundary; no new technical axis selected.
- `Notes/F10B-Pause-Visual-Surface-Authoring-Contract-Proof.md` — Pause visual surface authoring contract proof; Closed / PASS.
- `Notes/F10C-Pause-ContentAnchor-Binding-Request-Proof.md` — Pause ContentAnchor binding request proof; Closed / PASS, request-only derivation validated.
- `Notes/F10D-Pause-ContentAnchor-Binding-Execution-Proof.md` — Pause ContentAnchor binding execution proof; Ready for smoke, logical binding only.

### Setup

Documentos de setup do projeto consumidor.

### Notes
- [F9R-C — Unity ContentAnchor Materialization Pipeline Proof](Notes/F9R-C-Unity-ContentAnchor-Materialization-Pipeline-Proof.md)
- [F9R-D — Unity ContentAnchor Materialization Scope Release Proof](Notes/F9R-D-Unity-ContentAnchor-Materialization-Scope-Release-Proof.md)
- [F9R-E — Unity ContentAnchor Materialization Bridge Proof](Notes/F9R-E-Unity-ContentAnchor-Materialization-Bridge-Proof.md)
- [F9R-F — Unity ContentAnchor Materialization Bridge Set Proof](Notes/F9R-F-Unity-ContentAnchor-Materialization-Bridge-Set-Proof.md)
- [F9R-G — Unity ContentAnchor Materialization Bridge Set Preflight Proof](Notes/F9R-G-Unity-ContentAnchor-Materialization-Bridge-Set-Preflight-Proof.md)
- [F9R-H — Unity ContentAnchor Materialization Authoring Validation Proof](Notes/F9R-H-Unity-ContentAnchor-Materialization-Authoring-Validation-Proof.md)
- [F9R-I — Unity ContentAnchor Materialization Runtime Authoring Gate Proof](Notes/F9R-I-Unity-ContentAnchor-Materialization-Runtime-Authoring-Gate-Proof.md)
- [F9R-J — Unity ContentAnchor Materialization Diagnostics Snapshot Proof](Notes/F9R-J-Unity-ContentAnchor-Materialization-Diagnostics-Snapshot-Proof.md)
- [F9R-K — F9R Closeout / Documentation Sync](Notes/F9R-K-F9R-Closeout-Documentation-Sync.md)
- [F9R-L — Unity ContentAnchor Materialization Bridge Set Rollback Proof](Notes/F9R-L-Unity-ContentAnchor-Materialization-Bridge-Set-Rollback-Proof.md)
- [F9R-N — Lifecycle-Owned Materialization Registry Contract Proof](Notes/F9R-N-Lifecycle-Owned-Materialization-Registry-Contract-Proof.md)
- [F9R-O — Bridge Lifecycle Registry Registration Proof](Notes/F9R-O-Bridge-Lifecycle-Registry-Registration-Proof.md)
- [F9R-P — Lifecycle Materialization Registry Release Plan Proof](Notes/F9R-P-Lifecycle-Materialization-Registry-Release-Plan-Proof.md)
- [F9R-Q — Lifecycle Materialization Registry Release Execution Proof](Notes/F9R-Q-Lifecycle-Materialization-Registry-Release-Execution-Proof.md)
- [F9R-R — Route / Activity Exit Auto-Release Decision](Plans/F9R-R-PLAN-Route-Activity-Exit-Auto-Release-Decision.md)
- [F9R-S — Explicit Composite Lifecycle Release Executor Proof](Notes/F9R-S-Explicit-Composite-Lifecycle-Release-Executor-Proof.md)
- [F9R-T — QA Canvas Smoke Button Cleanup](Notes/F9R-T-QA-Canvas-Smoke-Button-Cleanup.md)
- [F9R-U — F9R Closure / Next Axis Decision](Notes/F9R-U-F9R-Closure-Next-Axis-Decision.md)
- [F10A — Pause ContentAnchor Consumer Re-entry Plan](Plans/F10A-PLAN-Pause-ContentAnchor-Consumer-Reentry.md)
- [F10B — Pause Visual Surface Authoring Contract Proof](Notes/F10B-Pause-Visual-Surface-Authoring-Contract-Proof.md)
- [F10C — Pause ContentAnchor Binding Request Proof](Notes/F10C-Pause-ContentAnchor-Binding-Request-Proof.md)
- [F10D — Pause ContentAnchor Binding Execution Proof](Notes/F10D-Pause-ContentAnchor-Binding-Execution-Proof.md)

Notas temporárias e achados de auditoria local.

Reset F25: `Notes/F25R-Activity-Scene-Operation-Architecture-Reset.md`.

Loading progress preparation: `Notes/F26C-Loading-Surface-Progress-Bar-Receiver.md`.

Determinate loading progress source: `Notes/F26D-Determinate-Loading-Progress-Source.md`.

Aggregated loading progress: `Notes/F26E-Aggregated-Loading-Progress.md`.

Loading progress closeout: `Notes/F26F-Loading-Progress-Polish-And-Closeout.md`.

F26F cleanup manifest: `Notes/F26F-DELETE-MANIFEST.txt`.

Pause UIGlobal surface baseline: `Notes/F27A-Pause-UIGlobal-Surface-Baseline.md`.
Pause input binding: `Notes/F27B-Pause-Input-Binding.md` (historical; superseded by F33B/F33C).
Gate / input capability audit: `Notes/F27C-Gate-Input-Capability-Audit.md`.
Pause capability Gate reframe: `Notes/F27D-Pause-Capability-Gate-Reframe.md`.

F27E cancelled / InputMode replan: `Notes/F27E-CANCELLED-Input-Consumers-Gate-Replan.md`.

F28 roadmap correction: `Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md`.

F28A baseline reconciliation: `Notes/F28A-Frozen-Baseline-Reconciliation.md`.

F28B completion dependency map: `Notes/F28B-Completion-Dependency-Map.md`.

F28C adapter module taxonomy: `Notes/F28C-Adapter-Module-Taxonomy.md`.

F28D player/actor/input ownership plan: `Notes/F28D-Player-Actor-Input-Ownership-Plan.md`.

F28E InputMode and Pause integration plan: `Notes/F28E-InputMode-Pause-Integration-Plan.md`.

F28F next implementation closeout: `Notes/F28F-Next-Implementation-Closeout.md`.

F29 Unity Input target ownership proof: `Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md`.

F29A Unity Input target declaration proof: `Notes/F29A-Unity-Input-Target-Declaration-Proof.md`.

F29B Input Target QA authoring fixture: `Notes/F29B-Input-Target-QA-Authoring-Fixture.md`.

F29C Input Target closeout: `Notes/F29C-Input-Target-Closeout.md`.

F30 InputMode identity/request result plan: `Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md`.

F30A InputMode identity/state/request result contracts: `Notes/F30A-InputMode-Identity-State-Request-Result.md`.

F30B Unity PlayerInput integration boundary correction: `Notes/F30B-Unity-PlayerInput-Integration-Boundary.md`.

F30C Unity PlayerInput component evidence validation: `Notes/F30C-Unity-PlayerInput-Component-Evidence-Validation.md`.

F30C1 PlayerInputManager smoke warning cleanup: `Notes/F30C1-PlayerInputManager-Smoke-Warning-Cleanup.md`.

F8R-B1 Runtime Root / Handle / Release Policy acceptance: `Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`.

F8R-C1 Runtime Materialization Adapter Boundary acceptance: `Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`.

F8R-D1 Physical Release Adapter acceptance: `Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md`.

F8R-E Unity Prefab Runtime Materialization Adapter Proof: `Notes/F8R-E-Unity-Prefab-Runtime-Materialization-Adapter-Proof.md`.

F9R-B Unity ContentAnchor Physical Placement Adapter Proof: `Notes/F9R-B-Unity-ContentAnchor-Physical-Placement-Adapter-Proof.md`.

F9R-C Unity ContentAnchor Materialization Pipeline Proof: `Notes/F9R-C-Unity-ContentAnchor-Materialization-Pipeline-Proof.md`.

F9R-D Unity ContentAnchor Materialization Scope Release Proof: `Notes/F9R-D-Unity-ContentAnchor-Materialization-Scope-Release-Proof.md`.

F9R-E Unity ContentAnchor Materialization Bridge Proof: `Notes/F9R-E-Unity-ContentAnchor-Materialization-Bridge-Proof.md`.

F9R-F Unity ContentAnchor Materialization Bridge Set Proof: `Notes/F9R-F-Unity-ContentAnchor-Materialization-Bridge-Set-Proof.md`.

F9R-G Unity ContentAnchor Materialization Bridge Set Preflight Proof: `Notes/F9R-G-Unity-ContentAnchor-Materialization-Bridge-Set-Preflight-Proof.md`.

F9R-H Unity ContentAnchor Materialization Authoring Validation Proof: `Notes/F9R-H-Unity-ContentAnchor-Materialization-Authoring-Validation-Proof.md`.

F9R-I Unity ContentAnchor Materialization Runtime Authoring Gate Proof: `Notes/F9R-I-Unity-ContentAnchor-Materialization-Runtime-Authoring-Gate-Proof.md`.

F9R-J Unity ContentAnchor Materialization Diagnostics Snapshot Proof: `Notes/F9R-J-Unity-ContentAnchor-Materialization-Diagnostics-Snapshot-Proof.md`.

F9R-K F9R Closeout / Documentation Sync: `Notes/F9R-K-F9R-Closeout-Documentation-Sync.md`.

F9R-L Unity ContentAnchor Materialization Bridge Set Rollback Proof: Closed / PASS, `Notes/F9R-L-Unity-ContentAnchor-Materialization-Bridge-Set-Rollback-Proof.md`.

F9R-M Lifecycle-Owned Materialization Registry Plan: Accepted / Plan / docs-only, `Plans/F9R-M-PLAN-Lifecycle-Owned-Materialization-Registry.md`.

F9R-N Lifecycle-Owned Materialization Registry Contract Proof: Closed / PASS, `Notes/F9R-N-Lifecycle-Owned-Materialization-Registry-Contract-Proof.md`.

F9R-O Bridge Lifecycle Registry Registration Proof: Closed / PASS, `Notes/F9R-O-Bridge-Lifecycle-Registry-Registration-Proof.md`.

F9R-P Lifecycle Materialization Registry Release Plan Proof: Closed / PASS, `Notes/F9R-P-Lifecycle-Materialization-Registry-Release-Plan-Proof.md`.

F9R-Q Lifecycle Materialization Registry Release Execution Proof: Closed / PASS, `Notes/F9R-Q-Lifecycle-Materialization-Registry-Release-Execution-Proof.md`.
F9R-R Route/Activity Exit Auto-Release Decision: Accepted / docs-only, `Plans/F9R-R-PLAN-Route-Activity-Exit-Auto-Release-Decision.md`.
F9R-S Explicit Composite Lifecycle Release Executor Proof: Closed / PASS, `Notes/F9R-S-Explicit-Composite-Lifecycle-Release-Executor-Proof.md`.
F9R-T QA Canvas Smoke Button Cleanup: Closed / PASS, `Notes/F9R-T-QA-Canvas-Smoke-Button-Cleanup.md`.

F9R-U F9R Closure / Next Axis Decision: Closed / docs-only, `Notes/F9R-U-F9R-Closure-Next-Axis-Decision.md`. F9R track closed; no next technical axis selected by this cut.

F10A Pause ContentAnchor Consumer Re-entry Plan: Accepted / Plan / docs-only, `Plans/F10A-PLAN-Pause-ContentAnchor-Consumer-Reentry.md`. Pause is selected as the next explicit consumer axis after F9R; no implementation is selected by F10A.

F10B Pause Visual Surface Authoring Contract Proof: Closed / PASS, `Notes/F10B-Pause-Visual-Surface-Authoring-Contract-Proof.md`. Validated passive Pause visual authoring contract and QA smoke; no materialization, input, timeScale or lifecycle auto-wiring.

F10C Pause ContentAnchor Binding Request Proof: Closed / PASS, `Notes/F10C-Pause-ContentAnchor-Binding-Request-Proof.md`. Validated request-only conversion from Pause visual surface contract to ContentAnchorBindingRequest, including canonical anchor owner; no binding execution, materialization, input, timeScale or lifecycle auto-wiring.

F10D Pause ContentAnchor Binding Execution Proof: Ready for smoke, `Notes/F10D-Pause-ContentAnchor-Binding-Execution-Proof.md`. Adds explicit logical binding execution for Pause visual surface contracts; still no visual materialization, input, timeScale or lifecycle auto-wiring.

## Regras

- Documentação viva do projeto fica em `Assets/_Documentation`.
- QA assets ficam em `Assets/ImmersiveFrameworkQA`.
- Assets de produto ficam em `Assets/_Project`.
- Experimentos ficam em `Assets/_Sandbox`.
- Ferramentas externas ficam em `Assets/_External`.
- Contratos/core genéricos do framework ficam em `Packages/com.immersive.framework`.
- Configuração singular de jogo/projeto consumidor fica em `Assets/_Project`.
- Testes Unity-facing novos devem preferir QA workspace isolado antes de tocar no QA baseline.



## F30D — Pause InputMode Request Boundary

F30D is closed as a passive runtime boundary plus QA smoke. It maps logical Pause `Running`/`Paused` state to `Gameplay`/`PauseOverlay` `InputModeRequest` values without owning Unity `PlayerInput`, `PlayerInputManager` or action-map switching.

Reference: `Notes/F30D-Pause-InputMode-Request-Boundary.md`.


## F31 — PlayerActor Identity

- `Assets/_Documentation/Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md`
- `Assets/_Documentation/Notes/F31A-PlayerActor-Identity-PlayerInput-Evidence.md`
- `Assets/_Documentation/Notes/F31B-Session-PlayerInputManager-Boundary.md`


## F30E — InputMode / Unity Input Boundary Closeout

F30 is closed. InputMode remains passive request/result language. Unity `PlayerInput` and `PlayerInputManager` remain official execution components. No action-map switching, join, player spawn or concrete input behavior is hidden in F30.

Reference: `Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md`.

## F31C — PlayerActor / Session Unity Input Reference Closeout

F31 is closed. The framework now has canonical references for later input work: `PlayerActor : IActor` with required `PlayerInput` evidence, and Session-scoped `PlayerInputManager` evidence.

Reference: `Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md`.

## F32 — InputMode Unity Adapter Application

F32 is the real continuation after F30E/F31C. `F31D — PlayerInput Reference Set` is cancelled and must not be applied or counted.

F32A adds a side-effect-free `InputModeUnityApplicationPreviewEvaluator`. It maps successful logical `InputModeRequestResult` values to already-closed evidence (`UnityInputTargetSet`, `PlayerActorSet`, Session `UnityInputPlayerInputManagerEvidence`) and reports whether a future Unity adapter could apply the requested mode.

Reference: `Plans/F32-PLAN-InputMode-Unity-Adapter-Application.md` and `Notes/F32A-InputMode-Unity-Application-Preview.md`.


- F32B — InputMode Unity Action Map Preview: `Assets/_Documentation/Notes/F32B-InputMode-Unity-Action-Map-Preview.md`.

- `Notes/F32C-InputMode-Unity-Application-Plan.md` — F32C dry-run Unity Input application plan.

- `Notes/F32D-InputMode-Unity-PlayerInput-Adapter.md` — F32D explicit Unity PlayerInput adapter; first allowed action-map side effect, no join/spawn/custom manager.

- `Notes/F32E-InputMode-Unity-PlayerInput-Application.md` — F32E explicit PlayerInput application wrapper; activates PlayerInput before selecting action maps and preserves no join/spawn/custom manager guardrails.

- F32F — InputMode Unity PlayerInput Request Application: composed explicit request-to-PlayerInput application path; no PlayerInputManager join/spawn/movement.

- `Notes/F32G-Pause-InputMode-Unity-PlayerInput-Application.md`

- `Notes/F32H-InputMode-Unity-PlayerInput-Application-Closeout.md` — F32H closeout; F32 closed, runtime wiring deferred to a later phase.

## F33 — Pause Runtime PlayerInput Wiring

F33 is closed through F33E. It adds the opt-in authored Pause input path: `PauseInputActionRuntimeBridgeTrigger` -> `PauseInputModeUnityPlayerInputRuntimeBridge` -> logical Pause request -> `InputMode` -> explicit Unity `PlayerInput` application. F33C retires the older direct `UnityPauseInputActionAdapter` as an active runtime path, and F33D flattens the trigger/bridge diagnostic strings.

References:

- `Plans/F33-PLAN-Pause-Runtime-PlayerInput-Wiring.md`
- `Notes/F33A-Pause-Runtime-PlayerInput-Bridge.md`
- `Notes/F33B-Pause-InputAction-Runtime-Bridge-Trigger.md`
- `Notes/F33C-Legacy-Pause-InputAction-Adapter-Retirement.md`
- `Notes/F33D-Pause-Input-Diagnostics-Flattening.md`
- `Notes/F33E-Pause-Runtime-PlayerInput-Wiring-Closeout.md`
- `Notes/F33E1-Next-Phase-Selection-Correction.md` — corrects F33E next-phase wording; F33 does not select the following implementation phase.
- `Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md` — closes the matrix reconciliation; F33 stays closed, but no F34/gameplay phase is selected.
- `Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md` — official reclassification of F28-F33 against the matrix; docs-only, no new implementation selected.

## POST-F33 — Matrix Reconciliation

POST-F33-A is accepted as documentation / roadmap governance only. F28-F33 are official as controlled anticipation of the Input / Pause / Unity `PlayerInput` axis, while RuntimeContent, ContentAnchor, materialization, runtime root, handles and release policy remain unresolved blockers before consumers.

References:

- `Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md`
- `Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`
- `Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
- `Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md` — audit-only RuntimeContent / ContentAnchor materialization state check; no implementation selected.
- `Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md` — accepted plan/ADR for logical runtime root, handle and release policy ownership; no physical materializer selected.
- `Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md` — accepts F8R-B ADR as logical ownership baseline; no implementation selected.
- `Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md` — accepted plan/ADR boundary between pure RuntimeContent core and future physical materialization adapters; no implementation selected.
- `Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md` — accepts F8R-C ADR as materialization adapter boundary baseline; no implementation selected.
- `Plans/F8R-D-PLAN-Physical-Release-Adapter.md` — accepted plan/ADR boundary for future physical release adapters; no implementation selected.
- `Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md` — accepts F8R-D ADR as physical release adapter boundary baseline; no implementation selected.
- `Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md` — draft plan/ADR re-entry for logical ContentAnchor runtime binding; no physical placement or implementation selected.
- `Plans/F9R-M-PLAN-Lifecycle-Owned-Materialization-Registry.md` — accepted plan for future lifecycle-owned materialization registry/release ownership; no implementation selected.

- `Assets/_Documentation/Notes/F9R-D-Unity-ContentAnchor-Materialization-Scope-Release-Proof.md` — implemented explicit scope release proof for materialized ContentAnchor content.
- `Assets/_Documentation/Notes/F9R-E-Unity-ContentAnchor-Materialization-Bridge-Proof.md` — implemented authored opt-in bridge over the validated materialization/binding/placement/release path.
- `Assets/_Documentation/Notes/F9R-F-Unity-ContentAnchor-Materialization-Bridge-Set-Proof.md` — implemented authored opt-in bridge set over multiple explicit bridges.
- `Assets/_Documentation/Notes/F9R-G-Unity-ContentAnchor-Materialization-Bridge-Set-Preflight-Proof.md` — implemented preflight-before-side-effects for bridge set materialization batches.
