# F5-02 — ADR-LOCAL-002 — Local Contribution Discovery and Requiredness

Status: Draft / Deferred  
Fase: F5  
Ordem no Plano: F5-02  
Tipo: Local / Discovery  
Escopo: LocalContributionSet  
Depende de: F5-01 — ADR-LOCAL-001 — Local Identity

---

## Contexto

O package possui bindings/adapters locais simples. A partir do F5C, `RouteContentBinding` e `ActivityLocalVisibilityAdapter` passam a carregar `Local Content Id` explícito; o precursor genérico `FrameworkContentContributionMarker` foi removido por ficar obsoleto e por não ter consumer real.

O `NewScripts` tinha capacidades úteis de discovery local, reports, scan targets, reset, snapshot, restore e release, mas elas se apoiavam demais em `targetId`, `sceneName`, paths e matching textual entre stages.

A F5 deve preservar a capacidade de descobrir contribuições locais, mas sem reintroduzir:

```text
targetId universal
FindObjectsByType global como fonte de verdade
GameObject.name como fallback funcional
scene path/hierarchy path como identity
capability inventory vivo antes de runtime handles
```

---

## Decisão proposta

Local discovery deve operar sobre ContentSets conhecidos e identidades locais explícitas.

Fluxo conceitual:

```text
RouteContentSet / ActivityContentSet
→ Local discovery scope
→ existing scene-authored bindings with explicit local ids
→ LocalContributionDiscovery
→ LocalContributionSet
→ Required/Optional policy
```

`ActivityContentSet` e `RouteContentSet` delimitam onde procurar. Eles não fornecem a identity funcional da contribuição.

`ActivityLocalVisibilityAdapter` / `RouteContentBinding` deve carregar ou produzir uma `LocalContentIdentity` válida conforme ADR-LOCAL-001.

---

## Requiredness

Requiredness pertence à contribuição ou à policy que consome aquela contribuição, não ao nome do GameObject.

Regras propostas:

| Caso | Resultado |
|---|---|
| Contribution required ausente | Failure estruturado / `FrameworkFact`. |
| Contribution optional ausente | Skip diagnosticado. |
| Binding/adapter sem identity explícita | Validation failure. |
| Duplicidade no mesmo escopo | Validation failure. |
| Binding/adapter fora do content set ativo | Ignorado ou diagnosticado como out-of-scope, não consumido. |

---

## Consequências

### Positivas

- Discovery deixa de ser varredura global oportunista.
- `LocalContributionSet` vira snapshot determinístico do ciclo local.
- Required/Optional fica validável antes de consumers reais.
- Surface, Snapshot, Reset e Actors podem consumir uma base comum depois, sem capturar o core.

### Negativas / trade-offs

- Depende de `LocalContentIdentity` implementado antes.
- Pode exigir validators editor-only por fase.
- Exige que discovery futuro consuma os bindings/adapters existentes, não um marker genérico paralelo.
- Não resolve ainda runtime references/lifetime.

---

## Fora do escopo

```text
Reset/snapshot/release execution
Actors
Surface
RuntimeMaterialization
Runtime capability references
Pooling
Input
Camera
Save backend
```

F5 não deve criar scanner por capability específica. Capability descriptors simples podem existir apenas se forem necessários para o `LocalContributionSet`; runtime refs vivos ficam para fases posteriores.

---

## Critérios de validação futuros

- Discovery limitado ao content set ativo.
- Nenhum caminho required usa fallback silencioso.
- `LocalContributionSet` é produzido a partir de bindings/adapters com `LocalContentIdentity` explícita.
- Duplicidade e missing identity aparecem no validator.
- Required ausente falha de forma estruturada.
- Optional ausente não bloqueia, mas fica diagnosticado.

---

## Impacto esperado

Pré-requisito para:

```text
Surface declaration
Snapshot participants
Reset participants
Capability descriptors
Runtime materialization consumers
```

---

## Notas de implementação futura

Não criar discovery por `FindObjectsByType` como eixo funcional.

Se for necessário usar API Unity para enumerar componentes, a enumeração deve ser limitada pelos roots/entries de ContentSet carregado e deve produzir diagnostics explícitos.
