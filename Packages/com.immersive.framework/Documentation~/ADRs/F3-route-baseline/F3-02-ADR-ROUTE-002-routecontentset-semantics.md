# F3-02 — ADR-ROUTE-002 — RouteContentSet Semantics

Status: Accepted  
Fase: F3  
Ordem no Plano: F3-02  
Tipo: Route / Content  
Escopo: RouteContentSet

---

## Contexto

`RouteContentSet` já existe e aparece nos diagnostics de Route com o handle da Primary Scene. Antes da F3, a semântica ainda era estreita: o set funcionava como registro/diagnóstico, mas o roadmap exige explicitar se ele representa ownership, release ou apenas observabilidade.

A F2 já criou `SessionContentSet` e `SessionContentOwnership`. A F3 deve alinhar Route ao mesmo padrão sem trazer persistent scenes, additive composition ou release avançado.

## Decisão

A F3 aceita as seguintes decisões:

1. `RouteContentSet` é o snapshot imutável do conteúdo conhecido da Route ativa.
2. `RouteContentSet` não é host, manager, service locator, loader ou release executor.
3. Ownership deve ser explícito por item ou por wrapper equivalente; não deve ficar implícito no simples fato de o item estar no set.
4. Para o baseline F3, a Primary Scene da Route pode ser registrada como conteúdo required e owned pela Route, mas a execução de load/unload continua pertencendo ao `SceneLifecycleRuntime`.
5. Route-local authored callbacks são lifecycle local; eles não obrigam `RouteContentSet` a virar inventário final de contribuições.
6. Additional scenes declaradas por `RouteContentProfileAsset` continuam deferred/planning-only até F6.
7. Release avançado fica para F6; F3 só prepara a semântica para que release futuro não seja inferido por string, path ou nome de GameObject.

## Consequências

### Positivas

- Evita `ContentSet` ambíguo.
- Alinha Route com a política de identity/content da F1 e ownership da F2.
- Mantém F3 pequena e focada em Route baseline.
- Prepara F6 sem carregar additive scene support cedo demais.

### Negativas / trade-offs

- Pode exigir um tipo `RouteContentOwnership` ou estrutura equivalente.
- O log/status de Route Content pode precisar mudar para exibir ownership.
- Release real continua não implementado nesta fase.

## Fora do escopo

- Additive scenes.
- RuntimeSpawned content.
- Surface content.
- Release policy final.
- LocalContributionSet final.
- Content materialization.

## Critérios de validação

- Route switch mostra `RouteContentSet` coerente.
- O que é diagnostic-only não tenta release.
- O que é owned tem owner explícito.
- Primary Scene continua required no baseline.
- Additional scenes continuam deferred/planning-only.

## Impacto esperado

Base para F6 — Route scene composition and release.

## Relação com roadmap

Cobre a decisão necessária para:

```text
IF-FW-ROAD-3D — RouteContentSet semantics
```

Também mantém alinhamento com:

```text
ADR-CONTENT-001 — Content Identity Domain
ADR-SESSION-002 — SessionContent Ownership Semantics
```

## Notas de implementação

A implementação deve criar apenas a semântica mínima necessária para a F3. Não implementar release, additive loading ou Surface.
