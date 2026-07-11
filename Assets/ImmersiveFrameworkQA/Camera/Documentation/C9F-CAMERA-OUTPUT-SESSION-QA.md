# C9F — CameraOutputSession QA

## Objective

Prove automatic and transactional coordination between:

```text
CameraOutputContext
CameraOutputRigApplicator
CameraOutputSession
```

## Installer

Run:

```text
Immersive Framework QA/Camera/C9F Install Camera Output Session QA
```

## Runtime cases

```text
automatic lifecycle
rejected mutation does not apply
admission application failure rolls back
release application failure rolls back
synchronize pre-existing context
mismatched output constructor blocked
Unity output unchanged
```

## Expected PASS

```text
[QA][C9F Camera Output Session] PASS. status='Passed' cases='7'
```

## Transactional evidence

Admission rollback:

```text
valid Route applied
invalid higher request admitted
application fails
invalid request removed
Route winner and presentation restored
```

Release rollback:

```text
invalid lower request registered
valid Player winner applied
Player released
invalid request becomes winner
application fails
Player re-admitted and presentation restored
```

## Suggested commit

```text
QA: prove C9F transactional camera output session
```
