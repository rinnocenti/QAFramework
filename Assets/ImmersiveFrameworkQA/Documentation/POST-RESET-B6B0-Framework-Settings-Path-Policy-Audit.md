# POST-RESET-B6B0 — Framework Settings Path Policy Audit

## Objetivo

Auditar e corrigir a politica de caminho do `ImmersiveFrameworkSettings.asset` para que o Framework QA Project possa migrar o settings para um root QA sem depender de `Assets/_Project`.

## Achados no package

| Arquivo | Achado | Classificacao |
|---|---|---|
| `Runtime/Authoring/ImmersiveFrameworkSettingsAsset.cs` | Define `ResourcesPath = "ImmersiveFrameworkSettings"`. | `runtime-load` |
| `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs` | Usa `Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath)`. | `runtime-load` |
| `Runtime/Diagnostics/FrameworkLogger.cs` | Usa o mesmo contrato de `Resources.Load`. | `runtime-load` |
| `Editor/Settings/ImmersiveFrameworkEditorSettingsUtility.cs` | Carregava/criava settings somente em `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`. | `editor-default-path` |
| `Editor/Settings/ImmersiveFrameworkSettingsProvider.cs` | Exibia o caminho fixo `_Project` na area de configuration files. | `editor-default-path` |
| `Documentation~/Current/03-Consumer-Project-Roles.md` | Nao definia a politica de localizacao do settings. | `documentation-only` |

## Achados no QA Project

| Arquivo | Achado | Classificacao |
|---|---|---|
| `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` | Settings QA atual ainda esta no caminho legado. | `qa-local-legacy` |
| `Assets/ImmersiveFrameworkQA/Documentation/POST-RESET-B6A-QA-Asset-Migration-Inventory.md` | Registrava o risco do path hardcoded antes do B6B. | `documentation-only` |

## Mudanças feitas

- `ImmersiveFrameworkEditorSettingsUtility.LoadOrCreateSettingsAsset()` agora procura `ImmersiveFrameworkSettings.asset` diretamente dentro de qualquer pasta `Resources` antes de criar um default.
- Se houver exatamente um settings valido em `Resources`, o tooling usa esse asset.
- Se houver mais de um settings valido em `Resources`, o tooling registra erro e nao escolhe silenciosamente.
- Se nao houver nenhum settings valido, o tooling ainda cria o default antigo em `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`.
- `ImmersiveFrameworkSettingsProvider` passou a mostrar o caminho real do settings carregado.
- A documentacao canonica do package recebeu a secao `Framework Settings Location Policy`.
- O relatorio B6A recebeu a secao `Follow-up B6B0 — Settings Path Policy`.

## Mudanças não feitas

- Nenhum asset Unity serializado foi movido, renomeado ou apagado.
- O settings QA ainda nao foi migrado para `Assets/ImmersiveFrameworkQA/SmokeData/Resources/ImmersiveFrameworkSettings.asset`.
- Paths default de criacao de `GameApplication`, `Route`, `Activity`, content profiles e `LoggingConfig` nao foram alterados neste corte.
- Runtime `Resources.Load("ImmersiveFrameworkSettings")` foi preservado.

## Política definida

O runtime resolve o settings por:

```text
Resources.Load("ImmersiveFrameworkSettings")
```

Logo, o asset deve:

- chamar `ImmersiveFrameworkSettings.asset`;
- estar diretamente dentro de uma pasta `Resources`;
- existir uma unica vez como settings valido do framework no consumer project.

`Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` e apenas o default de criacao para projetos sem settings. Ele nao e um caminho universal do framework.

## Impacto no B6B

B6B pode mover o settings QA via Unity Editor para:

```text
Assets/ImmersiveFrameworkQA/SmokeData/Resources/ImmersiveFrameworkSettings.asset
```

Condicoes:

- preservar `.meta` e referencias pelo Unity Editor;
- manter o nome `ImmersiveFrameworkSettings.asset`;
- remover ou mover o settings antigo para evitar dois settings validos em `Resources`;
- validar import/compile no Unity manualmente depois do corte.

## Validação textual

Executado:

- busca por `ImmersiveFrameworkSettings`;
- busca por `Resources.Load`;
- busca por `Assets/_Project`;
- busca por `_Project/Settings`;
- busca por `Settings/ImmersiveFramework/Resources`;
- busca por `Active Game Application`;
- busca final por `Assets/_Project/Settings/ImmersiveFramework/Resources`;
- `git status --short`.

Nao executado:

- Unity;
- build;
- playmode;
- smoke;
- batchmode.

## Próximo passo

Executar B6B via Unity Editor para migrar os ScriptableObjects/configs QA, comecando pelos assets dependidos e movendo `ImmersiveFrameworkSettings.asset` somente quando for possivel manter exatamente um settings valido em `Resources`.
