# F6-02 — ADR-SCENE-001 — Route Scene Composition Plan and Result

Status: Draft / Deferred  
Fase: F6  
Ordem no Plano: F6-02  
Tipo: Scene / Route  
Escopo: Route scene composition

---

## Contexto

Route additive execution deve ser tratado como scene composition, não como RuntimeSpawned/materialization genérica.

## Decisão

Route scene composition deve seguir plan/result:

```text
RouteSceneCompositionPlan
→ SceneLifecycle execution
→ RouteSceneCompositionResult
→ RouteContentSet update
```

Primary scene e additive scenes devem ter ownership e requiredness explícitos. Active scene policy deve ser declarada.

## Consequências

### Positivas

- Tira additive do improviso.
- Ajuda release e validation.
- Permite route content profile executável.

### Negativas / trade-offs

- Aumenta complexidade de Route.
- Requer smoke adicional.

## Fora do escopo

- Prefab materialization.
- Runtime roots.
- Surface.
- Addressables.

## Critérios de validação

- Required additive ausente falha.
- Route exit unloads/release content conforme plan.
- Active scene é previsível.

## Impacto esperado

Base para RouteContentProfile executável.

## Relação com roadmap

F6.

## Notas de implementação

Não depende de RuntimeRootRegistry.
