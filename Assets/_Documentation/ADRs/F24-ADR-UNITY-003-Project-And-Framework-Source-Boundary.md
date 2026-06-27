# F24 ADR UNITY 003 — Project and Framework Source Boundary

## Status

Accepted

## Context

Durante F24B, foi detectado que a regra “usar somente `Assets/` como fonte operacional” bloqueia cortes de Framework Core / Contracts.

A implementação real de Route, Activity, diagnostics, runtime host e contratos do framework está em `Packages/com.immersive.framework`, enquanto `Assets/` contém cenas, QA, documentação viva, assets de projeto e scripts editor/project-specific.

Criar contratos de core em `Assets/_Project/Scripts/Runtime` deixaria esses contratos desconectados do fluxo real do framework. Isso produziria uma falsa implementação.

## Decision

A fonte operacional passa a ser definida por fronteira:

### Assets

Usar `Assets/` para:

- cenas;
- assets de QA;
- assets de teste Unity-facing;
- documentação viva;
- configurações do projeto consumidor;
- scripts singulares do projeto;
- editor tools específicos do projeto consumidor;
- materiais, prefabs, sprites e ScriptableObjects de teste.

### Framework package

Usar `Packages/com.immersive.framework/` para:

- contratos genéricos do framework;
- lifecycle;
- Route/Activity/GameFlow;
- runtime state;
- diagnostics/facts;
- validators genéricos do framework;
- authoring genérico do framework;
- Unity-facing surfaces reutilizáveis como parte do framework.

### Project-specific assets

Usar `Assets/_Project` para:

- configuração singular de jogo;
- assets de produto;
- scripts que não devem virar API genérica;
- authoring de um jogo específico.

### QA assets

Usar `Assets/ImmersiveFrameworkQA` para:

- smokes manuais;
- cenas de teste do framework;
- assets de validação;
- workspace isolado de Unity Build Surface.

### Adapter modules

Adapters reutilizáveis podem ficar em:

- `Packages/com.immersive.framework` quando forem parte do framework;
- packages separados futuros quando tiverem dependências próprias fortes.

## Consequences

- F24B pode editar `Packages/com.immersive.framework`, porque Transition Contract Wiring pertence ao trilho Framework Core / Contracts.
- F24C pode editar `Packages/com.immersive.framework` para a surface genérica e `Assets/ImmersiveFrameworkQA` para a cena/asset de teste.
- Configurações singulares de um jogo não devem ser promovidas para o framework.
- Testes novos de Unity Build Surface devem usar workspace isolado antes de reaproveitar cenas antigas de QA baseline.
- Prompts para Codex devem ser usados principalmente para documentação ou cortes complexos com três ou mais módulos.
- Cortes simples, primitivos, criações pequenas e ajustes documentais pequenos podem ser feitos diretamente no chat.

## Non-goals

Este ADR não implementa:

- Transition Contract;
- Transition Surface;
- cenas novas de transition;
- loading;
- pause;
- save;
- adapters.

## Validation

Este ADR é válido quando:

- o plano F24 remove a restrição global “somente `Assets/`” para cortes de core;
- F24B é replanejado com permissão explícita para editar `Packages/com.immersive.framework`;
- novos assets de teste de transition são planejados dentro de `Assets/ImmersiveFrameworkQA/UnityBuildSurface`.
