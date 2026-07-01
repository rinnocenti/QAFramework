# FXX-AUDIT - LIFECYCLE-A - Route/Activity Lifecycle Sequence Audit

## Executive summary

`RouteLifecycle` e `ActivityFlow` compartilham a mesma forma geral de orquestracao:
validacao fail-fast, construcao de plano, composicao de cenas, aplicacao/descoberta de conteudo, limpeza de bindings, remocao de scope e fechamento diagnostico.

Elas nao compartilham a mesma semantica.

Os dois fluxos divergem em pontos estruturais:

- `Route` orquestra `ActivityFlow` e possui `startup activity` preview.
- `Activity` trabalha com `ActivityOperationPlan`, `ActivityReadinessState` e ledger de cenas proprias.
- `Route` faz release de conteudo por plano.
- `Activity` faz release por ledger e `ReleasePolicy`.
- `Route` descobre anchors em cenas de `Route`.
- `Activity` descobre anchors em cenas de `Route` e em cenas owned por `Activity`.

Conclusao: existe material suficiente para uma `LIFECYCLE-B` ADR, mas ainda nao para um kernel amplo sem escopo restrito. O primeiro corte seguro, se a equipe quiser implementar algo, e o tail mecanico de `scope enter/exit + cleanup + merge`, nao a composicao de cenas nem a semantica de conteudo.

## Route lifecycle sequence

Sequencia observada em `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`:

1. `StartRouteAsync(...)` valida `route`, `HasPrimaryScene` e normaliza `source/reason`.
2. `PreviewRouteStartupActivityOperation(...)` monta o preview da startup activity.
3. `CreateReleasePlan(...)` vem do `RouteContentSet` atual, ou `ContentReleasePlan.Empty(...)` quando nao ha conteudo anterior.
4. `RouteContentMaterializationPlan.FromRoute(route)` e `RouteSceneCompositionPlan.FromRoute(route, source, reason)` constroem os planos de conteudo e cena.
5. O runtime calcula budget de progresso com quatro blocos: release de cenas da activity anterior, release de conteudo de route, load de cenas de route e startup activity.
6. `RouteContentRuntime.ExitRouteContent(previousRoute, route, source, reason)` despacha callbacks de saida do conteudo da route anterior.
7. `_activityFlowRuntime.ReleaseActivityScenesForRouteChangeAsync(...)` libera cenas owned por Activity que devem cair na troca de Route.
8. `_contentReleaseRuntime.ExecuteAsync(...)` executa o release do conteudo da Route anterior.
9. `_routeSceneCompositionRuntime.ExecuteAsync(...)` carrega a primary scene e as additive scenes da Route nova.
10. `CreateRouteScopeRoot(...)` registra o scope root de `RuntimeContent`.
11. `_contentAnchorDiscoveryRuntime.DiscoverRouteAnchors(...)` faz descoberta de anchors em cenas carregadas da Route.
12. `RouteContentRuntime.EnterRouteContent(...)` despacha callbacks de entrada do conteudo de Route nova.
13. `_activityFlowRuntime.StartStartupActivityAsync(...)` inicia a startup activity, se existir.
14. `CleanupPreviousRouteContentAnchorBindings(...)` limpa bindings do owner da Route anterior.
15. `RemovePreviousRouteScopeRoot(...)` remove o scope root anterior.
16. `MergeRouteScopeResults(...)` consolida enter/exit/context em `RuntimeScopeLifecycleResult`.
17. `RouteLifecycleStartResult.StartedWith(...)` monta o shell diagnostico final.

Pontos estruturais relevantes:

- `RouteRuntimeState` guarda `SceneLifecycleResult`, `RouteSceneCompositionResult`, `RouteContentSet`, `ContentAnchorDiscoveryResult` e `ActivityFlowResult`.
- `RouteLifecycleStartResult` agrega `RouteExitResult`, `RouteSceneCompositionResult`, `RouteContentSet`, `ContentAnchorDiscoveryResult`, `RouteContentLifecycleDispatchResult`, `ContentReleaseResult`, `ActivityFlowStartResult`, `RuntimeScopeLifecycleResult`, `ContentAnchorBindingLifecycleResult` e `ActivitySceneReleaseResult`.
- O fluxo de Route e orquestrador sobre `ActivityFlow`, nao uma casca paralela.

## Activity lifecycle sequence

Sequencia observada em `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs`:

1. `StartStartupActivityAsync(...)` valida `route` e normaliza `source/reason`.
2. Se a `Route` nao tem startup activity, o runtime monta `ActivityOperationResult.NotRequested(...)`, zera o estado atual e executa:
   - `ApplyActivityContentThroughLifecycleEvents(...)`
   - `ExecuteActivityContentLifecycle(...)`
   - `CleanupPreviousActivityContentAnchorBindings(...)`
   - `RemovePreviousActivityScopeRoot(...)`
   - `ActivityFlowStartResult.SkippedNoStartupActivity(...)`
3. Se ha startup activity, `PreviewActivityOperation(...)` cria o preview da operacao.
4. `StartActivityCoreAsync(...)` cria o scope root da Activity atual com `CreateActivityScopeRoot(...)`.
5. O runtime resolve budget de progresso via `ResolveActivityOperationForProgress(...)` e `CountActivityOperationSceneSideEffects(...)`.
6. `ExecuteActivitySceneCompositionAsync(...)` carrega as cenas additive da Activity.
7. Em falha blocking, remove o scope root recem-criado e restaura o estado.
8. `ApplyActivityContentThroughLifecycleEvents(...)` dispara os eventos de entrada/saida de Activity e aplica o conteudo local.
9. `_contentAnchorDiscoveryRuntime.DiscoverActivityAnchors(...)` faz descoberta de anchors em Route primary scene e Activity-owned scenes.
10. `ExecuteActivityContentLifecycle(...)` executa o ciclo de participantes de conteudo.
11. `CleanupPreviousActivityContentAnchorBindings(...)` limpa bindings da Activity anterior.
12. `ReleasePreviousActivityScenesAsync(...)` libera cenas owned pela Activity anterior.
13. `RemovePreviousActivityScopeRoot(...)` remove o scope root anterior.
14. `MergeActivityScopeResults(...)` consolida enter/exit/context em `RuntimeScopeLifecycleResult`.
15. `ActivityFlowStartResult.StartedWith(...)` monta o shell final.

Pontos estruturais relevantes:

- `ActivityRuntimeState` e menor que `RouteRuntimeState`: ele guarda status, activity atual, activity anterior e identidade.
- `ActivityFlowStartResult` agrega `ActivityContentApplyResult`, `ActivityReadinessState`, `RuntimeScopeLifecycleResult`, `ContentAnchorBindingLifecycleResult`, `ActivityContentAnchorDiscoveryResult`, `ActivityContentExecutionLifecycleResult`, `ActivitySceneCompositionResult`, `ActivitySceneReleaseResult`, `ActivityOperationResult` e `ActivitySceneLedgerSnapshot`.
- O fluxo de Activity e um orquestrador sobre `ActivityOperationPlan`, `ActivityReadinessState` e ledger, nao uma copia de Route.

## Side-by-side parity matrix

| Stage | Route | Activity | Parity | Shared kernel? | Notes |
|---|---|---|---|---|---|
| Input validation | `StartRouteAsync(...)` rejeita route ausente e route sem primary scene | `StartActivityAsync(...)` e `StartStartupActivityAsync(...)` rejeitam activity ausente e route ausente | Similar | Needs ADR before extraction | Mesmo shape fail-fast, mas os guard rails dependem de dominios diferentes. |
| Operation preview | `PreviewRouteStartupActivityOperation(...)` | `PreviewActivityOperation(...)` / `ResolveActivityOperationForProgress(...)` | Similar | Needs ADR before extraction | Route previewa startup activity; Activity previewa operacao e visual mode. |
| Progress budget | `activitySceneRouteReleaseCount + routeContentReleaseCount + routeSceneLoadCount + startupActivityProgressCount` | `loadProgressCount + releaseProgressCount` | Similar | Needs ADR before extraction | Mesmo padrao mecanico de weighted reporters, mas os contadores sao dominio-especificos. |
| Scene composition | `_routeSceneCompositionRuntime.ExecuteAsync(...)` | `ExecuteActivitySceneCompositionAsync(...)` | Similar | Keep domain-specific | Route carrega primary + additive; Activity carrega additive tracked scenes. |
| Content apply / dispatch | `RouteContentRuntime.ExitRouteContent(...)` / `EnterRouteContent(...)` | `ApplyActivityContentThroughLifecycleEvents(...)` + `ActivityContentRuntime.ApplyActivityTransition(...)` | Similar | Keep domain-specific | Route despacha receivers de conteudo; Activity alterna visibilidade local e aplica conteudo ativo. |
| Anchor discovery | `DiscoverRouteAnchors(...)` | `DiscoverActivityAnchors(...)` | Similar | Keep domain-specific | O scan e o mesmo tipo de iteracao, mas os filtros de matching sao de dominios diferentes. |
| Scope enter/exit | `CreateRouteScopeRoot(...)` / `RemovePreviousRouteScopeRoot(...)` / `MergeRouteScopeResults(...)` | `CreateActivityScopeRoot(...)` / `RemovePreviousActivityScopeRoot(...)` / `MergeActivityScopeResults(...)` | Identical mechanical tail | Candidate for first safe extraction | Este e o seam mais seguro para um helper interno de kernel futuro. |
| Cleanup de bindings | `CleanupPreviousRouteContentAnchorBindings(...)` | `CleanupPreviousActivityContentAnchorBindings(...)` | Identical mechanical guard + unbind | Candidate for first safe extraction | Mesmo shape de guardas e mesma chamada final a `UnbindRuntimeOwner(...)`. |
| Final shell | `RouteLifecycleStartResult` | `ActivityFlowStartResult` | Similar | Needs ADR before extraction | Ambos agregam subresultados e status, mas com payloads diferentes e semantica diferente. |

## Shared mechanics candidates

| Pattern | Files / classes / methods | Approx repeated shape | Concrete call sites | Similarity | Safe for kernel? | Recommended action |
|---|---|---|---|---|---|---|
| Scope enter/exit + cleanup tail | `RouteLifecycleRuntime.CreateRouteScopeRoot`, `RemovePreviousRouteScopeRoot`, `MergeRouteScopeResults`; `ActivityFlowRuntime.CreateActivityScopeRoot`, `RemovePreviousActivityScopeRoot`, `MergeActivityScopeResults`; `CleanupPreviousRouteContentAnchorBindings`, `CleanupPreviousActivityContentAnchorBindings`; `RuntimeScopeLifecycleResult` | Create owner, create scope root, capture context, remove previous root, merge enter/exit/context, then cleanup old bindings | 8 methods across 2 flows | Identical mechanical shape | Yes, if kept internal and owner-mapped | Needs ADR before extraction |
| Progress budget + completion reporting | `RouteLifecycleRuntime.StartRouteAsync`, `ActivityFlowRuntime.StartStartupActivityAsync`, `StartActivityCoreAsync`, `ClearActivityAsync`, `RouteSceneCompositionRuntime.ExecuteAsync`, `ActivitySceneCompositionRuntime.ExecuteAsync` | Count steps, create weighted reporters, advance step index, report completed | 6 call paths | Similar, not identical | Not yet | Needs ADR before extraction |
| Scene composition execution | `RouteSceneCompositionRuntime.ExecuteAsync`, `ActivitySceneCompositionRuntime.ExecuteAsync` | Validate plan, skip invalid entries, load scenes, collect entries, emit diagnostic result | 2 runtimes | Similar, not identical | No | Keep domain-specific |
| Content anchor discovery | `ContentAnchorDiscoveryRuntime.DiscoverRouteAnchors`, `DiscoverActivityAnchors` | Scan loaded scenes, dedupe scene keys, filter anchors, build declarations, return result | 2 methods | Similar, not identical | No | Keep domain-specific |
| Content runtime dispatch/apply | `RouteContentRuntime.ExitRouteContent`, `EnterRouteContent`; `ActivityContentRuntime.ApplyActivityTransition`, `HandleActivityEntered`, `HandleActivityExited`, `ApplyActiveActivity` | Collect bindings, dispatch lifecycle callbacks, count receivers/failures, emit result shell | 2 runtimes | Related but not identical | No | Keep domain-specific |
| Result/status shells | `RouteLifecycleStartResult`, `ActivityFlowStartResult`, `RouteSceneCompositionResult`, `ActivitySceneCompositionResult`, `RuntimeScopeLifecycleResult`, `ContentAnchorDiscoveryResult`, `ActivityContentAnchorDiscoveryResult`, `LoadingResult`, `TransitionResult` | `Status + Source + Reason + Message`, `Succeeded/Failed`, `ToDiagnosticString`, factory methods | Many | Similar | No, unless ADR and exact shape is proven identical | Keep domain-specific |

## Domain-specific steps that must not be shared

- `Route` startup activity preview is Route-only.
- `ActivityOperationPlan`, `ActivityOperationResult` and `ActivityReadinessState` are Activity-only.
- `RouteContentSet.CreateReleasePlan(...)` is Route-only and depends on `ContentReleasePlan`.
- `ActivitySceneReleaseResult` depends on `ActivityContentReleasePolicy` and ledger state.
- `RouteContentRuntime` uses `IRouteContentLifecycleReceiver` and `RouteContentBinding`.
- `ActivityContentRuntime` uses `ActivityLocalVisibilityAdapter` and `IActivityContentLifecycleReceiver`.
- `RouteSceneCompositionPlan` has `RouteSceneActiveScenePolicy` and a mandatory primary scene.
- `ActivitySceneCompositionPlan` has `ReleasePolicy` and tracked additive scenes.
- `ContentAnchorDiscoveryRuntime` does Route and Activity discovery with different mismatch counters and different scene sources.
- `LoadingSurfaceRuntime`, `LoadingResult` and `TransitionResult` are separate domains and must not be absorbed into a lifecycle kernel.

## Diagnostics/status fields that must preserve parity

| Concern | Route side | Activity side | Must preserve |
|---|---|---|---|
| Top-level lifecycle status shell | `RouteLifecycleStartResult.Message`, `RouteRuntimeState.DiagnosticIdentity`, `RouteExitResult.DiagnosticStatus` | `ActivityFlowStartResult.Message`, `ActivityRuntimeState.DiagnosticStatus`, `ActivityReadinessState.DiagnosticStatus` | Textual status values and fallback wording must not drift. |
| Scope lifecycle result | `RuntimeScopeLifecycleResult.DiagnosticStatus`, `EnterStatus`, `ExitStatus`, `ContextStatus`, `RootCount` | Same type and same fields | The fallback values `Rejected`, `Applied`, `Observed`, `NotExecuted`, `None` must stay stable. |
| Scene composition status | `RouteSceneCompositionResult.Status`, `LoadedCount`, `AlreadyLoadedCount`, `FailedCount`, `SkippedCount`, `NotExecutedCount`, `IssueCount`, `BlockingIssueCount` | `ActivitySceneCompositionResult.Status`, `LoadedSceneCount`, `AlreadyLoadedSceneCount`, `FailedSceneCount`, `SkippedSceneCount`, `BlockingIssueCount` | Count names and failure gating must remain equivalent in meaning, not necessarily identical in payload. |
| Anchor discovery status | `ContentAnchorDiscoveryResult.HasIssues`, `IssueCount`, `DiagnosticMessage` | `ActivityContentAnchorDiscoveryResult.HasIssues`, `IssueCount`, `DiagnosticMessage` | Route mismatch and Activity mismatch counters must stay distinct. |
| Content cleanup status | `RouteContentLifecycleDispatchResult.DiagnosticStatus`, `BindingCount`, `ReceiverCount`, `FailedReceiverCount` | `ActivityContentApplyResult.Message`, `ActivityContentLifecycleResult.DiagnosticStatus`, enter/exit binding and receiver counts | Preserve the same diagnostic narrative for enter/exit and apply paths. |
| Progress/status prose | `SceneLifecycleLoadResult`, `ContentReleaseResult`, `ActivitySceneReleaseResult` message text | Same pattern in Activity route-change release and scene composition | Do not "normalize" text to make the flows look more similar than they are. |
| Loading / transition shells | `LoadingResult`, `LoadingProgressAggregationResult`, `TransitionResult` | N/A for lifecycle kernel | Their diagnostics prove the shared shell idiom, but they remain separate domains. |

## Scene operation comparison

### Route scene composition / release

- `RouteSceneCompositionRuntime.ExecuteAsync(...)` loads the primary scene first via `SceneLifecycleRuntime.LoadPrimarySceneAsync(...)`.
- Additional Route scenes are loaded additively via `SceneLifecycleRuntime.LoadAdditiveSceneAsync(...)`.
- Route composition result records `PrimarySceneLoadResult`, `ActiveScenePolicy`, `ActiveSceneName` and `ActiveScenePath`.
- Route release is plan-driven: `RouteContentSet.CreateReleasePlan(...)` feeds `ContentReleaseRuntime.ExecuteAsync(...)`.
- Route release is also coupled to Activity scene release on route change through `_activityFlowRuntime.ReleaseActivityScenesForRouteChangeAsync(...)`.

### Activity scene composition / release

- `ActivitySceneCompositionRuntime.ExecuteAsync(...)` loads tracked additive scenes from the ledger.
- The runtime uses `ActivitySceneLedger` to avoid reloading already tracked scenes and to mark stale records.
- Release is ledger-driven: `ReleaseTrackedScenesAsync(...)` decides whether to unload by `ActivityContentReleasePolicy`.
- `UnloadSceneAsync(...)` is called per tracked scene that is eligible for route-change or activity-change release.

### Comparison

- Shared mechanics: ordered iteration, progress weighting, result entry accumulation, no-op when nothing is ready, and blocking failure short-circuit.
- Route-specific: mandatory primary scene, active scene policy, release plan over `RouteContentSet`.
- Activity-specific: release policy, ledger tracking, route context and retained scenes.

## Runtime scope comparison

- Both flows call `_runtimeContentRuntime.CreateScopeRoot(...)` to register a scope root.
- Both flows call `_runtimeContentRuntime.TryCreateScopeContext(...)` to capture a context snapshot.
- Both flows call `_runtimeContentRuntime.RemoveScopeRoot(...)` to tear down the previous scope root.
- Both flows merge enter/exit/context into `RuntimeScopeLifecycleResult`.

Differences:

- `CreateRouteOwner(...)` derives owner from `route.PrimaryScenePath` and `route.RouteName`.
- `CreateActivityOwner(...)` derives owner from `activity.ActivityName`.
- `RouteRuntimeState` stores more orchestration payload than `ActivityRuntimeState`.
- `Route` state carries `ActivityFlowResult`, which confirms Route is the higher-level orchestrator.

## Content/anchor cleanup comparison

`CleanupPreviousRouteContentAnchorBindings(...)` and `CleanupPreviousActivityContentAnchorBindings(...)` are mechanically identical:

- no previous scope -> return default
- same reference -> return default
- same owner as next scope -> return default
- otherwise call `_contentAnchorBindingRuntime.UnbindRuntimeOwner(owner, source, reason)`

This is the narrowest shared seam visible in the audit.

Do not confuse that seam with the full content runtime:

- `RouteContentRuntime` dispatches Route content enter/exit callbacks.
- `ActivityContentRuntime` applies Activity content visibility and lifecycle callbacks.

Those two runtimes are domain-specific and should stay separate.

## Loading / transition comparison

`LoadingProgressAggregationResult`, `LoadingResult` and `TransitionResult` repeat a stable shell idiom:

- enum status validation in constructors
- immutable array copy / null to empty handling
- `IssueCount`, `BlockingIssueCount` or similar counters
- `Succeeded` / `Failed` style convenience booleans
- `ToDiagnosticString()`
- factory methods for `Succeeded`, `Skipped`, `Failed`, `Rejected`

That repetition is useful evidence that the framework likes explicit shells, but it does not justify collapsing Route and Activity into a shared lifecycle type.

`LoadingSurfaceRuntime` is also a useful negative example:

- it is a loading UI adapter executor
- it does not own RouteLifecycle, SceneLifecycle or ActivityFlow
- it should not be used as a generic lifecycle kernel

## Risks of extracting a shared kernel

- `Route` and `Activity` do not share the same stage order.
- `Route` includes startup activity preview and orchestrates ActivityFlow.
- `Activity` includes activity operation planning, readiness, and participant execution.
- `Route` composition and release are plan-driven; `Activity` release is ledger-driven.
- `Route` and `Activity` have different owner mapping rules.
- Progress counts can drift if a shared reporter helper assumes the wrong stage boundaries.
- Content anchor discovery has different scene sources and mismatch counters.
- A broad kernel would likely become a hidden policy layer for Route or Activity semantics.
- Result/status shells remain domain-owned; a shared shell could erase diagnostic nuance.
- Scene side effects are still high-risk without Unity validation.

## Suggested LIFECYCLE-B ADR outline

1. Problem statement: duplicated lifecycle sequencing between Route and Activity, with explicit examples.
2. Scope boundary: shared mechanics only, no Route/Activity concept merge.
3. Candidate kernel shape: internal, additive, non-public, no MonoBehaviour.
4. Accepted shared mechanics: scope enter/exit + cleanup tail, progress budgeting, result merge.
5. Excluded semantics: startup activity preview, release policy, ledger tracking, participant execution, anchor mismatch semantics.
6. Required mappers: Route owner mapper, Activity owner mapper, content/scene domain mappers.
7. Diagnostics contract: exact preservation of status text, counts and diagnostic message shape.
8. Smoke parity contract: named smokes, expected unchanged output, allowed explicit bug-fix differences.
9. Rollback gate: stop if diagnostics drift or if the kernel starts absorbing Route or Activity policy.
10. Future cuts: `LIFECYCLE-C` internal operation model shell, then one pilot per domain.

## Recommended first safe implementation candidate

The first safe implementation candidate is the narrow tail seam, not the full kernel:

- internal helper for scope enter/exit + merge
- internal helper for identical previous-scope cleanup/unbind guard
- domain owner mapping supplied by Route or Activity

That seam already has at least two concrete call sites on each side and stays mechanical.

It still needs a dedicated `LIFECYCLE-B` ADR before implementation.

## Smokes affected by any future implementation

Potentially affected smokes if a future cut touches the lifecycle kernel:

- `Standard Smoke`
- `Route Scene Composition Smoke`
- `Route Release Smoke`
- `Activity Baseline Smoke`
- `Activity Content Anchor diagnostics`
- `Composite Lifecycle Release Smoke`
- `LoadingResultQaSmokeRunner` if progress/result text changes
- `LoadingProgressQaSmokeRunner` if progress budgeting changes
- `LoadingObservationQaSmokeRunner` if progress observability changes
- `TransitionQaSmokeRunner` if diagnostic shell assumptions are touched
- `TransitionOrchestrationObservationQaSmokeRunner` if transition output shape changes
- `Local Contribution smoke` if Activity discovery scope changes

## Files altered

- `Assets/_Documentation/Audits/FXX-AUDIT-LIFECYCLE-A-Route-Activity-Lifecycle-Sequence.md`

