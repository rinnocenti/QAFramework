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

## Regras

- Documentação viva do projeto fica em `Assets/_Documentation`.
- QA assets ficam em `Assets/ImmersiveFrameworkQA`.
- Assets de produto ficam em `Assets/_Project`.
- Experimentos ficam em `Assets/_Sandbox`.
- Ferramentas externas ficam em `Assets/_External`.
- Contratos/core genéricos do framework ficam em `Packages/com.immersive.framework`.
- Configuração singular de jogo/projeto consumidor fica em `Assets/_Project`.
- Testes Unity-facing novos devem preferir QA workspace isolado antes de tocar no QA baseline.
