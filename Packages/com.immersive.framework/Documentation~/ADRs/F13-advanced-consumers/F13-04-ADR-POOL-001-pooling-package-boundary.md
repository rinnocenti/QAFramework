# F13-04 — ADR-POOL-001 — Pooling Package Boundary

Status: Draft / Renumbered
Fase: F13
Ordem no Plano: F13-04
Tipo: Consumer / Pooling
Escopo: Pooling

---

## Decisão

`com.immersive.pooling` permanece infra técnica separada. Pooled materializer usa RuntimeContentHandle and RuntimeReleasePolicy; Pool não é dono de lifecycle de gameplay.
