# ADR-018 — Progression Save Backend Conformance QA

Package prerequisite: ADR018-A core store/catalog split applied.

## Purpose

Prove that Progression Save consumer/runtime behavior does not depend on the built-in
JSON backend.

The test uses the same `ProgressionSaveRuntime` suite against:

```text
JsonProgressionSaveStore
QaInMemoryProgressionSaveStore
```

The in-memory backend intentionally implements only:

```text
IProgressionSaveStore
```

and does not implement:

```text
IProgressionSaveCatalog
```

## Run

Outside Play Mode:

```text
Immersive Framework
  QA
    Regressions
      Progression Save
        Run ADR-018 Backend Conformance
```

## Expected terminal

```text
[ADR018_QA_BACKEND_CONFORMANCE]
status='Passed'
contractCases='9'
jsonCoreCases='13'
alternateCoreCases='13'
catalogCases='5'
negativeCases='7'
alternateCatalog='False'
consumerRuntime='ProgressionSaveRuntime'
semanticFingerprint='Missing>Saved>Loaded>Saved>Loaded>Deleted>Missing>Missing'
```

## Contract proof

The regression verifies:

```text
IProgressionSaveStore
  BackendId
  ReadSlot
  WriteSlot
  DeleteSlot

IProgressionSaveCatalog
  ReadManifest
```

and proves that the core store has no:

```text
ReadManifest
WriteManifest
ContainsSlot
```

It also verifies that the former manifest write result is not public.

## Backend parity

For both JSON and in-memory backends the exact same runtime suite proves:

```text
missing load
save
load roundtrip
overwrite save
latest-record load
delete
load after delete -> missing
repeated delete -> missing
backend identity projection
same semantic fingerprint
```

## Optional catalog

JSON must implement `IProgressionSaveCatalog`.

The in-memory backend must not.

JSON catalog projection is verified after Save and Delete.

## Negative projection

The controllable in-memory backend proves the runtime maps typed backend results into:

```text
BackendUnavailable
Corrupt
Failed
Rejected
```

without JSON-specific behavior.

## Acceptance

ADR018-A backend-conformance QA is PASS when the single terminal marker reports
`status='Passed'`.

After this gate passes, the package can receive the separate Stable
promotion/certification cut for the core contract.

ADR018-B JSON physical consistency/recovery remains a later cut.
