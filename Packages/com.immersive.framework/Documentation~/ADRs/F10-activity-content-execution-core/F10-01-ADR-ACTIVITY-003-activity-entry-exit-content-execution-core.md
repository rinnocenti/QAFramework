# F10-01 — ADR-ACTIVITY-003 — Activity Entry/Exit Content Execution Core

Status: Accepted / F10 planning only / implementation not started  
Fase: F10  
Ordem no Plano: F10-01  
Tipo: Activity / Content Execution  
Escopo: Framework Core

---

## Contexto

F9 fechou o binding logico de Content Anchor. O framework agora possui Activity lifecycle, RuntimeContent ownership/root/context e ContentAnchor binding logico para Route e Activity.

Ainda falta recuperar corretamente a ideia de que uma Activity pode ter conteudo que entra e sai junto com ela. No sistema antigo, isso aparecia muitas vezes como `Presentation`, mas esse nome capturava um consumer especifico e misturava core com gameplay.

F10 define o conceito canonico de core: `Activity Content Execution`.

## Decisao

`Activity Content Execution` e a camada de framework core que orquestra entrada e saida logica de conteudos associados a uma Activity.

O core pode definir:

```text
Activity enters
  -> activity content enter execution

Activity exits
  -> activity content exit execution
```

O core nao sabe se o conteudo futuro sera Presentation, Camera, UI, Pause, Actor, Player, NPC, Audio, Pooling, prefab, scene ou Addressables. Ele conhece apenas contratos, ordering, readiness, status, requiredness, failure semantics e diagnostics.

## Separacoes obrigatorias

`Activity Content Execution` nao e:

```text
Presentation
Reset
Transition / Loading
physical materialization
Transform placement
GameObject hierarchy parenting
gameplay consumer
Unity adapter
```

Presentation e um consumer futuro. Reset e uma fase propria futura. Transition/Loading e uma fase propria futura. Materializacao fisica e placement pertencem a Unity adapters futuros.

## Guardrails

Framework Core nao deve executar:

```text
Instantiate
Destroy
Addressables.Load
Addressables.Release
pool rent/return
Transform placement
hierarchy parenting
Animator reset
camera blend
UI concrete show/hide
player/actor mutation
gameplay state mutation
```

## Consequencias

### Positivas

- Reintroduz entrada/saida de conteudo de Activity sem capturar Presentation como core.
- Cria uma fronteira para consumers futuros sem implementar gameplay.
- Permite readiness e diagnostics consistentes antes de adapters fisicos.

### Trade-offs

- Consumers visiveis continuam adiados.
- A primeira implementacao de F10 deve ficar logica/diagnostica.
- O contrato precisa evitar virar pipeline monolitica.

## Validacao esperada de F10

Esta ADR nao implementa API. Quando F10 entrar em implementacao, os criterios minimos serao:

- Activity content enter/exit possui contratos explicitos.
- A execucao nao chama `Instantiate`, `Destroy`, `GameObject.Find`, Addressables ou Pooling.
- Presentation continua consumer futuro.
- Reset e Transition/Loading continuam fora de F10.
- Diagnostics mostram enter/exit, required/optional, skipped/failure e readiness contribution.

## Relacao com roadmap

F10 abre como Framework Core planning para `Activity Entry/Exit Content Execution`. Implementacao continua nao iniciada ate corte proprio.
