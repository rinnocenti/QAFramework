# ADR-018-C — Product Composition Certification

**Date:** 2026-08-11  
**Type:** technical QA certification  
**Package baseline:** `79ff6ce6820263fb6a101dc0fed2f3958bf22780`  
**Package commit:** `feat(progression-save): add application backend authoring`

## Terminal evidence

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

## Certified contract

```text
GameApplicationAsset
  -> ProgressionSaveProfile
  -> ProgressionSaveApplicationComposition
  -> selected IProgressionSaveStore
  -> ProgressionSaveRuntime
```

## Positive certification

```text
Disabled            PASS
Built-in JSON       PASS
Custom Provider     PASS
```

The custom path materialized the QA in-memory backend and the resulting
`ProgressionSaveRuntime` executed Save/Load through that selected backend.

## Negative certification

```text
missing Profile              PASS — Rejected
missing Custom Provider      PASS — Rejected
invalid Provider             PASS — Rejected
Provider create failure      PASS — Rejected
Provider null Store          PASS — Rejected
invalid BackendId            PASS — Rejected
Provider exception           PASS — Rejected
```

Count:

```text
7/7
```

## No-fallback certification

All failing Custom Provider cases preserve:

```text
runtime = absent
fallback JSON = absent
```

The package never converts custom-provider failure into Built-in JSON composition.

## Selection isolation

A Built-in JSON Profile does not consult an unselected custom-provider reference.

This proves that backend selection has a single authored authority.

## Disposition

```text
ADR018-C4 technical QA       CERTIFIED
ADR018-C package gate        READY FOR C5 CLOSURE
technical QA remaining       0
next gate                    ADR018-D FIRSTGAME
```

## QA repository note

At inspection time the public QA repository still had ADR018-B as its latest committed
Progression Save state. The C4 runner was applied and executed locally to produce the
terminal evidence above.

Commit the C4 QA files together with this certification record so Git history contains
both the executable proof and its certified result.

## Suggested commit

```text
qa: certify ADR-018 progression save product composition
```
