# F11-02 — ADR-CAPABILITY-001 — Live Capability Inventory

Status: Draft / Planned
Fase: F11
Ordem no Plano: F11-02
Tipo: Capability / Runtime
Escopo: Local/Runtime content

---

## Contexto

F5 criou LocalContributionSet como snapshot/diagnóstico. Isso não é suficiente para consumers que precisam de referências vivas com validade de lifecycle.

---

## Decisão

Criar uma fronteira de capability runtime:

```text
LocalCapabilityDescriptor
RuntimeCapabilityReference
CapabilityOwnerScope
CapabilityValidity
Stale/foreign rejection
```

O inventory vivo depende de RuntimeContentHandle/lifetime e deve rejeitar referências de escopo morto.

---

## Regras

- LocalContributionSet continua sendo discovery/snapshot.
- RuntimeCapabilityReference não pode viver além do owner scope.
- Nenhuma capability é descoberta por nome/path como chave funcional.
- Consumers não criam inventories próprios.

---

## Critérios de validação

- Capability reference válida durante owner scope ativo.
- Capability reference stale é rejeitada após exit/release.
- Capability de Activity A não é aceita em Activity B.
