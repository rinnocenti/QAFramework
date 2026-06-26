# F15-ADR-RESET-004 — Unity Reset Adapters minimos

Status: Accepted / Planned through F15A  
Fase: F15 — Unity Reset Adapters minimos  
Tipo: Unity Adapter / Object Reset / Authoring  
Ultima atualizacao: 2026-06-26  
Corte: F15A — ADR

---

## 1. Contexto

F14 fechou a fundacao logica de Object Reset:

```text
ObjectEntryId + Scope + OwnerIdentity
-> ObjectEntryRuntimeContextSnapshot
-> ObjectResetTargetResolver
-> ObjectResetPlan
-> ObjectResetRuntime
-> FrameworkRuntimeHost.RequestObjectResetAsync
-> ObjectResetTrigger
-> ObjectResetTriggerUnityEventBridge opcional
```

F14 nao executa reset fisico Unity. Ela apenas resolve alvo logico, monta plano deterministico, executa participantes sinteticos/contratuais e agrega resultado.

F15 abre os primeiros adapters Unity reais para tornar Object Reset observavel em objetos comuns de cena, sem introduzir gameplay, Player, Actor, pooling ou save/checkpoint.

---

## 2. Problema

Depois da F14, um trigger valido pode terminar como `SucceededNoParticipants` porque ainda nao ha participantes Unity reais. Isso e correto para a fundacao, mas insuficiente para uso pratico.

A F15 precisa responder:

```text
Como um componente Unity real vira IObjectResetParticipant?
Como o Runtime Host recebe a participant source?
Quando ausencia de adapter e erro?
Como aplicar baseline fisico sem fallback silencioso?
Como evitar GameObject.name/path como identidade funcional?
```

---

## 3. Decisoes aceitas

### 3.1 Boundary de assembly para F15

F15 **nao fara split de asmdef**.

O package atual ja possui um Runtime asmdef com `UnityEngine` habilitado e ja contem componentes Unity de authoring/UX, como triggers e bridges. Para F15, os adapters Unity minimos ficam no proprio `com.immersive.framework`.

Decisao:

```text
Nao criar novo asmdef neste corte.
Nao splitar core puro vs Unity adapter dentro da F15.
Manter a separacao por namespace/pasta e contrato.
Registrar split futuro como refactor arquitetural separado, nao como pre-requisito de F15.
```

Regra pratica:

```text
ObjectResetTargetResolver, ObjectResetPlan e ObjectResetRuntime continuam sem dependencia conceitual de Unity.
Adapters Unity podem depender de UnityEngine.
Adapters nao podem mover lifecycle, ownership, identity ou policy para Unity components.
```

### 3.2 Registro de participants/adapters

`FrameworkRuntimeHost` continua sendo o owner do `IObjectResetParticipantSource` ativo.

O setter interno existente pode continuar interno. F15 nao deve exigir que gameplay/user code injete source manualmente.

Decisao:

```text
F15 implementara uma participant source Unity pertencente ao framework.
Essa source sera registrada pelo proprio framework, dentro do package.
Ela fornecera participants ao ObjectResetRuntime.
Ela nao executara reset por conta propria.
Ela nao sera service locator.
Ela nao cria lifecycle paralelo.
```

A source Unity deve coletar apenas participantes explicitos.

Permitido:

```text
Componentes que implementam IObjectResetParticipant.
Componentes de adapter declarados em GameObjects de cena.
Filtro por ObjectResetTarget canonico.
Include inactive quando necessario para objetos de cena explicitamente declarados.
```

Proibido:

```text
Registrar por GameObject.name.
Registrar por hierarchy path.
Registrar por tag/layer como identidade funcional.
Criar singleton global de participants.
Criar manager de reset separado do Runtime Host.
```

### 3.3 Identidade de target dos adapters

Todo adapter Unity real deve declarar para qual target logico ele participa.

Caminho preferido:

```text
ObjectEntryDeclaration
+ Adapter Unity no mesmo GameObject ou em filho controlado
+ adapter referencia a declaration ou resolve ObjectEntryId/scope/owner por ela
```

Caminho manual permitido apenas como authoring explicito:

```text
ObjectEntryId manual quando nao houver Target Declaration atribuida.
```

Nao permitido:

```text
GameObject.name
Transform hierarchy path
Scene path
InstanceID
Sibling index
```

### 3.4 `SucceededNoParticipants` em F15

`SucceededNoParticipants` continua valido para F14 foundation e para requests que explicitamente permitem ausencia de participantes.

Para F15, ausencia de adapter nao pode mascarar erro quando o fluxo espera reset fisico.

Decisao:

```text
Request/policy de reset fisico deve poder exigir pelo menos um participant.
Adapter required ausente deve gerar issue blocking.
Adapter optional ausente pode gerar skip/warning nao bloqueante.
Trigger/authoring deve deixar claro se no participants e permitido.
```

A F15 nao deve transformar `SucceededNoParticipants` em fallback silencioso para reset fisico nao executado.

### 3.5 Required vs Optional

Adapters Unity usam a mesma semantica de participant requiredness da F14:

```text
Required -> falha bloqueia o Object Reset.
Optional -> falha vira warning/skip conforme o motivo.
```

Baseline ausente:

```text
Required adapter + baseline ausente -> FailedBlocking.
Optional adapter + baseline ausente -> SkippedOptional ou warning nao bloqueante.
```

### 3.6 Baseline

F15 prioriza baseline minimo de `Transform`.

Fontes aceitas:

```text
Authored baseline.
Captured baseline em boundary deterministico, se implementado em corte proprio.
```

Proibido:

```text
Capturar baseline silenciosamente no momento do reset.
Usar posicao atual como fallback se baseline obrigatorio estiver ausente.
Recarregar cena para recuperar baseline.
Usar prefab/pool/save como baseline implicito na F15.
```

### 3.7 Ordem de adapters fisicos

A ordenacao canonica continua sendo:

```text
participant.Order
sourceIndex
participantId
```

Bandas sugeridas para F15:

```text
Transform reset: 100
Rigidbody reset/velocity cleanup: 200
Animator reset: 300
```

A decisao operacional e: reset fisico deve ser deterministico e diagnosticavel. Se uma ordem diferente for necessaria em corte especifico, o corte deve justificar no smoke/diagnostics.

### 3.8 GameObject inactive / component disabled

Objetos inativos podem precisar de reset fisico antes de serem reativados.

Decisao:

```text
A source Unity pode coletar adapters em GameObjects inativos quando eles sao explicitamente authored.
Component disabled nao deve ser tratado como participant ativo por default.
Se um reset required depende de adapter desabilitado/ausente, isso deve virar issue explicito.
```

Nenhuma dessas regras autoriza identity por hierarchy/name.

### 3.9 Diagnostics minimos

Logs/smokes de adapters devem declarar:

```text
objectEntry
scope
owner
participantId
adapterType
requiredness
baselineSource
order
applied fields
status
issues
blockingIssues
nonBlockingIssues
```

Diagnostics nao devem despejar detalhes excessivos de Transform por frame nem gerar ruido em smokes felizes.

---

## 4. Escopo da F15

Incluido:

```text
Unity Object Reset participant source.
Authoring minimo para adapters Unity.
Transform reset participant com baseline explicito.
Policy/diagnostics para ausencia de required adapter/baseline.
Smokes de reset fisico minimo.
Documentacao curta de uso.
```

Pode entrar se o corte permanecer pequeno:

```text
Rigidbody velocity cleanup.
Animator reset minimo.
```

Fora da F15:

```text
Player stats.
Player movement design.
Health.
Powerups.
Combat.
Actor behavior.
NPC behavior.
Projectile.
Pooling return/despawn.
Save/checkpoint restore.
Scene reload como reset.
Addressables/prefab rematerialization.
Camera/audio/gameplay consumers.
```

---

## 5. Sequencia proposta de cortes

```text
F15B — Unity Object Reset Participant Source
F15C — Transform Reset Participant / Authored Baseline
F15D — Missing Required Adapter/Baseline Guardrails
F15E — Optional Rigidbody/Animator candidate, only if still minimal
F15F — Unity Reset Adapters Closure Smoke + docs/QA cleanup
```

A sequencia pode ser ajustada se F15B revelar acoplamento indevido.

---

## 6. UX esperada

Fluxo recomendado para GameDesigner:

```text
1. Criar/selecionar um GameObject de cena.
2. Adicionar Object Entry Declaration.
3. Adicionar Object Reset Trigger onde fizer sentido.
4. Adicionar Transform Reset Participant no alvo ou em objeto relacionado.
5. Configurar baseline authored ou captura deterministica quando disponivel.
6. Chamar ObjectResetTrigger.RequestObjectReset() por UI/Button ou ContextMenu.
```

Inspector deve explicar:

```text
Este componente participa do Object Reset canonico.
Ele nao executa reset sozinho.
Ele nao recarrega cena.
Ele nao controla Route/Activity lifecycle.
Ele nao substitui pooling, save ou gameplay reset.
```

---

## 7. Consequencias

### Positivas

- Object Reset deixa de ser apenas orquestracao logica.
- Reset de Transform fica validavel por smoke real.
- Player/Actor futuro pode consumir adapters tecnicos sem redefinir core.

### Custos

- A superficie Unity do framework aumenta.
- A politica de no-participants precisa ser explicita.
- Source/adapter devem ser bem diagnosticados para nao virar fallback silencioso.

---

## 8. Guardrails finais

- Nao usar F15 para implementar Player/Actor reset.
- Nao usar F15 para pooling.
- Nao usar reset fisico como lifecycle paralelo.
- Nao mover identity/owner/scope para components Unity.
- Nao usar `GameObject.name`, path ou InstanceID como identidade funcional.
- Nao fazer fallback silencioso quando adapter required estiver ausente.
- Nao capturar baseline no momento do reset.
- Nao criar singleton/global reset manager.
