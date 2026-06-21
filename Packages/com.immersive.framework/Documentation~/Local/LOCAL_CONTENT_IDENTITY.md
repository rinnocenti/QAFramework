# F5B — LocalContentIdentity

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F5 — Local Contribution baseline  
Corte: F5B  
Tipo: Runtime identity primitive  

---

## Objetivo

Criar o primeiro tipo técnico da F5: `LocalContentIdentity`.

Este corte implementa apenas identidade local tipada. Ele não cria marker, discovery, validator, contribution set, requiredness, capability system ou integração com `ActivityContentSet`/`RouteContentSet`.

---

## Arquivos criados

```text
Runtime/LocalContribution/LocalContentId.cs
Runtime/LocalContribution/LocalContentIdentity.cs
Runtime/LocalContribution/LocalContentScopeKind.cs
```

---

## Modelo criado

`LocalContentIdentity` é uma identidade funcional para uma contribuição local dentro de um owner de conteúdo conhecido.

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

---

## Regras aplicadas no código

O construtor de `LocalContentIdentity` falha explicitamente quando:

```text
contentScope não é Session, Route ou Activity
scopeOwner é inválido
scopeOwner.Domain não corresponde ao contentScope
localScopeKind é Unknown
localId é vazio, nulo ou whitespace
```

`LocalContentId` exige valor explícito por `FrameworkIdentityValue`. Não existe fallback para:

```text
GameObject.name
Scene name
Scene path
Hierarchy path
targetId universal
```

---

## Escopo deliberadamente não implementado

Este corte não implementa:

```text
LocalContributionMarker
FrameworkContentContributionMarker refactor
LocalContributionDiscovery
LocalContributionSet
LocalContributionHandle
Required/Optional policy
Editor validator
Runtime scanner
ActivityContentSet integration
RouteContentSet integration
Surface
Actors
Input
Camera
Reset
Snapshot
Save
Pooling
```

---

## Resultado esperado

O pacote deve compilar sem alterar comportamento runtime.

Não há smoke funcional novo neste corte. A validação esperada é compile-smoke e execução dos smokes canônicos já existentes para garantir ausência de regressão.

Critério de aceite:

```text
error CS                                                        0
Exception                                                       0
FATAL                                                           0
Standard Smoke completed                                        1
Route Callback Smoke completed                                 1
Activity Baseline Smoke completed                              1
```
