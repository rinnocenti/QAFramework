# F3-01 — ADR-ROUTE-001 — RouteRuntimeState and RouteContentRuntime Status

Status: Accepted  
Fase: F3  
Ordem no Plano: F3-01  
Tipo: Route  
Escopo: Route lifecycle

---

## Contexto

O package já possui `RouteLifecycleRuntime`, `RouteContentRuntime`, `RouteContentBinding`, route events e `RouteContentSet` mínimo. A ambiguidade antes da F3 era que parte desse vocabulário existia, mas `RouteContentRuntime` estava congelado/deferred e não havia estado de Route tipado separado do resultado de startup.

O roadmap da F3 exige estabilizar Route antes de additive composition, Surface, RuntimeMaterialization ou consumers.

## Decisão

A F3 aceita as seguintes decisões:

1. Route deve ter `RouteRuntimeState` explícito como snapshot tipado da Route ativa.
2. `RouteRuntimeState` deve representar a Route ativa, seu estado de entrada, seu conteúdo conhecido e a última saída relevante sem transformar `RouteLifecycleRuntime` em service locator.
3. `RouteContentRuntime` deixa de ser ambíguo na F3: ele será **Active**, com escopo limitado a callbacks locais de conteúdo authored de Route na Primary Scene carregada.
4. A ativação de `RouteContentRuntime` não autoriza additive scene loading, runtime materialization, Surface, Camera, Actor, Input, Save, Pooling ou consumers.
5. A ordem canônica será:
   - exit de Route Content da Route anterior antes do carregamento `Single` da próxima Primary Scene;
   - load/ativação da Primary Scene da próxima Route;
   - enter de Route Content da nova Route;
   - startup Activity da nova Route.
6. Discovery de `RouteContentBinding` pode continuar como mecanismo transitório de baseline para a cena carregada, mas não vira ContributionSet final nem contrato público de descoberta global.

## Consequências

### Positivas

- Remove a ambiguidade de `RouteContentRuntime`.
- Dá à Route um estado próprio, separado de `SessionRuntimeState`.
- Prepara F6 sem colocar additive scene loading dentro da F3.
- Permite smoke específico de callbacks route enter/exit.

### Negativas / trade-offs

- Ativar callbacks pode expor bugs de ordem de lifecycle.
- O discovery transitório ainda não é o modelo final de contribuição local.
- A F3 precisará validar ordem de dispatch sem criar priorities configuráveis.

## Fora do escopo

- Additive scene loading.
- Route scene composition avançada.
- Surface.
- RuntimeMaterialization.
- Camera consumer.
- Actor lifecycle.
- LocalContributionSet final.

## Critérios de validação

- `RouteRuntimeState` existe e é atualizado no caminho de Route startup/switch.
- `RouteContentRuntime` não permanece `Deferred` sem ação.
- Se ativo, route enter/exit callbacks participam do smoke.
- Route switch continua funcionando.
- Startup Activity continua rodando depois do enter da Route.
- Não há fallback silencioso para Route required.

## Impacto esperado

Crítico antes de Route scene composition, release e Surface.

## Relação com roadmap

Cobre as decisões necessárias para:

```text
IF-FW-ROAD-3A — RouteRuntimeState tipado
IF-FW-ROAD-3B — RouteExitResult mínimo
IF-FW-ROAD-3C — RouteContentRuntime execution decision
IF-FW-ROAD-3E — Route local callback smoke
```

## Notas de implementação

A implementação deve ser incremental:

```text
F3B — RouteRuntimeState tipado
F3C — RouteExitResult mínimo
F3D — RouteContentRuntime active integration
F3E — RouteContentSet semantics
F3F — Route local callback smoke
F3G — Route validator expansion
```

Não pular para F6, F7, F8, F9 ou consumers.
