# Immersive Framework QA

Root local de provas técnicas sintéticas do framework. Não contém FIRSTGAME nem documentação arquitetural canônica.

## Superfícies atuais

- `Hub/`: navegação das superfícies de cena canônicas.
- `Lifecycle/`: Application e Scene Lifetime.
- `UnityBuildSurface/`: superfícies Unity de transição e UI global.
- `Camera/`: C9R Camera Override Authority e C9M Follow Pipeline.
- `Pooling/` e `Audio/`: contratos técnicos próprios.
- `Player/Editor/`: suíte canônica P3 pré-FIRSTGAME e módulos internos de casos/fixture.
- `Player/Profiles/`, `Player/P3G4/`, `Player/P3H4/` e `Player/P3J6/`: assets consolidados da fixture P3.

Player QA expõe somente `Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke`.
O fluxo e a validação manual estão documentados em
`Player/Documentation/P3-CANONICAL-PREFIRSTGAME-QA.md`.

## PROD-ID-1 — identidade estável

Execute `Immersive Framework > QA > Regressions > Contracts > Run PROD-ID-1 Identity Regression`.
O smoke cobre identidade de Route independente de nome/cena, admission token tipado,
fechamento de identidade de Activity em Object Entry/Local Contribution/Scene Ledger e
validação explícita de IDs missing, invalid e duplicate.
