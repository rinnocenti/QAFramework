# ADR-POOL-001 — Pooling Package Boundary

Status: Draft / Deferred  
Fase: F11  
Tipo: Consumer / Pooling  
Escopo: Pooling

---

## Contexto

Pooling é infraestrutura técnica. No `NewScripts`, pooling aparece acoplado a projectile e roots globais. O framework novo já tem package `com.immersive.pooling` separado.

## Decisão

Pooling deve ser package técnico consumido por `PooledContentMaterializer`.

Regras:

- Pooling não é projectile-first.
- Rent/return deve produzir/consumir `RuntimeContentHandle`.
- Pool scope deve alinhar com Session/Route/Activity/Transient.
- Pool service não deve virar global service locator.

## Consequências

### Positivas

- Reutiliza package técnico.
- Prepara projectile sem acoplar core.
- Evita destroy direto em pooled content.

### Negativas / trade-offs

- Depende de RuntimeContentHandle.
- Pode exigir adapter entre framework e pooling.

## Fora do escopo

- Projectile.
- Damage/impact.
- Audio pooling.

## Critérios de validação

- Pooled materializer rent/return funciona.
- Release por scope retorna conteúdo ao pool.
- Pooling não depende de actor/projectile.

## Impacto esperado

Destrava gameplay runtime e projectile.

## Relação com roadmap

F11/F12.

## Notas de implementação

Não adicionar `com.immersive.pooling` ao core antes deste ADR ser aceito.
