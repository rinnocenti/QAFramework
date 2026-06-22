# F6 — Route Scene Composition and Release Audit

Status: `CLOSED / ROUTE SCENE COMPOSITION + RELEASE BASELINE PASS`  
Tipo: auditoria/fechamento de fase  
Escopo: Route scene composition, additive scene loading e release por escopo de Route

---

## Objetivo

F6 separou Route scene composition de runtime spawned/materialization.

A fase permitiu que uma Route declare additional scenes via `RouteContentProfileAsset`, carregue essas scenes por composição controlada e libere cenas additive owned no exit da Route.

F6 não pula para:

```text
Surface
RuntimeRootRegistry
Prefab materialization
Runtime spawned content
Actors/Input/Camera/Reset/Save/Pooling
Activity canonical materialization
```

---

## Estado confirmado até F5

F5 estava fechada e validada antes da F6. A base disponível era:

```text
LocalContentIdentity
Local Content Id em RouteContentBinding e ActivityLocalVisibilityAdapter
LocalContributionDiscovery
LocalContributionSet
Requiredness metadata em LocalContributionHandle
LocalContributionValidator
Local Contribution Smoke dedicado
```

F5 não entregava release handle, runtime reference, materialização, Surface ou loading canônico de Activity.

---

## Cortes F6 aplicados

### F6A — ADR completion and audit

F6A aceitou os ADRs de Route scene composition e content release por escopo. Foi docs-only.

### F6B — RouteSceneCompositionPlan

```text
RouteSceneCompositionPlan
RouteSceneCompositionPlanEntry
RouteSceneRole
RouteSceneLoadMode
RouteSceneActiveScenePolicy
RouteContentSceneEntry.ExplicitContentId
```

F6B criou planejamento side-effect free para Primary Scene e additional scenes declaradas no `RouteContentProfileAsset`.

### F6C — RouteSceneCompositionResult

```text
RouteSceneCompositionResult
RouteSceneCompositionResultEntry
RouteSceneCompositionStatus
RouteSceneCompositionEntryStatus
```

F6C criou o resultado estruturado de composição, com status, contagens e issues/blocking issues.

### F6D — SceneLifecycle additive primitive

```text
SceneLifecycleRuntime.LoadAdditiveSceneAsync
SceneLifecycleLoadResult.LoadedAdditiveScene
```

F6D adicionou o primitivo interno para carregar scenes por `LoadSceneMode.Additive`, mantendo a Primary Scene no caminho `Single`.

### F6E — RouteContentProfileAsset execution

```text
RouteSceneCompositionRuntime
RouteLifecycleRuntime -> RouteSceneCompositionPlan -> RouteSceneCompositionResult
RouteContentSet.FromSceneCompositionResult
```

F6E conectou `RouteContentProfileAsset` ao fluxo real de Route. A Route passa a montar plano, carregar Primary Scene, carregar additional scenes válidas e registrar handles em `RouteContentSet`.

Semântica validada:

```text
Primary Scene -> Single + active
Required additional scene inválida/falha -> bloqueia composição
Optional additional scene inválida/falha -> issue não bloqueante
Loaded additional scenes -> Route-owned handles
```

### F6F — ContentReleasePlan / ContentReleaseResult

```text
ContentReleasePlan
ContentReleasePlanEntry
ContentReleaseResult
ContentReleaseResultEntry
ContentReleaseAction
ContentReleaseOwnership
ContentReleaseStatus
ContentReleaseEntryStatus
RouteContentSet.CreateReleasePlan
```

F6F introduziu o modelo estruturado de release por escopo e o primeiro builder de plano a partir de `RouteContentSet`. O corte foi side-effect free.

Política do plano:

```text
Primary Scene ativa -> action None / controlada por LoadSceneMode.Single
Owned additive Route Scene -> action UnloadScene planejada
Registered content -> action None
DiagnosticOnly content -> action None
```

### F6G — Scene release execution

```text
SceneLifecycleRuntime.UnloadSceneAsync
SceneLifecycleUnloadResult
ContentReleaseRuntime
RouteLifecycleRuntime release execution before next Route composition
FrameworkQaCanvas Route Release Smoke
```

F6G executou fisicamente apenas ações `UnloadScene` planejadas para cenas de Route `Owned` e não ativas.

Ordem runtime aplicada:

```text
1. Route exit callbacks da Route anterior.
2. ContentReleasePlan da Route anterior.
3. ContentReleaseRuntime executa unload de additional scenes owned.
4. RouteSceneCompositionRuntime compõe a próxima Route.
5. Route enter callbacks da próxima Route.
6. Startup Activity da próxima Route.
```

---

## Evidência de fechamento F6

### Route Scene Composition Smoke

```text
routeSceneComposition='Succeeded'
routeSceneEntries='2'
routeSceneLoaded='2'
routeSceneOwnedLoaded='2'
routeSceneFailed='0'
routeSceneIssues='0'
routeSceneBlockingIssues='0'
routeContentHandles='2'
routeContentOwned='2'
routeContentDiagnosticOnly='0'
```

### Route Release Smoke

```text
routeRelease='Succeeded'
routeReleasePlanned='2'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseIssues='0'
routeReleaseBlockingIssues='0'
```

### Restore composition after release

```text
routeSceneComposition='Succeeded'
routeSceneEntries='2'
routeSceneLoaded='2'
routeSceneOwnedLoaded='2'
routeSceneFailed='0'
routeSceneBlockingIssues='0'
routeContentHandles='2'
```

### Regression coverage

```text
Standard Smoke PASS
Activity Baseline Smoke PASS
Route Scene Composition Smoke PASS
Route Release Smoke PASS
Route Callback Smoke PASS
Local Contribution Smoke PASS
Loaded Local Contributions authoring validation PASS
```

---

## ADRs fechados para F6

| ADR | Status | Decisão central |
|---|---|---|
| `F6-01 — ADR-RELEASE-001` | `Accepted / F6G release smoke pass` | Release é planejado por `ContentReleasePlan`/`ContentReleaseResult`, guiado por ownership explícito; F6 executa unload de additional scenes owned. |
| `F6-02 — ADR-SCENE-001` | `Accepted / F6 scene composition smoke pass` | Route scene composition usa `RouteSceneCompositionPlan`/`RouteSceneCompositionResult`; profile execution carrega additional scenes via additive primitive. |

---

## Limites preservados

F6 não autoriza:

```text
Release de Activity
Surface
RuntimeRootRegistry
Prefab materializer
Runtime spawned content
Actor/Input/Camera/Save/Pooling
Activity canonical materialization
Addressables backend
```

---

## Próxima fase

Próximo passo recomendado:

```text
F7A — Surface ADR/detail audit
```

F7 deve começar por declaração de Surface, roots/slots/anchors e duplicate detection. Não deve começar por RuntimeRoot/materialization ou consumidores avançados.
