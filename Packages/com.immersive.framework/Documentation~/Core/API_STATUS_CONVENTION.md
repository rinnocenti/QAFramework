# API Status Convention

Status: Applied in F1B / pending compile-smoke validation  
Fase: F1  
Tipo: API Policy / Source metadata  
Escopo: Runtime public and semi-public API areas

---

## Objetivo

Este documento registra a convenção mínima de status de API aplicada após o aceite dos ADRs de F1.

A intenção é impedir que superfícies públicas, semi-públicas ou transitórias pareçam estáveis por acidente.

---

## Marcador canônico

O corte F1B adiciona dois tipos mínimos:

```text
Runtime/ApiStatus/FrameworkApiStatus.cs
Runtime/ApiStatus/FrameworkApiStatusAttribute.cs
```

O atributo é metadata de documentação/validação. Ele não muda runtime behavior, não cria lifecycle e não substitui ADR.

---

## Categorias

| Status | Uso |
|---|---|
| `Stable` | Contrato seguro para jogos/módulos externos; mudanças exigem ADR/migração. |
| `Experimental` | Superfície de desenvolvimento; pode mudar sem compatibilidade. |
| `Internal` | Implementação do framework; não consumir em código de jogo. |
| `Deferred` | Fonte congelada ou planning-only; não é baseline ativo. |
| `DevelopmentTooling` | Ferramenta de QA/editor/dev; não é API de produto. |
| `Removed` | Superfície removida ou agendada para remoção. |

---

## Regras de uso

1. Todo novo tipo público ou semi-público deve declarar status.
2. `Experimental` não é autorização para virar contrato estável.
3. `Deferred` não deve ser conectado ao fluxo ativo sem ADR/corte da fase correspondente.
4. `DevelopmentTooling` não deve ser dependência obrigatória de produto.
5. `Internal` não deve ser usado por gameplay como API.
6. O atributo não substitui comentários XML quando o Inspector/autor precisa de explicação textual.

---

## Estado aplicado no F1B

| Área | Status aplicado |
|---|---|
| `FrameworkApiStatus` e `FrameworkApiStatusAttribute` | `Stable` |
| Authoring baseline (`GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, settings e triggers) | `Experimental` |
| `ActivityFlow`, `GameFlow`, result/event structs públicos | `Experimental` |
| `ContentFlow` | `Experimental` |
| `RouteContentProfileAsset` e route content profile planning | `Deferred` |
| `RouteContentRuntime`/local callbacks de Route | `Deferred` |
| Runtime owners internos | `Internal` |
| `FrameworkLogger` | `Internal` |
| `FrameworkQaCanvas` | `DevelopmentTooling` |

## Atualização F3D

`RouteContentRuntime` deixou de ser deferred no baseline da F3. O owner técnico é `Internal`; as superfícies autorais locais de Route Content são `Experimental` enquanto a F3 estabiliza callback smoke e validators.

---

## Fora do escopo

F1B não cria:

```text
FrameworkFact
typed identity primitives
ContentIdentity final
ValidationMode semantics
RuntimeMaterialization
Content Anchor
SessionContentSet
```

---

## Validação

Critérios do F1B:

```text
1. O package compila.
2. Nenhum comportamento de boot/route/activity muda.
3. Tipos públicos/semi-públicos principais têm FrameworkApiStatus.
4. README e Documentation~/README apontam para esta convenção.
5. F1C pode criar FrameworkFact sem depender de parsing de log como contrato novo.
```
