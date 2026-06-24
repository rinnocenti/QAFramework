# Activity Content Execution Runtime

Status: APPLIED / RUNTIME EXECUTOR + DIAGNOSTIC SMOKE PENDING  
Fase: F10G  
Escopo: Framework Core

---

## Contexto

F10B-F10F adicionaram contratos passivos, resultado agregado, contrato de participant, collection ordenavel e phase plan/request factory. F10G adiciona o executor runtime para esses phase plans.

F10G adiciona o primeiro executor runtime de Activity Content Execution, mas apenas para executar uma fase ja planejada e composta por participants ja conhecidos. F10H adiciona um smoke diagnostico com participants sinteticos para validar o executor sem discovery e sem integracao no lifecycle.

---

## Contrato adicionado

```text
ActivityContentExecutionRuntime
```

Fluxo:

```text
ActivityContentExecutionPhasePlan
  -> ActivityContentExecutionRuntime.ExecutePhasePlan(...)
      -> IActivityContentExecutionParticipant.ExecuteActivityContent(...)
      -> ActivityContentExecutionAggregateResult
```

---

## Responsabilidade

O executor:

- valida o phase plan recebido;
- ignora plano sem requests;
- rejeita plano invalido/rejeitado;
- executa participants ja presentes no plano;
- captura exception de participant como resultado de falha;
- converte falha de participant required em blocking failure;
- converte falha de participant optional em non-blocking failure;
- agrega resultados por fase.

---

## Nao responsabilidade

F10G nao adiciona:

```text
participant discovery
ActivityFlow integration
readiness aggregation integrada ao lifecycle
smoke
Transform placement
GameObject hierarchy root
Instantiate
Destroy
Addressables
Pooling
Presentation
Actor/Player/Camera/Pause/Input/Save consumers
```

O executor nao descobre participants. Ele executa somente o que foi explicitamente recebido via `ActivityContentExecutionPhasePlan`.

---

## Fronteira de core

`ActivityContentExecutionRuntime` pertence ao Framework Core porque orquestra contrato, ordering, diagnostics e readiness semantics.

Ele nao deve executar materializacao fisica, placement, camera blend, UI concreta, actor/player mutation, pool return ou gameplay state mutation.

Essas responsabilidades pertencem a Unity Adapters ou Gameplay Consumers futuros.


---

## Smoke F10H

O QA Canvas agora inclui:

```text
Run Activity Content Execution Runtime Smoke
```

Esse smoke valida collection, enter plan, exit plan e aggregate result usando participants sinteticos. Ele nao descobre participants e nao integra o executor ao `ActivityFlowRuntime`.
