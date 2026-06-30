# FXX-PLAN — Participant & Flow Pattern Consolidation Plan

Status: Proposed / Revised Plan
Scope: implementation plan for `FXX-ADR-CONSOLIDATION-001`.
Última atualização: 2026-06-30

Este plano executa apenas a primeira consolidação interna do padrão Participant.

Ele não implementa UX de authoring, não altera gameplay, não migra Snapshot, não migra ActivityContentExecution, não migra LocalContribution e não transforma `ActivityRequestTrigger` / `RouteRequestTrigger` em uma base `MonoBehaviour` genérica nesta primeira leva.

Pré-requisito: `FXX-ADR-CONSOLIDATION-001-Participant-And-Flow-Pattern-Consolidation` revisado e aceito.

---

## 1. Objetivo do plano

Sair de:

```text
CycleReset e ObjectReset mantendo cópias manuais do mesmo algoritmo de participant execution
```

Para:

```text
CycleReset e ObjectReset compondo um executor comum em Runtime/Common/Participants,
sem mudança de API pública e sem mudança de comportamento observável.
```

O plano é propositalmente conservador. Ele prova o valor com dois domínios:

```text
CycleReset  -> alvo simples: CycleResetScope
ObjectReset -> alvo resolvido por domínio: ObjectResetTarget
```

Se esses dois pilotos passarem com paridade, a continuidade para `Snapshot` e `ActivityContentExecution` será decidida no closeout. Se não passarem, o genérico deve ser rejeitado ou reduzido.

---

## 2. Decisões incorporadas nesta revisão

### 2.1. LocalContribution corrigido

`LocalContribution` não será tratado como participante equivalente nesta fase.

Ele pode compartilhar padrões de result/issue/set/validation, mas não entra no piloto porque o pacote atual não apresenta o mesmo contrato direto de:

```text
I<Domain>Participant
I<Domain>ParticipantSource
<Domain>ParticipantDescriptor
<Domain>ParticipantEntry
<Domain>ParticipantResult
```

### 2.2. Requiredness público preservado

Não substituir enums públicos por um enum comum.

Permitido:

```text
Common requiredness interno para o executor.
Mapper explícito por domínio.
```

Proibido:

```text
Trocar CycleResetParticipantRequiredness por Common.ParticipantRequiredness.
Trocar ObjectResetParticipantRequiredness por Common.ParticipantRequiredness.
Trocar SnapshotParticipantRequiredness por Common.ParticipantRequiredness.
Assumir que ActivityContentExecutionRequiredness usa os mesmos valores dos outros domínios.
```

Motivo: `ActivityContentExecutionRequiredness` usa `Optional=10` e `Required=20`, enquanto `CycleReset`, `ObjectReset` e `Snapshot` usam `Required=10` e `Optional=20`.

### 2.3. FlowRequestTrigger removido da primeira implementação

A duplicação entre `ActivityRequestTrigger` e `RouteRequestTrigger` é real, mas a solução via `FlowRequestTrigger<TAsset> : MonoBehaviour` pode afetar serialização/Inspector.

Nesta fase, `CONS-E` vira apenas avaliação documental/spike de risco. A implementação de uma base `MonoBehaviour` fica fora de escopo até existir fase própria.

### 2.4. UX de authoring fica fora

Este plano melhora manutenção interna. Ele não promete que um game designer terá menos passos no Inspector.

Uma frente separada recomendada é:

```text
FXX-AUTHORING-UX — Framework Practical Use / Game Designer Surface
```

---

## 3. Linha de cortes revisada

| Corte | Nome | Tipo | Depende de | Entra na implementação piloto? |
|---|---|---|---|---|
| CONS-A | Common Results & Validation Primitives | Implementação aditiva | ADR aceito | Sim |
| CONS-B | Common Participant Executor | Implementação aditiva | CONS-A | Sim |
| CONS-C | CycleReset Pilot Migration | Refator interno | CONS-B | Sim |
| CONS-D | ObjectReset Pilot Migration | Refator interno | CONS-C | Sim |
| CONS-E0 | Flow Request Trigger Consolidation Risk Note | Documentação / spike | Independente | Não implementa base MonoBehaviour |
| CONS-F | Closeout / Decision Point | Documentação | CONS-C, CONS-D, CONS-E0 | Sim |

---

## 4. CONS-A — Common Results & Validation Primitives

### Objetivo

Criar primitivos comuns pequenos e aditivos, sem tocar em `CycleReset`, `ObjectReset`, `Snapshot`, `ActivityFlow` ou `LocalContribution`.

### Escopo incluído

```text
Runtime/Common/Results/OperationResult<TStatus>.cs
Runtime/Common/Validation/FrameworkEnumValidation.cs
Runtime/Common/Participants/CommonParticipantRequiredness.cs
```

`OperationResult<TStatus>` deve ser um container genérico, não um status universal.

Campos esperados:

```text
TStatus Status
string Source
string Reason
string Message
ToDiagnosticString()
```

`Succeeded`/`Failed` não devem ser hardcoded por valor numérico. Opções aceitáveis:

```text
1. O domínio fornece predicate externo.
2. O domínio mantém as propriedades Succeeded/Failed em seu próprio result wrapper.
```

`CommonParticipantRequiredness` deve ser interno/aditivo e usado apenas como representação normalizada para o executor.

### Escopo excluído

```text
Nenhuma mudança em CycleReset.
Nenhuma mudança em ObjectReset.
Nenhuma mudança em Snapshot.
Nenhuma mudança em ActivityContentExecution.
Nenhuma mudança em LocalContribution.
Nenhum enum público substituído.
Nenhum ParticipantDescriptor/Entry/Executor ainda.
```

### Critério de aceite

```text
Common compila.
Nenhum output de smoke existente muda.
OperationResult smoke sintético cobre ToDiagnosticString e normalização de texto.
Enum validation smoke cobre Unknown/defined/undefined com enum fictício.
```

### Observação de implementação

Reaproveitar `FrameworkStringExtensions.NormalizeText()` e `NormalizeTextOrFallback()` já existentes em `Runtime/Common/`.

---

## 5. CONS-B — Common Participant Executor

### Objetivo

Criar o executor genérico e seus wrappers mínimos usando participantes sintéticos. Nenhum domínio real é migrado neste corte.

### Escopo incluído

```text
Runtime/Common/Participants/ParticipantDescriptor<TId, TTarget>.cs
Runtime/Common/Participants/ParticipantEntry<TParticipant, TDescriptor>.cs
Runtime/Common/Participants/ParticipantExecutionIssue<TIssueKind>.cs ou shape equivalente
Runtime/Common/Participants/ParticipantExecutor<...>.cs
Runtime/Diagnostics/CommonParticipantExecutorSmokeRunner.cs
```

O executor deve ser orientado por delegates/policies fornecidos pelo domínio:

```text
invoke participant
map exception to result/issue
read participant id from descriptor
read target from descriptor
read requiredness from descriptor
validate returned result against entry
classify result as success / skipped / blocking failure / non-blocking failure
build aggregate result for the domain
```

### Decisão sobre ParticipantId genérico

Não criar `ParticipantId` genérico obrigatório neste corte.

Motivo:

```text
CycleResetParticipantId, ObjectResetParticipantId e SnapshotParticipantId já são typed IDs de domínio.
O genérico deve receber TId; não substituir identities existentes.
```

Um wrapper genérico de ID só pode ser criado se o corte demonstrar valor real sem reduzir typed identity.

### Escopo excluído

```text
Nenhuma migração de CycleReset.
Nenhuma migração de ObjectReset.
Nenhum wrapper genérico público de ID obrigatório.
Nenhuma source discovery genérica ainda, a menos que seja puramente sintética para smoke.
Nenhum dependency em UnityEngine.
Nenhuma alteração de asmdef além de arquivos no assembly runtime atual.
```

### Critério de aceite

`CommonParticipantExecutorSmokeRunner` cobre:

```text
1. Nenhum participante.
2. Participante required bem-sucedido.
3. Participante optional bem-sucedido.
4. Participante optional falha não bloqueante.
5. Participante required falha bloqueante.
6. Participante lança exception.
7. Participante retorna resultado inválido para a entry atual.
8. Participante duplicado, se o executor/plan builder assumir duplicidade.
```

A validação de resultado inválido deve reproduzir a proteção que já existe em `CycleResetRuntime` e que falta em `ObjectResetRuntime`.

---

## 6. CONS-C — CycleReset Pilot Migration

### Objetivo

Migrar `CycleReset` internamente para compor os primitivos comuns, sem alterar sua API pública.

### Escopo incluído

```text
CycleResetParticipantDescriptor
    - passa a compor ParticipantDescriptor<CycleResetParticipantId, CycleResetScope> internamente
    - mantém construtor público e propriedades públicas existentes
    - mantém Required()/Optional() públicos existentes

CycleResetParticipantEntry
    - passa a compor ParticipantEntry<> internamente, se isso não afetar API pública

CycleResetParticipantResult
    - pode compor OperationResult<CycleResetParticipantResultStatus>
    - mantém factories e propriedades públicas existentes

CycleResetRuntime.ExecutePlan
    - delega loop de execução para ParticipantExecutor
    - mantém assinatura pública de CreatePlan/ExecutePlan
```

### Escopo excluído

```text
Nenhuma mudança em ICycleResetParticipant.
Nenhuma mudança em ICycleResetParticipantSource.
Nenhuma mudança em CycleResetRequest.
Nenhuma mudança em CycleResetPlan.
Nenhuma mudança em CycleResetIssue.
Nenhuma mudança em CycleResetParticipantRequiredness público.
Nenhuma mudança em CycleResetParticipantResultStatus.
Nenhuma mudança nos triggers RouteCycleResetTrigger/ActivityCycleResetTrigger.
Nenhuma alteração de texto de smoke para mascarar diferença.
```

### Critério de aceite

Paridade obrigatória:

```text
CycleResetQaSmokeRunner produz o mesmo texto antes e depois.
Route Cycle Reset mantém status, participants e blockingIssues esperados.
Activity Cycle Reset mantém status, participants e blockingIssues esperados.
Trigger smoke mantém SucceededNoParticipants quando não há participantes.
```

Regra de rejeição:

```text
Se qualquer linha de diagnóstico mudar sem justificativa formal, o corte é rejeitado.
O ajuste deve ser no genérico ou na composição, não no smoke.
```

---

## 7. CONS-D — ObjectReset Pilot Migration

### Objetivo

Migrar `ObjectReset` internamente para compor os primitivos comuns, provando que o executor cobre um alvo resolvido por domínio.

### Escopo incluído

```text
ObjectResetParticipantDescriptor
    - compõe ParticipantDescriptor<ObjectResetParticipantId, ObjectResetTarget>
    - mantém construtor público e propriedades públicas existentes
    - mantém Required()/Optional() públicos existentes

ObjectResetParticipantEntry
    - compõe ParticipantEntry<> internamente, se isso não afetar API pública

ObjectResetResult / ObjectResetParticipantResult
    - pode compor OperationResult<TStatus> quando aplicável
    - mantém factories e propriedades públicas existentes

ObjectResetRuntime.BuildPlan
    - mantém ObjectResetTargetResolver.ResolveTarget fora do genérico
    - target resolution continua lógica do domínio ObjectReset

ObjectResetRuntime.Execute
    - delega execução dos participantes já resolvidos para ParticipantExecutor
    - passa a herdar a validação de resultado contra entry/request/participantId/target/requiredness
```

### Escopo excluído

```text
Nenhuma mudança em IObjectResetParticipant.
Nenhuma mudança em IObjectResetParticipantSource.
Nenhuma mudança em ObjectResetTargetResolver.
Nenhuma mudança em ObjectResetRequest.
Nenhuma mudança em ObjectResetIssue.
Nenhuma mudança em ObjectResetParticipantRequiredness público.
Nenhuma mudança em ObjectResetParticipantResultStatus.
Nenhuma mudança nos adapters Unity de ObjectReset.
```

### Critério de aceite

```text
ObjectResetQaSmokeRunner produz a mesma saída antes e depois.
```

Único desvio comportamental autorizado:

```text
Se um participante retornar resultado com participantId/request/target/requiredness diferente da entry atual,
o comportamento antigo podia aceitar silenciosamente; o novo deve rejeitar com issue explícita.
```

Esse desvio deve ser documentado no fechamento como correção de bug e paridade com a proteção já existente em `CycleReset`.

---

## 8. CONS-E0 — Flow Request Trigger Consolidation Risk Note

### Objetivo

Registrar a duplicação entre `ActivityRequestTrigger` e `RouteRequestTrigger`, mas não implementar base `MonoBehaviour` genérica nesta fase.

### Escopo incluído

Criar ou atualizar documentação com:

```text
Duplicação confirmada entre ActivityRequestTrigger e RouteRequestTrigger.
Lista de métodos/campos duplicados.
Risco de serialização Unity com base genérica MonoBehaviour.
Alternativa recomendada: extrair primeiro um core/helper não-MonoBehaviour.
Decisão: implementação adiada para fase própria.
```

### Escopo excluído

```text
Não criar FlowRequestTrigger<TAsset> : MonoBehaviour.
Não mover [SerializeField] para classe base genérica.
Não alterar ActivityRequestTrigger.
Não alterar RouteRequestTrigger.
Não alterar UnityEvent bridges.
Não alterar fields visíveis no Inspector.
```

### Critério de aceite

```text
Documento de risco criado ou seção de closeout adicionada.
Nenhum arquivo runtime de trigger alterado.
Nenhum prefab/scene precisa ser reserializado.
```

### Candidato futuro

Uma fase futura pode avaliar:

```text
GameFlow/FlowRequestStateCore.cs
GameFlow/FlowRequestEventPublisher.cs
GameFlow/FlowRequestOutcomeMapper.cs
```

Esses helpers não-MonoBehaviour reduziriam duplicação sem mexer primeiro em serialização Unity.

---

## 9. CONS-F — Closeout / Decision Point

### Objetivo

Fechar a fase com evidência, sem selecionar automaticamente a próxima migração.

### Conteúdo obrigatório

```text
1. Lista de arquivos adicionados em Common.
2. Lista de arquivos alterados em CycleReset.
3. Lista de arquivos alterados em ObjectReset.
4. Evidência de CommonParticipantExecutorSmokeRunner.
5. Evidência de CycleResetQaSmokeRunner.
6. Evidência de ObjectResetQaSmokeRunner.
7. Registro do bugfix ObjectReset, se aplicado.
8. Nota de risco sobre FlowRequestTrigger.
9. Decisão explícita: continuar ou parar a migração de domínios Participant.
10. Decisão explícita: abrir ou não uma fase separada de Authoring UX.
```

### Métricas úteis

```text
Quantidade de linhas removidas ou simplificadas.
Quantidade de métodos duplicados substituídos por Common.
Quantidade de domínios ainda não migrados.
Quantidade de diagnostics preservados byte-a-byte.
```

### Decisões possíveis no fechamento

```text
A. Migrar Snapshot em fase separada.
B. Migrar ActivityContentExecution em fase separada.
C. Revisar LocalContribution apenas para Result/Issue/Validation, sem tratá-lo como Participant.
D. Parar a migração e manter Common apenas para CycleReset/ObjectReset.
E. Abrir FXX-AUTHORING-UX como frente separada.
F. Abrir FlowRequest shared-core phase, sem MonoBehaviour genérico inicial.
```

---

## 10. Non-goals

```text
Nenhuma mudança de comportamento observável fora do bugfix ObjectReset explicitamente autorizado.
Nenhuma migração de Snapshot.
Nenhuma migração de ActivityContentExecution.
Nenhuma migração de LocalContribution.
Nenhuma mudança em ActivitySceneComposition / RouteSceneComposition.
Nenhuma mudança em ActivityContentLifecycle / RouteContentLifecycle.
Nenhuma alteração de MonoBehaviour serializado.
Nenhuma alteração de Inspector field order.
Nenhuma remoção de arquivo antes de paridade comprovada por smoke.
Nenhum singleton.
Nenhum service locator.
Nenhuma reflection nova.
Nenhuma mudança de asmdef estrutural.
Nenhuma seleção de F34/gameplay/adapter modules.
Nenhuma promessa de UX de authoring.
```

---

## 11. Open Questions

| Questão | Decisão nesta fase | Quem decide |
|---|---|---|
| `ParticipantId` genérico compensa? | Não obrigatório. `TId` tipado por domínio é suficiente para o piloto. | Revisão técnica no CONS-B |
| `OperationResult<TStatus>` deve calcular `Succeeded`/`Failed` sozinho? | Não hardcoded. Usar predicate ou wrapper de domínio. | CONS-A |
| Common requiredness deve ser public? | Preferir internal/aditivo. Não substituir enums públicos. | CONS-A |
| Snapshot entra agora? | Não. Só decisão em CONS-F. | CONS-F |
| ActivityContentExecution entra agora? | Não. Só decisão em CONS-F. | CONS-F |
| LocalContribution é Participant? | Não tratar como equivalente nesta fase. | CONS-F, se reavaliar |
| FlowRequestTrigger<TAsset> entra agora? | Não. Só risk note/spike documental. | CONS-E0 |
| Authoring UX entra agora? | Não. Frente separada. | CONS-F |

---

## 12. Candidate Implementation Cuts After CONS-F

Apenas candidatos. Este plano não autoriza estes cortes automaticamente.

| Candidato | Tipo | Depende de |
|---|---|---|
| Snapshot Participant Migration | Refator interno | CONS-F decision point |
| ActivityContentExecution Participant Migration | Refator interno | CONS-F decision point |
| LocalContribution Result/Issue/Validation Consolidation | Refator interno limitado | CONS-F decision point |
| Flow Request Shared Core | Refator interno sem MonoBehaviour genérico inicial | CONS-E0 + fase própria |
| Activity/Route SceneComposition Consolidation | Refator interno maior | Fase própria |
| Activity/Route ContentLifecycle Consolidation | Refator interno maior | Fase própria |
| Authoring UX / Game Designer Surface | Editor tooling / docs / templates | Fase própria |

---

## 13. Checklist por corte

Antes de implementar qualquer corte deste plano:

```text
1. O corte toca somente o escopo permitido?
2. O corte preserva API pública do domínio?
3. O corte preserva enums públicos e valores serializados?
4. O corte evita fallback silencioso?
5. O corte evita reflection/singleton/service locator?
6. O corte tem smoke sintético ou smoke de domínio?
7. O corte preserva texto de diagnostics existente?
8. O corte não antecipa Snapshot/ActivityContentExecution/LocalContribution?
9. O corte não mexe em MonoBehaviour serializado?
10. O fechamento explica o que foi feito em linguagem simples?
```

---

## 14. Decision Point

Executar:

```text
CONS-A -> CONS-B -> CONS-C -> CONS-D -> CONS-E0 -> CONS-F
```

Não executar nesta fase:

```text
Snapshot migration
ActivityContentExecution migration
LocalContribution migration
FlowRequestTrigger<TAsset> MonoBehaviour base
Authoring UX
Gameplay/consumer modules
```
