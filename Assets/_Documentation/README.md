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
- `Plans/F32-PLAN-InputMode-Unity-Adapter-Application.md` — InputMode Unity Adapter Application / open

### Setup

Documentos de setup do projeto consumidor.

### Notes

Notas temporárias e achados de auditoria local.

Reset F25: `Notes/F25R-Activity-Scene-Operation-Architecture-Reset.md`.

Loading progress preparation: `Notes/F26C-Loading-Surface-Progress-Bar-Receiver.md`.

Determinate loading progress source: `Notes/F26D-Determinate-Loading-Progress-Source.md`.

Aggregated loading progress: `Notes/F26E-Aggregated-Loading-Progress.md`.

Loading progress closeout: `Notes/F26F-Loading-Progress-Polish-And-Closeout.md`.

F26F cleanup manifest: `Notes/F26F-DELETE-MANIFEST.txt`.

Pause UIGlobal surface baseline: `Notes/F27A-Pause-UIGlobal-Surface-Baseline.md`.
Pause input binding: `Notes/F27B-Pause-Input-Binding.md`.
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
