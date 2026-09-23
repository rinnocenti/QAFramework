# ADR-017 — Project Frame Rate QA

Status: focused Stage A QA harness  
Package prerequisite: ADR017 A1–A4 applied

## Purpose

Prove that the canonical Frame Rate authority is now:

```text
Project Settings
  -> Framework boot validation
  -> explicit runtime-host baseline
  -> Unity frame pacing
```

and that `GameApplicationAsset` no longer owns Frame Rate.

## Coverage

```text
ADR017-QA-01 TargetFrameRate project baseline
ADR017-QA-02 VerticalSync project baseline
ADR017-QA-03 UseUnityDefaults project baseline
ADR017-QA-04 invalid project policy / no partial mutation
ADR017-QA-05 no dual GameApplication authority
```

## Execution

### 1. Edit validation

Outside Play Mode:

```text
Immersive Framework
  QA
    Regressions
      Application
        Run Project Frame Rate Edit Validation
```

Expected:

```text
[ADR017_QA_EDIT]
status='Passed'
cases='13'
```

### 2. Target Frame Rate E2E

Outside Play Mode:

```text
Immersive Framework
  QA
    Setup
      Application Frame Rate
        Prepare Target Frame Rate
```

Enter Play Mode and run:

```text
Immersive Framework
  QA
    Regressions
      Application
        Run Project Frame Rate Regression
```

Expected:

```text
[ADR017_QA_TARGET]
status='Passed'
cases='13'
source='ProjectSettings'
```

Exit Play Mode. The setup restores the original project policy automatically.

### 3. Vertical Sync E2E

Prepare:

```text
Prepare Vertical Sync
```

Enter a fresh Play Mode and run the same regression.

Expected:

```text
[ADR017_QA_VSYNC]
status='Passed'
cases='13'
source='ProjectSettings'
```

Exit Play Mode.

### 4. Use Unity Defaults E2E

Prepare:

```text
Prepare Use Unity Defaults
```

Enter a fresh Play Mode and run the same regression.

Expected:

```text
[ADR017_QA_DEFAULTS]
status='Passed'
cases='13'
source='ProjectSettings'
runtimeStatus='SkippedUnityDefaults'
```

Exit Play Mode.

## Preboot sentinel

The runtime QA fixture is armed for one Play Mode only.

Before the framework `AfterSceneLoad` bootstrap it applies:

```text
Application.targetFrameRate = 47
QualitySettings.vSyncCount = 2
```

The arm is consumed immediately.

This proves:

```text
TargetFrameRate
  47 / 2 -> configured target / 0

VerticalSync
  47 / 2 -> -1 / configured VSync

UseUnityDefaults
  47 / 2 -> 47 / 2
```

The `FrameworkRuntimeHost.LastFrameRateApplicationResult` must also report the same
sentinel as its previous values.

## Isolation / restoration

The QA:

- edits only the existing Framework Settings asset during preparation;
- changes no scenes;
- changes no Build Settings;
- creates no GameApplication fixture;
- consumes its runtime arm once;
- restores the original Frame Rate policy after Play Mode;
- restores pre-QA Editor Unity frame pacing values;
- provides an explicit `Restore Project Frame Rate` menu.

## Certification target

Stage A is technically certifiable when all four terminal markers pass:

```text
ADR017_QA_EDIT      PASS 13/13
ADR017_QA_TARGET    PASS 13/13
ADR017_QA_VSYNC     PASS 13/13
ADR017_QA_DEFAULTS  PASS 13/13
```
