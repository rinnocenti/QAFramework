# F13-03 — ADR-ACTOR-001 — Actor Runtime Boundary

Status: Draft / Renumbered
Fase: F13
Ordem no Plano: F13-03
Tipo: Consumer / Actor
Escopo: Actor runtime

---

## Contexto

Actor no `NewScripts` é rico, mas acoplado a Activity entry, input, camera, projectile e attributes. No framework deve entrar como runtime content/contribution/participant.

---

## Decisão

Actor deve usar:

- ActorMaterializationRequest/Result;
- RuntimeContentHandle;
- Participation boundary;
- RuntimeCapabilityReference;
- explicit owner scope.

Actor não deve possuir Session/Route/Activity lifecycle.
