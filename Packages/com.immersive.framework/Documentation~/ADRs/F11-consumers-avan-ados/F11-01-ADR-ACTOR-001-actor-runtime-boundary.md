# F11-01 — ADR-ACTOR-001 — Actor Runtime Boundary

Status: Draft / Deferred  
Fase: F11  
Ordem no Plano: F11-01  
Tipo: Consumer / Actor  
Escopo: Actor runtime

---

## Contexto

Actor no `NewScripts` é rico, mas acoplado a Activity entry, input, camera, projectile e attributes. No framework deve entrar como runtime content/contribution.

## Decisão

Actor é consumer de runtime materialization e Local/Activity contributions.

Actor deve usar:

- `ActorMaterializationRequest/Result`;
- `RuntimeContentHandle`;
- contribution/capability descriptors;
- explicit owner scope.

Actor não deve possuir Session/Route/Activity lifecycle.

## Consequências

### Positivas

- Permite player/npc depois sem pipeline monolítica.
- Prepara projectile/damage/attributes.
- Mantém lifecycle core independente.

### Negativas / trade-offs

- Actor chega mais tarde.
- Requer Runtime and Contribution maduros.

## Fora do escopo

- Player participation completo.
- Actor command hub.
- Attributes/damage/projectile.

## Critérios de validação

- Actor instancia via materializer.
- Actor contribui capabilities sem alterar ActivityFlow core.
- Release segue owner scope.

## Impacto esperado

Base de gameplay avançado.

## Relação com roadmap

F11/F12.

## Notas de implementação

Não copiar PlayerActorMaterializationAdapter do NewScripts.
