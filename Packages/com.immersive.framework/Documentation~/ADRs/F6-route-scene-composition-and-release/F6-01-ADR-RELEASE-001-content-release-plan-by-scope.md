# F6-01 — ADR-RELEASE-001 — Content Release Plan by Scope

Status: Accepted / F6G release smoke pass  
Fase: F6  
Ordem no Plano: F6-01  
Tipo: Release / Ownership / Lifecycle cleanup  
Escopo: Session/Route/Activity content

---

## Contexto

Até F5, o package tem ownership vocabularies e content sets mínimos, mas não tem release físico explícito:

- `SessionContentSet` existe como baseline de Session scope;
- `RouteContentSet` registra a Primary Scene como Route-owned;
- `ActivityContentSet` registra conteúdo local scene-authored mínimo;
- `LocalContributionSet` registra contribuições locais carregadas;
- `RouteContentRuntime.Exit` executa callbacks de saída de Route, mas não libera conteúdo;
- `LoadSceneMode.Single` ainda é o principal mecanismo físico de descarte de cena.

Isso é suficiente para F3-F5, mas não é suficiente para additive scenes, runtime spawned content, Content Anchor, Actor, Pooling ou Save. F6 precisa definir release por escopo antes de introduzir loading additive real e antes de RuntimeRoot/materialization.

---

## Decisão

O framework terá release explícito por escopo, representado por:

```text
ContentReleasePlan
ContentReleaseResult
```

O release não será implícito, global, nem baseado em `FindObjectsByType` como verdade funcional. Ele parte de conteúdo conhecido por estados/sets do framework.

Escopos aceitos:

```text
Session
Route
Activity
```

Semântica por escopo:

```text
Activity exit      → libera Activity-owned content.
Route exit         → libera Route-owned content e Activity-owned content associado à Route anterior.
Session shutdown   → libera Session-owned content depois de Route/Activity.
```

Conteúdo `DiagnosticOnly` nunca é liberado pelo framework. Conteúdo `Registered` só pode ser liberado se um corte posterior promover ownership explicitamente; por padrão, não é destruído/descarregado.

---

## Ordem de release aceita

### Troca de Route

Ordem final desejada:

```text
1. Construir/validar plan da próxima Route.
2. Executar callbacks de saída da Route anterior enquanto o conteúdo ainda existe.
3. Executar callbacks/clear da Activity anterior quando aplicável.
4. Executar ContentReleasePlan da Activity anterior.
5. Executar ContentReleasePlan da Route anterior.
6. Compor/carregar cenas da próxima Route.
7. Registrar RouteContentSet da próxima Route.
8. Executar callbacks de entrada da próxima Route.
9. Iniciar Startup Activity da próxima Route.
```

Enquanto F6 não tiver Activity canonical materialization, o release Activity-owned fica limitado ao que o framework realmente possui. O release não deve destruir objetos scene-authored que apenas foram observados ou diagnosticados.

### Clear Activity

```text
1. Executar Activity content exit callbacks se existirem.
2. Criar ContentReleasePlan para Activity-owned content.
3. Executar release somente do que for Activity-owned.
4. Atualizar ActivityRuntimeState para None.
```

### Session shutdown

```text
1. Encerrar Activity ativa.
2. Encerrar Route ativa.
3. Liberar Session-owned content.
4. Emitir ContentReleaseResult final.
```

Session shutdown completo pode ficar para fase posterior, mas a ordem acima é a política arquitetural aceita.

---

## Modelo aceito

### `ContentReleasePlan`

O plano deve ser imutável e produzido antes de executar side effects.

Campos conceituais mínimos:

```text
scope
owner identity
source content set / runtime state
release entries
source
reason
```

Cada release entry deve conter:

```text
content identity
content kind
ownership
release action
requiredness, quando aplicável
resource/display diagnostics
```

Ações conceituais iniciais:

```text
None
UnloadScene
DestroyRuntimeObject
ReturnToPool
CustomParticipantRelease
```

F6 inicial só deve implementar `UnloadScene` quando o framework realmente possuir cenas additive carregadas. `DestroyRuntimeObject`, `ReturnToPool` e custom participants são vocabulário futuro para F8/F11, não implementação de F6 inicial.

### `ContentReleaseResult`

O resultado deve ser estruturado e emitir facts/logs suficientes para QA.

Campos conceituais mínimos:

```text
completed
scope
owner identity
planned count
released count
skipped count
failed count
entries
issues/facts
```

Release parcial deve ser visível. Falha silenciosa não é permitida.

---

## Ownership

A política de release segue ownership, não localização na Hierarchy.

```text
Owned          → o framework pode liberar conforme scope e action.
Registered     → conhecido, mas não liberável por padrão.
DiagnosticOnly → nunca liberável pelo framework.
```

Um objeto/cena só pode ser liberado por um scope se:

```text
1. foi registrado como Owned por aquele scope;
2. tem release action suportada;
3. ainda não foi liberado;
4. o resultado anterior não marcou o handle como failed/stale.
```

Double release deve ser idempotente ou produzir issue explícita não fatal, dependendo da action. Para cenas Unity, unload de cena já descarregada deve ser tratado como skipped/stale, não como sucesso falso.

---

## Relação com scenes

`LoadSceneMode.Single` pode continuar removendo fisicamente a cena anterior no baseline, mas isso não conta como release plan completo.

Quando F6 introduzir additive scene loading:

```text
Additional scene carregada como Owned pela Route deve gerar release entry UnloadScene no Route exit.
Required additive scene que falhou no load não gera release entry loaded.
Optional additive scene skipped não gera release entry loaded.
Primary Scene carregada por LoadSceneMode.Single não deve ser descarregada manualmente antes da próxima Primary Scene assumir.
```

Primary Scene release explícito pode continuar representado como diagnóstico/transition result enquanto a operação real for controlada pelo próximo `LoadSceneMode.Single`.

---

## Relação com local contributions

Local contributions de F5 são discovery/validation de authored local content. Elas não são release handles.

Portanto:

```text
LocalContributionHandle não autoriza Destroy, Unload ou ReturnToPool.
LocalContributionSet não é release inventory.
GameObject local scene-authored observado por F5 não é framework-owned por causa do discovery.
```

Se uma contribuição local futura precisar participar de release, ela deve ser promovida por um contrato específico de release participant ou por runtime materialization em fase posterior.

---

## Fora do escopo

F6 release não implementa:

```text
Pooling
Runtime spawned content completo
RuntimeRootRegistry
Physical materializer adapter
Content Anchor binding
Actor reset/release
Snapshot/restore
Save backend
Addressables release
Object pooling return policy
```

Esses itens dependem de F8-F11.

---

## Consequências positivas

- Evita órfãos quando additive scenes forem introduzidas.
- Evita destruir conteúdo apenas observado por discovery.
- Torna cleanup testável via plan/result.
- Prepara RuntimeSpawned, Content Anchor e Pooling sem antecipá-los.
- Separa callback lifecycle de ownership físico.

---

## Trade-offs

- Exige distinguir content set, local contribution set e release inventory.
- Exige state explícito para loaded/released/skipped/failed.
- Requer ordem de transição mais rígida.
- Pode expor dívidas do uso atual de `LoadSceneMode.Single` como cleanup implícito.

---

## Critérios de validação

F6 release completo só pode ser considerado pronto quando houver smoke comprovando:

```text
Route exit gera ContentReleasePlan.
Route exit gera ContentReleaseResult.
Owned additive scene carregada é descarregada no Route exit.
DiagnosticOnly content não é descarregado.
Registered content não é descarregado por padrão.
Double release não causa exceção nem sucesso falso.
Falha de release aparece em result/facts.
Standard Smoke e Route Callback Smoke continuam passando.
```

---

## Impacto esperado no roadmap

Este ADR autoriza os cortes F6 relacionados a release:

```text
F6F — ContentReleasePlan / ContentReleaseResult [model/planning, no physical unload]
F6G — Scene/release smoke [physical unload execution + QA]
```

Ele não autoriza F8 runtime materialization, F9 Content Anchor binding ou F11 Pooling/Actor release.

---

## Relação com ADRs

Depende de:

```text
F3-02 — RouteContentSet semantics
F4-01 — ActivityContentSet and Readiness baseline
F5-02 — Local contribution discovery and requiredness
F6-02 — Route Scene Composition Plan and Result
```

Relaciona-se com:

```text
F8-01 — Runtime ownership and roots
F8-02 — Materialization request/result/handle
F11-04 — Pooling package boundary
```


---

## Implementação F6F

F6F aplica o modelo inicial sem executar side effects físicos.

Entraram no runtime:

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

Política aplicada pelo builder de Route:

```text
Primary Scene ativa -> action None; continua controlada por LoadSceneMode.Single.
Owned additive Route Scene -> action UnloadScene planejada.
Registered content -> action None.
DiagnosticOnly content -> action None.
```

`ContentReleaseResult.NotExecutedResult` existe para preservar evidência estruturada antes da execução. F6F não chama `SceneManager.UnloadSceneAsync`, não destrói GameObjects, não retorna objetos a pool e não altera a ordem de troca de Route.


---

## Implementação F6G

F6G executa o primeiro release físico autorizado por este ADR: unload de cenas additive owned no escopo de Route.

Entraram no runtime:

```text
SceneLifecycleRuntime.UnloadSceneAsync
SceneLifecycleUnloadResult
ContentReleaseRuntime
RouteLifecycleStartResult.ContentReleaseResult
Route Release Smoke no FrameworkQaCanvas
```

Ordem runtime aplicada na troca de Route:

```text
1. Dispatch de exit callbacks da Route anterior.
2. Criação do ContentReleasePlan a partir do RouteContentSet anterior.
3. Execução do ContentReleasePlan para ações UnloadScene owned.
4. Composição da próxima Route.
5. Dispatch de enter callbacks da próxima Route.
6. Startup Activity da próxima Route.
```

Limites preservados:

```text
Primary Scene ativa não recebe unload manual.
Somente ContentReleaseAction.UnloadScene é executável em F6G.
DestroyRuntimeObject, ReturnToPool e CustomParticipantRelease continuam fora de escopo.
Activity release, Content Anchor, RuntimeRootRegistry, materialização física runtime, Actor/Input/Camera/Save/Pooling continuam fora de escopo.
```

Diagnóstico validado em request que sai de uma Route com uma additional scene owned:

```text
routeRelease='Succeeded'
routeReleasePlanned='2'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseIssues='0'
routeReleaseBlockingIssues='0'
```

F6G foi fechado com `Route Release Smoke` confirmando unload físico da additional scene owned e restore posterior da Route canônica com duas scene handles carregadas.


---

## Closure note — F6H

F6H closes the release baseline authorized by this ADR.

F6G validated the first physical release action:

```text
ContentReleaseAction.UnloadScene
```

Validated behavior:

```text
routeRelease='Succeeded'
routeReleasePlanned='2'
routeReleaseReleased='1'
routeReleaseSkipped='1'
routeReleaseFailed='0'
routeReleaseIssues='0'
routeReleaseBlockingIssues='0'
```

Interpretation:

```text
Released 1 -> owned additive Route scene unloaded.
Skipped 1  -> active Primary Scene skipped; controlled by LoadSceneMode.Single.
```

No Activity release, runtime object destroy, pool return, Content Anchor, RuntimeRootRegistry or materialization behavior is authorized by this closure.
