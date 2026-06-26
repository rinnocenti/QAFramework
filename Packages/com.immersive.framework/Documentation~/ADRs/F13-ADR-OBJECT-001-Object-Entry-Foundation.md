# F13-ADR-OBJECT-001 — Object Entry Foundation

Status: In progress through F13H / reconciliation in F13I  
Fase: F13 — Object Entry Foundation  
Tipo: Core / Object Lifecycle / Diagnostics  
Última atualização: 2026-06-25

---

## 1. Contexto

F11 criou o caminho canônico de Cycle Reset e F12 tornou esse caminho utilizável por QA, triggers e bridges opcionais. Antes de criar Local/Object Reset, o framework precisa de uma representação estável dos objetos lógicos conhecidos pelo lifecycle.

F13 não cria Player, Actor, spawn ou reset físico. Ela cria a fundação passiva de Object Entry: identidade, escopo, requiredness, declaração authored, conjunto imutável, diagnóstico e snapshot runtime controlado.

---

## 2. Dor original

Sem Object Entry, fases posteriores tenderiam a identificar objetos por:

```text
GameObject.name
hierarchy path
scene path
component reference solta
scan global sem owner
```

Esses dados podem aparecer em diagnóstico, mas não podem ser a identidade funcional do objeto.

---

## 3. Decisão consolidada até F13H

Object Entry é, nesta fase, um catálogo lógico passivo. O formato aceito é:

```text
ObjectEntryId
ObjectEntryScope
ObjectEntrySourceKind
ObjectEntryRequiredness
ObjectEntryDescriptor
ObjectEntrySet
ObjectEntryIssue
ObjectEntryRequest / ObjectEntryResult
ObjectEntryDeclaration
ObjectEntryDeclarationSourceResult
ObjectEntryRuntimeContextSnapshot
```

O `FrameworkRuntimeHost` pode armazenar e expor o último snapshot coletado, mas esse snapshot ainda não é atualizado automaticamente pelo lifecycle e não é um registry vivo.

---

## 4. Implementação real

### F13A — Primitivas de Object Entry

Criou identidade tipada, escopo `Session/Route/Activity`, source kind, requiredness, descriptor, request/result, issues e `ObjectEntrySet` imutável.

Decisões:

```text
ObjectEntryId usa FrameworkIdentityDomain.ObjectEntry.
DisplayName é diagnóstico, não identidade.
ObjectEntrySet rejeita ObjectEntryId duplicado.
OwnerIdentity é opcional no descriptor enquanto a policy de owner não está fechada.
```

### F13B — Synthetic Set Smoke

Validou conjunto passivo, required/optional, scopes e rejeição de identidade duplicada sem usar GameObject real.

### F13C — Scene-authored Declaration

Criou `ObjectEntryDeclaration`, componente authored passivo com:

```text
Object Entry Id
Scope
Requiredness
Display Name
```

O componente não faz binding físico e não transforma o próprio GameObject em alvo runtime.

O Fix01 removeu `GetInstanceID()` do caminho de QA para compatibilidade com Unity 6.5.

### F13D/F13E — Declaration Source e diagnostics

`ObjectEntryDeclarationSource` converte declarações em descriptors e produz resultado agregado.

F13E separou corretamente:

```text
declarations
candidateDescriptors
acceptedDeclarations
rejectedDeclarations
resultStatus
```

Quando há identidade duplicada, o conjunto inteiro é rejeitado e nenhuma declaração entra no `ObjectEntrySet` final.

### F13F — Loaded-scene Integration Smoke

Validou coleta de declarações em cenas carregadas, incluindo declaração em GameObject inativo.

Esse caminho é evidência de integração/QA. Ele ainda não é aceito como fonte autoritativa final do lifecycle porque coleta todas as cenas carregadas sem owner ativo explícito.

### F13G — Runtime Context Snapshot

Criou `ObjectEntryRuntimeContextSnapshot` como fotografia passiva do resultado coletado.

O snapshot permite consulta por id e scope e expõe contagens/diagnósticos. Ele não contém binding para GameObject, Transform ou Component.

### F13H — Runtime Host Snapshot Exposure

`FrameworkRuntimeHost` passou a guardar o último snapshot e expor leitura interna controlada:

```text
RefreshObjectEntryRuntimeContextSnapshot(source)
TryGetObjectEntryRuntimeContextSnapshot(out snapshot)
```

O refresh atual é explícito. Boot, troca de Route, entrada/saída de Activity e release ainda não atualizam ou invalidam esse snapshot automaticamente.

---

## 5. Evidência aceita até F13H

### Scene-authored Declaration

```text
objectEntryDeclarations='1'
objectEntries='1'
objectEntryRequired='1'
objectEntryActivity='1'
issues='0'
```

### Declaration Source

```text
resultStatus='Accepted'
declarations='2'
candidateDescriptors='2'
acceptedDeclarations='2'
rejectedDeclarations='0'
objectEntries='2'
blockingIssues='0'
```

Duplicidade:

```text
resultStatus='Rejected'
acceptedDeclarations='0'
rejectedDeclarations='2'
objectEntries='0'
blockingIssues='1'
```

### Runtime Host Snapshot Exposure

```text
hostSnapshotAvailable='True'
snapshotAvailable='True'
resultStatus='Accepted'
declarations='3'
acceptedDeclarations='3'
rejectedDeclarations='0'
qaRouteFound='True'
qaActivityFound='True'
blockingIssues='0'
```

O total `3` é esperado nessa evidência: duas declarações temporárias do smoke e uma declaração authored real já carregada.

---

## 6. Escopo excluído

F13 continua excluindo:

```text
GameObject/Transform/Component binding funcional
entry execution de objeto
Object Reset ou Component Reset
Player, Actor, NPC ou Projectile
prefab materialization ou spawn
pool rent/return
save/checkpoint restore
registry público mutável
service locator
```

---

## 7. Correções necessárias antes do fechamento

### 7.1. Ownership ainda não está resolvido

`ObjectEntryDescriptor` aceita `OwnerIdentity`, mas `ObjectEntryDeclaration` não cria owner e o source atual não resolve owner pelo contexto ativo.

Antes do fechamento, Route/Activity entries precisam carregar owner coerente com o scope:

```text
Route entry -> FrameworkIdentityDomain.Route
Activity entry -> FrameworkIdentityDomain.Activity
Session entry -> FrameworkIdentityDomain.Session
```

Não pode haver fallback por nome de GameObject, path ou cena.

### 7.2. Coleta global não pode virar autoridade do lifecycle

`CollectLoadedSceneDeclarations()` usa `FindObjectsByType` sobre todas as cenas carregadas. Isso é aceitável para QA/diagnóstico temporário, mas não como fonte autoritativa final do runtime.

O caminho canônico deve ser scoped pelo contexto conhecido do framework, seguindo o precedente de Route scene composition, `SceneScopedComponentQuery`, Route owner e Activity owner.

### 7.3. Snapshot ainda pode ficar stale

O host armazena o último snapshot, mas não possui policy de refresh/invalidation. Antes de expor esse snapshot a outras fases, é necessário definir quando ele nasce, muda e deixa de ser válido.

### 7.4. Readiness não deve ser inventada sem execução

O ADR original previa readiness por objeto. Como F13 não executa entrada física nem possui participant runtime, um estado `Ready` seria apenas um espelho artificial de “descriptor aceito”.

Decisão proposta:

```text
F13 fecha catálogo lógico + ownership + snapshot scoped.
Readiness de execução entra em F16, quando existir Participant Entry real.
```

---

## 8. Sequência proposta para fechar F13

| Corte | Status | Objetivo |
|---|---|---|
| F13I | `DOC/AUDIT` | Reconciliar ADR, roadmap e implementação real após F13H. |
| F13J | `PROPOSED` | Fechar owner tipado por scope e substituir o refresh autoritativo global por coleta scoped. |
| F13K | `PROPOSED` | Definir refresh/invalidation do snapshot nos boundaries de Route/Activity, sem binding físico. |
| F13L | `PROPOSED` | Smoke de ownership, filtro de scope e invalidation; fechamento documental da fase. |

Os nomes e a divisão F13J–F13L só se tornam canônicos após revisão desta ADR.

---

## 9. Critério de fechamento

F13 só pode ser marcada `CLOSED / APPLIED` quando:

```text
identidade duplicada continuar bloqueando o set;
owner estiver presente e coerente com Route/Activity/Session scope;
coleta runtime autoritativa não usar all-loaded scan sem filtro;
snapshot tiver policy explícita de refresh/invalidation;
QA provar Route e Activity scope sem foreign/stale entries;
nenhum binding físico, reset, Player/Actor ou spawn entrar na fase.
```

---

## 10. Relação com fases futuras

F13 desbloqueia:

```text
F14 — Local/Object Reset Foundation
F15 — Unity Reset Adapters mínimos
F16 — Player/Participant Entry Baseline e readiness real
```

F13 não desbloqueia diretamente:

```text
Camera
Audio
Actor gameplay
Pooling consumer
Projectile
Damage
Attributes
Powerups
```
