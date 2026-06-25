# F15-ADR-RESET-004 — Unity Reset Adapters mínimos

Status: Proposed  
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

## 3. Decisão

F15 cria adapters Unity mínimos como consumidores de Local/Object Reset.

Esses adapters são técnicos, não gameplay.

---

## 4. Escopo incluído

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

## 5. Escopo excluído

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

## 6. Modelo conceitual

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

## 7. Baseline

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

## 8. Contratos esperados

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

## 9. Authoring UX

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

## 10. Diagnostics e validação

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

## 11. Consequências

### Positivas

- Reset deixa de ser apenas mock.
- O usuário consegue validar reset real de objetos simples.
- Player futuro pode reaproveitar os adapters técnicos.

### Custos

- Introduz componentes Unity específicos.
- Exige authoring claro para não virar reset implícito/frágil.

---

## 12. Guardrails

- Não colocar lógica de Player nos adapters Unity.
- Não misturar Transform reset com spawn/materialization.
- Não capturar baseline silenciosamente no momento do reset.
- Não usar Reset como Pool Return.
- Não usar scene reload para corrigir baseline ausente.
- Não criar singleton global de reset participants.

---

## 13. Relação com fases futuras

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
