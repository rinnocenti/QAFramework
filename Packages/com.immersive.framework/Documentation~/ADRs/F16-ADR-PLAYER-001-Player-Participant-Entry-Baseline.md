# F16-ADR-PLAYER-001 — Player/Participant Entry Baseline

Status: Proposed  
Fase: F16 — Player/Participant Entry Baseline  
Tipo: Consumer Controlado / Player / Participation  
Última atualização: 2026-06-25

---

## 1. Contexto

Após Cycle Reset, Object Entry, Object Reset e Unity Reset Adapters, o framework pode receber o primeiro objeto real de gameplay controlado: Player/Participant.

F16 não deve virar sistema completo de personagem. Ela introduz Player como consumidor dos contratos existentes.

---

## 2. Dor original

O usuário quer que objetos reais, como o jogador, entrem no lifecycle de forma previsível e possam participar de reset.

Exemplo:

```text
Activity Reset limpa movimento e volta player ao ponto inicial.
Object Reset do Player pode limpar estado local e aplicar policies próprias.
```

Isso só é seguro depois de existir Object Entry e reset local.

---

## 3. Decisão

F16 define Player/Participant como primeiro consumidor real de Object Entry e Reset.

Player não define o core. Player consome:

```text
Object Entry
Local/Object Reset
Unity Reset Adapters
Input ownership
Activity/Route lifecycle context
```

---

## 4. Escopo incluído

F16 pode incluir:

```text
PlayerParticipantIdentity
PlayerObjectEntry
PlayerParticipationDescriptor
Player readiness mínimo
Player reset participation mínimo
Player input binding mínimo, se dependências F10 estiverem prontas
Player smoke
```

---

## 5. Escopo excluído

F16 exclui:

```text
Combat
Damage
Attributes completos
Powerup system
Inventory
Advanced character controller
NPC framework
Actor framework completo
Projectile
Pooling gameplay
Save progression
```

---

## 6. Modelo conceitual

Player é um objeto participante do lifecycle, não owner do lifecycle.

```text
Route/Activity active
  -> Object Entry
    -> Player participant registered
      -> readiness
      -> reset participation
      -> input binding consumer
```

Player pode ter vários reset participants internos, mas o core só vê participants por contrato.

---

## 7. Identidade

Player identity deve ser tipada.

Exemplos conceituais:

```text
Player:Primary
Player:Slot1
Participant:LocalSolo
```

Não usar:

```text
GameObject.name = Player
Tag = Player como identidade canônica
Scene path
```

Tags/camadas podem ajudar em Unity authoring, mas não devem ser chave funcional.

---

## 8. Reset do Player

F16 pode permitir reset mínimo do Player por composição de participantes:

```text
TransformResetParticipant
MovementStateResetParticipant mínimo, se existir
PlayerParticipationResetParticipant mínimo
```

Mas não deve implementar gameplay-specific reset.

Diferença importante:

```text
Activity Reset do Player ≠ Object Reset do Player.
```

O participante recebe contexto e decide comportamento.

---

## 9. Input

Se Input ownership da F10 estiver aplicado, F16 pode conectar Player ao modo de input mínimo.

Guardrail:

```text
Player não pode controlar Input global diretamente.
Player recebe binding/contexto de input como consumer.
```

---

## 10. Diagnostics e validação

Smokes esperados:

```text
Player entry smoke
Player readiness smoke
Activity reset affects player participant smoke
Object reset player smoke
```

Diagnostics mínimos:

```text
playerIdentity
participantId
objectEntryStatus
readiness
resetParticipation
inputBindingStatus
issues
```

---

## 11. Consequências

### Positivas

- O framework ganha primeiro consumidor real sem perder genericidade.
- Reset passa a ter valor prático em gameplay básico.
- Player usa contratos existentes em vez de forçar novos atalhos.

### Custos

- É o primeiro ponto em que gameplay começa a tocar o framework.
- Exige disciplina para não antecipar Actor/Combat.

---

## 12. Guardrails

- Player não define lifecycle core.
- Player não cria service locator.
- Player não descobre Route/Activity por conta própria.
- Player não usa Tag/GameObject.name como identidade canônica.
- Não implementar combat/attributes/powerups em F16.
- Não tornar Player obrigatório para o framework funcionar.

---

## 13. Relação com fases futuras

F16 desbloqueia consumidores avançados e gameplay futuro com uma referência real de participante.

F16 mantém bloqueado:

```text
Actor framework completo
NPC framework completo
Projectile
Damage
Attributes
Powerups
Advanced gameplay capabilities
```
