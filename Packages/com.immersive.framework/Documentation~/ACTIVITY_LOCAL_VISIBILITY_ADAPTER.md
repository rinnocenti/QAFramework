# Activity Local Visibility Adapter

Status: CLOSED / COMPILE-SMOKE PASS  
Fase: F4  
Corte: F4E  
Roadmap: IF-FW-ROAD-4E — Reclassificar ActivityContentBinding

---

## Contexto

F4A-F4D criaram o baseline mínimo de Activity:

```text
ActivityRuntimeState
ActivityContentSet
ActivityContentLifecycleResult
ActivityReadinessState
```

O componente `ActivityContentBinding` continuava correto tecnicamente, mas o nome de uso podia sugerir que ele era a materialização canônica da Activity.

Isso ainda não é verdade nesta fase.

## Decisão

Em F4E, `ActivityContentBinding` passa a ser tratado como:

```text
Activity Local Visibility Adapter
```

A classe C# permanece `ActivityContentBinding` para não quebrar assets e serialização já criados.

O papel semântico é:

```text
Scene-authored adapter que liga/desliga um GameObject local conforme a Activity ativa.
```

## Não é

```text
ActivityContentProfile
canonical Activity materialization
spawn
load/unload
release policy
Surface
RuntimeMaterialization
LocalContributionSet
actor/input/camera/reset/snapshot pipeline
```

## Mudanças de authoring

O menu de componente passa a expor:

```text
Immersive Framework/Activity Local Visibility Adapter
```

O campo `Activity` recebe tooltip explícito:

```text
Activity that owns this local visibility adapter. This only toggles this GameObject when that Activity is active; it is not canonical Activity materialization.
```

## Mudanças de diagnóstico

Os diagnósticos internos passam a usar a nomenclatura:

```text
Activity Local Visibility Adapter diagnostics
Activity Local Visibility Adapter warning
```

O resultado de runtime continua sendo `ActivityContentApplyResult`, porque ele ainda pertence ao baseline de Activity Content.

## Critério de smoke

F4E não adiciona comportamento funcional novo. O smoke preservou:

```text
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content'
activityReadiness='Ready'
activityReadiness='None'
```
