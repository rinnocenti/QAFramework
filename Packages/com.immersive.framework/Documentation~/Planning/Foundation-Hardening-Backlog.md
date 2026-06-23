# Foundation Hardening and Productization Backlog

Status: `TRACKED / NOT BLOCKING F8-F9`
Escopo: pós-core / multi-game productization

---

## Decisão

Estes itens são importantes para transformar a arquitetura em artefato reutilizável em múltiplos jogos, mas não devem interromper o fechamento de F8/F9.

---

## Backlog FX

| ID | Tema | Objetivo | Momento recomendado |
|---|---|---|---|
| FX1 | Settings Source Hardening | Formalizar ou substituir `Resources.Load` por provider explícito sem fallback silencioso. | Após F9/F10 ou quando houver fricção real de distribuição. |
| FX2 | Assembly / Build / Stripping Boundary Audit | Separar runtime core, Unity runtime, authoring, QA/dev tooling, editor e adapters opcionais. | Após F8/F9 estabilizarem fronteiras reais. |
| FX3 | Documentation Hygiene | Remover ambiguidade de conceitos históricos/removidos e manter status Active/Deferred/Removed/Historical. | Pode ocorrer em qualquer corte documental. |
| FX4 | Framework Versioning & Migration | Package versioning, API compatibility, asset migration, snapshot migration. | Após F12 Snapshot/Save. |
| FX5 | Pre-build Content Validation Pipeline | Validar anchors, required contributions, scenes, content profiles e bindings antes do build. | Após F9/F10/F11. |
| FX6 | Scoped Messaging Policy | Definir eventos internos/session/route/activity/local, escopo e lifetime. | Antes de F13 se consumers avançados precisarem. |
| FX7 | Editor Simulation / Visualizer | Visualizar Session -> Route -> Activity -> content sets -> runtime roots/handles. | Após F8/F9. |
| FX8 | Asset Provider / Addressables / DLC Boundary | Provider local primeiro; Addressables/DLC/modding como adapters opcionais. | Após adapter boundary e release policy. |
| FX9 | Domain Reload / Hot Reload Resilience | Limpar/invalidar static state, registries e handles no Editor sem fallback silencioso. | Após F8. |
| FX10 | Telemetry / Analytics Hooks | Hooks opcionais para transition duration, time in Route, Activity completion etc. | Após transition/activity lifecycle estabilizar. |

---

## Explicitamente fora do core atual

```text
Multiplayer/networking
Localization
Achievements/progression completo
Replay system
Accessibility layer
Remote config/experimentation
```

Esses itens podem virar consumers futuros. Eles não alteram o plano F8/F9/F10.
