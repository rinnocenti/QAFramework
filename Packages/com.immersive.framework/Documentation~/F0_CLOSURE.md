# F0 Closure — Baseline ADRs e higiene

Status: Closed / PASS  
Fase: F0  
Escopo: reconciliação de baseline do `com.immersive.framework`  
Fonte de evidência: smoke F0B enviado após `IF-FW-F0B-baseline-hygiene`.

---

## 1. Decisão de fechamento

F0 está fechado.

| Item | Status | Evidência |
|---|---|---|
| `F0A` | `CLOSED / ADRS ACCEPTED` | ADRs de baseline aceitos. |
| `F0B` | `CLOSED / HYGIENE APPLIED / SMOKE PASS` | Higiene mínima aplicada e smoke F0B aprovado. |
| `F0C` | `CLOSED / FORMAL CLOSURE` | Este documento registra o fechamento. |
| `F0` | `CLOSED / PASS` | Nenhum bloqueador F0 permanece aberto. |

Nota: os arquivos ADR permanecem com `Status: Accepted`, porque `Accepted` é status de decisão arquitetural. `Closed` é status de corte/fase.

Este fechamento não autoriza implementação direta de F1. Ele autoriza abrir a etapa de revisão/aceite dos ADRs da F1.

---

## 2. Resultado do smoke

Resultado observado:

| Checagem | Resultado |
|---|---:|
| Boot | PASS |
| Route Smoke | PASS |
| Activity Smoke | PASS |
| Clear Activity Smoke | PASS |
| Fatal log | 0 |
| Exception | 0 |
| Compile error no smoke enviado | 0 |

Sequência bem-sucedida observada:

```text
Boot succeeded.
Route Smoke completed.
Activity Smoke completed.
Clear Activity Smoke completed.
```

---

## 3. Matriz de fechamento dos ADRs

| ADR | Área de decisão | Resultado F0 |
|---|---|---|
| ADR-BL-001 — Baseline Reconciliation | CameraFlow, RouteContentRuntime, ContentFlow, RouteContentProfileAsset, FrameworkQaCanvas, ValidationMode | PASS. Todas as superfícies listadas têm status F0 declarado. |
| ADR-BL-002 — Core vs Consumers | Consumers concretos não podem possuir o lifecycle core | PASS. CameraFlow saiu do core; nenhum Input, Save, Camera, Audio, Actor, Pooling ou Projectile foi introduzido como dependência core. |
| ADR-BL-003 — Public API Status Policy | Superfícies públicas/semi-públicas precisam de status | PASS. Superfícies F0 estão marcadas como Experimental, Deferred, Development Tooling ou Removed from core. |
| ADR-BL-004 — QA and Diagnostics Boundary | QA UI é tooling, não API de produto | PASS. FrameworkQaCanvas ficou development tooling. |
| ADR-BL-005 — Dependency Policy | Dependências core devem ser mínimas | PASS. Cinemachine não é dependência obrigatória do core. |

---

## 4. Status aplicado do baseline

| Superfície | Status F0 | Nota de fechamento |
|---|---|---|
| Bootstrap / Game Application / Route / Activity / request triggers | Experimental | Usáveis para smoke/desenvolvimento; identidade e diagnostics ainda precisam de F1/F3/F4. |
| ContentFlow vocabulary | Experimental | Mantido como vocabulário; não é contrato estável de materialização/contribution. |
| RouteContentRuntime family | Deferred até F3 | Fonte permanece congelada e não conectada ao fluxo ativo de Route. Entradas de menu autoral estão escondidas. |
| RouteContentProfileAsset | Deferred / Planning-only | Cenas adicionais são declaração para planejamento/diagnóstico; F0 não carrega conteúdo additive de Route. |
| CameraFlow | Removed from core baseline | Camera retorna depois como consumer, após pré-requisitos de Surface/Runtime. |
| FrameworkQaCanvas | Development Tooling | Ferramenta de smoke; não é API de produto. |
| ValidationMode | Experimental | Configuração obrigatória continua falhando; semântica concreta dos modos vai para F1. |

---

## 5. Trabalho deferred explicitamente não fechado pelo F0

Estes itens permanecem fora do F0 e não devem ser tratados como implementados:

```text
SessionContentSet
FrameworkFact
Typed identity policy
ValidationMode concrete semantics
RouteContentRuntime execution
Route additive scene execution
ActivityContentSet/readiness model
LocalContribution
Surface
RuntimeSpawned/materialization/release
Input
Save/Snapshot
Pause
Camera
Audio
Actor
Pooling
Projectile/Damage/Attributes
```

---

## 6. Próximo passo autorizado

O próximo passo autorizado não é um corte técnico de implementação.

```text
Next: F1A — revisão e aceite dos ADRs de API status, Identity and Diagnostics.
```

A implementação técnica de F1 só começa depois que os ADRs relevantes da F1 forem aceitos.
