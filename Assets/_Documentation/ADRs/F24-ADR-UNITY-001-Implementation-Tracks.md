# F24 ADR UNITY 001 - Implementation Tracks

## Status

Accepted

## Context

O framework ja possui um core sintetico funcional para boot, route, activity, content, runtime roots e smokes basicos.

A proxima etapa nao deve avancar diretamente para gameplay, player, camera, audio, pause visual ou loading visual sem antes separar os trilhos de implementacao.

A partir da etapa F24, a fonte operacional deste projeto e `Assets/`. Material fora de `Assets/` nao orienta este corte.

## Decision

O projeto passa a organizar implementacao em tres trilhos:

### 1. Framework Core / Contracts

Responsavel por contratos, estado, lifecycle e regras de framework.

Exemplos:

- Route
- Activity
- Runtime scope
- Content identity
- Content Anchor
- Transition contracts
- Loading contracts
- Pause contracts
- Snapshot/Save contracts
- Preferences contracts
- Diagnostics/facts

Regras:

- nao expor detalhes tecnicos desnecessarios para level/game designers;
- nao depender de objetos de cena por nome;
- nao usar path como chave funcional, salvo quando Unity scene path ainda for necessario para authoring/Build Settings;
- nao criar fallback silencioso para modulo obrigatorio ausente;
- nao capturar responsabilidade de consumer.

### 2. Unity Build Surface

Responsavel por componentes, assets e inspectors que tornam o framework usavel dentro da Unity.

Este trilho traduz contratos do framework para authoring compreensivel.

Exemplos futuros:

- Transition Curtain Surface
- Loading Screen Surface
- Pause Overlay Surface
- Save Moment Trigger
- Preference Setting
- Route/Activity authoring profiles
- validators e inspectors orientados a designer

Regras:

- campos publicos devem ter nomes compreensiveis para level/game designers;
- componentes devem declarar Owner, Intent, Requiredness/Policy, References, Runtime Preview e Authoring Validation;
- componentes Unity-facing nao devem criar lifecycle paralelo;
- editor tooling deve criar assets nos paths canonicos de `Assets/_Project`;
- QA assets devem permanecer em `Assets/ImmersiveFrameworkQA`.

### 3. Adapter Modules

Responsavel por integracoes especificas com sistemas concretos.

Exemplos futuros:

- Input adapter
- Camera adapter
- Audio adapter
- Player adapter
- Actor adapter
- Pooling adapter
- UI adapter

Regras:

- adapters consomem o framework, nao redefinem lifecycle;
- adapters nao devem ser necessarios para o core compilar/rodar;
- adapters opcionais nao devem virar dependencia obrigatoria do nucleo;
- adapters entram depois das contracts e Unity Build Surfaces minimas.

## Consequences

- Proximos cortes devem declarar explicitamente qual trilho estao alterando.
- Consumers nao entram antes de owner, identity, lifetime/release e authoring minimo.
- A documentacao e os planos vivos desta etapa ficam em `Assets/_Documentation`.
- `Assets/ImmersiveFrameworkQA` continua sendo superficie de QA, nao produto.
- `Assets/_Project` e a superficie de produto/projeto consumidor.
- Material fora de `Assets/` pode ser ignorado para esta etapa.

## Non-goals

Este ADR nao implementa:

- transition runtime visual;
- loading screen;
- pause overlay;
- player;
- camera;
- audio;
- save backend;
- gameplay adapters;
- pooling;
- runtime materializers novos.

## Validation

Este ADR e valido quando:

- existe em `Assets/_Documentation/ADRs`;
- o README de documentacao aponta para ele;
- o plano F24 referencia os tres trilhos;
- nenhum runtime foi alterado neste corte.
