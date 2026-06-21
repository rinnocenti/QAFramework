# F4 Closure — Activity Content and Readiness Baseline

Status: CLOSED / ACTIVITY BASELINE PASS  
Fase: F4  
Último corte: F4G  
Próxima fase autorizada: F5 — Local Contribution, iniciando por ADR/identidade local

---

## Cortes fechados

```text
F4A — CLOSED / COMPILE-SMOKE PASS
F4B — CLOSED / COMPILE-SMOKE PASS
F4C — CLOSED / COMPILE-SMOKE PASS
F4D — CLOSED / COMPILE-SMOKE PASS
F4E — CLOSED / COMPILE-SMOKE PASS
F4F — CLOSED / COMPILE-SMOKE PASS
F4G — CLOSED / COMPILE-SMOKE PASS
F4  — CLOSED / ACTIVITY BASELINE PASS
```

## Shape final da F4

F4 foi fechada após o smoke final do F4G. O baseline mínimo de Activity não copia `ActivityEntryPipeline` da Base antiga:

```text
ActivityAsset
  -> ActivityRuntimeState
  -> ActivityLocalVisibilityAdapter
  -> ActivityContentSet
  -> ActivityContentLifecycleResult
  -> ActivityReadinessState
  -> Activity Baseline Smoke
```

## Decisões estabilizadas

- `ActivityRuntimeState` expõe Activity ativa/None com identidade diagnóstica `Activity:*`.
- `ActivityLocalVisibilityAdapter` é adapter scene-authored de visibilidade local, não materialização canônica.
- `ActivityContentSet` é snapshot mínimo de conteúdo local conhecido para a Activity ativa.
- `ActivityContentLifecycleResult` agrega callbacks locais enter/exit e falhas.
- `ActivityReadinessState` cobre readiness baseline `Ready`, `None` e `NotReady`.
- O smoke dedicado valida switch, content set, readiness, clear e restore.

## Fronteira preservada para F5

F4 não define identidade funcional de contribuição local.

```text
ActivityContentSet F4 = snapshot local/diagnóstico de adapters de visibilidade.
LocalContributionSet F5 = identidade funcional própria, sem depender de GameObject name/path/scene path como chave funcional.
```

Portanto F5 não deve reutilizar handles derivados de nome de cena/objeto como identidade canônica.

## Fora do escopo preservado

```text
ActivityContentProfile loading
canonical Activity materialization
scene composition
additive Activity scene loading
release/unload policy
actors
input
camera
reset
snapshot
Surface
RuntimeMaterialization
LocalContributionSet implementation
```

## Próximo passo

```text
F5A — ADR Local Identity
```

F5A começa por decisão/contrato de identidade local antes de discovery, requiredness ou runtime inventory.
