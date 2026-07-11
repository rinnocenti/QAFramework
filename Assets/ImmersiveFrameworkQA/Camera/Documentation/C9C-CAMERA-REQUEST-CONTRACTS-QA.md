# C9C — Camera Request Contracts QA

Status: implementation ready; Unity import/install/Play Mode validation pending  
Repository: `QAFramework`  
Package dependency: `com.immersive.framework` with C9C contracts

## Objective

Prove the C9C `CameraRequest` contract through the canonical QA workflow:

```text
QA Hub
-> Camera / C9C Request Contracts
-> dedicated Route
-> dedicated synthetic scene
-> runtime fixture
```

## Installation

Run:

```text
Immersive Framework QA
  Camera
    C9C Install Camera Request Contracts QA
```

The installer idempotently creates or repairs:

```text
Assets/ImmersiveFrameworkQA/Camera/Recipes/QA_C9C_CameraRigRecipe.asset
Assets/ImmersiveFrameworkQA/Camera/Activities/QA_CameraRequestContractsActivity.asset
Assets/ImmersiveFrameworkQA/Camera/Routes/QA_CameraRequestContractsRoute.asset
Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_CameraRequestContracts.unity
```

It also:

```text
adds the scene to Build Settings when absent
adds or repairs the C9C entry in QA_Hub.unity
adds a Back to QA Hub panel to the C9C scene
validates saved references
```

## Runtime cases

The scene fixture runs automatically on `Start`.

Positive cases:

```text
valid CameraRigComposer-backed request
valid CameraRigRecipe-backed request
```

Negative cases:

```text
missing request id
missing output id
invalid owner
invalid lifetime
missing rig
missing target source
missing release condition
missing diagnostic source
missing diagnostic reason
```

Neutrality proof:

```text
Camera.enabled remains unchanged
camera GameObject active state remains unchanged
camera transform remains unchanged
no request is admitted or arbitrated
no Cinemachine state is applied
```

## Expected PASS

```text
[QA][C9C Camera Request Contracts] PASS. status='Passed' cases='12'
```

The exact completed-case list is included in the log.

## Boundaries

This QA does not implement or test:

```text
CameraOutputContext
request registry
winner selection
request release execution
Cinemachine priority/channel application
Route/Activity/Player publishers
fallback output creation
```

Those belong to later C9D–C9G cuts.

## Manual validation

1. Import the package C9C and these QA files.
2. Confirm compilation.
3. Run the C9C installer.
4. Run the installer a second time and confirm no duplicate asset, scene, trigger or Hub entry.
5. Open `QA_Hub.unity`.
6. Enter Play Mode.
7. Select `Camera / C9C Request Contracts`.
8. Confirm the Route loads the dedicated scene.
9. Confirm all positive and negative logs.
10. Confirm the final PASS.
11. Use `Back to QA Hub` and confirm navigation works.
12. Inspect the synthetic scene and confirm no camera authority component was introduced.

## Suggested commit message

```text
QA: add C9C camera request contract route proof
```
