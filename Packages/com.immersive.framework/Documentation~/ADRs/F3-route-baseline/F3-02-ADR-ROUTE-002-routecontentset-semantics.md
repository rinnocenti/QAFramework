# F3-02 — ADR-ROUTE-002 — RouteContentSet Semantics

Status: Draft / Deferred  
Fase: F3  
Ordem no Plano: F3-02  
Tipo: Route / Content  
Escopo: RouteContentSet

---

## Contexto

`RouteContentSet` existe, mas precisa definir se é apenas registro diagnóstico ou se governa ownership/release.

## Decisão

Separar papéis:

- `RouteContentSet`: snapshot/registro do conteúdo conhecido da route.
- `RouteContentOwnership` ou policy equivalente: define o que deve ser liberado e quando.

Na fase inicial, `RouteContentSet` pode registrar primary scene e route local content; release avançado fica em fase específica.

## Consequências

### Positivas

- Evita ContentSet ambíguo.
- Prepara Route scene composition e release.
- Mantém Route baseline simples.

### Negativas / trade-offs

- Pode criar dois conceitos onde hoje há um.
- Exige atualização de logs/status.

## Fora do escopo

- Additive scenes.
- RuntimeSpawned.
- Surface content.

## Critérios de validação

- Route switch mostra content set coerente.
- O que é diagnostic-only não tenta release.
- O que é owned tem owner explícito.

## Impacto esperado

Base para F6.

## Relação com roadmap

F3/F6.

## Notas de implementação

Deve alinhar com ADR-CONTENT-001.
