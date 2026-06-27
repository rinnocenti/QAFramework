# F24D1A — QA Loading Hold Unity Update Method Fix

## Goal

Corrigir o erro de import do Unity causado por método público `Update(LoadingSurfaceRequest)` no `QaLoadingSurfaceVisibilityHoldAdapter`.

## Fix

`ILoadingSurfaceAdapter.Update(LoadingSurfaceRequest)` agora é implementado explicitamente. Isso evita que o Unity trate o método como lifecycle `Update`, que não pode ter parâmetros.

## Boundary

- Não altera o core de Loading.
- Não altera SceneLifecycle, GameFlow ou FrameworkRuntimeHost.
- Não altera prefab.
- Não adiciona delay no lifecycle.
- Mantém o hold QA por coroutine local.

## Validation

Abrir Unity e confirmar que o erro `Update() can not take parameters` desaparece. Depois repetir o smoke de Route A/B para confirmar loading diagnostics e hold visual.
