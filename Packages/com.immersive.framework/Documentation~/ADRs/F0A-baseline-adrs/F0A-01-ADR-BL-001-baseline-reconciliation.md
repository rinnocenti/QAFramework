# F0A-01 — ADR-BL-001 — Baseline Reconciliation

Status: Accepted  
Fase: F0A  
Ordem no Plano: F0A-01  
Tipo: Baseline / Reconciliação  
Escopo: Package atual

---

## Contexto

O package atual possui baseline funcional, mas ainda mistura superfícies em maturidades diferentes. Antes de avançar para F1 ou qualquer feature nova, o baseline precisa declarar o status oficial de `CameraFlow`, `RouteContentRuntime`, `ContentFlow`, `RouteContentProfileAsset`, `FrameworkQaCanvas` e `ValidationMode`.

A fase F0A não implementa comportamento novo. Ela fecha a decisão arquitetural que destrava a higiene mínima de F0B.

## Decisão

O baseline ativo do `com.immersive.framework` fica reconciliado com a seguinte classificação:

| Item | Status F0A | Decisão aceita | Ação posterior |
|---|---|---|---|
| `CameraFlow` | `Deferred` | Camera é consumer avançado, não core inicial. Não deve permanecer como dependência obrigatória do runtime core. | Em F0B, remover/congelar do baseline ativo e eliminar a dependência core de Cinemachine, salvo se for isolada em package/assembly opcional posterior. |
| `RouteContentRuntime` | `Deferred` | A existência do tipo não autoriza fluxo ativo. Route content local só será decidido/conectado na fase F3. | Em F0B, remover alegações de execução ativa dos docs/guia. Em F3, conectar ou remover definitivamente. |
| `ContentFlow` materializer/contribution | `Experimental` | Vocabulário preservado, mas ainda não é API estável de materialização. | Em F1/F2/F3/F4, estabilizar identity, owners e content sets antes de ampliar materialização. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Perfil de Route é dado de planejamento. Não executa additive scenes no baseline atual. | Em F0B, Inspector/docs devem deixar isso explícito. Execução só em F6. |
| `FrameworkQaCanvas` | `Development Tooling` | Ferramenta manual de smoke, não API de produto. | Em F0B, proteger por política de build/dev ou marcar claramente como tooling. |
| `ValidationMode` | `Experimental` | Enum público ainda não tem semântica operacional suficiente. | Em F1, definir comportamento mínimo de `Strict`, `Standard` e `Release`. |

## Consequências

### Positivas

- Impede crescimento sobre uma base ambígua.
- Evita que camera, QA, materialization e route content capturem o core antes dos owners corretos.
- Torna F0B objetivo: aplicar higiene sem inventar feature.
- Define quais contradições são dívida conhecida, não comportamento canônico.

### Negativas / trade-offs

- Alguns códigos já existentes deixam de ser considerados baseline ativo.
- Pode exigir remoção/congelamento de classes e dependências no corte seguinte.
- A documentação de uso precisa ser podada para não prometer callbacks ou fluxos ainda não conectados.

## Fora do escopo

- Implementar `SessionContentSet`.
- Conectar `RouteContentRuntime`.
- Executar additive scenes.
- Criar Surface, RuntimeSpawned, Camera, Actor, Input, Save, Pooling, Projectile, Damage ou Attributes.
- Reorganizar packages opcionais.

## Critérios de validação

- Os cinco ADRs de F0A estão `Accepted`.
- README principal e `Documentation~/README.md` apontam F0B como próximo corte.
- As superfícies listadas neste ADR têm status explícito.
- Nenhuma feature nova foi adicionada por F0A.

## Impacto esperado

Este ADR destrava F0B. Sem ele, qualquer corte técnico corre risco de consolidar um baseline errado.

## Relação com roadmap

Fase 0A. Pré-requisito para F0B e para todas as fases posteriores.

## Notas de implementação

A implementação das ações aceitas deve ocorrer em F0B. F0A é decisão e documentação.
