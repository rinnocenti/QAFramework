# F15-ADR-RESET-004 — Unity Reset Adapters mínimos

Status: Proposed / Planning  
Fase: F15 — Unity Reset Adapters mínimos  
Tipo: Unity Adapter / Reset / Authoring  
Última atualização: 2026-06-25

---

## 1. Contexto

F14 define o contrato de Object Reset. F15 entrega os primeiros adapters Unity úteis sem introduzir gameplay. O objetivo é permitir reset prático de objetos de cena mantendo o core limpo.

---

## 2. Dor original

Depois que o caminho canônico existe, o usuário precisa ver reset funcionando em objetos comuns da Unity.

Exemplos:

```text
Voltar Transform ao baseline.
Limpar velocidade de Rigidbody.
Resetar grupo local simples.
```

---

## 3. Proposta

F15 propõe criar adapters Unity mínimos como consumidores de Local/Object Reset.

Esses adapters são técnicos, não gameplay.

---

## 4. Decisões pendentes antes de implementar

F15 ainda não está aceita como implementação. Antes de criar qualquer adapter real, a fase deve decidir:

```text
Boundary de assembly para adapters Unity.
API de registro de IObjectResetParticipantSource.
Política para adapter obrigatório ausente.
Se SucceededNoParticipants continua permitido em triggers F15.
Required vs optional adapters.
Ordenação de adapters físicos.
Tratamento de GameObject inactive.
Proibição de GameObject.name/path como identidade funcional.
Como evitar lifecycle paralelo.
```

Esses pontos são perguntas de planejamento, não decisões aplicadas.

---

## 5. Escopo incluído

F15 pode incluir:

```text
TransformResetParticipant
RigidbodyResetParticipant, se necessário
AnimatorResetParticipant, se escopo couber
LocalResetGroup
ResetBaselineAuthoring mínimo
CapturedOnEnterBaseline mínimo
Unity adapter smoke
```

A prioridade deve ser Transform, porque ele prova o fluxo de baseline sem amarrar o framework a física, animação ou gameplay.

---

## 6. Escopo excluído

F15 exclui:

```text
Player stats
Player movement design
Health
Powerups
Combat
Actor behavior
NPC behavior
Projectile
Pooling
Save/checkpoint restore
```

---

## 7. Modelo conceitual

Adapters Unity implementam contratos de reset existentes:

```text
ObjectResetRuntime
  -> IObjectResetParticipant
    -> TransformResetParticipant
    -> RigidbodyResetParticipant
    -> AnimatorResetParticipant
```

O adapter conhece Unity. O core não conhece detalhes de Unity além da fronteira já aceita no package.

---

## 8. Baseline

F15 deve suportar baseline mínimo de Transform.

Fontes permitidas:

```text
Authored baseline
Capture on enter
```

Regras:

```text
Baseline ausente em participant required -> issue explícito.
Baseline ausente em participant optional -> skip/warning.
Não criar fallback silencioso para posição atual no momento do reset.
```

---

## 9. Contratos esperados

Adapter típico:

```csharp
public sealed class TransformResetParticipant : MonoBehaviour, IObjectResetParticipant
{
    public ObjectResetParticipantResult Reset(ObjectResetContext context)
    {
        // aplica baseline explícito/capturado
    }
}
```

O código final deve seguir o estilo do framework, mas a decisão importante é: adapter executa, core orquestra.

---

## 10. Authoring UX

Inspector deve deixar claro:

```text
Este componente participa do reset canônico.
Ele não executa reset sozinho.
Ele não recarrega cena.
Ele não controla lifecycle de Activity/Route.
```

Campos possíveis:

```text
Requiredness
Baseline Source
Position
Rotation
Scale
Capture On Enter
```

---

## 11. Diagnostics e validação

Smokes esperados:

```text
Transform reset baseline smoke
Object reset group smoke
Missing baseline warning/failure smoke, se couber
```

Diagnostics mínimos:

```text
objectIdentity
participant
baselineSource
appliedPosition
appliedRotation
appliedScale
status
issues
```

---

## 12. Consequências

### Positivas

- Reset deixa de ser apenas mock.
- O usuário consegue validar reset real de objetos simples.
- Player futuro pode reaproveitar os adapters técnicos.

### Custos

- Introduz componentes Unity específicos.
- Exige authoring claro para não virar reset implícito/frágil.

---

## 13. Guardrails

- Não colocar lógica de Player nos adapters Unity.
- Não misturar Transform reset com spawn/materialization.
- Não capturar baseline silenciosamente no momento do reset.
- Não usar Reset como Pool Return.
- Não usar scene reload para corrigir baseline ausente.
- Não criar singleton global de reset participants.
- Não usar GameObject.name, hierarchy path ou scene path como identidade funcional.
- Não criar lifecycle paralelo para reset.

---

## 14. Relação com fases futuras

F15 desbloqueia:

```text
F16 — Player/Participant Entry Baseline
```

F15 mantém bloqueado:

```text
Gameplay capabilities
Combat reset
Powerup reset
Projectile reset
Pooling reset
```
