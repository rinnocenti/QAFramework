# Immersive Framework QA

Root local de provas técnicas sintéticas do framework. Não contém FIRSTGAME nem documentação arquitetural canônica.

## Superfícies atuais

- `Hub/`: navegação para regressões que exigem cenas persistidas.
- `Lifecycle/`: Application e Scene Lifetime.
- `UnityBuildSurface/`: superfícies Unity de transição e UI global.
- `Camera/`: regressões de authoring, integração com Runtime Host e autoridade de câmera.
- `Pooling/` e `Audio/`: contratos técnicos próprios.
- `Player/Editor/`: regressões focadas de authoring, Slots, provisioning, gameplay admission e lifecycle.
- `Player/Profiles/`, `Player/P3G4/`, `Player/P3H4/`, `Player/P3J6/` e `Player/P3M5B/`: assets das fixtures Player preservadas.

As regressões públicas ficam sob:

```text
Immersive Framework/QA/Regressions/<Domain>/Run <Regression Name>
```

Setups e repairs ficam separados sob:

```text
Immersive Framework/QA/Setup/<Domain>/...
```

Não existe suite global nem mega-runner Player. Cada regressão deve ser executada e validada pela própria evidência.

## Player QA

Superfícies públicas atuais:

```text
Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression
Immersive Framework/QA/Regressions/Player/Run Session Player Slots Regression
Immersive Framework/QA/Regressions/Player/Run Local Player Provisioning Regression
Immersive Framework/QA/Regressions/Player/Run Player Actor Selection Runtime Binding Regression
Immersive Framework/QA/Regressions/Player/Run Player Gameplay Admission Regression
Immersive Framework/QA/Regressions/Player/Run Scene Player Route Lifecycle Regression
```

O antigo `P3 Run Canonical Pre-FIRSTGAME Smoke` foi removido durante a consolidação. Seu inventário histórico permanece em `Player/Documentation/P3-CANONICAL-PREFIRSTGAME-QA.md`; ele não é uma instrução operacional atual.

Regressões Edit Mode:

- Player Participation Authoring;
- Session Player Slots;
- Local Player Provisioning.

Regressões Play Mode:

- Player Actor Selection Runtime Binding;
- Player Gameplay Admission;
- Scene Player Route Lifecycle.

As regressões Play Mode exigem o contexto indicado por cada fixture. Não considere uma regressão aprovada por evidência emitida por outra regressão.

## Route and Activity identity

Execute `Immersive Framework > QA > Regressions > Authoring > Run Route and Activity Identity Validation Regression` em Edit Mode e depois `Immersive Framework > QA > Regressions > Game Flow > Run Route and Activity Identity Regression` em um Play Mode novo.

As regressões cobrem identidade independente de nome/cena, runtime state, admission token, Object Entry, Activity Scene Ledger e validação isolada de IDs missing, invalid e duplicate.

## Consolidação

O inventário histórico, as decisões de consolidação e a superfície pública resultante estão documentados em `Documentation/QA-SMOKE-CONSOLIDATION-AUDIT.md`.
