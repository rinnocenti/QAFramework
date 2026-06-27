# F24 ADR UNITY 002 - Implementation Workflow and QA Workspace

## Status

Accepted

## Context

A etapa F24 mudou o foco de core sintetico para surfaces Unity-facing.

Isso aumenta o risco de misturar:
- core/framework generico;
- configuracao singular do projeto consumidor;
- QA baseline;
- assets experimentais;
- adapters avancados.

Tambem ha uma decisao operacional nova: nem todo corte deve ir para Codex. Cortes documentais e cortes complexos com 3 ou mais modulos continuam adequados para Codex. Cortes simples, primitivos e pequenas criacoes podem ser feitos diretamente no chat.

## Decision

### Workflow de implementacao

Usar Codex para:

- documentacao maior;
- cortes complexos;
- cortes que coordenam 3 ou mais modulos;
- migracoes com muitos arquivos;
- edicoes que exigem checagem ampla de referencias.

Usar o chat para:

- cortes simples;
- primitivos;
- criacoes pequenas;
- ajustes documentais pequenos;
- analise e decisao arquitetural antes de implementar.

Se um corte iniciado como simples passar a tocar 3 ou mais modulos, ele deve ser reclassificado como corte Codex.

### Workspace QA de Unity Build Surface

Criar e usar um workspace isolado:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  README.md
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
```

Esse workspace e destinado a validar os novos elementos Unity-facing de F24:

- Transition surfaces;
- Loading surfaces;
- Pause surfaces;
- Save Moment authoring;
- Preferences authoring;
- futuros exemplos de inspector para designers.

### Separacao de ownership

- Coisa singular de jogo fica em `Assets/_Project`.
- QA do framework fica em `Assets/ImmersiveFrameworkQA`.
- Experimento descartavel fica em `Assets/_Sandbox`.
- Ferramenta externa fica em `Assets/_External`.
- Componente generico, contrato, surface reutilizavel ou adapter avancado pode entrar no framework.
- Adapter opcional nao deve virar requisito do core.

## Consequences

- As cenas baseline de QA nao devem ser contaminadas com cada novo teste visual.
- F24B/F24C/F24D/F24E/F24F/F24G devem preferir validar no workspace Unity Build Surface.
- Novos assets de teste devem ser explicitamente classificados como QA, projeto ou sandbox.
- O framework so deve receber o que for generico, reutilizavel e coerente com os trilhos aceitos.
- A documentacao viva deve refletir quando um corte e feito no chat ou enviado para Codex.

## Non-goals

Este ADR nao implementa:

- runtime novo;
- transition visual;
- loading screen;
- pause overlay;
- save backend;
- preferences runtime;
- player/camera/audio/gameplay adapters;
- cenas finais de produto.

## Validation

Este ADR e valido quando:

- existe em `Assets/_Documentation/ADRs`;
- o plano F24 referencia este ADR;
- o workspace QA de Unity Build Surface existe;
- o README do workspace explica sua finalidade;
- nenhum runtime foi alterado para aceitar esta decisao.
