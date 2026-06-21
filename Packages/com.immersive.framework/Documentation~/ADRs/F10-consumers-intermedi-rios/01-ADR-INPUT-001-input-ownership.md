# ADR-INPUT-001 — Input Ownership

Status: Draft / Deferred  
Fase: F10  
Tipo: Consumer / Input  
Escopo: Input

---

## Contexto

Input é consumer de Route/Activity/Participation. Não deve ditar lifecycle core nem usar string solta como action map identity.

## Decisão

Input mode deve ser declarado por contrato e aplicado por consumer.

- Owner atual do input mode é explícito.
- Activity/Route declaram requirement.
- Consumer aplica e libera.
- Action map string pode ser implementation detail, não chave funcional pública.

## Consequências

### Positivas

- Evita global input service como locator.
- Permite pause e activity mode sem acoplamento.
- Prepara actor command depois.

### Negativas / trade-offs

- Precisa adapter Unity Input System depois.
- Requer mapping authoring cuidadoso.

## Fora do escopo

- PlayerInput concreto.
- Actor commands.
- Multiplayer participation.

## Critérios de validação

- Activity enter muda input mode.
- Activity exit restaura/libera.
- Sem global discovery de PlayerInput no core.

## Impacto esperado

Primeiro consumer intermediário.

## Relação com roadmap

F10.

## Notas de implementação

Pode começar com contrato sem Unity Input System adapter.
