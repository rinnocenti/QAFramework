# RESET-035-E isolated Activity Restart QA

These are two independent runtime entry points. They do not use the QA-NEW-002 readiness scenario or a component ContextMenu.

## Run E1 — successful restart

1. In `ImmersiveFrameworkSettings`, set `activeGameApplication` to `Assets/QA-RESET-035-E1-ActivityRestartSuccess/Settings/GameApplication_QaReset035E1.asset`.
2. Enter a fresh Play Mode session and wait for the framework to start Activity A. The Route scene starts the success scenario automatically after its baseline is registered.
3. Capture the single `[QA-RESET-035-E] case='ActivityRestartSuccess' status='PASS|FAIL'` terminal line.

## Run E2 — reset failure blocks restart

1. In `ImmersiveFrameworkSettings`, set `activeGameApplication` to `Assets/QA-RESET-035-E2-ActivityRestartFailure/Settings/GameApplication_QaReset035E2.asset`.
2. Enter a fresh Play Mode session and wait for Activity A. The Route scene arms the participant's one-shot controlled failure and requests restart automatically.
3. Capture the single `[QA-RESET-035-E] case='ResetFailureBlocksRestart' status='PASS|FAIL'` terminal line.

Use a fresh Play Mode session after each case. Both applications disable Player and use the current persistent Camera Output prefab with its explicit fallback rig; neither requires a Session Assignment. `cleanup='BaselineRestored'` is required for PASS. `FreshBootRequired` means the current run cannot be reused.
