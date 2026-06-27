# F24 ADR UNITY 001 — Implementation Tracks

## Status

Accepted

## Context

O framework já possui um core sintético funcional para boot, route, activity, content, runtime roots e smokes básicos.

A próxima etapa não deve avançar diretamente para gameplay, player, camera, audio, pause visual ou loading visual sem antes separar os trilhos de implementação.

A regra inicial de “somente `Assets/`” foi corrigida: ela vale para assets, cenas, QA, documentação viva e configurações do projeto consumidor. O framework core real continua em `Packages/com.immersive.framework` e deve ser editado quando o corte pertencer ao trilho Framework Core / Contracts.

## Decision

O projeto passa a organizar implementação em três trilhos:

### 1. Framework Core / Contracts

Responsável por contratos, estado, lifecycle e regras de framework.

Fonte operacional primária deste trilho:

- `Packages/com.immersive.framework/`

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

- não expor detalhes técnicos desnecessários para level/game designers;
- não depender de objetos de cena por nome;
- não usar path como chave funcional, salvo quando Unity scene path ainda for necessário para authoring/Build Settings;
- não criar fallback silencioso para módulo obrigatório ausente;
- não capturar responsabilidade de consumer.

### 2. Unity Build Surface

Responsável por componentes, assets e inspectors que tornam o framework usável dentro da Unity.

Fonte operacional primária deste trilho:

- `Assets/` para cenas, QA, assets de teste, configurações do projeto consumidor e documentação viva;
- `Packages/com.immersive.framework/` quando a surface for genérica e reutilizável pelo framework.

Este trilho traduz contratos do framework para authoring compreensível.

Exemplos futuros:

- Transition Curtain Surface
- Loading Screen Surface
- Pause Overlay Surface
- Save Moment Trigger
- Preference Setting
- Route/Activity authoring profiles
- validators e inspectors orientados a designer

Regras:

- campos públicos devem ter nomes compreensíveis para level/game designers;
- componentes devem declarar Owner, Intent, Requiredness/Policy, References, Runtime Preview e Authoring Validation;
- componentes Unity-facing não devem criar lifecycle paralelo;
- editor tooling deve criar assets nos paths canônicos de `Assets/_Project` ou `Assets/ImmersiveFrameworkQA`, conforme o objetivo;
- QA assets devem permanecer em `Assets/ImmersiveFrameworkQA`.

### 3. Adapter Modules

Responsável por integrações específicas com sistemas concretos.

Fonte operacional primária deste trilho:

- `Packages/com.immersive.framework/` para adapters genéricos e reutilizáveis;
- packages próprios futuros quando o adapter justificar isolamento;
- `Assets/_Project` apenas para configuração singular do jogo/projeto consumidor.

Exemplos futuros:

- Input adapter
- Camera adapter
- Audio adapter
- Player adapter
- Actor adapter
- Pooling adapter
- UI adapter

Regras:

- adapters consomem o framework, não redefinem lifecycle;
- adapters não devem ser necessários para o core compilar/rodar;
- adapters opcionais não devem virar dependência obrigatória do núcleo;
- adapters entram depois das contracts e Unity Build Surfaces mínimas.

## Consequences

- Próximos cortes devem declarar explicitamente qual trilho estão alterando.
- Próximos cortes devem declarar explicitamente a fonte editável: `Assets`, `Packages/com.immersive.framework` ou ambos.
- Consumers não entram antes de owner, identity, lifetime/release e authoring mínimo.
- A documentação e os planos vivos desta etapa ficam em `Assets/_Documentation`.
- `Assets/ImmersiveFrameworkQA` continua sendo superfície de QA, não produto.
- `Assets/_Project` é a superfície de produto/projeto consumidor.
- `Packages/com.immersive.framework` é o local do core/framework genérico.
- Material fora dessas fronteiras não deve orientar uma etapa, salvo quando o corte declarar uma exceção explícita.

## Non-goals

Este ADR não implementa:

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

Este ADR é válido quando:

- existe em `Assets/_Documentation/ADRs`;
- o README de documentação aponta para ele;
- o plano F24 referencia os três trilhos;
- o plano F24 não impede edição de `Packages/com.immersive.framework` para cortes Framework Core / Contracts.
