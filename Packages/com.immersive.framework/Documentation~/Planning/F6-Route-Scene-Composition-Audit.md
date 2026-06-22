# F6 — Route Scene Composition and Release Audit

Status: `F6B ROUTE SCENE COMPOSITION PLAN APPLIED / PENDING COMPILE-SMOKE`  
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

F6B é inerte: cria dados de planejamento side-effect free para Primary Scene e additional scenes declaradas em RouteContentProfileAsset. Ele registra role, load mode esperado, requiredness, ownership, execution order, active scene policy e diagnóstico de explicit content id. Não carrega additive scenes e não produz result.

### Presente

```text
RouteAsset com Primary Scene
RouteContentProfileAsset planning-only
RouteContentSceneEntry planning-only
RouteContentMaterializationPlan planning-only
RouteContentScenePlanEntry planning-only
SceneLifecycleRuntime com Primary Scene loading
RouteContentSet com Primary Scene Route-owned
RouteContentRuntime com callbacks locais de Route
RouteContentLifecycleDispatchResult
RouteExitResult mínimo
```

### Ausente

```text
RouteSceneCompositionPlan
RouteSceneCompositionResult
Additive scene execution
Active scene policy explícita
ContentReleasePlan
ContentReleaseResult
Additive scene unload/release
Expected contribution asset
RuntimeContentHandle avançado
RuntimeRootRegistry
```

---

## Riscos encontrados

### 1. RouteContentProfileAsset ainda é planning-only

O asset já declara additional scenes, mas elas ainda não são executadas.

Decisão de F6:

```text
RouteContentProfileAsset só pode ser executado depois de RouteSceneCompositionPlan/Result.
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
| `F6-02 — ADR-SCENE-001` | `Accepted / implementation not started` | Route scene composition será por `RouteSceneCompositionPlan`/`RouteSceneCompositionResult`, antes de additive execution. |

---

## Ordem recomendada de cortes F6

```text
F6A — ADR completion and audit                  [docs-only]
F6B — RouteSceneCompositionPlan                 [runtime inert/pure] [APPLIED / PENDING COMPILE-SMOKE]
F6C — RouteSceneCompositionResult               [runtime inert/result]
F6D — SceneLifecycle additive primitive         [runtime execution primitive]
F6E — RouteContentProfileAsset execution        [route profile → composition]
F6F — ContentReleasePlan / ContentReleaseResult [release planning/execution]
F6G — Scene/release smoke                       [QA]
```

---

## Critério para fechar F6B

F6B pode ser fechado quando o package compilar no Unity e o smoke baseline continuar sem regressão. O corte deve permanecer limitado ao plan inerte. Não deve carregar additive scenes ainda.

---

## Não autorizado pela F6A

```text
Additive execution antes de RouteSceneCompositionResult
Release físico antes de ContentReleasePlan
Surface
RuntimeRootRegistry
Prefab materializer
Runtime spawned content
Actor/Input/Camera/Save/Pooling
Activity canonical materialization
```
