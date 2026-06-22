# F5 — Local Content Identity and scene-authored local ids

Status: F5 CLOSED / LOCAL CONTRIBUTION FOUNDATION PASS  
Fase: F5 — Local Contribution baseline  
Documento técnico vivo: identidade local e authoring local mínimo

---

## Fechamento F5

F5 está fechado como foundation de Local Contribution. O smoke dedicado passou nos três passos planejados (`loaded`, `secondary`, `primary`) e os smokes de regressão `Standard Smoke` e `Route Callback Smoke` continuaram passando.

O fechamento não autoriza materialização canônica nem F6 direto; a próxima fase deve começar por auditoria/plano de Route Scene Composition e release.

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

O F5D também muda a QA interface: a validação autoral local deve ser rodada pelo `Framework QA Canvas`, botão `Validate Loaded Local Contributions`. O log esperado em sucesso usa:

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

F5E ainda não implementava:

```text
Required/Optional policy
Runtime scanner por capability
ActivityContentSet integration funcional
RouteContentSet integration funcional
Content Anchor
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


## F5F — Requiredness policy mínima

F5F registra `Required/Optional` nas contribuições locais reais sem alterar materialização ou comportamento visual.

A superfície de authoring passa a expor `Requiredness` em:

```text
RouteContentBinding
ActivityLocalVisibilityAdapter
```

O discovery copia esse valor para cada `LocalContributionHandle`. O `LocalContributionSet` passa a expor:

```text
RequiredCount
OptionalCount
CountByRequiredness(requiredness)
GetByRequiredness(requiredness)
```

O diagnóstico de QA passa a incluir resumo por requiredness, por exemplo:

```text
handles='2' session='0' route='1' activity='1' routeBindings='1' activityAdapters='1' required='2' optional='0'
```

Regras do F5F:

```text
- Requiredness é dado funcional da contribuição, não nome de GameObject.
- GameObject.name, scene name e hierarchy path continuam apenas diagnósticos.
- Binding/adapter presente continua exigindo Local Content Id explícito, mesmo se Optional.
- F5F não valida ausência de contribuição required porque ainda não existe lista declarativa de expected contributions.
- F5F não materializa, não carrega, não descarrega e não consome requiredness no runtime visual.
```

Escopo ainda diferido após F5F:

```text
Required ausente bloquear lifecycle
Optional ausente gerar skip estruturado
Integração funcional com RouteContentSet/ActivityContentSet
Content Anchor
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

## F5G — Local validators

F5G adiciona uma camada explícita de validação local sobre o resultado de discovery.

O corte não muda a cena, não materializa conteúdo e não cria lifecycle visual. Ele separa três conceitos que antes estavam misturados no QA:

```text
LocalContributionDiscovery   -> encontra bindings/adapters carregados
LocalContributionSet         -> snapshot das contribuições válidas encontradas
LocalContributionValidator   -> aplica regras de authoring/policy sobre o snapshot
```

Tipos adicionados:

```text
LocalContributionRequirement
LocalContributionValidator
LocalContributionValidationResult
LocalContributionValidationIssue
LocalContributionValidationIssueKind
```

Regras validadas ou preparadas em F5G:

```text
- Discovery issue vira validation issue bloqueante.
- Expected contribution inválida vira erro bloqueante.
- Required expected contribution ausente vira erro bloqueante.
- Optional expected contribution ausente vira skip/diagnóstico não bloqueante.
- O QA de Loaded Local Contributions passa a consumir LocalContributionValidator.
```

Observação importante: o QA atual ainda não possui uma lista declarativa de expected contributions por Route/Activity. Portanto, no cenário carregado atual, F5G valida discovery + set + metadata já existente. A política de ausência required está implementada no validator para consumidores futuros, mas ainda não é alimentada por authoring declarativo.

Escopo ainda diferido após F5G:

```text
Declarar expected contributions em assets
Fazer required ausente bloquear lifecycle real
Fazer optional ausente aparecer como skip em smoke dedicado
Integração funcional com RouteContentSet/ActivityContentSet
Content Anchor
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

## F5H — Local smoke dedicado

F5H adiciona um smoke específico para validar o trilho de Local Contribution sem misturar com Standard Smoke ou Route Callback Smoke.

O botão novo no QA Canvas é:

```text
Run Local Contribution Smoke
```

O smoke executa somente validação autoral/runtime leve:

```text
- valida o conjunto carregado inicial;
- solicita a Secondary Activity;
- valida novamente o conjunto carregado;
- restaura a Primary Activity;
- valida novamente o conjunto carregado.
```

Em cada passo, o smoke exige:

```text
- pelo menos um RouteContentBinding carregado;
- pelo menos um ActivityLocalVisibilityAdapter carregado;
- LocalContributionValidator sem blocking issues;
- LocalContributionSet com handles válidos;
- contagem de handles compatível com os componentes authored carregados;
- required + optional igual ao total de contribuições.
```

Logs esperados em sucesso:

```text
QA Local Contribution Smoke step completed. step='loaded'
QA Local Contribution Smoke step completed. step='secondary'
QA Local Contribution Smoke step completed. step='primary'
QA Smoke completed. name='Local Contribution Smoke'
```

Escopo ainda diferido após F5H:

```text
Declarar expected contributions em assets
Fazer required ausente bloquear lifecycle real
Fazer optional ausente aparecer como skip alimentado por authoring declarativo
Integração funcional com RouteContentSet/ActivityContentSet
Content Anchor
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

