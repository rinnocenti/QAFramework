# FXX-AUDIT — Participant & Flow Pattern Duplication

Status: Draft / audit-only / documentation governance

Scope: code-level audit of `com.immersive.framework` Runtime. Does not implement or authorize
any code change. Feeds `FXX-ADR-CONSOLIDATION-001` and `FXX-PLAN — Participant & Flow Pattern
Consolidation Plan`.

Revisão baseada em leitura direta do código (não só nos nomes de arquivo). Comparei o padrão
"Participant" em `CycleReset`, `ObjectReset` e `Snapshot`, e o par `ActivityRequestTrigger` /
`RouteRequestTrigger` em `GameFlow`.

## Contexto rápido

O pacote é bem ambicioso: ~30 subpastas em `Runtime/`, centenas de arquivos `.cs`, um por tipo
(cada enum, struct, result e status tem o próprio arquivo). O README mostra que isso é
deliberado — é um projeto guiado por fases (F11, F21, F24...), com ADRs e "anti-regression
rules". Ou seja, não é descuido, é uma arquitetura "contract-first" levada ao extremo. Isso muda
o tipo de recomendação: o problema não é falta de disciplina, é falta de **abstração
compartilhada** — a disciplina foi reaplicada manualmente (ou via IA) toda vez que um domínio
novo nasceu, em vez de ser extraída uma vez só.

---

## 1. Duplicação estrutural — o "padrão Participant"

Encontrei o mesmo desenho, quase arquivo por arquivo, em pelo menos **5 domínios**: `CycleReset`,
`ObjectReset`, `Snapshot`, `ActivityFlow` (`IActivityContentExecutionParticipant`) e
`LocalContribution`.

Para cada domínio existe:

```
I<Domain>Participant.cs
I<Domain>ParticipantSource.cs
<Domain>ParticipantDescriptor.cs      (struct, ~120 linhas)
<Domain>ParticipantEntry.cs           (struct, ~70 linhas)
<Domain>ParticipantId.cs
<Domain>ParticipantRequiredness.cs    (enum: Unknown/Required/Optional)
<Domain>ParticipantResult.cs          (struct, ~150 linhas)
<Domain>ParticipantResultStatus.cs    (enum: Unknown/Succeeded/Skipped.../Failed...)
Empty<Domain>ParticipantSource.cs
```

Comparando `CycleResetParticipantDescriptor` e `ObjectResetParticipantDescriptor`: construtor
idêntico (mesma ordem de validações, mesmas exceptions, mesmo `Equals`/`GetHashCode` com
`unchecked` + `397`, mesmo `ToDiagnosticString`, mesmos factory methods
`Required(...)`/`Optional(...)`). A única diferença real é o campo de "alvo" (`CycleResetScope`
vs `ObjectResetTarget` vs `SnapshotScope+OwnerIdentity+SchemaId+SchemaVersion`).

O mesmo vale para o runtime executor: `CycleResetRuntime.ExecutePlan` e `ObjectResetRuntime.Execute`
fazem exatamente a mesma coisa — iteram entries, chamam o participante num `try/catch`, mapeiam
exceção para `BlockingFailure`/`NonBlockingFailure`, contam issues bloqueantes vs não-bloqueantes,
decidem `Succeeded` / `CompletedWithWarnings` / `Failed`. É o mesmo algoritmo reescrito 4-5 vezes
com nomes diferentes.

**Por que isso importa:** cada vez que um "tipo de participante" novo é necessário (ex: um
sistema de achievements que reage a reset), nada é reaproveitado — copiam-se ~8 arquivos e troca-se
o prefixo. Isso é o oposto de "fácil de replicar". E bug-fix em um executor não se propaga para
os outros 4 — já estão dessincronizados sutilmente hoje (ex: `CycleResetRuntime` valida
`IsValidResultForEntry` comparando request/participantId/scope/requiredness; `ObjectResetRuntime`
não faz essa validação simétrica).

**Recomendação concreta:** extrair um genérico em `Common/Participants/`:

```csharp
public interface IParticipant<TDescriptor, TContext, TResult> { ... }
public readonly struct ParticipantDescriptor<TId, TTarget> { ... }
public readonly struct ParticipantResult<TStatus> { ... }
public sealed class ParticipantExecutor<TParticipant, TDescriptor, TContext, TResult, TIssue>
{
    public ParticipantExecutionResult Execute(IReadOnlyList<...> entries, Func<TParticipant,TContext,TResult> invoke, ...) { ... }
}
```

Cada domínio (`CycleReset`, `ObjectReset`, `Snapshot`...) vira uma instanciação fina desse
genérico + o que for *realmente* específico (ex: `SnapshotParticipantDescriptor` mantém
`SchemaId`/`SchemaVersion` como dados extras, mas a validação de requiredness/equals/diagnostics
vem do genérico).

---

## 2. Duplicação por "espelhamento" Activity ↔ Route

O framework trata `Activity` e `Route` como dois conceitos paralelos, e isso se repete em quase
toda pasta:

- `ActivityRequestTrigger.cs` / `RouteRequestTrigger.cs` (`GameFlow/`) — estrutura idêntica
  (mesmo `EventBus`, mesmo padrão `_requestInFlight`/`PublishSubmitted`/`PublishCompleted`/
  `SetRequestState`/`MapOutcome`). A única diferença real é que Activity também tem
  `ClearActivity()`.
- `ActivityCycleResetTrigger` / `RouteCycleResetTrigger` (+ seus `...UnityEventBridge`)
- `ActivitySceneCompositionPlan/Result/Runtime/Status` / `RouteSceneCompositionPlan/Result/Runtime/Status`
- `ActivityContentLifecycle...` / `RouteContentLifecycle...`

Isso sugere que `Activity` e `Route` deveriam compartilhar uma base comum
(`FlowRequestTrigger<TAsset>`, `SceneCompositionRuntime<TScope>`) com Activity adicionando só o
que é exclusivo dela (ex: `ClearActivity`). Hoje, qualquer ajuste de comportamento precisa ser
replicado nos dois lados manualmente, e é fácil esquecer um — como o `ClearActivity` que só
existe do lado Activity sugere que já houve divergência.

---

## 3. Pasta `Common/` praticamente vazia

`Runtime/Common/` tem **um único arquivo** (`FrameworkStringExtensions.cs`, normalização de
string). Dado o volume de lógica duplicada em §1 e §2, isso é o sintoma mais claro do problema:
existe disciplina para "todo domínio precisa ter Descriptor/Result/Status/Issue", mas não existe
o reflexo de "então isso vira uma abstração comum". `Common/` deveria concentrar:

- O genérico de Participant (§1)
- Um `OperationResult<TStatus>` genérico (§4)
- Helpers de validação repetidos (`Enum.IsDefined(...) || value == Unknown` aparece dezenas de
  vezes, sempre igual)

---

## 4. Result/Status/Issue: um trio reinventado por domínio

Praticamente todo domínio define seu próprio:
- `<X>Result` (struct com `Status`, `Source`, `Reason`, `Message`, validações)
- `<X>ResultStatus` (enum `Unknown=0, Succeeded=10, ...Failed...=1xx`)
- `<X>Issue` / `<X>IssueKind` / `<X>IssueSeverity`

A convenção de valores de enum (`Unknown=0`, sucesso na casa de `10-30`, skip em `20-30`, falha
em `100+`) é **consistente** entre domínios — o que prova que existe um padrão mental único, só
não foi materializado em código. A *estrutura* do struct `Result` (Status + Source + Reason +
Message + Succeeded/Failed/issue aggregation) dá perfeitamente para um genérico
`OperationResult<TStatus> where TStatus : Enum`.

---

## 5. Coisas boas que valem a pena preservar

- **Separação contracts vs Unity vs adapters** é saudável e rara de ver bem feita.
- **Imutabilidade** (`readonly struct`, validação no construtor, sem setters) deixa o estado
  difícil de corromper — bom para lifecycle de jogo.
- **Result objects em vez de exceptions para fluxo de controle** é a escolha certa para
  lifecycle.
- **Fontes explícitas (`I...ParticipantSource`) em vez de busca global de cena** evita o clássico
  `FindObjectsOfType` espalhado.

O problema não é a filosofia, é que ela foi **copiada manualmente** em vez de **extraída uma
vez**.

---

## 6. Riscos práticos de manutenção, dado o tamanho atual

- **Custo de onboarding**: alguém novo no time vai abrir `ActivityFlow/` e ver ~90 arquivos para
  um conceito que, simplificado, é "executa N participantes, agrega resultado".
- **Risco de regressão silenciosa**: `CycleResetRuntime` vs `ObjectResetRuntime` já divergem
  sutilmente (um valida resultado retornado contra o request original, o outro não).
- **Build/compile time**: múltiplos `.asmdef` + centenas de arquivos pequenos custam tempo de
  domain reload no editor Unity.

---

## 7. Recomendações priorizadas

| Prioridade | Ação | Esforço | Ganho |
|---|---|---|---|
| Alta | Extrair `ParticipantExecutor<T>` genérico e migrar `CycleReset`/`ObjectReset`/`Snapshot`/`ActivityContentExecution` para ele | Alto (refator extenso, mas mecânico) | Elimina ~60-70% da duplicação de código nesses 4 domínios |
| Alta | Unificar `Activity*Trigger`/`Route*Trigger` numa base genérica | Médio | Reduz divergência comportamental (`ClearActivity` já é uma assimetria) |
| Média | Criar `OperationResult<TStatus>` genérico em `Common/` | Médio | Padroniza o trio Result/Status/Issue que hoje é copy-paste |
| Média | Adicionar um `Common/Validation/` com helpers (`EnsureDefinedNotUnknown<T>`, etc.) | Baixo | Remove repetição de `Enum.IsDefined(...) || x == Unknown` |
| Baixa | Revisar se todo enum/struct realmente precisa de arquivo próprio | Baixo | Reduz ruído de navegação, não muda comportamento |

---

## Referências

- ADR: `Packages/com.immersive.framework/Documentation~/ADRs/FXX-ADR-CONSOLIDATION-001-Participant-And-Flow-Pattern-Consolidation.md`
- Plan: `Assets/_Documentation/Plans/FXX-PLAN-Participant-And-Flow-Pattern-Consolidation.md`
