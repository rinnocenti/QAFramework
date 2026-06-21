# F0A-04 — ADR-BL-004 — QA and Diagnostics Boundary

Status: Accepted  
Fase: F0A  
Ordem no Plano: F0A-04  
Tipo: Diagnostics / Tooling  
Escopo: Runtime / QA / Editor

---

## Contexto

`FrameworkQaCanvas` é útil para smoke manual, mas está em runtime público. Diagnostics são necessários no core, mas painéis e botões de QA não devem virar requisito de produto.

## Decisão

Separar diagnostics de tooling.

| Camada | Pode ficar no runtime core? | Status | Regra |
|---|---:|---|---|
| `FrameworkLogger` | Sim | `Internal` | Logging mínimo via `com.immersive.logging`. |
| `FrameworkFact` | Sim, quando existir | `Experimental` em F1 | Diagnostics estruturado, não UI. |
| Validators editor-only | Sim, em assembly editor | `Development Tooling` | Não entram em player runtime. |
| QA Canvas / smoke UI | Temporariamente, mas não como API de produto | `Development Tooling` | Deve ser protegido por política de build/dev ou movido futuramente. |

Decisão aceita: manter `FrameworkQaCanvas` como development tooling enquanto o framework amadurece, mas F0B deve impedir que ele seja confundido com superfície runtime de produto.

## Consequências

### Positivas

- Preserva smokes manuais.
- Evita poluir a API de produto.
- Mantém logs humanos separados de facts estruturados.
- Permite evoluir validators sem criar UI obrigatória no runtime.

### Negativas / trade-offs

- Pode exigir diretivas de build, asmdef separado ou package de tooling posterior.
- QA em player release fica limitado por padrão.
- Smokes manuais continuam fora de garantia automatizada.

## Fora do escopo

- Criar dashboard novo.
- Criar telemetry externa.
- Criar `FrameworkFact` completo em F0A.
- Reorganizar todo o tooling em packages separados.

## Critérios de validação

- QA Canvas não é tratado como API obrigatória do framework.
- Build release não inclui tooling sem decisão explícita.
- Logs mínimos continuam disponíveis para diagnóstico.
- F1 pode introduzir `FrameworkFact` sem misturá-lo com UI.

## Impacto esperado

Destrava F0B e prepara F1 diagnostics.

## Relação com roadmap

F0A/F0B/F1.

## Notas de implementação

Não confundir log humano com `FrameworkFact`. O log explica; o fact valida/estrutura evidência.
