# ADR-018-C4 — Progression Save Product Composition QA

Package prerequisite:

```text
ADR018-C1-C3-Package-Product-Composition.zip
```

## Purpose

Prove the canonical public application composition path for Progression Save.

The QA does not test Inspector layout. It proves the technical contract behind the
authoring surface:

```text
GameApplicationAsset
  -> ProgressionSaveProfile
  -> ProgressionSaveApplicationComposition
  -> selected IProgressionSaveStore
  -> ProgressionSaveRuntime
```

## Run

Outside Play Mode:

```text
Immersive Framework
  QA
    Regressions
      Progression Save
        Run ADR-018 Product Composition
```

## Expected terminal

```text
[ADR018_QA_PRODUCT_COMPOSITION]
status='Passed'
cases='12'
disabled='Passed'
builtIn='Passed'
custom='Passed'
negative='7/7'
noFallback='Passed'
selectionIsolation='Passed'
runtimeRequest='Passed'
composition='ProgressionSaveApplicationComposition'
```

## Cases

### Positive

```text
Disabled application -> Disabled, no Runtime
Built-in JSON -> Ready, JsonProgressionSaveStore
Custom Provider -> Ready, QA in-memory store
Built-in selection ignores stale/unselected custom provider
composed Runtime executes Save/Load through selected custom backend
```

### Negative

```text
enabled + missing Profile -> Rejected
Custom Provider + missing provider -> Rejected
invalid provider configuration -> Rejected before create
provider create returns false -> Rejected
provider returns success + null store -> Rejected
provider returns invalid BackendId -> Rejected
provider throws -> Rejected
```

## No-fallback proof

The failure cases require:

```text
result.HasRuntime == false
```

and selected-provider creation evidence.

Provider failure diagnostics must include the explicit no-fallback semantics where
materialization was attempted.

The QA never accepts a `JsonProgressionSaveStore` as recovery from a failing Custom
Provider selection.

## Acceptance

C4 passes when the single terminal marker reports:

```text
status='Passed'
cases='12'
negative='7/7'
noFallback='Passed'
```

After C4, the package may receive the C5 certification cut. FIRSTGAME then owns the
game-facing usability proof.
