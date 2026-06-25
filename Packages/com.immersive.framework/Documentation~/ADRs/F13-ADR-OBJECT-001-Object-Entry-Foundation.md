# F13-ADR-OBJECT-001 — Object Entry Foundation

Status: Proposed  
Fase: F13 — Object Entry Foundation  
Tipo: Core / Object Lifecycle / Contribution  
Última atualização: 2026-06-25

---

## 1. Contexto

Após Cycle Reset existir como caminho canônico para Route/Activity, o framework precisa preparar a entrada de objetos reais. Sem isso, Local/Object Reset não tem alvo funcional estável.

F13 não cria Player completo nem gameplay. F13 define o formato genérico para objetos entrarem no lifecycle do framework.

---

## 2. Dor original

O framework precisa receber objetos reais — authored ou runtime — de forma controlada, identificável e diagnosticável, para que fases posteriores possam resetar, snapshotar, liberar ou conectar capabilities.

Sem Object Entry, qualquer reset local tenderia a usar:

```text
GameObject.name
hierarchy path
component reference solta
scan global
```

Esses caminhos já foram rejeitados como chave funcional.

---

## 3. Decisão

F13 cria **Object Entry Foundation** como camada core para registrar objetos participantes de Route/Activity sem assumir gameplay.

Object Entry define:

```text
identidade
escopo
owner
entry result
readiness mínimo
contribution descriptor
lifetime relation
```

F13 não define o que é Player, Actor, NPC ou Projectile.

---

## 4. Escopo incluído

F13 inclui:

```text
ObjectContentIdentity ou ObjectRuntimeIdentity
ObjectEntryRequest
ObjectEntryResult
ObjectLifecycleContext
ObjectContributionDescriptor
ObjectReadinessState mínimo
ObjectEntryParticipant / provider contract
ObjectEntrySet ou extensão de contribution set, se necessário
Mock object entry smoke
```

---

## 5. Escopo excluído

F13 exclui:

```text
Player implementation
Actor implementation
NPC implementation
Projectile implementation
Health/attributes
Movement binding real
Powerups
Object Reset execution
Component Reset execution
Pooling
Save backend
```

---

## 6. Modelo conceitual

Object Entry é a entrada de um objeto no contexto ativo do framework.

```text
Route/Activity active scope
  -> discovers/receives object entry participants
  -> builds ObjectEntrySet/ContributionSet
  -> records identity + scope + readiness
```

Objeto pode ser:

```text
scene-authored local object
runtime-materialized object
future actor/player object
```

Mas F13 deve tratar todos como objeto participante genérico.

---

## 7. Identidade

F13 deve preservar a política de identidade tipada:

```text
GameObject.name = diagnostic only
scene path = diagnostic only
hierarchy path = diagnostic only
object reference = runtime reference, not canonical identity
```

Identidade funcional deve carregar domínio explícito:

```text
Object:PlayerPrimary
Object:DoorA
Object:RuntimeActor:<runtime-id>
```

O formato final pode variar, mas não pode usar fallback silencioso instável.

---

## 8. Contratos esperados

```csharp
public readonly struct ObjectEntryRequest
{
    public ObjectContentIdentity Identity { get; }
    public FrameworkContentScope Scope { get; }
    public string Source { get; }
    public string Reason { get; }
}
```

```csharp
public readonly struct ObjectEntryResult
{
    public ObjectEntryStatus Status { get; }
    public ObjectContentIdentity Identity { get; }
    public ObjectReadinessState Readiness { get; }
}
```

```csharp
public interface IObjectEntryParticipant
{
    ObjectContributionDescriptor Descriptor { get; }
    ObjectEntryResult EnterObject(ObjectLifecycleContext context);
}
```

---

## 9. Readiness

F13 deve manter readiness mínimo.

Estados conceituais:

```text
Unknown
Ready
NotReady
Skipped
Failed
```

Readiness não deve virar gate complexo nesta fase. Ele deve apenas permitir que o framework saiba se o objeto entrou de forma utilizável.

---

## 10. Diagnostics e validação

Smoke inicial pode usar mock objects.

Diagnostics mínimos:

```text
objectIdentity
scope
ownerRoute
ownerActivity
entryStatus
readiness
requiredness
source
reason
issues
```

---

## 11. Consequências

### Positivas

- O framework ganha formato para objetos reais antes de reset local.
- Player/Actor não ditam o contrato inicial.
- Object Reset futuro terá alvo canônico.

### Custos

- Mais uma camada foundation antes de gameplay.
- Pode parecer abstrato enquanto não houver Player real.

---

## 12. Guardrails

- Não implementar Player em F13.
- Não implementar Object Reset em F13.
- Não usar scan global como fonte de verdade.
- Não usar nome/path como identidade funcional.
- Não criar service locator de objetos.
- Não criar registry público mutável que subsistemas possam usar como atalho.

---

## 13. Relação com fases futuras

F13 desbloqueia:

```text
F14 — Local/Object Reset Foundation
F15 — Unity Reset Adapters mínimos
F16 — Player/Participant Entry Baseline
```

F13 mantém bloqueado:

```text
Player gameplay
Actor gameplay
Projectile
Damage
Powerups
Pooling consumer
```
