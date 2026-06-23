# F10-02 — ADR-ACTIVITY-002 — Activity Content Profile Execution

Status: Draft / Planned
Fase: F10
Ordem no Plano: F10-02
Tipo: Activity / Content
Escopo: Activity-owned content

---

## Contexto

F4 fechou ActivityContentSet/readiness baseline sem profile loading. F6 fechou Route scene composition/release. Depois de F8/F9, Activity pode ganhar conteúdo próprio com ownership e release corretos.

---

## Decisão

ActivityContentProfile execution deve ser planejada/executada como Activity-owned content:

```text
ActivityContentProfileAsset
ActivityContentExecutionPlan
ActivityContentExecutionResult
Activity-owned scene entries
Activity-owned runtime content entries
Activity content readiness contribution
Activity content release plan
```

---

## Regras

- Não reabrir F4 para transformar local visibility em materialization.
- Não reutilizar RouteContentProfile sem distinguir ownership.
- Activity-owned content deve liberar no Activity exit antes de Route release.
- Required content failure bloqueia readiness quando policy exigir.
- Optional content failure vira diagnostic/skip.

---

## Critérios de validação

- Activity with profile carrega/declara content owned.
- Activity readiness considera content required.
- Activity clear/exit libera content owned.
- Route exit libera Activity content antes de Route content.
