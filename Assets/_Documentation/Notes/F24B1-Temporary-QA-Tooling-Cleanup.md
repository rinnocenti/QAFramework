# F24B1 — Temporary QA Tooling Cleanup

## Status

Ready to apply

## Goal

Remove editor-only setup helpers that were used to create the Unity Build Surface QA fixtures after those fixtures were successfully generated and validated.

This cleanup keeps the actual QA assets, scenes, runtime panels and documentation.

## Keep

Keep these runtime/QA fixtures:

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/TransitionRouteA.unity
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/TransitionRouteB.unity
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes/QA_TransitionRouteA.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes/QA_TransitionRouteB.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityA.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityA_Alt.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityB.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityB_Alt.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications/QA_TransitionGameApplication.asset
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scripts/Runtime/TransitionQaRouteSwitchPanel.cs
Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scripts/Runtime/TransitionQaActivitySwitchPanel.cs
```

## Delete

Delete the temporary editor helpers listed in:

```text
Assets/_Documentation/Notes/F24B1-DELETE-MANIFEST.txt
```

These scripts were one-shot fixture installers/creators. Once the fixtures exist, they should not remain as permanent project tooling.

## Do not delete

Do not delete:

```text
Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs
```

It is still the project setup helper, not a temporary F24 QA fixture installer.

## Validation

After deletion:

1. Open Unity and wait for compile/import.
2. Confirm there are no missing scripts on:
   - `TransitionRouteA.unity`
   - `TransitionRouteB.unity`
3. Enter Play Mode with `QA Transition Game Application` active.
4. Validate route switch A ↔ B.
5. Validate activity switch/clear in Route A and Route B.
6. Confirm transition diagnostics still appear in logs.

## Expected status after validation

```text
IF-FW-F24B1 — CLOSED / TEMPORARY QA TOOLING CLEANUP PASS
```
