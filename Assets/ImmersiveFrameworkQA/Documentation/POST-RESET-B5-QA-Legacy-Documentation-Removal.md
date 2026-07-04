# POST-RESET-B5 — QA Legacy Documentation Removal

## Objetivo

Auditar e remover a pasta legada `Assets/_Documentation` do Framework QA Project, mantendo no QA Project apenas documentacao operacional curta.

## Pasta auditada

```text
C:\Projetos\My project\Assets\_Documentation
```

Resultado da auditoria textual:

- 155 arquivos `.md`;
- 163 arquivos `.meta`;
- conteudo composto por ADRs, planos, auditorias, closeouts, notas, prompts e setup historicos;
- nenhum asset Unity serializado foi movido ou alterado;
- nenhum arquivo foi classificado como `unknown`;
- nenhum arquivo foi classificado como `keep-temporarily`.

## Arquivos removidos

Removido todo o conteudo de `Assets/_Documentation`, incluindo os 155 `.md`, seus `.meta` correspondentes, `.meta` de pastas e a pasta raiz `Assets/_Documentation.meta`.

Classificacao por arquivo Markdown removido:

| Arquivo | Classificacao | Motivo |
|---|---|---|
| `README.md` | `remove-candidate` / `obsolete` | Apontava para a antiga raiz documental do QA Project. |
| `ADRs/F24-ADR-UNITY-001-Implementation-Tracks.md` | `remove-candidate` / `canonical-duplicate` | Coberto por package `Documentation~/ADRs` e `Documentation~/History`. |
| `ADRs/F24-ADR-UNITY-002-Implementation-Workflow-And-QA-Workspace.md` | `remove-candidate` / `canonical-duplicate` | Coberto por package `Documentation~/Current/03-Consumer-Project-Roles.md`. |
| `ADRs/F24-ADR-UNITY-003-Project-And-Framework-Source-Boundary.md` | `remove-candidate` / `canonical-duplicate` | Coberto por package `Documentation~/Current/03-Consumer-Project-Roles.md`. |
| `ADRs/FXX-ADR-ARCH-0001-Architecture-Consolidation-Governance.md` | `remove-candidate` / `canonical-duplicate` | Governanca historica consolidada no package. |
| `ADRs/FXX-ADR-ARCH-0002-Route-Activity-Lifecycle-Operation-Kernel.md` | `remove-candidate` / `canonical-duplicate` | Lifecycle atual fica no package. |
| `ADRs/FXX-ADR-CONSOLIDATION-002-RuntimeContent-ContentAnchor-Materialization-Orchestration.md` | `remove-candidate` / `canonical-duplicate` | Materialization/ContentAnchor cobertos por ADRs do package. |
| `Architecture/F34-ADR-Architecture-Consolidation.md` | `remove-candidate` / `canonical-duplicate` | Arquitetura canonica fica no package. |
| `Architecture/F34-PLAN-Architecture-Consolidation.v1.md` | `remove-candidate` / `obsolete` | Plano historico, nao operacional para QA. |
| `Architecture/F34-TRACK-Architecture-Consolidation.md` | `remove-candidate` / `obsolete` | Tracker historico, substituido por docs atuais do package. |
| `Architecture/F35-ADR-Extension-Surface-Model.md` | `remove-candidate` / `canonical-duplicate` | Surface/adapter decisions ficam no package. |
| `Architecture/F36-AUDIT-Surface-Adapter-Inventory.md` | `remove-candidate` / `obsolete` | Auditoria historica, nao operacional para QA. |
| `Architecture/F37-ADR-Pause-InputMode-Apply-Boundary.md` | `remove-candidate` / `canonical-duplicate` | Pause/Input ADRs atuais ficam no package. |
| `Architecture/F39-ADR-Status-Mapping-Policy.md` | `remove-candidate` / `canonical-duplicate` | Politica historica coberta por package docs/ADRs. |
| `Architecture/F40-ADR-Loading-Surface-Adapter-Contract-Pattern.md` | `remove-candidate` / `canonical-duplicate` | Loading surface docs ficam no package. |
| `Architecture/F41-ADR-Pause-Visual-Consumer-Readiness.md` | `remove-candidate` / `canonical-duplicate` | Pause consumer readiness fica no package. |
| `Architecture/F42-ADR-FlowTrigger-Request-State-Helper.md` | `remove-candidate` / `canonical-duplicate` | GameFlow/FlowTrigger docs ficam no package. |
| `Architecture/F43-ADR-Loading-Runtime-Reference-Hardening.md` | `remove-candidate` / `canonical-duplicate` | Loading runtime docs ficam no package. |
| `Architecture/F44-ADR-Lifecycle-Operation-Evidence-Kernel.md` | `remove-candidate` / `canonical-duplicate` | Lifecycle evidence atual fica no package. |
| `Architecture/F45-ADR-Lifecycle-Content-Readiness-Evidence-Projection.md` | `remove-candidate` / `canonical-duplicate` | Lifecycle readiness atual fica no package. |
| `Architecture/F46-ADR-Lifecycle-Kernel-Readiness-Decision.md` | `remove-candidate` / `canonical-duplicate` | Lifecycle kernel atual fica no package. |
| `Architecture/F47-ADR-GameFlow-Request-Envelope-Boundary.md` | `remove-candidate` / `canonical-duplicate` | GameFlow docs ficam no package. |
| `Architecture/F48-ADR-GameFlow-Request-Envelope-Shell.md` | `remove-candidate` / `canonical-duplicate` | GameFlow docs ficam no package. |
| `Architecture/F49-ADR-GameFlow-Envelope-Runtime-Ownership-Trigger-Migration-Decision.md` | `remove-candidate` / `canonical-duplicate` | GameFlow docs ficam no package. |
| `Architecture/F50-ADR-Route-Activity-Trigger-Migration.md` | `remove-candidate` / `canonical-duplicate` | Route/Activity trigger docs ficam no package. |
| `Architecture/F51-ADR-Route-Activity-Trigger-FlowTrigger-Adoption.md` | `remove-candidate` / `canonical-duplicate` | FlowTrigger docs ficam no package. |
| `Architecture/F52-ADR-GameFlow-Request-API-Decision.md` | `remove-candidate` / `canonical-duplicate` | GameFlow API docs ficam no package. |
| `Architecture/F53-ADR-Architecture-Consolidation-Next-Track-Decision.md` | `remove-candidate` / `obsolete` | Track historico, nao operacional para QA. |
| `Architecture/F54-ADR-Transition-Surface-Effects-Contract.md` | `remove-candidate` / `canonical-duplicate` | Transition surface docs ficam no package. |
| `Architecture/F55-ADR-Transition-Runtime-Evidence-Hardening.md` | `remove-candidate` / `canonical-duplicate` | Transition evidence docs ficam no package. |
| `Architecture/F56-ADR-First-Practical-Flow-Transition-Authoring.md` | `remove-candidate` / `canonical-duplicate` | Authoring oficial fica no package. |
| `Architecture/F57-ADR-Framework-Authoring-Model-Boundary.md` | `remove-candidate` / `canonical-duplicate` | Authoring boundary fica no package. |
| `Architecture/F58-ADR-Minimal-Authoring-Validation-Project-Readiness.md` | `remove-candidate` / `canonical-duplicate` | Validade do projeto QA ja esta coberta pelo README QA e package roles. |
| `Architecture/F59-ADR-Git-Package-Readiness.md` | `remove-candidate` / `canonical-duplicate` | Readiness Git fica no package. |
| `Architecture/F60-ADR-Sync-Local-Framework-Package-Repository.md` | `remove-candidate` / `canonical-duplicate` | Sync de package fica no package. |
| `Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md` | `remove-candidate` / `canonical-duplicate` | Materialization audit consolidada por ADRs do package. |
| `Audits/F8R-F-Materialization-Release-Implementation-Readiness-Review.md` | `remove-candidate` / `obsolete` | Review historico, nao operacional para QA. |
| `Audits/FXX-AUDIT-Common-Internal-Mechanics-Repetition-Inventory.md` | `remove-candidate` / `obsolete` | Auditoria de arquitetura historica. |
| `Audits/FXX-AUDIT-General-Architecture-Pattern-Review.md` | `remove-candidate` / `obsolete` | Auditoria de arquitetura historica. |
| `Audits/FXX-AUDIT-LIFECYCLE-A-Route-Activity-Lifecycle-Sequence.md` | `remove-candidate` / `obsolete` | Auditoria historica, substituida por package docs. |
| `Audits/FXX-AUDIT-Participant-And-Flow-Pattern-Duplication.md` | `remove-candidate` / `obsolete` | Auditoria historica. |
| `Audits/FXX-AUDIT-RuntimeContent-ContentAnchor-Materialization-Orchestration.md` | `remove-candidate` / `obsolete` | Auditoria historica. |
| `Audits/README-FXX-Architecture-Pattern-Review.md` | `remove-candidate` / `obsolete` | Indice historico local. |
| `Closeouts/FXX-CLOSEOUT-ARCH-REALIGN-1-Architecture-Consolidation-Roadmap-Reconciliation.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-COMMON-B-Enum-Status-Validation-Helper.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-COMMON-C-Defensive-Copy-Empty-Collection-Helpers.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-COMMON-D-Issue-Counting-Helpers.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-COMMON-E-Common-Internal-Mechanics-Track-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-CONS-A-Participant-Common-Primitives-Alignment.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-CONS-B-Common-Participant-Executor.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-CONS-C-CycleReset-ParticipantExecutor-Pilot.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-CONS-D-ObjectReset-ParticipantExecutor-Pilot.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-CONS-F-Participant-Consolidation-Track-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-LIFECYCLE-C-Internal-Scope-Tail-Operation-Model-Shell.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-LIFECYCLE-C1-Scope-Tail-Cleanup-Exit-Ordering-Alignment.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-LIFECYCLE-D-Route-Scope-Tail-Pilot.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-LIFECYCLE-E-Activity-Scope-Tail-Pilot.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-LIFECYCLE-F-Scope-Tail-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-MAT-1-RuntimeContent-ContentAnchor-Materialization-Ownership.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-MAT-2-ContentAnchor-Binding-Cleanup-Ownership.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-MAT-3-Materialization-Release-Ownership-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-MAT-4-ContentAnchorMaterializationService-Extraction.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Closeouts/FXX-CLOSEOUT-PAUSE-1-Retired-Pause-Adapter-Cleanup.md` | `remove-candidate` / `obsolete` | Closeout historico; active QA operation remains in `UnityBuildSurface/README.md`. |
| `Closeouts/FXX-CLOSEOUT-PAUSE-2-Pause-InputMode-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/Capability-Traceability-Matrix.md` | `remove-candidate` / `canonical-duplicate` | Matriz consolidada por `Documentation~/Current` e `Documentation~/History`. |
| `Notes/F10B-Pause-Visual-Surface-Authoring-Contract-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F10C-Pause-ContentAnchor-Binding-Request-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F10D-Pause-ContentAnchor-Binding-Execution-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F10E-Pause-Visual-Materialization-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F10G-Pause-UIGlobal-Resident-Surface-Proof.md` | `remove-candidate` / `obsolete` | Prova historica; operacao atual no README da `UnityBuildSurface`. |
| `Notes/F10H-Pause-Logical-Toggle-Resident-Surface-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F28E-InputMode-Pause-Integration-Plan.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F28F-Next-Implementation-Closeout.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F29A-Unity-Input-Target-Declaration-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F29B-Input-Target-QA-Authoring-Fixture.md` | `remove-candidate` / `obsolete` | Fixture QA historico; estado operacional atual fica nos READMEs QA. |
| `Notes/F29C-Input-Target-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F30A-InputMode-Identity-State-Request-Result.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F30B-Unity-PlayerInput-Integration-Boundary.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F30C-Unity-PlayerInput-Component-Evidence-Validation.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F30C1-PlayerInputManager-Smoke-Warning-Cleanup.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F30D-Pause-InputMode-Request-Boundary.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F31A-PlayerActor-Identity-PlayerInput-Evidence.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F31B-Session-PlayerInputManager-Boundary.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F31B1-Session-PlayerInputManager-Smoke-Warning-Fix.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F32A-InputMode-Unity-Application-Preview.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32B-InputMode-Unity-Action-Map-Preview.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32C-InputMode-Unity-Application-Plan.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32D-InputMode-Unity-PlayerInput-Adapter.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32E-InputMode-Unity-PlayerInput-Application.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32F-InputMode-Unity-PlayerInput-Request-Application.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32G-Pause-InputMode-Unity-PlayerInput-Application.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F32H-InputMode-Unity-PlayerInput-Application-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F33A-Pause-Runtime-PlayerInput-Bridge.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F33B-Pause-InputAction-Runtime-Bridge-Trigger.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F33C-Legacy-Pause-InputAction-Adapter-Retirement.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F33D-Pause-Input-Diagnostics-Flattening.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F33E-Pause-Runtime-PlayerInput-Wiring-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F33E1-Next-Phase-Selection-Correction.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/F8R-E-Unity-Prefab-Runtime-Materialization-Adapter-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F8R-F-Materialization-Release-Readiness-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/F9R-B-Unity-ContentAnchor-Physical-Placement-Adapter-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-C-Unity-ContentAnchor-Materialization-Pipeline-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-D-Unity-ContentAnchor-Materialization-Scope-Release-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-E-Unity-ContentAnchor-Materialization-Bridge-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-F-Unity-ContentAnchor-Materialization-Bridge-Set-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-G-Unity-ContentAnchor-Materialization-Bridge-Set-Preflight-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-H-Unity-ContentAnchor-Materialization-Authoring-Validation-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-I-Unity-ContentAnchor-Materialization-Runtime-Authoring-Gate-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-J-Unity-ContentAnchor-Materialization-Diagnostics-Snapshot-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-K-F9R-Closeout-Documentation-Sync.md` | `remove-candidate` / `obsolete` | Sync historico. |
| `Notes/F9R-L-Unity-ContentAnchor-Materialization-Bridge-Set-Rollback-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-N-Lifecycle-Owned-Materialization-Registry-Contract-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-O-Bridge-Lifecycle-Registry-Registration-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-P-Lifecycle-Materialization-Registry-Release-Plan-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-Q-Lifecycle-Materialization-Registry-Release-Execution-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-S-Explicit-Composite-Lifecycle-Release-Executor-Proof.md` | `remove-candidate` / `obsolete` | Prova historica. |
| `Notes/F9R-T-QA-Canvas-Smoke-Button-Cleanup.md` | `remove-candidate` / `obsolete` | Ajuste QA historico; operacao local atual fica no README QA. |
| `Notes/F9R-U-F9R-Closure-Next-Axis-Decision.md` | `remove-candidate` / `obsolete` | Nota historica. |
| `Notes/Package-System-XRay-Consolidated.md` | `remove-candidate` / `obsolete` | XRay historico, nao operacional para QA. |
| `Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md` | `remove-candidate` / `obsolete` | Closeout historico. |
| `Notes/README.md` | `remove-candidate` / `obsolete` | Indice de notas legado. |
| `Plans/F10A-PLAN-Pause-ContentAnchor-Consumer-Reentry.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F10F-PLAN-Pause-Presentation-Model-Decision.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F24-PLAN-Unity-Build-Surface.md` | `remove-candidate` / `obsolete` | Plano historico; operacao atual no README da `UnityBuildSurface`. |
| `Plans/F25-PLAN-Activity-Content-Scene-Composition.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F26-PLAN-Activity-Discovery-And-Loading-Progress.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F27-PLAN-Pause-UIGlobal-And-Input.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F31-PLAN-PlayerActor-Identity-And-Unity-Input-Evidence.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F32-PLAN-InputMode-Unity-Adapter-Application.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F32-PLAN-InputMode-Unity-PlayerInput-Application.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F33-PLAN-Pause-Runtime-PlayerInput-Wiring.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F8R-D-PLAN-Physical-Release-Adapter.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F9R-M-PLAN-Lifecycle-Owned-Materialization-Registry.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/F9R-R-PLAN-Route-Activity-Exit-Auto-Release-Decision.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/FXX-PLAN-Architecture-Consolidation-Roadmap.md` | `remove-candidate` / `canonical-duplicate` | Roadmap atual fica no package. |
| `Plans/FXX-PLAN-General-Architecture-Pattern-Review.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/FXX-PLAN-General-Architecture-Pattern-Review.REVISED.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/FXX-PLAN-Participant-And-Flow-Pattern-Consolidation.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Plans/POST-F33-PLAN-Matrix-Reconciliation.md` | `remove-candidate` / `obsolete` | Plano historico. |
| `Prompts/CODEX-PROMPT-FXX-General-Architecture-Pattern-Audit.md` | `remove-candidate` / `obsolete` | Prompt historico, nao operacional para QA. |
| `Prompts/CODEX-PROMPT-FXX-Materialization-Orchestration-Audit.md` | `remove-candidate` / `obsolete` | Prompt historico, nao operacional para QA. |
| `Prompts/CODEX-PROMPT-FXX-Materialization-Orchestration-Audit.REVISED.md` | `remove-candidate` / `obsolete` | Prompt historico, nao operacional para QA. |

Todos os arquivos `.meta` sob `Assets/_Documentation` receberam a mesma classificacao do arquivo/pasta que acompanhavam: `remove-candidate` / `obsolete`.

## Arquivos migrados/consolidados

Nenhum arquivo foi migrado.

A informacao operacional realmente necessaria ja estava consolidada em:

- `Assets/ImmersiveFrameworkQA/README.md`;
- `Assets/ImmersiveFrameworkQA/UnityBuildSurface/README.md`.

## Arquivos mantidos temporariamente

Nenhum.

## Arquivos pendentes de decisão manual

Nenhum.

Referencia tecnica encontrada apos B5 e resolvida em B5A:

- `Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs` continha `DocumentationRootFolder = "Assets/_Documentation"`.
- B5A removeu a constante e as entradas de `InitialFolders` que poderiam recriar a pasta legada.
- A referencia ativa nao existe mais em `_Project`.

## Relação com a documentação canônica do package

A documentacao canonica do framework fica em:

```text
C:\Projetos\ImmersivePackages\com.immersive.framework\Documentation~
```

Evidencias usadas:

- `Documentation~/Current/03-Consumer-Project-Roles.md` define que o package possui docs oficiais, ADRs, roadmap, usage map e history;
- `Documentation~/Current/00-Current-State.md` define que projetos consumidores podem manter apenas READMEs locais de operacao;
- `Documentation~/History/000-INDEX.md` consolida historico;
- `Documentation~/ADRs/ADR-INDEX.md` concentra a navegacao canonica de ADRs.

## Validação textual

Validacao executada:

- inventario textual de `Assets/_Documentation`;
- comparacao textual com `com.immersive.framework/Documentation~`;
- busca inicial por referencias a `Assets/_Documentation`, `_Documentation`, `Documentation~`, `canonical` e `legacy documentation`.
- verificacao de existencia da pasta apos remocao: `REMOVED`;
- buscas finais em `C:\Projetos\My project\Assets` para `Assets/_Documentation`, `_Documentation`, `Documentation~`, `canonical` e `legacy documentation`;
- `git status --short`.

Classificacao dos hits finais:

| Busca | Resultado |
|---|---|
| `Assets/_Documentation` | Hits documentais aceitaveis em `README.md` e relatorios B4/B5/B5A; nenhum hit ativo em `_Project`. |
| `_Documentation` | Mesma classificacao acima; a pasta fisica foi removida e o setup nao a recria. |
| `Documentation~` | Hits validos apontando para documentacao canonica do package. |
| `canonical` | Hits validos em relatorio, README, cenas QA e comentarios de adapters QA. |
| `legacy documentation` | Hits validos em B5 e no README da `UnityBuildSurface`. |

Unity import, compile, build, playmode, smoke e batchmode nao foram executados.

## Próximo passo

Executar `POST-RESET-B6 - QA Asset Migration To ImmersiveFrameworkQA` para tratar `Assets/_Project` e assets serializados via Unity Editor, sem recriar `Assets/_Documentation`.

## Follow-up B5A — Setup Reference Cleanup

Arquivos auditados:

- `Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs`;
- `Assets/_Project/Scripts/Editor/Project.Editor.asmdef`;
- `Assets/_Project/Scripts/Runtime/Project.Runtime.asmdef`;
- `Assets/ImmersiveFrameworkQA/README.md`;
- `Assets/ImmersiveFrameworkQA/Documentation/POST-RESET-B5-QA-Legacy-Documentation-Removal.md`.

Referencias removidas:

- removida a constante `DocumentationRootFolder = "Assets/_Documentation"`;
- removidas de `InitialFolders` as entradas `Assets/_Documentation`, `Assets/_Documentation/ADRs`, `Assets/_Documentation/Notes` e `Assets/_Documentation/Setup`.

Referencias mantidas:

- referencias documentais em relatorios B4/B5/B5A explicando a remocao;
- referencias a `Documentation~` como ponte para a documentacao canonica do package.

Motivo:

- `ImmersiveInitialProjectSetup.CreateFolderStructure()` usava `ProjectFolders.EnsureFolder(...)` para todos os itens de `InitialFolders`;
- manter `Assets/_Documentation` nessa lista permitiria recriar a pasta legada removida em B5;
- o QA Project agora documenta operacao local em `Assets/ImmersiveFrameworkQA/Documentation`.

Validacao textual:

- `rg -n "Assets/_Documentation" "C:\Projetos\My project\Assets\_Project"` nao encontrou hits apos a correcao;
- buscas finais em `C:\Projetos\My project\Assets` mantem `Assets/_Documentation` apenas em relatorios/README historicos de remocao;
- `Documentation~` aparece apenas como referencia documental ao package.
