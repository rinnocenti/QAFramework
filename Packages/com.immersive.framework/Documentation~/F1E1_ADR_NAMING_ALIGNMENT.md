# F1E1 — ADR Naming and Roadmap Alignment

Status: CLOSED / DOCUMENTATION ONLY  
Fase: F1E1  
Tipo: Documentation hygiene  
Escopo: ADR navigation, roadmap alignment

---

## Resultado

```text
F1E1 — CLOSED / DOCUMENTATION ONLY
```

Este corte corrige a navegação dos ADRs para evitar a impressão de que o plano está pulando etapas.

## Problema corrigido

Os arquivos ADR usavam apenas uma numeração local por pasta:

```text
01-ADR-CONTENT-001-content-identity-domain.md
02-ADR-DIAG-001-frameworkfact-vs-human-log.md
03-ADR-ID-001-typed-identity-policy.md
```

Isso mantinha o id arquitetural, mas escondia a ordem real do plano. Na prática, a leitura ficava inconsistente com cortes como `F1A`, `F1B`, `F1C`, `F1D` e `F1E`.

## Decisão aplicada

Os arquivos ADR agora seguem:

```text
<plan-order>-<adr-id>-<slug>.md
```

Exemplo:

```text
F1A-01-ADR-ID-001-typed-identity-policy.md
F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md
F1A-03-ADR-CONTENT-001-content-identity-domain.md
```

## O que foi alterado

- ADRs de `F0A` até `F11` foram renomeados para começar pela ordem do plano.
- Os títulos internos dos ADRs receberam o prefixo de ordem do plano.
- Cada ADR renomeado recebeu `Ordem no Plano` no cabeçalho.
- `Documentation~/README.md` passou a listar a ordem do plano em tabela própria.
- O roadmap passou a exibir `Ordem no Plano` no backlog de ADRs.
- Foi criado `ADR_NAMING_CONVENTION.md` com a regra e a tabela old path → new path.
- Foi criado `F1E1_RENAMED_ADRS_REMOVED_FILES.txt` para orientar remoção dos nomes antigos em aplicação por overlay.

## O que não foi alterado

```text
runtime
editor tooling
asmdefs
package.json
status dos ADRs
status dos cortes fechados
semântica de F1E
```

## Status de smoke

Este corte é somente documentação/renomeação de arquivos `.md`. Não exige smoke próprio.

F1E continua aguardando compile-smoke.
