# F6-02 — ADR-SCENE-001 — Route Scene Composition Plan and Result

Status: Accepted / F6C RouteSceneCompositionResult applied / execution not started  
Fase: F6  
Ordem no Plano: F6-02  
Tipo: Scene / Route / Content composition  
Escopo: Route scene composition

---



## Aplicação F6C

F6C adiciona o modelo inerte de resultado:

```text
RouteSceneCompositionResult
RouteSceneCompositionResultEntry
RouteSceneCompositionStatus
RouteSceneCompositionEntryStatus
```

O resultado registra evidência por entry depois de uma tentativa de composição: loaded, already loaded, skipped, failed e not executed. Ele também agrega status, contagens de loaded/failed/skipped/not executed, active scene policy, active scene diagnóstica, issues e blocking issues.

O corte não carrega additive scenes, não descarrega cenas, não altera `SceneLifecycleRuntime`, não cria `ContentReleasePlan`, não cria `ContentReleaseResult` e não registra handles adicionais no `RouteContentSet`.

---

## Aplicação F6B

F6B adiciona o modelo inerte de planejamento:

```text
RouteSceneCompositionPlan
RouteSceneCompositionPlanEntry
RouteSceneRole
RouteSceneLoadMode
RouteSceneActiveScenePolicy
```

Também separa `ExplicitContentId` em `RouteContentSceneEntry` para que additional scenes possam ser diagnosticadas sem usar o fallback legado de `ContentId` como identidade funcional de execução.

O corte não carrega additive scenes, não altera `SceneLifecycleRuntime` e não cria release.

## Contexto

Até F5, o package tem uma base estável para lifecycle, conteúdo local e contribuição local:

- `RouteLifecycleRuntime` carrega a Primary Scene da Route em `LoadSceneMode.Single`;
- `RouteContentRuntime` executa callbacks locais de Route já carregados;
- `RouteContentSet` registra a Primary Scene como conteúdo Route-owned;
- `RouteContentProfileAsset` e `RouteContentSceneEntry` existem, mas ainda são planning-only;
- `LocalContributionDiscovery`, `LocalContributionSet` e `LocalContributionValidator` validam contribuições locais carregadas;
- não existe materialização canônica, Surface, RuntimeRoot, runtime spawned content ou release/unload policy completo.

O próximo problema de F6 é transformar declarações de cenas da Route em um plano explícito antes de executar qualquer side effect. Additive scene support não deve entrar como materialização genérica, nem como extensão de Surface ou RuntimeRoot. Ele pertence ao domínio de composição de cenas da Route.

A composição de cenas também precisa corrigir uma dívida de F3: hoje a Primary Scene é conhecida pelo `RouteContentSet`, mas a ordem completa de planejamento, load, callbacks, state update e futura liberação ainda não é modelada como uma operação única com plan/result.

---

## Decisão

Route scene composition será implementada por um par explícito:

```text
RouteSceneCompositionPlan
RouteSceneCompositionResult
```

O plano é criado antes de carregar ou descarregar cenas. O resultado é produzido depois da execução e se torna a evidência canônica do que foi carregado, ignorado, falhou, ficou owned ou ficou apenas diagnóstico.

Fluxo conceitual de entrada de Route:

```text
RouteAsset
  + Primary Scene declaration
  + RouteContentProfileAsset optional
      + additional scene declarations
→ RouteSceneCompositionPlan
→ SceneLifecycle execution
→ RouteSceneCompositionResult
→ RouteContentSet update
→ RouteContentRuntime Enter callbacks
→ Startup Activity policy
```

Fluxo conceitual de troca de Route:

```text
previous Route state
→ previous RouteContentRuntime Exit callbacks
→ previous ContentReleasePlan execution, quando existir no F6F
→ next RouteSceneCompositionPlan
→ next RouteSceneCompositionResult
→ next RouteContentSet
→ next RouteContentRuntime Enter callbacks
→ next Startup Activity policy
```

Enquanto `ContentReleasePlan` ainda não existir, a liberação física continua limitada ao comportamento atual de `LoadSceneMode.Single` para Primary Scene. F6 não deve fingir que additive release está resolvido antes do ADR de release e do corte de release.

---

## Modelo aceito

### `RouteSceneCompositionPlan`

O plano deve ser imutável e side-effect free. Ele descreve intenção.

Campos conceituais mínimos:

```text
route
routeIdentity / route owner identity
primaryScene entry
additionalScene entries
activeScenePolicy
source
reason
```

Cada entry do plano deve conter:

```text
content id explícito
scene path ou scene reference resolvível
scene display name diagnóstico
content scope = Route
scene role = Primary ou Additive
requiredness = Required ou Optional
ownership = Owned ou DiagnosticOnly
load mode esperado
ordem de execução
```

### Primary Scene

A Primary Scene da Route é sempre:

```text
required
Route-owned
parte do RouteSceneCompositionPlan
carregada antes das additive scenes
base da active scene policy padrão
```

A Primary Scene continua sendo declarada no `RouteAsset`. F6 não move a Primary Scene para `RouteContentProfileAsset`.

### Additional scenes

Additional scenes vêm de `RouteContentProfileAsset`.

Regras:

```text
Required additive scene ausente ou inválida bloqueia a composição.
Optional additive scene ausente ou inválida vira issue não bloqueante e não cria handle loaded.
Additional scene carregada com sucesso entra no RouteContentSet como Route-owned.
Additional scene não carregada não pode ser registrada como loaded.
```

### Active scene policy

A política inicial aceita é:

```text
PrimarySceneActive
```

Ou seja: após composição bem-sucedida, a Primary Scene da Route permanece a cena ativa. Additive scenes carregadas não substituem a cena ativa por padrão.

Políticas como `FirstLoadedAdditiveActive`, `ExplicitActiveSceneId` ou Activity-owned active scene ficam fora do F6 inicial.

### Requiredness

Requiredness de cenas não é igual à requiredness de local contributions, mas usa o mesmo vocabulário de conteúdo:

```text
FrameworkContentRequiredness.Required
FrameworkContentRequiredness.Optional
```

Interpretação em F6:

```text
Required scene falha → Route scene composition falha.
Optional scene falha → Route scene composition pode continuar com issue diagnóstica.
```

### Identidade

F6 não deve introduzir novo fallback funcional por `GameObject.name`, hierarchy path ou scene display name.

Para additional scenes, `contentId` deve ser tratado como authoring id explícito antes da execução. O fallback atual de `RouteContentSceneEntry.ContentId` para `SceneName` é tolerado somente como legado planning-only até o corte que criar validator/execution. O corte de execução deve reportar missing explicit content id como issue de authoring ou plan issue.

Para Primary Scene, o id técnico atual `primary-scene:<SceneName>` pode permanecer como baseline transitório, mas F6 deve preservar a separação entre:

```text
functional content id
scene path / scene name diagnóstico
loaded scene handle/status
```

`scenePath` é permitido como referência de recurso Unity para carregar cena, mas não deve ser usado como substituto universal de identidade funcional de contribuição local.

---

## Ordem de callbacks e state

A ordem aceita para troca de Route é:

```text
1. Validar próxima Route e construir RouteSceneCompositionPlan.
2. Executar RouteContentRuntime.Exit para a Route anterior ainda observável.
3. Executar release da Route anterior quando ContentReleasePlan existir.
4. Carregar Primary Scene da próxima Route.
5. Carregar additive scenes da próxima Route conforme plan e requiredness.
6. Produzir RouteSceneCompositionResult.
7. Produzir RouteContentSet da próxima Route.
8. Executar RouteContentRuntime.Enter para a próxima Route.
9. Iniciar Startup Activity da próxima Route.
10. Publicar eventos de Route transition.
```

Observações:

- F6 pode manter a ordem F3 existente enquanto release explícito ainda não existir, mas a direção final deve seguir a ordem acima.
- `RouteContentRuntime.Enter` deve observar conteúdo local depois da composição carregada.
- Startup Activity não deve iniciar antes da Route composition ser considerada concluída.
- Activity content loading canônico não entra em F6 inicial; ele depende da estabilização da composição e do release.

---

## Fora do escopo

F6 Scene Composition não implementa:

```text
Prefab materialization
RuntimeRootRegistry
RuntimeContentHandle avançado
Surface
Surface binding
Runtime spawned content
Actors
Input
Camera
Reset
Snapshot
Save
Pooling
Activity canonical materialization
Addressables
```

Addressables pode ser considerado futuramente como backend de loading, mas F6 inicial usa o mecanismo Unity scene loading disponível no package.

---

## Consequências positivas

- Additive scenes entram por um contrato Route-specific, não por materialização genérica.
- RouteContentProfile deixa de ser dado morto quando o corte de execução chegar.
- Required/Optional passa a ter semântica clara para scene loading.
- `RouteContentSet` ganha origem mais confiável.
- F6 prepara release sem acoplar Surface/RuntimeRoot cedo demais.

---

## Trade-offs

- A Route passa a ter uma etapa de composição mais explícita.
- O package precisará distinguir melhor plan, execution result e content set.
- O release real de additive scenes precisa de ADR/corte próprio para não depender de side effect implícito.
- Validators terão de rejeitar authoring incompleto antes de execução.

---

## Critérios de validação

F6 scene composition só pode ser considerada pronta quando houver smoke comprovando:

```text
Primary Scene required carregada com sucesso.
RouteContentSet registra Primary Scene como Route-owned.
Additional Required scene válida é carregada e registrada.
Additional Optional scene válida é carregada e registrada.
Additional Optional scene inválida não bloqueia, mas emite issue.
Additional Required scene inválida bloqueia a Route composition.
Active scene final é previsível e documentada.
RouteContentRuntime.Enter roda depois da composição.
Startup Activity roda depois da composição.
Standard Smoke e Route Callback Smoke continuam passando.
```

---

## Impacto esperado no roadmap

Este ADR autoriza os próximos cortes F6 de scene composition:

```text
F6B — RouteSceneCompositionPlan
F6C — RouteSceneCompositionResult
F6D — Additive route scene loading primitive
F6E — RouteContentProfileAsset execution
```

Não autoriza F7 Surface, F8 runtime roots/materialization ou F9 runtime placement.

---

## Relação com ADRs

Depende de:

```text
F3-01 — RouteRuntimeState and RouteContentRuntime status
F3-02 — RouteContentSet semantics
F5-01 — Local identity
F5-02 — Local contribution discovery and requiredness
```

Relaciona-se com:

```text
F6-01 — Content Release Plan by Scope
F8-01 — Runtime ownership and roots
F8-02 — Materialization request/result/handle
F9-01 — Surface binding and content placement
```
