# IF-FW-F25H — Activity Scene Ledger

## Status

Implemented as internal runtime infrastructure.

## Purpose

F25H replaces the experimental implicit Activity scene tracking list with an explicit Activity scene ledger.

The ledger records Activity-owned Unity scenes with:

- route instance id;
- route asset reference;
- activity asset reference;
- activity id/name;
- content identity and content id;
- scene name/path;
- release policy;
- ownership = Activity;
- entry state: Loaded, Released or Stale.

## Runtime behavior

The behavior validated by F25F1/F25F2/F25G is preserved:

- `ReleaseOnActivityChange` unloads on Activity switch/clear.
- `KeepOnActivityChange` is skipped on Activity switch/clear.
- Route change force-releases all Activity-owned loaded entries regardless of Activity release policy.
- Stale ledger entries are marked stale when Unity no longer reports the scene as loaded.
- Stale entries are not planned as release side-effects.

## Diagnostics

Activity and Route logs now include ledger snapshot fields:

```text
activitySceneLedger
activitySceneLedgerEntries
activitySceneLedgerLoaded
activitySceneLedgerReleased
activitySceneLedgerStale
```

These fields are snapshots captured in the `ActivityFlowStartResult` after Activity scene composition/release side-effects for the operation.

## Not included

F25H does not add:

- validators;
- Inspector UI;
- Addressables;
- progress aggregation;
- route-owned release policy;
- final ActivityOperationExecutor migration;
- gameplay adapters.
