# F3E — RouteContentSet semantics

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F3  
Roadmap: IF-FW-ROAD-3D — RouteContentSet semantics

---

## Objetivo

F3E remove a ambiguidade semântica de `RouteContentSet`.

Antes do corte, o set registrava handles de conteúdo da Route, mas ownership era inferido pelo simples fato de o handle estar no set. A partir deste corte, ownership é explícito por item.

## Decisão aplicada

`RouteContentSet` continua sendo snapshot imutável da Route ativa.

Ele não é:

```text
host
manager
service locator
loader
release executor
registry global
```

Cada item conhecido pela Route passa a ter uma entrada explícita:

```text
RouteContentEntry
```

com ownership declarado por:

```text
RouteContentOwnership.Registered
RouteContentOwnership.Owned
RouteContentOwnership.DiagnosticOnly
```

## Semântica

| Ownership | Significado |
|---|---|
| `Registered` | A Route conhece o item, mas não reivindica ownership/release. |
| `Owned` | A Route declara ownership sem executar release ainda. |
| `DiagnosticOnly` | O item existe apenas para diagnóstico/status. |

## Baseline F3

A Primary Scene da Route é registrada como:

```text
scope='Route'
kind='Scene'
requiredness='Required'
ownership='Owned'
```

Isso não muda quem carrega a cena. O load continua pertencendo ao `SceneLifecycleRuntime`.

Também não muda release. Release avançado fica para F6.

## Diagnóstico esperado

O log de `RouteContentSet` deve mostrar contadores de ownership:

```text
registered='0' owned='1' diagnosticOnly='0'
```

para o baseline atual com uma Primary Scene.

## O que não entra

F3E não implementa:

```text
release policy
additive scene loading
RouteContentProfile execution
Content Anchor
RuntimeMaterialization
LocalContributionSet
consumers
```

## Validação

Smoke padrão:

```text
Boot
Route Smoke
Activity Smoke
Clear Activity Smoke
```

Critério específico:

```text
owned='1'
registered='0'
diagnosticOnly='0'
```

em diagnostics de `RouteContentSet` para a Primary Scene.
