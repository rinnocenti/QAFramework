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

## Work mode

- Codex deve ser usado para documentacao, cortes complexos e cortes com tres ou mais modulos.
- Cortes simples, primitivos e criacoes pequenas podem ser resolvidos diretamente no chat.
- Novos elementos Unity-facing devem ter cenas/assets de QA proprios quando necessario.
- Configuracoes singulares de jogo ficam em `Assets/_Project`.
- Elementos genericos, reutilizaveis ou adapters avancados podem entrar no framework ou em packages de adapter.
