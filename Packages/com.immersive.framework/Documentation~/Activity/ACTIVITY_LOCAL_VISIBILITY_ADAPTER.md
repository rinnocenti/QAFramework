# Activity Local Visibility Adapter

Status: CLOSED / COMPILE-SMOKE PASS  
Fase: F4  
Corte: F4E  
Roadmap: IF-FW-ROAD-4E — Reclassificar Activity Local Visibility Adapter

---

## Contexto

F4A-F4D criaram o baseline mínimo de Activity:

```text
ActivityRuntimeState
ActivityContentSet
ActivityContentLifecycleResult
ActivityReadinessState
```

O componente de visibilidade local de Activity precisava ter nome coerente com seu papel real para não sugerir materialização canônica da Activity.

Isso ainda não é verdade nesta fase.

## Decisão

Em F4E, o componente passa a ser tratado como:

```text
Activity Local Visibility Adapter
```

No fechamento de F4, a classe C# foi renomeada para `ActivityLocalVisibilityAdapter`, preservando o `.meta`/GUID pelo fluxo de rename seguro da Unity/IDE.

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
Content Anchor
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

O resultado de runtime continua sendo `ActivityContentApplyResult`, porque ele ainda pertence ao baseline de Activity Content. O adapter local é apenas a superfície scene-authored desse baseline.

## Critério de smoke

F4E não adiciona comportamento funcional novo. O smoke preservou:

```text
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content'
activityReadiness='Ready'
activityReadiness='None'
```
