# F1F — Content identity / FrameworkContentHandle review

Status: APPLIED / PENDING COMPILE-SMOKE

## Objetivo

Aplicar a política de identidade da F1 ao `FrameworkContentHandle` sem avançar fases futuras.

Este corte fecha o ponto do roadmap:

```text
IF-FW-ROAD-1F — Revisar FrameworkContentHandle
```

## Decisão aplicada

`FrameworkContentHandle` passa a expor uma identidade funcional composta:

```text
owner identity + content scope + content kind + content id
```

Essa identidade fica representada por:

```text
FrameworkContentIdentity
FrameworkContentId
```

## O que mudou

### 1. `FrameworkContentId`

Novo tipo pequeno para o id local do conteúdo.

Regras:

```text
- não aceita null;
- não aceita string vazia;
- não aceita whitespace;
- usa comparação ordinal;
- não gera fallback;
- não é label, path ou nome de GameObject.
```

### 2. `FrameworkContentIdentity`

Novo tipo composto para identificar um handle de content.

Campos conceituais:

```text
Owner      — FrameworkIdentityKey do owner.
Scope      — FrameworkContentScope.
Kind       — FrameworkContentKind.
ContentId  — FrameworkContentId.
```

Texto estável:

```text
<scope>:<kind>:<owner-domain>:<owner-value>:<content-id>
```

### 3. `FrameworkContentHandle`

`FrameworkContentHandle` agora contém:

```text
FrameworkContentIdentity Identity
FrameworkIdentityKey OwnerIdentity
```

Os campos antigos `ContentId`, `OwnerId`, `Scope` e `Kind` continuam expostos para compatibilidade de leitura e diagnósticos, mas passam a derivar da identidade composta.

### 4. Remoção de fallback Guid

Antes, `RoutePrimaryScene` podia gerar um id com `Guid.NewGuid()` quando owner/scene não estavam disponíveis.

Esse fallback foi removido.

Agora a identidade de content required precisa de dados válidos. Se não houver owner/scene suficiente, o erro deve aparecer em validação/compile-smoke em vez de criar uma chave instável silenciosa.

## O que não mudou

F1F não cria:

```text
SessionContentSet
ActivityContentSet
RuntimeContentHandle
ContentAnchorContentHandle
LocalContentIdentity
RuntimeContentIdentity
ContentAnchorIdentity
RuntimeMaterialization
Content Anchor
Additive scene execution
release policy
consumer integration
```

F1F também não migra `RouteAsset` ou `ActivityAsset` para IDs finais. Essa decisão fica para fases próprias.

## Validação esperada

Aplicar o pacote e rodar o smoke padrão:

```text
1. Unity compila sem erro CS.
2. Boot passa.
3. Route Smoke passa.
4. Activity Smoke passa.
5. Clear Activity Smoke passa.
```

Se passar:

```text
F1F — CLOSED / COMPILE-SMOKE PASS
```

Depois disso, a F1 pode receber um fechamento formal antes de abrir F2.
