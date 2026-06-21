# F1A — API status, Identity and Diagnostics ADR acceptance

Status: Accepted  
Fase: F1A  
Tipo: ADR / Governance  
Escopo: API status, Identity, Diagnostics, Content identity

---

## 1. Resultado

```text
F1A — CLOSED / ACCEPTED
```

Este corte aceita os ADRs de F1 necessários antes de qualquer implementação técnica de API status, typed identity, `FrameworkFact`, `ValidationMode` ou revisão de `FrameworkContentHandle`.

F1A não altera runtime, editor tooling, asmdef, package manifest ou comportamento em Play Mode.

---

## 2. ADRs aceitos

| Ordem no Plano | ADR | Resultado | Decisão central |
|---|---|---|---|
| `F1A-01` | `ADR-ID-001 — Typed Identity Policy` | Accepted | Identidade funcional nova deve ter domínio explícito e tipo próprio; string crua fica para label/debug/source/reason. |
| `F1A-02` | `ADR-DIAG-001 — FrameworkFact vs Human Log` | Accepted | Log humano e fact estruturado são contratos diferentes; não parsear log como fonte funcional de verdade. |
| `F1A-03` | `ADR-CONTENT-001 — Content Identity Domain` | Accepted | Content identity deve compor owner, scope, kind e id; path/name não bastam como chave funcional pública. |

---

## 2.1. Arquivos ADR

Os arquivos seguem a ordem do plano antes do id arquitetural:

```text
F1A-01-ADR-ID-001-typed-identity-policy.md
F1A-02-ADR-DIAG-001-frameworkfact-vs-human-log.md
F1A-03-ADR-CONTENT-001-content-identity-domain.md
```

---

## 3. Ordem lógica decidida

A ordem conceitual de F1 é:

```text
Typed Identity Policy
  ↓
FrameworkFact vs Human Log
  ↓
Content Identity Domain
  ↓
Cortes técnicos mínimos de F1
```

Racional:

- `ADR-ID-001` define a regra geral de identidade.
- `ADR-DIAG-001` define como validar/relatar sem depender de parsing de log.
- `ADR-CONTENT-001` aplica a política de identidade ao domínio de content/handles.

---

## 4. O que F1A autoriza

F1A autoriza abrir cortes técnicos pequenos para:

1. criar convenção/marcador mínimo de API status;
2. criar tipos mínimos de identity quando o corte exigir;
3. criar `FrameworkFact` mínimo;
4. dar semântica inicial a `ValidationMode`;
5. revisar `FrameworkContentHandle` sem criar materializer genérico;
6. atualizar validators/smoke para usar facts/resultados tipados quando houver suporte.

---

## 5. O que F1A não autoriza

F1A não autoriza:

```text
SessionContentSet
RouteContentRuntime connection
RouteContentSet execution
ActivityContentSet redesign
LocalContribution
Surface
RuntimeMaterialization
Addressables
Input
Save
Pause
Camera
Audio
Actor
Pooling
Projectile
```

Também não autoriza migrar todos os IDs existentes em um único corte.

---

## 6. Critérios para abrir F1 técnico

Antes do primeiro corte técnico de F1:

```text
1. O corte deve apontar qual ADR F1 implementa.
2. O corte deve manter escopo mínimo.
3. O corte não deve antecipar F2/F3/F4/F5.
4. APIs novas não devem usar string crua como identidade funcional.
5. Logs novos não devem ser tratados como contrato funcional.
6. Qualquer fallback required ausente deve falhar ou gerar diagnóstico explícito.
```

---

## 7. Próximo corte recomendado

```text
F1B — API status convention and minimal markers
```

Motivo: antes de criar facts e typed IDs, o package precisa de uma forma simples e consistente de marcar `Stable`, `Experimental`, `Internal`, `Deferred`, `DevelopmentTooling` e `Removed` nas superfícies existentes e novas.

