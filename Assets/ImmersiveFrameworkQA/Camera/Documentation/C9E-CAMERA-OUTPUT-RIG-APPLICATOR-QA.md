# C9E — CameraOutputRigApplicator QA

## Objective

Prove that the winner selected by `CameraOutputContext` is applied to one explicit Cinemachine output.

## Installer

Run:

```text
Immersive Framework QA/Camera/C9E Install Camera Output Rig Applicator QA
```

The installer creates:

```text
QA_CameraOutputRigApplicatorActivity.asset
QA_CameraOutputRigApplicatorRoute.asset
QA_CameraOutputRigApplicator.unity
```

It also registers:

```text
Camera / C9E Output Rig Applicator
```

in the QA Hub.

## Runtime cases

```text
winner application lifecycle
unchanged winner preserved
recipe-only winner blocked
composer without CinemachineCamera blocked
foreign output context blocked
clear while already clear
Unity output remains unchanged
```

## Expected PASS

```text
[QA][C9E Camera Output Rig Applicator] PASS. status='Passed' cases='7'
```

## Boundaries

The proof does not cover:

```text
runtime Recipe materialization
request publishers
automatic Apply after Admit/Release
Cinemachine blend configuration
multi-output registry
```

## Suggested commit

```text
QA: prove C9E camera output rig application
```
