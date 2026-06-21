# ADR-BL-001 — Baseline Reconciliation

Status: Proposed  
Fase: F0A  
Tipo: Baseline / Reconciliação  
Escopo: Package atual

---

## Contexto

O package atual possui baseline funcional, mas também contradições ativas: `CameraFlow` existe no runtime apesar de estar congelado/deferido em docs, `RouteContentRuntime` existe mas não está claramente conectado ao fluxo real, `ContentFlow` expõe APIs públicas ainda pouco consumidas, `RouteContentProfileAsset` parece executável mas funciona como planejamento, `FrameworkQaCanvas` está em runtime público, e `ValidationMode` ainda tem semântica fraca.

## Decisão

Antes de qualquer feature nova, o baseline deve classificar cada superfície como `Stable`, `Experimental`, `Internal`, `Deferred` ou `Removed`.

Decisão proposta inicial:

| Item | Status recomendado | Ação |
|---|---|---|
| `CameraFlow` | `Deferred` | Congelar/remover do baseline ativo até existir `Surface` + `Runtime` + consumer model. |
| `RouteContentRuntime` | `Experimental` ou `Deferred` | Não deixar ambíguo: conectar em fase Route ou remover/congelar claramente. |
| `ContentFlow` materializer/contribution | `Experimental` | Manter vocabulário, mas impedir que pareça contrato estável antes de owners/release. |
| `RouteContentProfileAsset` | `Deferred / Planning-only` | Inspector/docs devem dizer explicitamente que não executa cenas adicionais ainda. |
| `FrameworkQaCanvas` | `Development Tooling` | Proteger por política de build/dev ou mover futuramente. |
| `ValidationMode` | `Experimental` até semântica real | Definir comportamento mínimo em ADR próprio se permanecer público. |

## Consequências

### Positivas

- Impede crescimento sobre contradição.
- Evita que consumers avançados capturem o core.
- Cria uma base documental confiável antes de novas fases.
- Permite smoke e validação coerentes.

### Negativas / trade-offs

- Atrasa features visíveis.
- Pode exigir remoção/congelamento de código já escrito.
- Exige atualização de README/ADRs/validators antes de seguir.

## Fora do escopo

- Implementar `SessionContentSet`.
- Conectar additive scenes.
- Criar Surface, RuntimeSpawned, Camera, Actor, Input, Save ou Pooling.

## Critérios de validação

- README, ADRs e código concordam sobre o status de cada superfície.
- Smoke baseline: boot → route switch → activity switch → activity clear.
- Nenhuma API pública fica sem status declarado.

## Impacto esperado

Este ADR destrava F0B. Sem ele, qualquer corte técnico corre risco de consolidar o baseline errado.

## Relação com roadmap

Fase 0A. Pré-requisito de todas as demais fases.

## Notas de implementação

A implementação da decisão deve ocorrer em F0B, não neste ADR.
