# F12-ADR-RESET-002 — Cycle Reset Integration & Authoring UX

Status: Applied through F12E  
Fase: F12 — Cycle Reset Integration & Authoring UX  
Tipo: Core / Tooling / Authoring / QA  
Ultima atualizacao: 2026-06-25

---

## 1. Contexto

F11 criou o contrato e o executor minimo de Cycle Reset. F12 torna esse caminho usavel dentro do fluxo real do framework, sem ainda introduzir reset local/object ou gameplay.

A F12 existe para impedir que Cycle Reset fique como contrato tecnico sem superficie de uso, validacao ou authoring claro.

---

## 2. Dor original

O usuario precisa acionar reset de ciclo a partir de UI, QA, triggers de cena ou ferramentas de desenvolvimento, com logs consistentes e sem criar caminhos paralelos.

Exemplos:

```text
Botao QA: Reset Activity atual.
Botao QA: Reset Route atual.
Componente authored: solicitar reset da Activity.
Componente authored: solicitar reset da Route.
```

---

## 3. Decisao

F12 integra Cycle Reset ao runtime publico do framework por superficies authoring/dev controladas.

F12 nao cria reset real de objetos. Ela apenas expoe o caminho canonico criado na F11 para uso e validacao.

A decisao central de UX e:

```text
Trigger e o componente principal.
Unity Event Bridge e opcional.
```

Botoes/UI podem chamar diretamente os metodos do trigger. A bridge so existe quando o objeto precisa de callbacks UnityEvent de resultado no Inspector.

---

## 4. Escopo incluido

F12 inclui:

```text
ActivityCycleResetTrigger
RouteCycleResetTrigger
FrameworkQaCanvas reset buttons/smokes
Trigger result UX
Unity Event Bridges opcionais
Authoring validation minima de triggers
Cycle Reset smoke criteria
Diagnostics de request concluido
```

F12 tambem garante que os triggers chamem o owner correto do runtime, e nao executem reset localmente.

---

## 5. Escopo excluido

F12 exclui:

```text
Object Reset
Component Reset
Player Reset
Actor Reset
TransformResetParticipant
RigidbodyResetParticipant
AnimatorResetParticipant
ResetBaseline authoring real
Save/checkpoint restore
Pool return
Scene reload
```

---

## 6. Modelo conceitual

F12 adiciona superficie de uso para o contrato F11:

```text
Scene/UI/QA Trigger
  -> FrameworkRuntimeHost
    -> GameFlowRuntime
      -> RouteLifecycleRuntime
        -> CycleResetRuntime
          -> participants/probes
          -> result
```

Os triggers sao request sources, nao owners de reset.

A bridge e apenas um adapter de evento:

```text
CycleResetTriggerEvent typed
  -> UnityEvent callbacks opcionais
```

A bridge nao cria request novo, nao executa reset e nao substitui o trigger.

---

## 7. Contratos esperados

### ActivityCycleResetTrigger

Componente publico para scene/UI solicitar reset da Activity ativa.

Requisitos:

```text
Nao precisa conhecer Activity asset alvo.
Usa Activity ativa do runtime.
Falha explicitamente se nao houver runtime ou Activity ativa.
Possui Source/Reason para diagnostics.
Expor ultimo resultado para Inspector/UX.
```

### RouteCycleResetTrigger

Componente publico para scene/UI solicitar reset da Route ativa.

Requisitos:

```text
Nao precisa conhecer Route asset alvo.
Usa Route ativa do runtime.
Pode expor Include Active Activity quando a policy permitir.
Falha explicitamente se nao houver runtime ou Route ativa.
Expor ultimo resultado para Inspector/UX.
```

### Unity Event Bridges

As bridges sao opcionais.

Modelo correto:

```text
1 RouteCycleResetTrigger pode ter 0 ou 1 RouteCycleResetTriggerUnityEventBridge.
1 ActivityCycleResetTrigger pode ter 0 ou 1 ActivityCycleResetTriggerUnityEventBridge.
A bridge fica no mesmo GameObject do trigger.
```

Usar bridge apenas quando o GameObject precisa expor callbacks de resultado via Inspector/UnityEvent.

Exemplos de callbacks:

```text
Request Submitted
Request Succeeded
Request Succeeded With Participants
Request Succeeded No Participants
Request Completed With Warnings
Request Ignored
Request Failed
Request Completed
```

---

## 8. Authoring UX

Inspector deve explicar de forma direta:

```text
Este componente solicita reset do ciclo ativo.
Ele nao recarrega cena.
Ele nao troca Route/Activity.
Ele nao reseta objeto especifico.
Ele nao restaura save/checkpoint.
```

Campos recomendados:

```text
Reason
Include Active Activity, somente para Route reset se aplicavel
```

O Inspector/guia deve deixar explicito:

```text
Unity Event Bridge is optional.
Add it only when this trigger needs Inspector callbacks for request results.
Buttons can call the trigger method directly without a bridge.
```

O caminho simples para game designers deve aparecer primeiro:

```text
Button.onClick -> RouteCycleResetTrigger.RequestRouteCycleReset()
Button.onClick -> ActivityCycleResetTrigger.RequestActivityCycleReset()
```

---

## 9. Diagnostics e validacao

F12 expande QA para validar o caminho real de request e a UX dos triggers.

Smokes aceitos:

```text
Run Cycle Reset Trigger Smoke
Run Cycle Reset Bridge Smoke
```

Logs/resultados devem deixar explicito:

```text
requestSource
requestReason
scope
activeRoute
activeActivity
resultStatus
participantCounts
blockingIssues
nonBlockingIssues
```

Authoring validation deve reportar contadores de:

```text
routeCycleResetTriggers
activityCycleResetTriggers
```

O validator nao deve exigir bridges.

Warnings aceitaveis:

```text
Reason usa vocabulario futuro de object/player/component/etc.
Route e Activity trigger no mesmo GameObject, se isso gerar ambiguidade de authoring.
Trigger inativo na hierarquia, se o smoke/QA depender dele ativo.
```

---

## 10. Evidencia aceita da F12

### F12A — Authoring Guardrails

```text
QA Authoring Validation completed.
routeCycleResetTriggers='1'
activityCycleResetTriggers='1'
issues='0'
```

### F12C — Trigger Smoke

```text
QA Smoke completed. name='Cycle Reset Trigger Smoke'.
Route trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
Activity trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
```

### F12D — Bridge Smoke

```text
QA Smoke completed. name='Cycle Reset Bridge Smoke'.
Route bridge: submittedEvents='1', succeededEvents='1', succeededNoParticipantsEvents='1', completedEvents='1', failedEvents='0', ignoredEvents='0'.
Activity bridge: submittedEvents='1', succeededEvents='1', succeededNoParticipantsEvents='1', completedEvents='1', failedEvents='0', ignoredEvents='0'.
```

`SucceededNoParticipants` e resultado esperado nesta fase para triggers reais sem participantes fisicos.

---

## 11. Consequencias

### Positivas

- Cycle Reset passa a ser utilizavel no Unity.
- QA consegue validar a mecanica sem objetos reais.
- Triggers tem feedback de resultado suficiente para Inspector/UI.
- Bridges opcionais permitem callbacks de resultado sem obrigar toda UI a usar bridge.
- O core nao fica dependente de implementacao futura de Player.

### Custos

- Ainda nao ha reset local util para gameplay.
- A UX precisa ser explicita para nao prometer reset de objeto.
- Bridges aumentam a superficie authoring, entao precisam permanecer claramente opcionais.

---

## 12. Guardrails

- Triggers nao podem executar reset diretamente.
- QA Canvas nao pode virar owner de reset.
- Inspector nao pode sugerir que reset recarrega cena ou restaura save.
- Nao adicionar campos de Player/Actor/Component nos triggers de cycle reset.
- Nao implementar fallback silencioso se nao houver runtime ativo.
- Validator nao deve exigir bridge.
- Bridge nao deve existir como segundo caminho de request.
- Bridge nao deve ser documentada como obrigatoria.
- Guia deve mostrar primeiro o caminho simples sem bridge.

---

## 13. Relacao com fases futuras

F12 desbloqueia uso pratico do Cycle Reset enquanto F13/F14 ainda nao existem.

F12 nao desbloqueia gameplay reset diretamente. Object Reset e reset de objetos reais continuam para F14/F15/F16.

F13 deve iniciar Object Entry Foundation sem reaproveitar Cycle Reset como atalho para reset local.
