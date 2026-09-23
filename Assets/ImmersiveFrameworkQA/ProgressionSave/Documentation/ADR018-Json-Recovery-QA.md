# ADR-018-B — Built-in JSON Recovery QA

Package prerequisite:

```text
ADR018-B-Package-Json-Hardening.zip
```

Package baseline before the cut:

```text
bc6159efc95c46fc1f34a706d24dfd9fda243222
feat(progression-save): stabilize certified backend contract
```

## Purpose

Prove that the official built-in JSON backend does not expose an interrupted
slot/manifest update as silent partial state.

The regression simulates committed transaction states directly on disk and then
invokes the public `JsonProgressionSaveStore` API.

No package-private test hook or runtime reflection is used.

## Run

Outside Play Mode:

```text
Immersive Framework
  QA
    Regressions
      Progression Save
        Run ADR-018 JSON Recovery
```

## Expected terminal

```text
[ADR018_QA_JSON_RECOVERY]
status='Passed'
cases='18'
writeRecovery='3/3'
deleteRecovery='3/3'
uncommittedStaging='Discarded'
failClosed='6/6'
idempotentReplay='Passed'
normalWriteDelete='Passed'
transactionResidue='None'
backend='JsonProgressionSaveStore'
```

## Write recovery

The QA proves recovery when a committed Write transaction is interrupted:

```text
before either canonical file is applied
after slot is applied but before manifest
after manifest is applied but before slot
```

All paths converge to the new slot record and matching manifest entry.

## Delete recovery

The QA proves recovery when a committed Delete transaction is interrupted:

```text
before either canonical change
after slot deletion but before manifest
after manifest update but before slot deletion
```

All paths converge to:

```text
slot Missing
manifest does not contain slot
transaction directory removed
```

## Uncommitted staging

Staging without `intent.json` is treated as uncommitted.

QA proves it is discarded without changing the existing canonical record.

## Fail-closed cases

QA proves committed invalid state blocks access rather than being bypassed:

```text
corrupt intent blocks ReadSlot
corrupt slot stage blocks recovery before mutation
corrupt manifest stage blocks recovery before mutation
corrupt intent blocks WriteSlot
corrupt intent blocks DeleteSlot
corrupt intent blocks ReadManifest
unsupported transaction version blocks recovery
```

The `failClosed='6/6'` terminal field groups the six corrupt-data/access-bypass cases;
the unsupported-version case is counted separately in the total case count.

## Idempotence

QA also simulates a transaction whose slot and manifest were already applied but whose
transaction directory was not cleaned.

Replay must preserve the already-committed record and remove the transaction residue.

## Out of scope

```text
multi-process concurrent writers
filesystem/device power-loss guarantees beyond recoverable artifacts
cloud
encryption
compression
autosave
```

## Acceptance

ADR018-B5 passes when the single terminal marker reports:

```text
status='Passed'
cases='18'
```

After that evidence, ADR018-B6 can decide the certification/API maturity of the
built-in JSON backend and catalog surface.
