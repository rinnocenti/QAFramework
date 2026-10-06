# QA-NEW-003 — Route Primary Scene Replacement A → B → A

**Estado:** CERTIFIED  
**Resultado atual:** PASS — Play Mode 2026-10-06  
**Evidência histórica:** PASS ×2

A cobertura RESET-035-B adicionada ao cenário foi executada no estado atual. O ciclo Route A → B → nova A verificou owner registration/release dos Resettables das Routes A/B, preservação do execution owner e restauração da baseline.

## Evidência certificada

- A substituição física da Primary Scene foi comprovada no ciclo `A → B → nova A`.
- O execution owner persistente sobreviveu ao ciclo completo.
- As cardinalidades observadas foram unitárias e os requests de Route terminaram com `Succeeded`.
- A baseline da Route A foi restaurada nas duas execuções.
- Não houve `firstDivergence` nem `cleanupIssue` em nenhuma das duas execuções.

## Execução atual — 2026-10-06

```text
[QA-NEW-003] status='Passed' verdict='PASS' submittedB='1' completedB='1' succeededB='1' exitA='1' releasingA='1' unloadA='1' loadB='1' availableB='1' enterB='1' submittedA='1' completedA='1' succeededA='1' exitB='1' releasingB='1' unloadB='1' loadA='1' availableA='1' enterA='1' ownerSurvived='True' baselineRestored='True' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''.
```

A execução atual fecha a extensão RESET-035-B desse cenário; os dois resultados históricos anteriores permanecem evidência datada do boundary anterior.

## Boundary arquitetural

- A certificação não exigiu nova infraestrutura nem Runner.
- Não existe e não foi assumida uma identidade pública de `Route occurrence`.
