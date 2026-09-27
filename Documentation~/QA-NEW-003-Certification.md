# QA-NEW-003 — Route Primary Scene Replacement A → B → A

**Estado:** CERTIFIED  
**Resultado:** PASS ×2

## Evidência certificada

- A substituição física da Primary Scene foi comprovada no ciclo `A → B → nova A`.
- O execution owner persistente sobreviveu ao ciclo completo.
- As cardinalidades observadas foram unitárias e os requests de Route terminaram com `Succeeded`.
- A baseline da Route A foi restaurada nas duas execuções.
- Não houve `firstDivergence` nem `cleanupIssue` em nenhuma das duas execuções.

## Boundary arquitetural

- A certificação não exigiu nova infraestrutura nem Runner.
- Não existe e não foi assumida uma identidade pública de `Route occurrence`.
