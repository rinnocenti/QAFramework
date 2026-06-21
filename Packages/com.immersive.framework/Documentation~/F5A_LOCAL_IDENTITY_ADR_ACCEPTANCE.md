# F5A — Local Identity ADR Acceptance

Status: CLOSED / ADR ACCEPTED  
Fase: F5  
Tipo: ADR checkpoint  
Runtime changes: none

---

## Resultado

F5A aceita o ADR necessário para iniciar a implementação técnica da identidade local.

```text
F5-01 — ADR-LOCAL-001 — Local Identity — Accepted
F5-02 — ADR-LOCAL-002 — Local Contribution Discovery and Requiredness — permanece Draft / Deferred
```

Este corte não altera runtime, asmdefs, package dependencies, editor tooling, validators ou comportamento em Play Mode.

---

## Decisões consolidadas

### 1. `LocalContentIdentity` é identidade funcional própria

A F5 não deve reutilizar `ActivityContentSet` F4, `FrameworkContentHandle`, `GameObject.name`, scene path ou hierarchy path como identity funcional de contribuição local.

### 2. `targetId` não volta como cola universal

A auditoria do `NewScripts` mostrou que `targetId` conectava contributor, requirement, reset, snapshot, restore, release, placement e diagnostics por convenção textual.

F5 preserva a intenção de contribuição local, mas substitui essa cola textual por identidade tipada e escopada.

### 3. Marker local futuro exige id explícito

Se um marker local existir, a identidade é obrigatória.

```text
Sem id explícito = falha de validação.
Fallback por nome/path = proibido como chave funcional.
```

### 4. `FrameworkContentContributionMarker` atual é precursor experimental

O marker atual não pode ser promovido diretamente para F5 canônica enquanto permitir fallback de `ContributionId` para `GameObject.name`.

F5B/F5C devem escolher entre criar novo marker local ou refatorar o marker atual, mas não podem preservar fallback silencioso.

### 5. F5B fica limitado ao tipo de identidade

O próximo corte técnico deve criar o primitivo de identidade local e testes/validações compatíveis com esse escopo, sem discovery, marker, contribution set ou requiredness.

---

## Próximo corte autorizado

```text
F5B — IF-FW-ROAD-5B — LocalContentIdentity
```

Escopo esperado:

```text
- criar `LocalContentIdentity` como tipo pequeno, imutável e validável;
- definir comparação ordinal e texto diagnóstico estável;
- rejeitar null, empty e whitespace nos campos funcionais;
- não criar marker;
- não criar discovery;
- não alterar ActivityContentRuntime;
- não alterar ActivityContentSet;
- não alterar FrameworkContentContributionMarker ainda, salvo se for inevitável para compilar o tipo, o que não é esperado.
```

---

## Validação

F5A é documentation/ADR only. Não exige smoke próprio.

A próxima validação de runtime deve ocorrer no primeiro corte técnico da F5 que alterar código.
