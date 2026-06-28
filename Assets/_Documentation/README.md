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

### Setup

Documentos de setup do projeto consumidor.

### Notes

Notas temporárias e achados de auditoria local.

Reset F25: `Notes/F25R-Activity-Scene-Operation-Architecture-Reset.md`.

Loading progress preparation: `Notes/F26C-Loading-Surface-Progress-Bar-Receiver.md`.

Determinate loading progress source: `Notes/F26D-Determinate-Loading-Progress-Source.md`.

## Regras

- Documentação viva do projeto fica em `Assets/_Documentation`.
- QA assets ficam em `Assets/ImmersiveFrameworkQA`.
- Assets de produto ficam em `Assets/_Project`.
- Experimentos ficam em `Assets/_Sandbox`.
- Ferramentas externas ficam em `Assets/_External`.
- Contratos/core genéricos do framework ficam em `Packages/com.immersive.framework`.
- Configuração singular de jogo/projeto consumidor fica em `Assets/_Project`.
- Testes Unity-facing novos devem preferir QA workspace isolado antes de tocar no QA baseline.
