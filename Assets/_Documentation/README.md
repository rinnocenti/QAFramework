# Immersive Framework - Project Documentation

Esta pasta contem a documentacao viva da etapa Unity-facing do projeto.

A partir de F24, a fonte operacional deste projeto e `Assets/`.

## Indice

### ADRs

- `ADRs/F24-ADR-UNITY-001-Implementation-Tracks.md`
- `ADRs/F24-ADR-UNITY-002-Implementation-Workflow-And-QA-Workspace.md`

### Plans

- `Plans/F24-PLAN-Unity-Build-Surface.md`

### Setup

Documentos de setup do projeto consumidor.

### Notes

Notas temporarias e achados de auditoria local.

## Regras

- Documentacao viva do projeto fica em `Assets/_Documentation`.
- QA assets ficam em `Assets/ImmersiveFrameworkQA`.
- Assets de produto ficam em `Assets/_Project`.
- Experimentos ficam em `Assets/_Sandbox`.
- Ferramentas externas ficam em `Assets/_External`.
- Material fora de `Assets/` nao orienta esta etapa.
- Prompts para Codex ficam reservados para documentacao e cortes complexos com coordenacao de 3 ou mais modulos.
- Cortes simples, primitivos, criacoes pequenas e atualizacoes documentais pequenas podem ser tratados diretamente no chat.
- Novos elementos Unity Build Surface devem ter QA/workspace proprio antes de contaminar as cenas baseline.
