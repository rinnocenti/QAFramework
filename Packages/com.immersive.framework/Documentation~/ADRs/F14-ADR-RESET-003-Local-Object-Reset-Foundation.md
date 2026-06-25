# F14-ADR-RESET-003 — Local/Object Reset Foundation

Status: Proposed  
Fase: F14 — Local/Object Reset Foundation  
Tipo: Core / Local Lifecycle / Reset  
Última atualização: 2026-06-25

---

## 1. Contexto

F11/F12 criam e integram reset de ciclo. F13 cria entrada genérica de objetos reais. F14 conecta essas duas bases para permitir reset local/object sem ainda depender de Player ou Actor específico.

---

## 2. Dor original

Além de resetar Route/Activity, o usuário quer reiniciar somente um objeto ou conjunto de componentes independentes.

Exemplos futuros:

```text
Resetar apenas Player.
Resetar apenas um puzzle object.
Resetar um grupo local.
Resetar apenas participantes de movimento.
```

F14 cria o contrato para isso, mas ainda pode validar com mocks/probes.

---

## 3. Decisão

F14 define **Local/Object Reset** como reset direcionado a um alvo de objeto/contribution conhecido pelo framework.

Local/Object Reset é diferente de Cycle Reset:

```text
Cycle Reset mira Route/Activity.
Object Reset mira objeto/contribution específica.
Component Reset mira participante/capability específica.
```

F14 deve criar o shape canônico, não gameplay reset.

---

## 4. Escopo incluído

F14 inclui:

```text
ObjectResetRequest
ObjectResetTarget
ObjectResetPolicy
ObjectResetPlan
ObjectResetResult
ObjectResetIssue
IObjectResetParticipant
ILocalResetParticipant
ResetBaseline concept mínimo
Object reset smoke com mocks/probes
```

---

## 5. Escopo excluído

F14 exclui:

```text
TransformResetParticipant real
RigidbodyResetParticipant real
AnimatorResetParticipant real
PlayerStatsResetParticipant real
Actor reset real
Powerup reset real
Projectile reset
Pool return
Save/checkpoint restore
```

---

## 6. Modelo conceitual

Object Reset usa identidade de objeto/contribution já conhecida.

```text
ObjectResetRequest
  -> resolve ObjectResetTarget
  -> builds ObjectResetPlan
  -> dispatches reset participants for that target
  -> aggregates ObjectResetResult
```

Object Reset não deve descobrir o mundo por scan global durante o request. Ele deve operar sobre sets/contributions já conhecidos no escopo.

---

## 7. Reset Target

Target pode ser conceitualmente:

```text
Object identity
LocalContentIdentity
RuntimeContentHandle
ObjectContributionDescriptor
Capability identity futura
```

O alvo não pode ser:

```text
GameObject.name
scene path
hierarchy path
string livre sem domínio
```

---

## 8. Reset Baseline

F14 introduz o conceito de **Reset Baseline**, mas não precisa implementar todos os adapters concretos.

Reset Baseline é diferente de Snapshot:

```text
Reset Baseline = estado base local para reset no ciclo atual.
Snapshot = payload versionado para save/restore.
```

Fontes possíveis futuras:

```text
authored baseline
captured-on-enter baseline
runtime-materialization baseline
```

F14 deve registrar a distinção, não implementar todos os casos.

---

## 9. Contratos esperados

```csharp
public readonly struct ObjectResetRequest
{
    public ObjectResetTarget Target { get; }
    public string Source { get; }
    public string Reason { get; }
}
```

```csharp
public interface IObjectResetParticipant
{
    ObjectResetParticipantDescriptor Descriptor { get; }
    ObjectResetParticipantResult Reset(ObjectResetContext context);
}
```

```csharp
public readonly struct ObjectResetResult
{
    public ObjectResetStatus Status { get; }
    public int Participants { get; }
    public int Succeeded { get; }
    public int Skipped { get; }
    public int Failed { get; }
}
```

---

## 10. Ordem de execução

Object Reset deve preparar evolução para:

```text
PrepareReset
ApplyReset
PostReset
```

F14 pode executar fase única se necessário, mas o modelo não deve impedir ordering futuro.

---

## 11. Diagnostics e validação

Smokes esperados:

```text
Object Reset mock success
Object Reset target missing
Object Reset participant skipped
Object Reset participant failed, se couber
```

Diagnostics mínimos:

```text
targetIdentity
scope
source
reason
participants
succeeded
skipped
failed
status
issues
```

---

## 12. Consequências

### Positivas

- Reset local ganha contrato sem depender de Player.
- Componentes futuros terão contexto claro para decidir como resetar.
- O core mantém separação entre reset de ciclo e reset de objeto.

### Custos

- Ainda não haverá adapters Unity úteis até F15.
- Object Reset precisa depender de Object Entry/Contribution para não usar descoberta frágil.

---

## 13. Guardrails

- Não resolver target por nome/path.
- Não executar scan global como fonte de verdade do reset.
- Não misturar Object Reset com Cycle Reset.
- Não transformar Reset Baseline em Save Snapshot.
- Não criar Player-specific reset em F14.
- Não usar pool return como reset.

---

## 14. Relação com fases futuras

F14 desbloqueia:

```text
F15 — Unity Reset Adapters mínimos
F16 — Player/Participant Entry Baseline
```

F14 mantém bloqueado:

```text
Gameplay-specific reset
Player stats reset definitivo
Actor reset definitivo
Projectile reset
Powerup reset
```
