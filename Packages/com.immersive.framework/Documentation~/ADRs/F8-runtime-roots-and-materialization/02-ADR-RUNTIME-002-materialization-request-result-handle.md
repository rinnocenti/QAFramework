# ADR-RUNTIME-002 — Materialization Request Result Handle

Status: Draft / Deferred  
Fase: F8  
Tipo: RuntimeSpawned / Content  
Escopo: Materialization

---

## Contexto

O package tem interface de materializer, mas ainda não tem request/result/handle concreto com release/ownership.

## Decisão

Materialização runtime deve seguir:

```text
RuntimeMaterializationRequest
→ IFrameworkContentMaterializer.Materialize
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

O handle possui identity, owner scope, release action, state e diagnostics. Materializer não deve conhecer gameplay consumer concreto.

## Consequências

### Positivas

- Cria API testável.
- Desacopla prefab spawn de consumers.
- Base para SurfaceBinding e Pooling.

### Negativas / trade-offs

- Exige desenhar release callback com cuidado.
- Pode exigir revisão da interface existente.

## Fora do escopo

- Pooled materializer.
- Actor/projectile.
- Save/snapshot de runtime content.

## Critérios de validação

- Prefab materializa e retorna handle.
- Release muda estado e limpa objeto.
- Double release é seguro/diagnosticado.

## Impacto esperado

Pré-requisito de Surface runtime placement e consumers avançados.

## Relação com roadmap

F8.

## Notas de implementação

O primeiro materializer concreto deve ser simples e local.
