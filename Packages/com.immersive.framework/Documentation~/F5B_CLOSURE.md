# F5B — LocalContentIdentity Closure

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F5 — Local Contribution baseline  
Último corte aplicado: F5B  

---

## Resumo

O corte F5B cria a identidade local tipada mínima da fase F5.

Ele adiciona `LocalContentIdentity`, `LocalContentId` e `LocalContentScopeKind` em `Runtime/LocalContribution`, mantendo a decisão aceita no F5A:

```text
LocalContentIdentity é explícita.
Nome/path/hierarchy/scene path não são chave funcional.
targetId universal não é recriado.
ActivityContentSet F4 não vira identidade funcional F5.
```

---

## Arquivos criados

```text
Runtime/LocalContribution.meta
Runtime/LocalContribution/LocalContentId.cs
Runtime/LocalContribution/LocalContentId.cs.meta
Runtime/LocalContribution/LocalContentIdentity.cs
Runtime/LocalContribution/LocalContentIdentity.cs.meta
Runtime/LocalContribution/LocalContentScopeKind.cs
Runtime/LocalContribution/LocalContentScopeKind.cs.meta
Documentation~/LOCAL_CONTENT_IDENTITY.md
Documentation~/F5B_CLOSURE.md
```

---

## Fora do escopo confirmado

Não houve criação ou alteração de:

```text
LocalContributionMarker
FrameworkContentContributionMarker
IFrameworkContentContribution
LocalContributionDiscovery
LocalContributionSet
LocalContributionHandle
ActivityContentRuntime
ActivityContentSet
RouteContentSet
Authoring validators
QA Canvas
Smokes
```

---

## Validação esperada

Como o corte altera runtime code, o status permanece pendente até compile-smoke no Unity.

Critérios mínimos:

```text
error CS                                                        0
Exception                                                       0
FATAL                                                           0
Boot succeeded                                                  1
QA Smoke completed. name='Standard Smoke'                       1
QA Smoke completed. name='Route Callback Smoke'                 1
QA Smoke completed. name='Activity Baseline Smoke'              1
```

---

## Próximo corte autorizado se F5B passar

```text
F5C — LocalContributionMarker sem fallback funcional
```

F5C deve criar ou refatorar o marker local, exigindo id explícito e mantendo falha/diagnóstico estruturado para id ausente. F5C ainda não deve criar discovery amplo nem `LocalContributionSet`.
