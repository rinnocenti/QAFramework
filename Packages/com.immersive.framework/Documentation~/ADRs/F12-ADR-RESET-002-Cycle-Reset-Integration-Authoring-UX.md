# F12-ADR-RESET-002 — Cycle Reset Integration & Authoring UX

Status: Proposed  
Fase: F12 — Cycle Reset Integration & Authoring UX  
Tipo: Core / Tooling / Authoring / QA  
Última atualização: 2026-06-25

---

## 1. Contexto

F11 cria o contrato e executor mínimo de Cycle Reset. F12 torna esse caminho usável dentro do fluxo real do framework, sem ainda introduzir reset local/object ou gameplay.

A F12 existe para impedir que Cycle Reset fique como contrato técnico sem superfície de uso, validação ou authoring claro.

---

## 2. Dor original

O usuário precisa acionar reset de ciclo a partir de UI, QA, triggers de cena ou ferramentas de desenvolvimento, com logs consistentes e sem criar caminhos paralelos.

Exemplos:

```text
Botão QA: Reset Activity atual.
Botão QA: Reset Route atual.
Componente authored: solicitar reset da Activity.
Componente authored: solicitar reset da Route.
```

---

## 3. Decisão

F12 integra Cycle Reset ao runtime público do framework por superfícies authoring/dev controladas.

F12 não cria reset real de objetos. Ela apenas expõe o caminho canônico criado na F11 para uso e validação.

---

## 4. Escopo incluído

F12 inclui:

```text
ActivityCycleResetTrigger
RouteCycleResetTrigger
FrameworkQaCanvas reset buttons
Reset request event bridge, se necessário
Authoring validation mínima de triggers
Cycle Reset smoke docs ou seção no plano canônico
Diagnostics de request concluído
```

F12 também deve garantir que os triggers chamem o owner correto do runtime, e não executem reset localmente.

---

## 5. Escopo excluído

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

F12 adiciona superfície de uso para o contrato F11:

```text
Scene/UI/QA Trigger
  -> FrameworkRuntimeHost
    -> GameFlow / CycleReset owner
      -> CycleResetRuntime
        -> participants/probes
        -> result
```

Os triggers são request sources, não owners de reset.

---

## 7. Contratos esperados

### ActivityCycleResetTrigger

Componente público para scene/UI solicitar reset da Activity ativa.

Requisitos:

```text
Não precisa conhecer Activity asset alvo.
Usa Activity ativa do runtime.
Falha explicitamente se não houver runtime ou Activity ativa.
Possui Source/Reason para diagnostics.
```

### RouteCycleResetTrigger

Componente público para scene/UI solicitar reset da Route ativa.

Requisitos:

```text
Não precisa conhecer Route asset alvo.
Usa Route ativa do runtime.
Pode expor Include Active Activity quando a policy permitir.
Falha explicitamente se não houver runtime ou Route ativa.
```

---

## 8. Authoring UX

Inspector deve explicar de forma direta:

```text
Este componente solicita reset do ciclo ativo.
Ele não recarrega cena.
Ele não troca Route/Activity.
Ele não reseta objeto específico.
Ele não restaura save/checkpoint.
```

Campos recomendados:

```text
Reason
Include Active Activity, somente para Route reset se aplicável
```

---

## 9. Diagnostics e validação

F12 deve expandir QA para validar o caminho real de request.

Smokes esperados:

```text
Run Activity Cycle Reset Smoke
Run Route Cycle Reset Smoke
Run Route Cycle Reset Include Activity Smoke
Run Reset Negative Smoke, se houver alvo ausente
```

Logs/resultados devem deixar explícito:

```text
requestSource
requestReason
scope
activeRoute
activeActivity
includeActiveActivity
resultStatus
participantCounts
```

---

## 10. Consequências

### Positivas

- Cycle Reset passa a ser utilizável no Unity.
- QA consegue validar a mecânica sem objetos reais.
- O core não fica invisível nem dependente de implementação futura de Player.

### Custos

- Ainda não há reset local útil para gameplay.
- A UX precisa ser explícita para não prometer reset de objeto.

---

## 11. Guardrails

- Triggers não podem executar reset diretamente.
- QA Canvas não pode virar owner de reset.
- Inspector não pode sugerir que reset recarrega cena ou restaura save.
- Não adicionar campos de Player/Actor/Component nos triggers de cycle reset.
- Não implementar fallback silencioso se não houver runtime ativo.

---

## 12. Relação com fases futuras

F12 desbloqueia uso prático do Cycle Reset enquanto F13/F14 ainda não existem.

F12 não desbloqueia gameplay reset diretamente. Object Reset e reset de objetos reais continuam para F14/F15/F16.
