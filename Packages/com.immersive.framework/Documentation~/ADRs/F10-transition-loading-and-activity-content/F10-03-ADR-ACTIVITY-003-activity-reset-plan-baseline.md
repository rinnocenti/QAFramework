# F10-03 — ADR-ACTIVITY-003 — Activity Reset Plan Baseline

Status: Draft / Planned
Fase: F10
Ordem no Plano: F10-03
Tipo: Activity / Reset
Escopo: Activity lifecycle

---

## Contexto

O `NewScripts` tinha reset de Activity como operação distinta de clear/reload. O roadmap antigo preservava o conceito, mas não tinha fase concreta.

---

## Decisão

Definir reset como operação formal:

```text
ActivityResetRequest
ActivityResetPlan
ActivityResetResult
ActivityResetScope
```

F10 define o baseline de reset e sua relação com transition/readiness. F11 adiciona participants locais. F12 pode adicionar snapshot-backed reset/restore.

---

## Regras

- Reset não é Clear Activity.
- Reset não troca Route.
- Reset não cria pipeline monolítica.
- Reset não descobre objetos por nome/path.
- Reset usa participants/capabilities tipados quando eles existirem.

---

## Critérios de validação

- Reset request é rejeitado se Activity não está ativa.
- Reset plan é diagnosticável antes de executar side effects.
- Reset não deixa stale handles ou contributions.
- Reset respeita transition guard.
