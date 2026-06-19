# ADR-0002 — Activity Content Binding mínimo e observável

## Status

Accepted

## Contexto

O `com.immersive.framework` já possui bootstrap, Game Application, Route, Scene Lifecycle, Activity Flow, requests runtime de Route/Activity e integração de logging via `com.immersive.logging`.

A próxima necessidade funcional mínima foi tornar uma Activity ativa visível na cena sem importar a arquitetura pesada da Base 2.0 / `NewScripts`.

Na Base 2.0, conteúdo de Activity era tratado por um conjunto maior de conceitos, como discovery, contributors, inventory, setup/release, reset, runtime state e pipeline. Essa arquitetura é útil como referência conceitual de ownership, mas não deve ser copiada como skeleton inicial do novo framework.

O ADR-0001 definiu que o framework deve crescer por necessidade real, evitando criar antecipadamente pipelines, registries, descriptors, contributors e managers preventivos.

## Decisão

Criar um marcador mínimo de cena:

```text
Activity Content Binding
```

Esse componente indica que um GameObject authored diretamente na cena pertence a uma Activity específica.

Quando a Activity ativa muda, o runtime de Activity Content aplica uma regra simples:

```text
binding.Activity == activeActivity -> GameObject ativo
binding.Activity != activeActivity -> GameObject inativo
sem Activity ativa -> todos os bindings ficam inativos
```

O owner dessa aplicação é:

```text
ActivityContentRuntime
```

O owner da identidade ativa continua sendo:

```text
ActivityFlowRuntime
```

O `ActivityContentBinding` não é owner de lifecycle. Ele é apenas um marcador de authoring em cena.

## Fronteiras

`ActivityContentBinding` pode fazer:

- marcar um GameObject de cena como conteúdo de uma Activity;
- permitir que `ActivityContentRuntime` ative/desative esse GameObject;
- expor uma UX simples no Inspector.

`ActivityContentBinding` não deve fazer:

- carregar cena;
- iniciar Activity;
- solicitar Activity;
- executar spawn;
- integrar pooling;
- configurar actor;
- configurar input/camera/save;
- executar reset/release complexo;
- substituir futuro sistema de Actor, Presentation ou Activity Content avançado.

## Observabilidade

Como `ActivityContentBinding` é uma configuração scene-authored, a observabilidade precisa deixar claro o que foi avaliado.

O runtime deve emitir:

- contagem de bindings aplicados;
- quantidade ativada;
- quantidade desativada;
- quantidade inalterada;
- quantidade com Activity ausente;
- diagnóstico por binding com objeto, cena, Activity atribuída, ação e motivo;
- warning explícito para binding sem Activity atribuída.

Exemplo de resumo:

```text
Activity Content applied 2 binding(s) for Activity 'Activity 02'. activated='1' deactivated='1' unchanged='0'.
```

Exemplo de detalhe:

```text
Activity Content Binding diagnostics. activeActivity='Activity 02' observations=[object='Panel_Activity02' scene='StartupScene' assignedActivity='Activity 02' action='Activate' reason='MatchedActiveActivity'; object='Panel_Activity01' scene='StartupScene' assignedActivity='Activity 01' action='Deactivate' reason='DifferentActivity'].
```

Exemplo de warning:

```text
Activity Content Binding warning. warnings=[object='Panel_MissingActivity' scene='StartupScene' reason='MissingActivityReference'].
```

Esses diagnostics devem ser emitidos pela fronteira de logging do framework, não por `UnityEngine.Debug` direto.

## Relação com NewScripts / Base 2.0

Preservar como referência conceitual:

- Activity possui conteúdo;
- owners de lifecycle precisam ser explícitos;
- runtime state e diagnostics devem mostrar decisões relevantes;
- ausência de configuração obrigatória deve ser visível.

Descartar neste momento:

- contributor/discovery/inventory para esse caso mínimo;
- pipelines/stages para ligar/desligar conteúdo simples;
- registries globais;
- profiles de conteúdo antes de existir necessidade concreta;
- fallbacks silenciosos.

## Consequências

Benefícios:

- Activity ativa tem efeito visual real na cena;
- UX de authoring é simples;
- evita copiar a estrutura pesada do Base 2.0;
- mantém Activity Flow como owner da Activity ativa;
- mantém Activity Content Runtime como owner da visibilidade de conteúdo;
- melhora rastreabilidade com diagnostics por binding.

Custos aceitos:

- configuração pode ficar espalhada em GameObjects se usada sem disciplina;
- hierarquias com bindings aninhados ainda não têm policy própria;
- conteúdo complexo, spawn, pooling, actors e presentation precisarão de contratos futuros;
- esse binding é uma solução mínima, não o sistema final de composição de gameplay.

## Critério de aceitação

A decisão é respeitada se:

- `ActivityContentBinding` ativa/desativa apenas GameObjects de cena;
- `ActivityFlowRuntime` continua owner da Activity ativa;
- `ActivityContentRuntime` aplica visibilidade;
- logs de resumo, detalhes e warnings passam por `FrameworkLogger`;
- bindings sem Activity atribuída geram warning explícito;
- não há criação de contributor/discovery/inventory/pipeline para esse caso;
- não há Actor/Input/Camera/Save/Pooling acoplado a esse corte.


## Amendment — IF-FW-2R Authoring Guardrails

`ActivityContentBinding` now has a custom Inspector as an authoring guardrail.

The Inspector makes explicit that the binding only controls the active state of one scene-authored GameObject. It does not replace future Actor, Spawn, Pooling, Presentation, Input, Camera or Save systems.

The Inspector shows an authoring error when the binding has no Activity assigned. Runtime behavior remains unchanged: incomplete bindings are skipped and reported through framework diagnostics.

The Inspector also warns when Activity Content Bindings are nested under each other. Nested Activity content policy is intentionally not defined yet, so authored Activity content roots should remain flat for now.


## Atualização IF-FW-2T

O `ActivityContentRuntime` passou a consumir os eventos canônicos de lifecycle emitidos pelo `ActivityFlowRuntime` por meio de `Foundation.Events`. O comportamento público do `ActivityContentBinding` permanece o mesmo: ligar/desligar `GameObject` de cena conforme a Activity ativa. A mudança é apenas de ownership/integração: o conteúdo passa a reagir ao lifecycle canônico em vez de depender de chamada direta como caminho principal.
