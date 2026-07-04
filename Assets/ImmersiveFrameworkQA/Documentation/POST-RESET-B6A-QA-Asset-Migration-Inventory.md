# POST-RESET-B6A — QA Asset Migration Inventory

## Objetivo

Inventariar `Assets/_Project` no Framework QA Project e classificar o que deve migrar para `Assets/ImmersiveFrameworkQA` em cortes futuros.

Este corte nao moveu assets serializados, nao apagou `Assets/_Project` e nao alterou runtime do framework.

## Pasta auditada

```text
C:\Projetos\My project\Assets\_Project
```

## Sumario

| Tipo | Quantidade | Observacao |
|---|---:|---|
| `.asset` | 9 | Todos sao Unity serialized e exigem Unity Editor para migracao. |
| `.cs` | 1 | Script Editor-only; sem referencia por cena/prefab encontrada por GUID. |
| `.asmdef` | 2 | Texto/config de assembly local. |
| `.meta` | 39 | Preservar com os assets/pastas ao migrar/remover via Unity Editor. |
| `.unity` | 0 | Nenhuma cena sob `_Project`. |
| `.prefab` | 0 | Nenhum prefab sob `_Project`. |
| `.mat` | 0 | Nenhum material sob `_Project`. |
| `.controller` | 0 | Nenhum controller sob `_Project`. |
| `.inputactions` | 0 | Nenhum input actions sob `_Project`. |

Conclusao: o conteudo ativo de `_Project` e principalmente config/ScriptableObject do QA Project. A maior parte tem dependencia por GUID e deve migrar somente via Unity Editor.

## Inventario por pasta

### Pastas vazias ou estruturais

| Caminho atual | Tipo | `.meta` | Funcao provavel | Serializado Unity | Referenciado por asset | Destino recomendado | Risco | Acao recomendada |
|---|---|---:|---|---:|---|---|---|---|
| `Assets/_Project/Art` | unknown | Sim | Pasta gerada por setup inicial; sem arquivos. | Nao | Nao identificado | Nenhum, salvo novo uso QA | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Audio` | unknown | Sim | Pasta gerada por setup inicial; sem arquivos. | Nao | Nao identificado | Nenhum, salvo novo uso QA | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Audio/Music` | unknown | Sim | Pasta gerada por setup inicial; sem arquivos. | Nao | Nao identificado | Nenhum, salvo novo uso QA | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Audio/SFX` | unknown | Sim | Pasta gerada por setup inicial; sem arquivos. | Nao | Nao identificado | Nenhum, salvo novo uso QA | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Materials` | material | Sim | Pasta gerada por setup inicial; sem materiais. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Materials/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Prefabs` | prefab | Sim | Pasta gerada por setup inicial; sem prefabs. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Prefabs/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scenes` | scene | Sim | Pasta gerada por setup inicial; sem cenas. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Scenes/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scenes/Boot` | scene | Sim | Pasta gerada por setup inicial; sem cenas. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Scenes/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scenes/Menu` | scene | Sim | Pasta gerada por setup inicial; sem cenas. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Scenes/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scenes/Gameplay` | scene | Sim | Pasta gerada por setup inicial; sem cenas. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Scenes/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scenes/Sandbox` | scene | Sim | Pasta gerada por setup inicial; sem cenas. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/Scenes/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/Scripts/UI` | ui | Sim | Pasta gerada por setup inicial; sem scripts. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/UI/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/UI` | ui | Sim | Pasta gerada por setup inicial; sem UI assets. | Nao | Nao identificado | `Assets/ImmersiveFrameworkQA/UI/` se houver uso futuro | `remove-candidate` | Remover em B6F se continuar vazia. |
| `Assets/_Project/VFX` | unknown | Sim | Pasta gerada por setup inicial; sem arquivos. | Nao | Nao identificado | Nenhum, salvo novo uso QA | `remove-candidate` | Remover em B6F se continuar vazia. |

### ScriptableObjects e configs

| Caminho atual | Tipo | `.meta` | Funcao provavel | Serializado Unity | Parece referenciado | Destino recomendado | Risco | Acao recomendada |
|---|---|---:|---|---:|---|---|---|---|
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/GameApplication.asset` | `scriptable-object` | Sim | `GameApplicationAsset` ativo/configuravel; aponta para `QA_CanonicalRoute` e `QA_UIGlobal`. | Sim | Sim, por `ImmersiveFrameworkSettings.asset`. | `Assets/ImmersiveFrameworkQA/GameApplications/GameApplication.asset` ou nome QA explicito. | `unity-serialized-manual` | Mover via Unity Editor em B6B; confirmar `Active Game Application` apos mover. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes/StartupRoute.asset` | `scriptable-object` | Sim | Route legacy para `StartupScene`; usado no reset QA e referencia `StartupActivity`. | Sim | Sim, por `StartupScene.unity`. | `Assets/ImmersiveFrameworkQA/Routes/StartupRoute.asset` ou consolidar com `QA_CanonicalRoute` se for duplicado. | `unity-serialized-manual` | B6B via Unity Editor; antes decidir se e duplicado de `QA_CanonicalRoute`. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes/SecondRoute.asset` | `scriptable-object` | Sim | Route legacy para `SecondScene`; referencia `SecondActivity`. | Sim | Sem referencia externa encontrada alem do proprio `.meta`. | `Assets/ImmersiveFrameworkQA/Routes/SecondRoute.asset` se ainda for usado; senao candidato a remocao manual. | `unity-serialized-manual` | Revisar no Editor; mover ou remover somente apos confirmar uso. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/Activities/StartupActivity.asset` | `scriptable-object` | Sim | Activity legacy usada por `StartupRoute` e por `StartupScene`. | Sim | Sim, por `StartupRoute.asset` e `StartupScene.unity`. | `Assets/ImmersiveFrameworkQA/Activities/StartupActivity.asset` ou consolidar com `QA_PrimaryContentActivity` se for duplicado. | `unity-serialized-manual` | B6B via Unity Editor; confirmar referencias da cena. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/Activities/SecondActivity.asset` | `scriptable-object` | Sim | Activity legacy usada por `SecondRoute`. | Sim | Sim, por `SecondRoute.asset`; sem cena externa encontrada. | `Assets/ImmersiveFrameworkQA/Activities/SecondActivity.asset` se ainda for usado; senao candidato a remocao manual. | `unity-serialized-manual` | Revisar no Editor; mover ou remover somente apos confirmar uso. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/RouteContentProfiles/RouteContentProfile.asset` | `scriptable-object` | Sim | Perfil de conteudo adicional de Route; aponta para `AdditionalRouteScene`. | Sim | Sim, por `QA_CanonicalRoute.asset` e `StartupRoute.asset`. | `Assets/ImmersiveFrameworkQA/SmokeData/RouteContentProfile.asset` ou `Routes/` se mantido junto da route. | `unity-serialized-manual` | B6B via Unity Editor; migrar antes das Routes que o referenciam. |
| `Assets/_Project/ScriptableObjects/ImmersiveFramework/ActivityContentProfiles/ActivityContentProfile.asset` | `scriptable-object` | Sim | Perfil de conteudo adicional de Activity; aponta para `ActivityAdditionalContent.unity`. | Sim | Sim, por `UnityBuildSurface/Activities/QA_TransitionActivityA.asset`. | `Assets/ImmersiveFrameworkQA/SmokeData/ActivityContentProfile.asset` ou `UnityBuildSurface/` se for especifico da superficie. | `unity-serialized-manual` | B6B via Unity Editor; mover antes das activities que o referenciam. |
| `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` | `config` | Sim | Settings carregado por `Resources.Load("ImmersiveFrameworkSettings")`; referencia GameApplication e LoggingConfig. | Sim | Sem GUID externo; carregamento provavel por Resources path/nome. | `Assets/ImmersiveFrameworkQA/SmokeData/Resources/ImmersiveFrameworkSettings.asset`, se aceito pelo tooling; caso contrario manter ate package/editor path ser ajustado. | `unity-serialized-manual` | B6B somente via Editor e com validacao de bootstrap/settings provider. |
| `Assets/_Project/Settings/ImmersiveFramework/Logging/LoggingConfig.asset` | `config` | Sim | Config de logging usada por `ImmersiveFrameworkSettings`. | Sim | Sim, por `ImmersiveFrameworkSettings.asset`. | `Assets/ImmersiveFrameworkQA/SmokeData/LoggingConfig.asset` ou junto do Settings. | `unity-serialized-manual` | B6B via Unity Editor; mover junto do settings ou antes dele. |

### Scripts e assembly definitions

| Caminho atual | Tipo | `.meta` | Funcao provavel | Serializado Unity | Parece referenciado | Destino recomendado | Risco | Acao recomendada |
|---|---|---:|---|---:|---|---|---|---|
| `Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs` | `script-editor` | Sim | Tooling local de setup inicial e instalacao de packages. | Nao como asset de cena; script tem `.meta`. | GUID nao aparece em cenas/prefabs/assets. | `Assets/ImmersiveFrameworkQA/Scripts/Editor/ImmersiveInitialProjectSetup.cs`, se ainda for necessario; senao remover em corte proprio. | `safe-with-meta` | B6D ou setup cleanup; mover com `.meta` ou remover apos confirmar que nao e mais usado. |
| `Assets/_Project/Scripts/Editor/Project.Editor.asmdef` | `config` | Sim | Assembly Editor local para tooling `_Project`. | Nao | Sem GUID scan exigido; usado por Unity assembly import. | `Assets/ImmersiveFrameworkQA/Scripts/Editor/Project.Editor.asmdef` se scripts migrarem. | `safe-with-meta` | B6D junto dos scripts Editor. |
| `Assets/_Project/Scripts/Runtime/Project.Runtime.asmdef` | `config` | Sim | Assembly Runtime local vazio ou sem scripts runtime ativos sob `_Project`. | Nao | Sem GUID scan exigido; usado por Unity assembly import. | Remover se continuar sem scripts runtime; ou mover para `Assets/ImmersiveFrameworkQA/Scripts/Runtime/`. | `safe-with-meta` | B6D; revisar depois dos scripts. |

## Referencias por GUID

| Asset | GUID | Referencias encontradas em `Assets` |
|---|---|---|
| `Activities/SecondActivity.asset` | `65dc5dc1b271ad34a95734ee924a46ef` | `Assets/_Project/.../Routes/SecondRoute.asset`; proprio `.meta`. |
| `Activities/StartupActivity.asset` | `92ed42b61ac7d9e47ab9e64ea50c9f1a` | `Assets/_Project/.../Routes/StartupRoute.asset`; `Assets/ImmersiveFrameworkQA/Scenes/StartupScene.unity`; proprio `.meta`. |
| `ActivityContentProfiles/ActivityContentProfile.asset` | `630d293a74f1d1c4d8e320ac811a3317` | `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Activities/QA_TransitionActivityA.asset`; proprio `.meta`. |
| `GameApplication.asset` | `0cfafd842c412a846b7bc0917eec03df` | `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`; proprio `.meta`. |
| `RouteContentProfiles/RouteContentProfile.asset` | `fca82a09ef87fce40846919ed75acbbb` | `Assets/ImmersiveFrameworkQA/Routes/QA_CanonicalRoute.asset`; `Assets/_Project/.../Routes/StartupRoute.asset`; proprio `.meta`. |
| `Routes/SecondRoute.asset` | `b151e96aaf1658848b76986cd5c8e128` | Proprio `.meta` apenas. |
| `Routes/StartupRoute.asset` | `ab232d2f09b89b1458dde990193e781c` | `Assets/ImmersiveFrameworkQA/Scenes/StartupScene.unity`; proprio `.meta`. |
| `Scripts/Editor/ImmersiveInitialProjectSetup.cs` | `ec76d82aea90c36428d852651974b1cf` | Proprio `.meta` apenas; sem cena/prefab/asset referenciando script por GUID. |
| `Settings/ImmersiveFramework/Logging/LoggingConfig.asset` | `97df6567969008f48a18ff532c9dd1d2` | `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`; proprio `.meta`. |
| `Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` | `49007cdcaa5ee364aa1eb42e95d1dfb0` | Proprio `.meta` apenas; runtime resolve por `Resources.Load`, nao por GUID textual. |

## Destinos recomendados

| Origem | Destino recomendado | Observacao |
|---|---|---|
| `ScriptableObjects/ImmersiveFramework/GameApplication.asset` | `Assets/ImmersiveFrameworkQA/GameApplications/` | Mover por Unity Editor e validar `Active Game Application`. |
| `ScriptableObjects/ImmersiveFramework/Routes/*.asset` | `Assets/ImmersiveFrameworkQA/Routes/` | Antes decidir duplicacao com `QA_CanonicalRoute`, `QA_AlternateRoute`, `QA_NoActivityRoute`. |
| `ScriptableObjects/ImmersiveFramework/Activities/*.asset` | `Assets/ImmersiveFrameworkQA/Activities/` | Antes decidir duplicacao com `QA_PrimaryContentActivity` e outras activities QA. |
| `RouteContentProfiles/*.asset` | `Assets/ImmersiveFrameworkQA/SmokeData/` | Perfil e dado de smoke/config, nao route por si so. |
| `ActivityContentProfiles/*.asset` | `Assets/ImmersiveFrameworkQA/SmokeData/` ou `UnityBuildSurface/` | Se continuar especifico da `UnityBuildSurface`, manter nessa superficie. |
| `Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` | `Assets/ImmersiveFrameworkQA/SmokeData/Resources/` com cautela | Precisa preservar lookup por `Resources.Load("ImmersiveFrameworkSettings")`; package editor utility ainda aponta para `_Project`. |
| `Settings/ImmersiveFramework/Logging/LoggingConfig.asset` | `Assets/ImmersiveFrameworkQA/SmokeData/` | Mover junto do settings ou antes dele. |
| `Scripts/Editor/ImmersiveInitialProjectSetup.cs` | `Assets/ImmersiveFrameworkQA/Scripts/Editor/` ou remover | Editor tooling legado; sem referencia por GUID em cena/prefab. |
| `Scripts/*.asmdef` | `Assets/ImmersiveFrameworkQA/Scripts/...` ou remover | Depende da decisao sobre o script Editor e existencia de scripts runtime. |
| Pastas vazias de `_Project` | Nenhum | Remover em B6F se `_Project` ficar vazio. |

## Migracao manual recomendada

1. Duplicacao/decisao no Editor: confirmar se `StartupRoute`, `SecondRoute`, `StartupActivity`, `SecondActivity` ainda sao necessarios ou se foram substituidos pelos assets `QA_*`.
2. Migrar primeiro os perfis referenciados: `RouteContentProfile.asset` e `ActivityContentProfile.asset`.
3. Migrar activities e routes que dependem dos perfis.
4. Migrar `GameApplication.asset`.
5. Migrar `LoggingConfig.asset` e `ImmersiveFrameworkSettings.asset` somente depois de decidir o destino com `Resources` e o path hardcoded do package editor utility.
6. Tratar scripts/asmdefs depois dos assets serializados.
7. Remover pastas vazias de `_Project` somente quando nenhum asset/script/meta ativo restar.

## Itens seguros para mover depois

| Item | Condicao |
|---|---|
| `ImmersiveInitialProjectSetup.cs` | Seguro com `.meta` se mantido como tooling QA; nao ha GUID em cena/prefab/asset. |
| `Project.Editor.asmdef` | Seguro com `.meta` junto do script Editor, se ainda houver script. |
| `Project.Runtime.asmdef` | Seguro com `.meta` ou remocao se continuar sem scripts runtime. |
| Pastas vazias | Remover em B6F apos Unity confirmar que `_Project` nao e recriado e nao contem assets ativos. |

## Itens que exigem Unity Editor

| Item | Motivo |
|---|---|
| Todos os 9 `.asset` | Sao Unity serialized e varios sao referenciados por GUID em cenas/assets. |
| `StartupActivity.asset` | Referenciado por `StartupScene.unity` e `StartupRoute.asset`. |
| `StartupRoute.asset` | Referenciado por `StartupScene.unity`. |
| `RouteContentProfile.asset` | Referenciado por `QA_CanonicalRoute.asset` e `StartupRoute.asset`. |
| `ActivityContentProfile.asset` | Referenciado por `QA_TransitionActivityA.asset`. |
| `ImmersiveFrameworkSettings.asset` | Carregado por `Resources.Load`; destino deve preservar `Resources` e tooling. |

## Itens desconhecidos

Nenhum arquivo ativo ficou como `unknown-manual`.

Pastas vazias foram classificadas como `remove-candidate`, mas a remocao fica para B6F.

## Ordem recomendada B6B-B6F

| Corte | Acao |
|---|---|
| B6B | Mover ou consolidar ScriptableObjects/configs QA via Unity Editor: perfis, activities, routes, GameApplication, LoggingConfig e Settings. |
| B6C | Mover prefabs/materiais/UI QA via Unity Editor, se surgirem assets nesses roots; no inventario atual nao ha arquivos desses tipos em `_Project`. |
| B6D | Mover ou remover scripts QA e asmdefs, com `.meta`, apos confirmar que o setup inicial ainda e necessario. |
| B6E | Mover cenas QA e atualizar Build/Profile se necessario; no inventario atual nao ha cenas em `_Project`, mas validar referencias em cenas existentes. |
| B6F | Remover `Assets/_Project` e `.meta` somente se a pasta ficar vazia e Unity confirmar ausencia de referencias. |

## Validacao textual

Executado neste corte:

- inventario textual de `Assets/_Project`;
- contagem por extensao;
- leitura dos `.asset`;
- leitura do script Editor local;
- extracao dos GUIDs de `.cs` e `.asset`;
- busca de cada GUID em `C:\Projetos\My project\Assets`;
- inspecao textual do package para `ImmersiveFrameworkSettings` e `Resources.Load`.

Buscas finais obrigatorias em `C:\Projetos\My project\Assets`:

| Busca | Classificacao |
|---|---|
| `Assets/_Project` | Hits esperados no README, relatorios B4/B5/B5A/B6A, no script setup e nos assets legados inventariados. |
| `_Project` | Hits esperados no namespace/script setup, relatorios e assets legados. |
| `ImmersiveFrameworkQA` | Hits validos no root canonico QA, scenes, routes, UnityBuildSurface e relatorios. |
| `Game Application` | Hits validos no README, `GameApplication.asset`, `QA_TransitionGameApplication.asset` e relatorio B6A. |
| `QA Canonical Route` | Hit valido em `Assets/ImmersiveFrameworkQA/Routes/QA_CanonicalRoute.asset`. |
| `StartupScene` | Hits validos em routes QA e no relatorio B6A; tambem confirma dependencia do `StartupRoute.asset` legado. |
| `QA_UIGlobal` | Hits validos no README, UnityBuildSurface, `GameApplication.asset` legado e relatorios. |
| `git status --short` | Mostrou `README.md` modificado e o relatorio B6A novo com `.meta`. |

Nao executado:

- Unity import;
- compile;
- build;
- playmode;
- smoke;
- batchmode.

## Follow-up B6B0 — Settings Path Policy

Arquivos auditados:

- `C:\Projetos\ImmersivePackages\com.immersive.framework\Editor\Settings\ImmersiveFrameworkEditorSettingsUtility.cs`;
- `C:\Projetos\ImmersivePackages\com.immersive.framework\Editor\Settings\ImmersiveFrameworkSettingsProvider.cs`;
- `C:\Projetos\ImmersivePackages\com.immersive.framework\Runtime\Authoring\ImmersiveFrameworkSettingsAsset.cs`;
- `C:\Projetos\ImmersivePackages\com.immersive.framework\Runtime\Bootstrap\ImmersiveFrameworkBootstrap.cs`;
- `C:\Projetos\ImmersivePackages\com.immersive.framework\Runtime\Diagnostics\FrameworkLogger.cs`;
- `C:\Projetos\My project\Assets\_Project\Settings\ImmersiveFramework\Resources\ImmersiveFrameworkSettings.asset`.

Referencias removidas ou corrigidas:

- o tooling Editor do package deixou de carregar settings apenas pelo caminho fixo `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`;
- a tela Project Settings passou a mostrar o caminho real do settings carregado, em vez de apresentar `_Project` como local universal.

Referencias mantidas:

- `Resources.Load("ImmersiveFrameworkSettings")` foi mantido no runtime como contrato de carregamento;
- o caminho `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` foi mantido apenas como default de criacao quando nenhum settings valido existe;
- o asset QA ainda permanece no caminho legado ate uma migracao via Unity Editor.

Motivo:

- B6B0 foi textual/editor-tooling only. Nao moveu asset serializado e nao alterou cenas, prefabs, `.asset`, `.mat`, `.controller` ou `.inputactions`.

Validacao textual:

- buscas por `ImmersiveFrameworkSettings`, `Resources.Load`, `Assets/_Project`, `_Project/Settings`, `Settings/ImmersiveFramework/Resources` e `Active Game Application` confirmaram que o risco ativo estava no utility Editor do package;
- buscas finais registradas no relatorio B6B0 confirmam que nao restou dependencia ativa de settings em `_Project` no tooling, apenas default/documentacao/asset legado ainda nao migrado.

## Proximo passo

Executar B6B com Unity Editor aberto para mover/consolidar os ScriptableObjects e configs QA. Depois do B6B0, `ImmersiveFrameworkSettings.asset` pode migrar para `Assets/ImmersiveFrameworkQA/SmokeData/Resources/ImmersiveFrameworkSettings.asset`, desde que fique exatamente um settings valido em `Resources`.
