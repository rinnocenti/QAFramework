# ADR-BL-004 — QA and Diagnostics Boundary

Status: Proposed  
Fase: F0A  
Tipo: Diagnostics / Tooling  
Escopo: Runtime / QA / Editor

---

## Contexto

`FrameworkQaCanvas` é útil para smoke manual, mas está em runtime público. Diagnostics são necessários no core, mas QA panels não devem virar requisito de produto.

## Decisão

Separar:

| Camada | Pode ficar no runtime core? | Regra |
|---|---:|---|
| `FrameworkLogger` | Sim | Logging mínimo via `com.immersive.logging`. |
| `FrameworkFact` | Sim | Diagnostics estruturado, não UI. |
| Validators editor-only | Sim, em assembly editor | Não entram em player runtime. |
| QA Canvas / smoke UI | Não como API de produto | Deve ser development-only/tooling ou package separado. |

Decisão proposta: manter QA Canvas como development tooling enquanto o framework amadurece, protegido por política clara de build.

## Consequências

### Positivas

- Preserva smokes manuais.
- Evita poluir runtime de produto.
- Mantém diagnostics estruturado separado de UI.

### Negativas / trade-offs

- Pode exigir diretivas de build ou reorganização posterior.
- QA em player release fica limitado.

## Fora do escopo

- Criar dashboard novo.
- Criar telemetry externa.

## Critérios de validação

- QA Canvas não é tratada como API obrigatória do framework.
- Build release não inclui tooling sem decisão explícita.
- Facts/logs continuam disponíveis para diagnóstico mínimo.

## Impacto esperado

Destrava higiene de baseline e evolução de validators.

## Relação com roadmap

F0A/F0B/F1.

## Notas de implementação

Não confundir logs humanos com `FrameworkFact` estruturado.
