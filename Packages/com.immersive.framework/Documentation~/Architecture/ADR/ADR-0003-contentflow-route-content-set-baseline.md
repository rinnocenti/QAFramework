# ADR-0003 — ContentFlow core e Route Content Set baseline

## Status

Accepted

## Contexto

O framework chegou a um limite ao tentar avançar subsistemas como câmera sobre o modelo atual de conteúdo. `ActivityContentBinding` usa `SetActive` em GameObjects de cena e isso mistura identificação, ativação visual, lifecycle e contribuição para subsistemas.

A auditoria do `NewScripts` mostrou que não existia uma única forma de materialização. Havia pelo menos estes padrões: runtime persistente de sessão, composição de cenas de rota, content profiles de Activity, discovery de contributors scene-authored, materialização física runtime em slots, surface/slot contribution e runtime spawn/pool.

A melhoria desejada não é copiar a pipeline antiga, mas centralizar a linguagem de materialização para que Route, Activity e subsistemas usem handles, scopes e contribution sets explícitos.

## Decisão

Criar o módulo `Runtime/ContentFlow` com uma base comum mínima:

```text
FrameworkContentScope
FrameworkContentKind
FrameworkContentRequiredness
FrameworkContentHandle
FrameworkContentSet
IFrameworkContentMaterializer
IFrameworkContentContribution
FrameworkContentContributionMarker
```

A primeira aplicação concreta é Route, não Activity. A Route materializa o contexto onde Activities depois contribuem conteúdo.

O `RouteLifecycleRuntime` passa a registrar a Primary Scene carregada como um handle de conteúdo:

```text
scope = Route
kind = Scene
requiredness = Required
owner = Route
resource = Primary Scene
```

Esse registro fica em `RouteContentSet`, sem mudar ainda o carregamento real da cena. O `SceneLifecycleRuntime` continua carregando a Primary Scene como antes.

## Fronteiras

Este corte pode:

- criar a linguagem comum de content scope/kind/handle/set;
- registrar a primary scene ativa da Route como conteúdo materializado;
- expor diagnóstico de Route Content Set em logs de Route lifecycle;
- preparar o caminho para composição de cenas de rota.

Este corte não deve:

- criar câmera;
- criar áudio;
- criar actors;
- criar pause/presentation;
- implementar Addressables;
- implementar additive scene composition;
- transformar `ActivityContentBinding` em trilho canônico de materialização;
- criar fallback silencioso para conteúdo obrigatório.

## Relação com ActivityContentBinding

`ActivityContentBinding` continua existindo como adapter simples de visibilidade local. Ele serve para conteúdo pequeno já presente na cena.

Ele não é a materialização canônica para subsistemas estruturais como camera, audio, actor, pause, presentation ou content streaming.

## Consequências

Benefícios:

- Route passa a ter um `ContentSet` rastreável;
- logs deixam claro que a Primary Scene é conteúdo de Route;
- futuros materializers podem produzir handles homogêneos;
- subsistemas futuros poderão consumir contributions em vez de depender de GameObjects ativados por efeito colateral.

Custos aceitos:

- ainda não existe composição additive de cenas;
- ainda não existe release policy avançada;
- ainda não existe discovery real de contributions;
- ainda não há ActivityContentSet.

## Próximos passos congelados

A evolução anterior estava route-heavy e foi congelada. A próxima frente deve voltar para uma leitura top-down dos escopos:

```text
Session
Route
Activity
Local
```

A composição additive de cenas de Route é deferida até existir uma base homogênea de ContentSet/runtime state para os escopos principais.

Próximos cortes recomendados:

```text
IF-FW-4E-R1 — Content Scope Sets Baseline
IF-FW-4F — Runtime Root Scope Baseline
IF-FW-4G — Local Binding Reclassification
IF-FW-4H — Route Materialization Execution
IF-FW-4I — Activity Materialization Baseline
```

## Critério de aceitação

A decisão é respeitada se:

- o framework compila sem `CameraFlow`;
- a Route continua carregando sua Primary Scene como antes;
- um Route request bem-sucedido registra um `RouteContentSet`;
- o log de Route lifecycle mostra ao menos um handle Route/Scene/Required;
- nenhuma Activity materialization nova é introduzida neste corte;
- nenhum subsistema estrutural volta a depender de `SetActive` como materialização canônica.
