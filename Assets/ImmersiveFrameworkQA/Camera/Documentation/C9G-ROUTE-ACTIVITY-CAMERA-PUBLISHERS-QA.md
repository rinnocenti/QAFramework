# C9G — Route and Activity Camera Request Publishers QA

## Objective

Prove typed Route and Activity publishers over one explicit `CameraOutputSession`.

## Installer

Run:

```text
Immersive Framework QA/Camera/C9G Install Route Activity Publishers QA
```

## Runtime cases

```text
Route publish applies Route rig
Activity publish overrides Route rig
Activity release restores Route rig
Route release clears output
duplicate Publish/Release are preserved
wrong owner blocks creation
wrong lifetime blocks creation
foreign output blocks creation
failed session operation preserves publisher state
Unity Camera remains unchanged
```

## Expected PASS

```text
[QA][C9G Route Activity Publishers] PASS. status='Passed' cases='7'
```

## Suggested commit

```text
QA: prove C9G Route and Activity camera publishers
```
