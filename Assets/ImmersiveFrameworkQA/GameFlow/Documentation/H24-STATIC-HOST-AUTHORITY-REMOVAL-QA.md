# H2.4 — Static Host Authority Removal QA

## Objective

Close H2 by removing:

```text
FrameworkRuntimeHost._current
FrameworkRuntimeHost.TryGetCurrent
```

The package retains a stateless host factory but no stored global host reference.

QAFramework resolves the host only inside its friend assemblies by:

```text
loaded FrameworkRuntimeHost components
-> loaded valid Unity scenes
-> reference deduplication
-> exactly one candidate required
```

This resolver is QA harness infrastructure. It is not a package service locator,
product API or runtime fallback.

## Installation

Run the installer from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\Install-H2.4.ps1 `
  -FrameworkRoot "C:\Projetos\ImmersivePackages\com.immersive.framework" `
  -QaRoot "C:\Projetos\QAFramework"
```

The installer:

```text
performs a baseline preflight
rejects unsupported QA call sites
removes static host storage and lookup
migrates H2 QA bootstraps
installs the final smoke
scans package and QA source for remaining invocations
supports an idempotent second execution
```

## Smoke

In Play Mode run:

```text
Immersive Framework
  > QA
    > Game Flow
      > H2.4 Run Static Host Authority Removal Smoke
```

Expected:

```text
[H24_STATIC_HOST_AUTHORITY_REMOVAL_SMOKE]
status='Passed'
cases='10'
```

## Coverage

```text
Play Mode
exactly one loaded host resolved by QA
resolved runtime ready
static host field absent
static lookup method absent
host source contains no static authority
factory retains no host reference
package has zero static lookup invocations
QA has zero static lookup invocations
QA resolver rejects ambiguous candidate sets
```


## Installer R2

The static-authority source check is structural. It rejects only the legacy
`FrameworkRuntimeHost` field and lookup declaration, rather than every unrelated
identifier named `_current` elsewhere in the large host source.


## Installer R3

R3 removes the legacy `OnDestroy` cleanup that referenced `_current`. The
removal is independent from the field/factory transformation, making the
installer safe to run after a partially-applied R2 installation.
