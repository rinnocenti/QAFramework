# F24B - Transition Contract Wiring Notes

## Goal

Garantir que Route/Activity requests passam por um contrato de Transition antes da criacao de surfaces visuais.

## Current behavior

NoOp transition only.

No visual.
No waiting.
No loading screen.
No pause overlay.
No lifecycle ownership.

## Integration points

- `Packages/com.immersive.framework/Runtime/Transition`: contrato minimo de request/orchestrator e implementacao NoOp.
- `Packages/com.immersive.framework/Runtime/GameFlow/GameFlowRuntime.cs`: Route request, Activity request e Activity clear executam Transition before/after em torno do fluxo real.
- `Packages/com.immersive.framework/Runtime/GameFlow/FrameworkRouteRequestResult.cs`: resultado de Route request carrega diagnostico de Transition.
- `Packages/com.immersive.framework/Runtime/GameFlow/FrameworkActivityRequestResult.cs`: resultado de Activity request carrega diagnostico de Transition.
- `Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`: logs de Route Request e Activity Request incluem campos de Transition.

## Validation

Standard Smoke deve continuar passando.

Logs de Route Request e Activity Request devem incluir diagnostico de transition.
