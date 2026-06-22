# F6 — Route Scene Composition and Release Audit

Status: `F6E ROUTE CONTENT PROFILE EXECUTION APPLIED / BASELINE SMOKE PASS / PROFILE SMOKE PENDING`  
Tipo: planejamento/auditoria documental  
Escopo: Route scene composition, additive scene loading e release por escopo

---

## Objetivo

Este documento registra a auditoria que antecede a implementação da F6.

A F6 deve evoluir Route scene composition e release sem pular para:

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

F5 está fechada e validada. A base disponível para F6 é:

```text
LocalContentIdentity
Local Content Id em RouteContentBinding e ActivityLocalVisibilityAdapter
LocalContributionDiscovery
LocalContributionSet
Requiredness metadata em LocalContributionHandle
LocalContributionValidator
Local Contribution Smoke dedicado
```

F5 não entrega release handle, runtime reference, materialização, Surface ou loading canônico de Activity.

---

## Estado atual do package relevante para F6

### Aplicado em F6B

```text
RouteSceneCompositionPlan
RouteSceneCompositionPlanEntry
RouteSceneRole
RouteSceneLoadMode
RouteSceneActiveScenePolicy
RouteContentSceneEntry.ExplicitContentId
```

F6B é inerte: cria dados de planejamento side-effect free para Primary Scene e additional scenes declaradas em RouteContentProfileAsset. Ele registra role, load mode esperado, requiredness, ownership, execution order, active scene policy e diagnóstico de explicit content id. Não carrega additive scenes. F6B foi validado por Standard Smoke, Activity Baseline Smoke, Local Contribution Smoke, Route Callback Smoke e Authoring Validation sem issues.

### Aplicado em F6C

```text
RouteSceneCompositionResult
RouteSceneCompositionResultEntry
RouteSceneCompositionStatus
RouteSceneCompositionEntryStatus
```

F6C é inerte: cria dados de resultado para registrar evidência de composição depois da execução, mas ainda não executa additive loading, não altera SceneLifecycleRuntime e não cria release. O resultado diferencia loaded, already loaded, skipped, failed e not executed, preservando contagem de issues e blocking issues para o executor futuro.

### Aplicado em F6D

```text
SceneLifecycleRuntime.LoadAdditiveSceneAsync
SceneLifecycleLoadResult.LoadedAdditiveScene
```

F6D adicionou o primitivo interno de carregamento additive e foi validado por smoke baseline. Ele valida cena vazia, verifica se a cena já está carregada, carrega via `LoadSceneMode.Additive` quando necessário, resolve a cena carregada e retorna `SceneLifecycleLoadResult` com `Loaded`, `AlreadyLoaded`, `SceneName`, `ScenePath`, `LoadMode` e mensagem diagnóstica.

### Aplicado em F6E

```text
RouteSceneCompositionRuntime
RouteLifecycleRuntime -> RouteSceneCompositionPlan -> RouteSceneCompositionResult
RouteContentSet.FromSceneCompositionResult
```

F6E conecta a execução de `RouteContentProfileAsset` ao fluxo de Route. O runtime monta `RouteSceneCompositionPlan`, carrega a Primary Scene em `Single`, carrega additional scenes válidas em `Additive`, produz `RouteSceneCompositionResult` e registra cenas carregadas em `RouteContentSet`. Required additional scene inválida ou com load failure bloqueia a Route composition. Optional inválida/falha é registrada como issue não bloqueante. F6E ainda não descarrega cenas e não cria ContentReleasePlan/Result. O baseline sem profile passou com `routeSceneLoaded='1'`; falta evidência com profile/additional scene carregando `routeSceneLoaded > 1`.

### Presente

```text
RouteAsset com Primary Scene
RouteContentProfileAsset executado pela Route scene composition
RouteContentSceneEntry exige ExplicitContentId para execution readiness
RouteContentMaterializationPlan usado como declaração diagnóstica
RouteContentSet registra Primary e additional scenes carregadas
SceneLifecycleRuntime com Primary Scene loading
RouteContentSet com Primary Scene Route-owned
RouteContentRuntime com callbacks locais de Route
RouteContentLifecycleDispatchResult
RouteExitResult mínimo
```

### Ausente

```text
ContentReleasePlan
ContentReleaseResult
Additive scene unload/release
Expected contribution asset
RuntimeContentHandle avançado
RuntimeRootRegistry
```

---

## Riscos encontrados

### 1. RouteContentProfileAsset execution não faz release

O asset agora declara additional scenes consumidas por Route scene composition, mas isso ainda não autoriza descarregamento físico.

Decisão de F6:

```text
RouteContentProfileAsset execution carrega additional scenes; release físico começa somente em F6F com ContentReleasePlan/Result.
```

### 2. Fallback de content id em RouteContentSceneEntry

`RouteContentSceneEntry.ContentId` ainda possui fallback para SceneName.

Decisão de F6:

```text
Para execução, additional scene precisa de content id explícito.
Fallback pode existir como compatibilidade planning-only, mas validator/executor deve reportar missing explicit id antes de carregar.
```

### 3. LoadSceneMode.Single ainda mascara release

Hoje o descarte físico da rota anterior depende principalmente de carregamento Single da nova Primary Scene.

Decisão de F6:

```text
Isso pode continuar como baseline transitório para Primary Scene, mas additive scenes exigem release explícito.
```

### 4. LocalContributionHandle não é release handle

A F5 descobre authored local content carregado, mas não possui lifecycle físico.

Decisão de F6:

```text
LocalContributionSet pode validar authoring carregado, mas não autoriza destroy/unload/release.
```

### 5. Route callbacks dependem de conteúdo carregado

`RouteContentRuntime.Enter` procura bindings depois da cena carregada.

Decisão de F6:

```text
Enter callbacks da Route devem rodar depois da scene composition da próxima Route.
Exit callbacks da Route anterior devem rodar antes do release da Route anterior.
```

---

## ADRs completados

| ADR | Status | Decisão central |
|---|---|---|
| `F6-01 — ADR-RELEASE-001` | `Accepted / implementation not started` | Release será por `ContentReleasePlan`/`ContentReleaseResult`, guiado por ownership explícito. |
| `F6-02 — ADR-SCENE-001` | `Accepted / F6E baseline smoke pass / profile smoke pending` | Route scene composition usa `RouteSceneCompositionPlan`/`RouteSceneCompositionResult`; Route profile execution carrega additional scenes via additive primitive. |

---

## Ordem recomendada de cortes F6

```text
F6A — ADR completion and audit                  [docs-only]
F6B — RouteSceneCompositionPlan                 [runtime inert/pure] [CLOSED / PASS]
F6C — RouteSceneCompositionResult               [runtime inert/result] [CLOSED / PASS]
F6D — SceneLifecycle additive primitive         [runtime execution primitive] [CLOSED / PASS]
F6E — RouteContentProfileAsset execution        [route profile → composition] [BASELINE PASS / PROFILE SMOKE PENDING]
F6F — ContentReleasePlan / ContentReleaseResult [release planning/execution]
F6G — Scene/release smoke                       [QA]
```

---

## Critério para fechar F6E

F6E pode ser fechado quando o package compilar no Unity, o smoke baseline continuar sem regressão e o `Route Scene Composition Smoke` confirmar uma Route com `RouteContentProfileAsset` carregando pelo menos uma additional scene. A evidência esperada é `RouteSceneCompositionResult` com `routeSceneLoaded > 1`, `routeSceneBlockingIssues='0'` e `RouteContentSet` com handle adicional. O corte deve permanecer sem unload/release físico.

---

## Não autorizado pela F6E

```text
Release físico antes de F6F / ContentReleasePlan
Surface
RuntimeRootRegistry
Prefab materializer
Runtime spawned content
Actor/Input/Camera/Save/Pooling
Activity canonical materialization
```
