# ADR-0001 — Bootstrap mínimo e construção incremental do Immersive Framework

## Status

Accepted

## Contexto

O projeto **Immersive Framework** será desenvolvido como package Unity:

```text
com.immersive.framework
```

O framework será usado como base para lifecycles de jogos em Unity 6.5.

Já existem três packages técnicos versionados e congelados:

```text
com.immersive.foundation
com.immersive.logging
com.immersive.pooling
```

Esses packages são infraestrutura técnica genérica. Eles não devem conter lifecycle de jogo, Session, Route, Activity, Actor, Input, Camera, Save ou diagnostics específicos do framework.

A Base 2.0 / `NewScripts` é uma referência arquitetural e uma fonte de problemas aprendidos, mas não deve ser copiada como produto novo.

Problemas observados na Base 2.0 que o novo framework deve evitar:

- duplicação de informação;
- muitas etapas manuais para configurar comportamentos simples;
- nomes técnicos demais para o usuário do framework;
- conceitos úteis no código, mas ruins no Inspector;
- configuração espalhada entre assets e objetos;
- fallbacks silenciosos;
- managers, registries e coordinators virando owners reais de lifecycle;
- pipelines e stages grandes demais;
- serviços globais difíceis de rastrear;
- dificuldade para entender quem é o owner real de cada decisão.

## Decisão

O `com.immersive.framework` será iniciado com um **bootstrap mínimo e incremental**.

O framework **não deve nascer com o esqueleto completo da arquitetura final**.
Devem ser criados apenas os conceitos necessários para o corte atual, expandindo o package conforme necessidades reais aparecerem.

A raiz pública de configuração será o conceito:

```text
Game Application
```

O `Game Application` será o asset público principal do framework. Ele representa a aplicação/jogo que será inicializado pelo framework.

O Project Settings do Unity deve expor uma seção:

```text
Immersive Framework
```

Essa seção deve apontar para o `Game Application` ativo.

O bootstrap será interno ao framework. Ele pode existir como código runtime, mas não deve ser o principal conceito público no Inspector.

O conceito público para controlar severidade de validação será:

```text
Validation Mode
```

Não serão expostos no fluxo principal de authoring termos internos como:

```text
Bootstrap
Composition
Pipeline
Stage
Command
Fact
Snapshot
Registry
Installer
Host
Runtime Config Set
Descriptor
```

Esses termos podem existir internamente se forem necessários, mas não devem ser exigidos para configurar o framework no Inspector.

## Bootstrap mínimo

O bootstrap inicial deve ser responsável apenas por:

1. resolver o `Game Application` ativo;
2. validar a configuração mínima;
3. aplicar logging mínimo necessário;
4. criar um contexto runtime mínimo;
5. produzir diagnostics básicos de boot;
6. iniciar o primeiro owner real de lifecycle, quando esse owner existir.

O bootstrap não deve decidir diretamente:

- carregamento de cena;
- lifecycle de route;
- lifecycle de activity;
- lifecycle de actor;
- input;
- camera;
- save;
- lifetime de pools;
- pause;
- gameplay.

Essas decisões devem pertencer a owners específicos, criados somente quando suas responsabilidades forem necessárias.

## Conteúdo mínimo inicial

O primeiro corte do framework deve conter apenas o necessário para provar o bootstrap:

```text
Immersive Framework Settings
Game Application
Validation Mode
Bootstrap interno
Boot Result / Boot Diagnostics mínimo
Runtime Context mínimo
```

Ainda não devem entrar no primeiro corte, salvo necessidade concreta:

```text
Scene Lifecycle
Game Flow
Route
Activity
Actor
Input
Camera
Save
Pooling integration
Diagnostics avançado
Module graph completo
Validators completos
```

## Regra de expansão

O framework deve seguir a regra:

```text
Criar apenas o que será usado no corte atual.
Expandir somente quando uma necessidade real aparecer.
```

Não devem ser criados antecipadamente:

- module graph completo;
- registries genéricos;
- descriptors extensíveis;
- pipelines vazios;
- adapters sem uso;
- hosts globais;
- managers preventivos;
- assets “guarda-chuva” com configuração de domínios ainda inexistentes.

Interfaces, registries, descriptors e adapters só devem nascer quando houver uma necessidade concreta ou pelo menos dois usos reais que justifiquem a abstração.

## UX pública

A UX do framework deve ser pensada para usuários Unity/Inspector, não apenas para programadores.

Os nomes públicos devem ser compreensíveis:

| Conceito público | Papel |
|---|---|
| `Game Application` | asset raiz do jogo/aplicação |
| `Immersive Framework Settings` | configuração de projeto que aponta para a aplicação ativa |
| `Validation Mode` | modo de severidade de validação |
| `Startup Route` | primeira rota do jogo, quando Route existir |
| `Game Flow` | fluxo macro do jogo, quando existir |
| `Activity Flow` | fluxo de activities, quando existir |
| `Diagnostics` | informações de validação e boot |

Termos técnicos internos não devem aparecer no fluxo principal de configuração.

## Relação com packages técnicos

`com.immersive.framework` pode depender dos packages técnicos existentes, mas não deve deslocar responsabilidades de lifecycle para eles.

### Foundation

Pode fornecer:

```text
Validation
Events
Fsm
```

Não deve conter:

```text
Game lifecycle
Route
Activity
Actor
Input
Camera
Save
Framework diagnostics
```

### Logging

Pode fornecer:

```text
Logger
Policies
Formatter
Unity console sink
```

Não deve decidir lifecycle nem diagnostics específicos do framework.

### Pooling

Pode fornecer:

```text
IPoolable
PoolableBehaviour
GameObjectPool
PoolReturnHandle
```

Não deve decidir escopo de pool, lifetime de route, lifetime de activity ou política de destruição/materialização.

O framework pode futuramente criar integração de pooling, mas o owner do lifetime deve ser framework-side, não package-side.

## Validation Mode

`Validation Mode` será o conceito público para controlar severidade e comportamento de validação.

Modos iniciais previstos:

```text
Strict
Standard
Release
```

A diferença entre os modos pode afetar:

- volume de logs;
- severidade de warnings;
- diagnostics editor-only;
- relatórios de validação.

Mas nenhum modo deve permitir fallback silencioso para configuração obrigatória ausente.

Configuração obrigatória ausente deve continuar sendo erro.

## Princípios herdados da Base 2.0

A Base 2.0 deve ser usada como referência conceitual para:

- fail fast;
- ausência de fallback silencioso;
- owners explícitos;
- separação entre authoring e runtime;
- validação clara;
- diagnostics rastreáveis;
- policies classificando decisão em vez de executar lifecycle;
- registries como índices, não owners;
- side-effects isolados em owners específicos.

A Base 2.0 não deve ser copiada em:

- nomes públicos;
- estrutura de skeleton;
- quantidade de stages/pipelines;
- composition root global;
- service registry amplo;
- dependency manager como padrão de consumo;
- runtime config set central;
- exposição de termos técnicos no Inspector;
- managers/coordinators acumulando lifecycle;
- bridges e fallbacks para cobrir fronteiras mal definidas.

## Consequências

Essa decisão permite que o framework nasça pequeno e compreensível.

Benefícios esperados:

- menor superfície inicial;
- menos configuração manual;
- Inspector mais claro;
- menor chance de duplicação;
- menor risco de managers globais;
- owners de lifecycle mais fáceis de rastrear;
- evolução mais barata durante a fase de desenvolvimento.

Custos aceitos:

- a arquitetura ainda poderá mudar nos primeiros cortes;
- alguns conceitos serão criados depois, não agora;
- nem toda extensão futura estará prevista no skeleton inicial;
- refactors são esperados durante a fase de desenvolvimento.

## Critério de aceitação

O ADR é considerado respeitado se o primeiro skeleton do package:

- não criar o framework completo antecipadamente;
- não expor pipeline/stage/command/fact/snapshot no Inspector;
- usar `Game Application` como raiz pública;
- usar `Immersive Framework Settings` como ponto de configuração do projeto;
- usar `Validation Mode` como conceito público;
- mantiver o bootstrap como detalhe interno;
- não copiar a estrutura da Base 2.0;
- criar apenas o necessário para o corte atual;
- preservar fail fast para configuração obrigatória ausente;
- não introduzir fallback silencioso.
