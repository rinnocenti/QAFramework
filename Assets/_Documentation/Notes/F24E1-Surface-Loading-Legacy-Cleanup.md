# F24E1 - Surface/Loading Legacy Cleanup

## Goal

Remover o caminho legado `GameApplicationAsset -> Transition/Loading prefab` e deixar `UIGlobal` como a unica origem runtime para surfaces globais.

## Decision

`FrameworkRuntimeHost` resolve Transition e Loading somente a partir de `GlobalUiSceneRuntime`.
Se `Global UI Scene Policy` for `NoneConfigured`, o boot usa NoOp explicito.
Se `Global UI Scene Policy` for `Required` e `UIGlobal` estiver ausente ou sem adapters, o boot falha explicitamente.

## Removed runtime shape

```text
GameApplicationAsset
  -> Transition Surface Prefab
  -> Loading Surface Prefab
```

## Canonical runtime shape

```text
GameApplicationAsset
  -> UIGlobal Scene
      -> Transition adapter
      -> Loading adapter
```

## Validation scope

- `UIGlobal` is loaded additively and persisted under `FrameworkRuntimeHost`.
- Transition and Loading adapters are discovered from the persisted roots.
- No prefab fallback remains in the runtime host.

## QA

The QA application now references only:

```text
Global UI Scene Policy: Required
Global UI Scene Path: Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity
Global UI Scene Name: QA_UIGlobal
```

Legacy prefab templates may remain in the repository as manual assets, but they are not runtime paths.
