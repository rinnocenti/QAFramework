# F5 — Local Content Identity and scene-authored local ids

Status: F5E APPLIED / PENDING COMPILE-SMOKE  
Fase: F5 — Local Contribution baseline  
Documento técnico vivo: identidade local e authoring local mínimo

---

## Objetivo

F5 começa criando identidade local explícita para contribuições scene-authored, sem reintroduzir `targetId` universal e sem usar nome/path de GameObject como fallback funcional.

A F5 usa os componentes reais já existentes na cena como superfície de authoring:

```text
RouteContentBinding
ActivityLocalVisibilityAdapter
```

Não há `LocalContributionMarker` separado no F5C.

---

## F5B — LocalContentIdentity

F5B criou apenas os tipos de identidade:

```text
Runtime/LocalContribution/LocalContentId.cs
Runtime/LocalContribution/LocalContentIdentity.cs
Runtime/LocalContribution/LocalContentScopeKind.cs
```

`LocalContentIdentity` identifica uma contribuição local dentro de um owner de conteúdo conhecido.

Composição técnica:

```text
FrameworkContentScope contentScope
FrameworkIdentityKey scopeOwner
LocalContentScopeKind localScopeKind
LocalContentId localId
```

Forma diagnóstica:

```text
local:<ContentScope>:<ScopeOwnerValue>:<LocalScopeKind>:<LocalId>
```

Exemplo:

```text
local:Activity:QA_PrimaryContentActivity:SceneAuthored:primary-panel
```

Regras aplicadas no código:

```text
contentScope deve ser Session, Route ou Activity
scopeOwner deve ser válido
scopeOwner.Domain deve corresponder ao contentScope
localScopeKind não pode ser Unknown
localId não pode ser vazio/nulo/whitespace
```

---

## F5C — explicit local ids on existing scene-authored bindings

F5C não cria marker paralelo. O corte adiciona `Local Content Id` aos componentes que já marcam conteúdo local de cena:

```text
Runtime/RouteLifecycle/RouteContentBinding.cs
Runtime/ActivityFlow/ActivityLocalVisibilityAdapter.cs
```

Cada um passa a expor:

```text
LocalScopeKind = SceneAuthored
LocalContentIdText
HasExplicitLocalContentId
TryGetLocalContentId(out LocalContentId localId)
```

O id é obrigatório para validação autoral. Ele ainda não é consumido por discovery runtime, `LocalContributionSet`, requiredness ou materialização.

---

## Remoção do precursor obsoleto

F5C remove o trilho genérico experimental:

```text
Runtime/ContentFlow/FrameworkContentContributionMarker.cs
Runtime/ContentFlow/IFrameworkContentContribution.cs
```

Motivo: esse precursor não tinha consumer real e competia com os bindings/adapters reais de cena. A identidade local deve entrar no ponto de authoring que o usuário já configura, não em um componente paralelo.

---

## Não são identidade funcional

Estes valores podem aparecer em diagnostics, labels e mensagens de validator, mas não são chave funcional:

```text
GameObject.name
Scene name
Scene path
Hierarchy path
Transform path
targetId universal
ownerPath
componentPath
```

---

## Como configurar cenas para validação

### Route content

Em cada GameObject com `Route Content Binding`:

```text
Route: Route dona da cena
Local Content Id: id estável e explícito dentro daquela Route
```

Exemplos QA:

```text
QA_RouteContent_Canonical  -> qa-route-canonical-probe
QA_RouteContent_Alternate  -> qa-route-alternate-probe
```

### Activity local visibility

Em cada GameObject com `Activity Local Visibility Adapter`:

```text
Activity: Activity dona desse conteúdo local
Local Content Id: id estável e explícito dentro daquela Activity
```

Exemplo QA:

```text
Local Visibility Adapter -> qa-activity-secondary-local-visibility
```

Use ids curtos, estáveis, em minúsculas e orientados ao papel autoral. Não copie o nome do GameObject como regra.

---

## F5D — loaded local contribution discovery

F5D cria discovery carregado para contribuições locais scene-authored.

Arquivos novos:

```text
Runtime/LocalContribution/LocalContributionDiscovery.cs
Runtime/LocalContribution/LocalContributionDiscoveryResult.cs
Runtime/LocalContribution/LocalContributionDiscoveryIssue.cs
Runtime/LocalContribution/LocalContributionDiscoveryIssueKind.cs
Runtime/LocalContribution/LocalContributionHandle.cs
Runtime/LocalContribution/LocalContributionSet.cs
Runtime/LocalContribution/LocalContributionSourceKind.cs
```

O discovery lê os componentes de authoring existentes:

```text
RouteContentBinding
ActivityLocalVisibilityAdapter
```

E produz:

```text
LocalContributionSet
LocalContributionHandle[]
LocalContributionDiscoveryIssue[]
```

Regras do F5D:

```text
Local Content Id ausente = issue estruturada
Route/Activity owner ausente = issue estruturada
LocalContentIdentity inválida = issue estruturada
LocalContentIdentity duplicada no mesmo escopo/owner/local id = issue estruturada
GameObject.name/scene/path/hierarchy = diagnostics only
```

O F5D também muda a QA surface: a validação autoral local deve ser rodada pelo `Framework QA Canvas`, botão `Validate Loaded Local Contributions`. O log esperado em sucesso usa:

```text
QA Authoring Validation completed. scope='Loaded Local Contributions' ... issues='0'
```

## F5E — LocalContributionSet consolidation

F5E não cria nova superfície de authoring e não muda comportamento visual. O corte consolida o `LocalContributionSet` como snapshot consultável.

O set passa a expor consultas internas por:

```text
FrameworkContentScope
LocalContributionSourceKind
LocalContentIdentity
```

APIs adicionadas:

```text
SessionCount
RouteCount
ActivityCount
RouteContentBindingCount
ActivityLocalVisibilityAdapterCount
HasScope(contentScope)
CountByScope(contentScope)
CountBySource(sourceKind)
Contains(identity)
TryGet(identity, out handle)
GetByScope(contentScope)
GetBySource(sourceKind)
```

O diagnóstico de QA continua vindo de `Framework QA Canvas > Validate Loaded Local Contributions`, mas agora o texto do set inclui resumo por escopo/source, por exemplo:

```text
handles='2' session='0' route='1' activity='1' routeBindings='1' activityAdapters='1'
```

Esse corte ainda não torna o set consumidor de lifecycle, não aplica requiredness e não materializa conteúdo.

---

## Escopo ainda não implementado

F5E ainda não implementa:

```text
Required/Optional policy
Runtime scanner por capability
ActivityContentSet integration funcional
RouteContentSet integration funcional
Surface
Actors
Input
Camera
Reset
Snapshot
Save
Pooling
Materialization
Release/unload policy
```

---

## Resultado esperado

O pacote deve compilar sem alterar comportamento runtime.

Critério de compile-smoke mínimo:

```text
error CS                                      0
Exception                                     0
FATAL                                         0
Boot succeeded                                1
QA Smoke completed. name='Standard Smoke'     1
```

Depois de configurar os `Local Content Id` nos bindings/adapters das cenas abertas, use o `Framework QA Canvas` e rode `Validate Loaded Local Contributions`. O log deve reportar `issues='0'` para o escopo carregado e incluir contagens por escopo/source no `LocalContributionSet`.
