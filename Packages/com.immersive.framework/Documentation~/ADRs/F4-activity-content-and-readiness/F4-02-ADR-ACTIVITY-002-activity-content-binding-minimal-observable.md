# F4-02 - ADR-ACTIVITY-002 - Activity Content Binding minimal and observable

Status: Accepted
Fase: F4
Ordem no Plano: F4-02
Tipo: Activity / Local Visibility Adapter
Escopo: ActivityFlow / scene-authored Activity content

---

## Nota de classificacao

Este ADR registra `ActivityContentBinding` como adapter local de visibilidade de cena.

Ele nao e F9 Content Anchor Binding, nao autoriza runtime physical placement, nao inicia F10 e nao define gameplay consumer.

## Contexto

`com.immersive.framework` precisava tornar a Activity ativa observavel na cena sem importar a arquitetura pesada da Base 2.0 / `NewScripts`.

Base 2.0 continha conceitos maiores como discovery, contributors, inventory, setup/release, reset, runtime state e pipelines. Esses conceitos podem servir como referencia conceitual, mas nao devem ser copiados como skeleton inicial do framework.

## Decisao

Criar um marcador minimo de cena:

```text
Activity Content Binding
```

Esse componente indica que um `GameObject` authored diretamente na cena pertence a uma Activity especifica.

Quando a Activity ativa muda, o runtime de Activity Content aplica:

```text
binding.Activity == activeActivity -> GameObject ativo
binding.Activity != activeActivity -> GameObject inativo
sem Activity ativa -> todos os bindings ficam inativos
```

Owner da aplicacao:

```text
ActivityContentRuntime
```

Owner da identidade ativa:

```text
ActivityFlowRuntime
```

`ActivityContentBinding` nao e owner de lifecycle. Ele e marcador de authoring em cena.

## Fronteiras

`ActivityContentBinding` pode:

- marcar um `GameObject` de cena como conteudo de uma Activity;
- permitir que `ActivityContentRuntime` ative/desative esse `GameObject`;
- expor UX simples no Inspector.

`ActivityContentBinding` nao pode:

- carregar cena;
- iniciar Activity;
- solicitar Activity;
- executar spawn;
- integrar pooling;
- configurar actor;
- configurar input/camera/save;
- executar reset/release complexo;
- substituir futuro sistema de Actor, Presentation ou Activity Content avancado.

## Observabilidade

O runtime deve emitir diagnostics claros sobre:

- contagem de bindings aplicados;
- quantidade ativada;
- quantidade desativada;
- quantidade inalterada;
- quantidade com Activity ausente;
- objeto, cena, Activity atribuida, acao e motivo por binding;
- warning explicito para binding sem Activity atribuida.

Diagnostics passam pela fronteira de logging do framework, nao por `UnityEngine.Debug` direto.

## Relacao com F4/F9/F10

F4 classifica esse comportamento como Local Visibility Adapter.

F9 fecha Content Anchor logical binding, que e outro conceito.

F10 continua `NOT STARTED`; este ADR nao autoriza Activity Entry/Exit Content Execution, Reset, Transition/Loading, physical placement, adapters fisicos ou gameplay consumers.

## Criterios de aceite

A decisao e respeitada se:

- `ActivityContentBinding` ativa/desativa apenas `GameObject` de cena;
- `ActivityFlowRuntime` continua owner da Activity ativa;
- `ActivityContentRuntime` aplica visibilidade;
- logs de resumo, detalhes e warnings passam pelo logging do framework;
- bindings sem Activity atribuida geram warning explicito;
- nao ha contributor/discovery/inventory/pipeline para esse caso minimo;
- nao ha Actor/Input/Camera/Save/Pooling acoplado a esse corte.

