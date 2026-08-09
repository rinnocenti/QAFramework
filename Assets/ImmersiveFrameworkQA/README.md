# Immersive Framework QA

## Player QA canonical architecture

Player QA is organized by current product contracts, not by P3/M07 history:

```text
Player/Session
Player/Scene Provided
Player/Manager Provisioned
Player/Actor
Player/Public Surface
Game Flow/Participation
```

The canonical operational entry point is:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Normal use is one button: the full orchestrator prepares each owned fixture,
opens isolated Play Mode sessions, runs the canonical Player proofs, restores
the next fixture boundary, and emits one `[QA_PLAYER_FULL]` summary.

The individual menus remain available for advanced diagnosis:

```text
Immersive Framework/QA/Player/Session/Run Session Contract
Immersive Framework/QA/Player/Scene Provided/Prepare Fixture
Immersive Framework/QA/Player/Scene Provided/Run Integration
Immersive Framework/QA/Player/Manager Provisioned/Prepare Fixture
Immersive Framework/QA/Player/Actor/Run Lifecycle Integration
Immersive Framework/QA/Player/Public Surface/Prepare Certification Fixture
Immersive Framework/QA/Player/Public Surface/Run Certification
```

`QaPlayerSessionQaSupport` is the single Player Session support layer in the
existing Player Internal Editor assembly. It owns profile/bridge validation and
does not introduce a second Session limit. See
`Documentation/PLAYER-QA-ARCHITECTURE.md` for fixture ownership and the
reclassification of historical P3/M07 sources.

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

The Full Player QA is only an orchestrator of typed entrypoints; it does not
replace or reimplement the evidence in the individual regressions. Each
regression remains responsible for its own proof, fixture and cleanup.

## Player QA

> Historical inventory below. Use the canonical menu tree above and
> `Documentation/PLAYER-QA-ARCHITECTURE.md` for current operation.

Superfícies públicas atuais:

```text
Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression
Immersive Framework/QA/Regressions/Player/Run Session Player Slots Regression
Immersive Framework/QA/Regressions/Player/Run Local Player Provisioning Regression
Immersive Framework/QA/Regressions/Player/Run Manager-Provisioned Lifecycle Public Contract Regression
Immersive Framework/QA/Regressions/Player/Run Manager-Provisioned Lifecycle Waiting Projection Regression
Immersive Framework/QA/Regressions/Player/Run Player Actor Selection Runtime Binding Regression
Immersive Framework/QA/Regressions/Player/Run Player Gameplay Admission Regression
Immersive Framework/QA/Regressions/Player/Run Scene Player Route Lifecycle Regression
```

O antigo `P3 Run Canonical Pre-FIRSTGAME Smoke` foi removido durante a consolidação. Seu inventário histórico permanece em `Player/Documentation/P3-CANONICAL-PREFIRSTGAME-QA.md`; ele não é uma instrução operacional atual.

Regressões Edit Mode:

- Player Participation Authoring;
- Session Player Slots;
- Local Player Provisioning;
- Manager-Provisioned Lifecycle Public Contract.

Regressões Play Mode:

- Manager-Provisioned Lifecycle Waiting Projection;
- Player Actor Selection Runtime Binding;
- Player Gameplay Admission;
- Scene Player Route Lifecycle.

A regressão `Manager-Provisioned Lifecycle Public Contract` usa somente APIs públicas. Ela prova normalização, imutabilidade, escopo da evidência de gate, estados terminais e indisponibilidade explícita do Authoring. Ela não substitui uma regressão Play Mode de Activity/Session real.

A regressão `Manager-Provisioned Lifecycle Waiting Projection` reutiliza a fixture M07 real em Play Mode e observa somente o snapshot público do Authoring. Ela prova `WaitingForJoin`, contribuição do Player em `Preparing`, saída `Released` e preservação da Session sem Players. `Ready` e `Failed` permanecem em regressão posterior.

As regressões Play Mode exigem o contexto indicado por cada fixture. Não considere uma regressão aprovada por evidência emitida por outra regressão.

## Identity Authority (IF-ID)

### Superfície canônica única

```text
Immersive Framework QA/Game Flow/Run Identity Authority Regression
```

Implementação: `GameFlow/InternalEditor/QaRouteActivityIdentityRegression.cs` +
`GameFlow/InternalEditor/QaIdentityAuthorityFixture.cs`.

Não existem outros menus públicos de Identity Authority. Smokes antigos de Activity ID
e validação autoral de Route/Activity IDs foram removidos na consolidação Corte 5.

### Pré-condições

- Unity `6000.5.0f1`
- **Play Mode** com Game Flow já iniciado
- exatamente um `FrameworkRuntimeHost` carregado e pronto
- cena QA válida (ex.: hub ou lifecycle boot) com Route e Activity atuais

### Seis casos (ordem fixa)

1. `baseline-authority-snapshot` — owners/tokens/roots da autoridade atual
2. `route-collision-transition` — Route A→B com mesmo stable ID, refs distintas
3. `activity-collision-transition` — Activity A→B com mesmo stable ID, refs distintas
4. `ownership-release-isolation` — release de Root A não remove Root B
5. `readiness-collision-isolation` — wait de A não pertence a B
6. `legitimate-supersession-preservation` — supersession tipada + colisão não finge autoridade

### Package NUnit vs QA smoke

| Onde | O que prova |
|------|-------------|
| **Package NUnit** | igualdade por referência e stable ID, hash, token obrigatório, validação autoral, regeneração/Undo, supersession determinística de wait/status |
| **QA IF-ID runner** | lifecycle/runtime real em Play Mode: colisão, ownership release no registry do host, readiness isolation, supersession legítima com ocorrência |

Não reexecute testes determinísticos do package como MenuItem de QA.

### Como executar

1. Abra o projeto no Unity `6000.5.0f1`
2. Entre em Play Mode na cena QA com host único
3. Menu: `Immersive Framework QA > Game Flow > Run Identity Authority Regression`
4. Um único resumo com prefixo `[IF_ID_QA]` (status, casos, refs, tokens, owners, roots, waits, cleanup)

### Smokes de domínio relacionados (não IF-ID)

Preservados em seus domínios; **não** são a superfície IF-ID:

- `Descriptors/Editor/QaB1DescriptorEqualitySmoke` — igualdade de Actor/PlayerActor descriptors
- `Player/.../QaP3M5BRouteTransitionAndNegativeMatrixSmoke` — Player/admission/route matrix
- demais regressões Player/Camera/Game Flow que apenas observam owners

### Removidos (Corte 5)

| Smoke | Menu removido | Motivo |
|-------|---------------|--------|
| `QaA1ActivityIdSmoke` | `.../Contracts/Run Activity Identity Regression` | Cobertura determinística de ID/owner/token no package + baseline IF-ID |
| `QaRouteActivityIdentityValidationRegression` | `.../Authoring/Run Route and Activity Identity Validation Regression` | Validação autoral missing/invalid/duplicate no package NUnit |

## Consolidação

O inventário histórico e as decisões anteriores estão em
`Documentation/QA-SMOKE-CONSOLIDATION-AUDIT.md`.
A consolidação IF-ID (Corte 5) está resumida na seção **Identity Authority (IF-ID)** acima.
