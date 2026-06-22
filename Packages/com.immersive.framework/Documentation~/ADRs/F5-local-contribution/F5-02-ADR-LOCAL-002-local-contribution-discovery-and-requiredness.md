# F5-02 — ADR-LOCAL-002 — Local Contribution Discovery and Requiredness

Status: Partially Accepted / Discovery, Set Consolidation, Requiredness Metadata and Validator Policy Applied / Expected Declarations Deferred  
Fase: F5  
Ordem no Plano: F5-02  
Tipo: Local / Discovery  
Escopo: LocalContributionDiscovery / LocalContributionSet / Requiredness futura  
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

## Decisão aplicada parcialmente em F5D

Local discovery deve operar sobre identidades locais explícitas. O F5D aplica a primeira versão sobre conteúdo scene-authored carregado e mantém integração funcional com `ActivityContentSet`/`RouteContentSet` para corte posterior.

Fluxo conceitual:

```text
RouteContentSet / ActivityContentSet
→ Local discovery scope
→ existing scene-authored bindings with explicit local ids
→ LocalContributionDiscovery
→ LocalContributionSet
→ Required/Optional policy
```

`ActivityContentSet` e `RouteContentSet` continuam não fornecendo a identity funcional da contribuição. No F5D, o discovery carregado ainda usa os bindings/adapters existentes como superfície de enumeração; integração funcional por ContentSet fica diferida.

`ActivityLocalVisibilityAdapter` / `RouteContentBinding` deve carregar ou produzir uma `LocalContentIdentity` válida conforme ADR-LOCAL-001.

---

## Requiredness — F5F metadata applied / F5G validator policy applied

Requiredness pertence à contribuição ou à policy que consome aquela contribuição, não ao nome do GameObject. Em F5F, `RouteContentBinding` e `ActivityLocalVisibilityAdapter` carregam `FrameworkContentRequiredness` e o discovery copia esse valor para o `LocalContributionHandle`. Em F5G, a política de ausência passa a existir no `LocalContributionValidator` para listas expected fornecidas por consumidores futuros.

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

## Critérios de validação

Aplicado em F5D:

- `LocalContributionSet` é produzido a partir de bindings/adapters com `LocalContentIdentity` explícita.
- Missing identity aparece como issue estruturada.
- Duplicidade aparece como issue estruturada.
- `GameObject.name`, scene name, scene path e hierarchy path não são fallback funcional.
- QA usa `Framework QA Canvas > Validate Loaded Local Contributions`.

Futuro:

- Discovery limitado formalmente ao content set ativo.
- Nenhum caminho required usa fallback silencioso.
- Required ausente falha de forma estruturada.
- Optional ausente não bloqueia, mas fica diagnosticado.

---


## F5F — aplicação

O F5F aplica requiredness como metadata funcional da contribuição descoberta:

```text
RouteContentBinding.Requiredness
ActivityLocalVisibilityAdapter.Requiredness
LocalContributionHandle.Requiredness
LocalContributionSet.RequiredCount / OptionalCount
```

A policy de ausência continua diferida. Portanto, F5F ainda não sabe que uma contribuição required “deveria existir” se ela não está declarada em uma lista expected. O corte apenas garante que, quando uma contribuição existe e é descoberta, seu requiredness viaja junto com a identity local.

Binding/adapter presente sem `Local Content Id` continua sendo erro de authoring mesmo se `Requiredness = Optional`; optionalidade não é fallback de identity.

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


---

## F5D — aplicação

O F5D introduz discovery carregado e inerte:

```text
LocalContributionDiscovery
LocalContributionDiscoveryResult
LocalContributionDiscoveryIssue
LocalContributionHandle
LocalContributionSet
```

A saída é diagnóstica/autoral neste corte. Não materializa, não carrega, não descarrega, não aplica requiredness e não cria runtime references vivos.

---

## F5E — aplicação

O F5E consolida `LocalContributionSet` como snapshot consultável. O set agora expõe contagens e filtros por `FrameworkContentScope`, `LocalContributionSourceKind` e `LocalContentIdentity`.

Este corte ainda não introduz requiredness, policy, materialização, release ou consumers. O objetivo é permitir que o próximo corte de policy consulte o set sem varrer novamente a cena nem depender de `GameObject.name`, scene path ou hierarchy path.


## F5G — aplicação

F5G adiciona validators locais explícitos:

```text
LocalContributionRequirement
LocalContributionValidator
LocalContributionValidationResult
LocalContributionValidationIssue
LocalContributionValidationIssueKind
```

A validação carregada continua usando o QA Canvas, mas agora passa pela camada `LocalContributionValidator` em vez de contar apenas issues do discovery.

Política aplicada:

```text
Discovery issue -> validation issue bloqueante
Expected contribution inválida -> erro bloqueante
Expected required ausente -> erro bloqueante
Expected optional ausente -> skip diagnóstico não bloqueante
```

O corte não cria ainda authoring declarativo de expected contributions. Sem essa lista, o QA atual valida o conjunto carregado e mantém `required='2' optional='0'` como metadata dos handles encontrados.

## F5H — aplicação

F5H adiciona um smoke dedicado para Local Contribution no `FrameworkQaCanvas`. O smoke valida o conjunto carregado em três momentos: estado carregado atual, após solicitar a Secondary Activity e após restaurar a Primary Activity.

O smoke não materializa conteúdo, não altera `ActivityContentRuntime`, não declara expected contributions em assets e não transforma requiredness em bloqueio de lifecycle real. Ele apenas garante que discovery, set, requiredness metadata e validator continuam consistentes durante activity enter/restore no cenário QA carregado.

Critério do smoke dedicado:

```text
QA Local Contribution Smoke step completed. step='loaded'
QA Local Contribution Smoke step completed. step='secondary'
QA Local Contribution Smoke step completed. step='primary'
QA Smoke completed. name='Local Contribution Smoke'
```


## F5 closure

F5 fecha como Local Contribution foundation. Discovery, set, requiredness metadata, validator policy e local smoke dedicado estão aplicados e validados por QA smoke. Expected contribution declarations, lifecycle blocking por required ausente, canonical materialization, Surface e release/unload policy permanecem fora da F5 e devem ser tratados por fases posteriores.
