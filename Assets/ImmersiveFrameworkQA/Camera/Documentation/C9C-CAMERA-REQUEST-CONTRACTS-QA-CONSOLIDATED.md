# C9C — Camera Request Contracts QA (Consolidated)

## Replaces

This package replaces every previous C9C QA installer/fix file.

Keep only:

```text
QaC9CCameraRequestContractsFixture.cs
QaC9CCameraRequestContractsInstaller.cs
```

Delete obsolete files if present:

```text
QaC9CHubRouteRepair.cs
```

The old persistent asset is unused and may be removed:

```text
Assets/ImmersiveFrameworkQA/Camera/Recipes/QA_C9C_CameraRigRecipe.asset
```

## Design corrections

The consolidated installer:

- does not persist a CameraRigRecipe;
- does not carry RouteAsset instances across scene opens;
- reloads the Route after opening the Hub;
- reloads assets again during validation;
- sets enums by serialized enum name;
- reports success only after all persisted assets and scenes are reopened and validated;
- creates one dedicated Route, Activity and scene;
- repairs exactly one C9C Hub entry;
- validates the target Route by asset path.

## Run

```text
Immersive Framework QA/Camera/C9C Install Camera Request Contracts QA
```

Expected setup log:

```text
[C9C_CAMERA_REQUEST_CONTRACTS_SETUP] status='Succeeded'
```

Then:

```text
QA_Hub.unity
-> Play Mode
-> Camera / C9C Request Contracts
```

Expected runtime conclusion:

```text
[QA][C9C Camera Request Contracts] PASS. status='Passed' cases='12'
```
