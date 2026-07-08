# POST-RESET-B4 - QA Project Cleanup

## Objetivo

Organizar o Framework QA Project como projeto tecnico de QA, sem misturar FIRSTGAME, sem mover assets Unity serializados automaticamente e sem alterar o framework/package.

Este corte cria a superficie documental local do QA Project, registra a estrutura canonica desejada e classifica legados que devem ser tratados por cortes dedicados.

## Estrutura canonica do QA Project

```text
Assets/ImmersiveFrameworkQA/
  README.md
  Documentation/
  Scenes/
  GameApplications/
  Routes/
  Activities/
  Prefabs/
  Materials/
  Scripts/
  UI/
  SmokeData/
```

Estado atual:

- `Scenes/`, `Routes/`, `Activities/` e `UnityBuildSurface/` ja continham assets QA ativos.
- `Documentation/`, `GameApplications/`, `Prefabs/`, `Materials/`, `Scripts/`, `UI/` e `SmokeData/` foram criados como roots canonicos vazios ou documentais.
- `UnityBuildSurface/` permanece como superficie QA ativa existente; nao foi desmontada neste corte.

## Pastas auditadas

| Pasta | Classificacao | Evidencia | Decisao |
|---|---|---|---|
| `Assets/ImmersiveFrameworkQA` | Canonico | Contem cenas QA, routes QA, activities QA, scripts QA e README. | Manter e documentar como root canonico. |
| `Assets/ImmersiveFrameworkQA/UnityBuildSurface` | Valid QA / legacy local surface | Contem `QA_TransitionGameApplication`, cenas `QA_UIGlobal`, `TransitionRouteA/B`, scripts `Qa*` e paineis de transition. | Manter; migracao fina pode ser futura. |
| `Assets/_Project` | Legacy QA / pending migration | Contem `.asset`, settings e asmdefs de projeto, incluindo `GameApplication.asset`, `StartupRoute.asset`, `SecondRoute.asset`, `LoggingConfig.asset` e `ImmersiveFrameworkSettings.asset`. | Nao remover; migrar via Unity Editor em `POST-RESET-B6`. |
| `Assets/_Documentation` | Documentation legacy | Contem 155 arquivos `.md` e 163 `.meta`, com ADRs, plans, notes, audits e closeouts historicos. | Nao remover; revisar em `POST-RESET-B5`. |
| `Assets/_Sandbox` | Sandbox legacy | Contem `SampleScene.unity`. | Manter ate decisao de cleanup dedicada. |
| `Assets/_External` | External/local package staging | Contem roots de plugins/tools/local packages. | Fora do corte. |
| `Assets/Settings` | Unity project settings assets | URP/global render pipeline assets. | Falso positivo para `Probe`/`Test`; nao alterar. |
| `Assets/TextMesh Pro` | Third-party/generated TextMesh Pro resources | Shaders e settings TMP. | Falso positivo para `ZTest`; nao alterar. |

## Mudancas feitas

- Atualizado `Assets/ImmersiveFrameworkQA/README.md`.
- Criado `Assets/ImmersiveFrameworkQA/Documentation/`.
- Criado este relatorio local de cleanup.
- Criadas pastas canonicas vazias: `GameApplications/`, `Prefabs/`, `Materials/`, `Scripts/`, `UI/`, `SmokeData/`.
- Adicionados `.meta` e `.gitkeep` para versionar os roots canonicos vazios.

## Mudancas nao feitas

- Nenhum asset `.unity`, `.prefab`, `.asset`, `.mat`, `.controller` ou `.inputactions` foi movido.
- Nenhum MonoBehaviour ou asmdef foi movido.
- Nenhum arquivo em `Assets/_Project` foi removido.
- Nenhum arquivo em `Assets/_Documentation` foi removido ou migrado.
- Nenhum arquivo fora de `C:\Projetos\QAFramework` foi modificado.

## Assets serializados deferidos

`Assets/_Project` contem assets serializados que parecem pertencer ao QA Project, mas exigem migracao via Unity Editor:

- `ScriptableObjects/ImmersiveFramework/GameApplication.asset`
- `ScriptableObjects/ImmersiveFramework/Activities/StartupActivity.asset`
- `ScriptableObjects/ImmersiveFramework/Activities/SecondActivity.asset`
- `ScriptableObjects/ImmersiveFramework/Routes/StartupRoute.asset`
- `ScriptableObjects/ImmersiveFramework/Routes/SecondRoute.asset`
- `ScriptableObjects/ImmersiveFramework/ActivityContentProfiles/ActivityContentProfile.asset`
- `ScriptableObjects/ImmersiveFramework/RouteContentProfiles/RouteContentProfile.asset`
- `Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`
- `Settings/ImmersiveFramework/Logging/LoggingConfig.asset`

Motivo do deferimento: sao assets Unity serializados com `.meta` e possiveis referencias cruzadas. Mover fora do Unity Editor pode quebrar referencias.

## Documentacao legada encontrada

`Assets/_Documentation` existe e foi classificado como legado documental:

- `ADRs/`: ADRs antigos e governanca inicial.
- `Architecture/`: ADRs/plans/tracker F34-F60.
- `Audits/`: auditorias historicas.
- `Closeouts/`: closeouts de cortes antigos.
- `Notes/`: notas de provas, smokes e aceites.
- `Plans/`: planos historicos.
- `Prompts/`: prompts usados em cortes anteriores.
- `Setup/`: pasta de setup.

Parte do conteudo parece coberta ou substituida por `Packages/com.immersive.framework/Documentation~`, especialmente ADRs, roadmap/current docs e history consolidado. A decisao final exige revisao dedicada em:

```text
POST-RESET-B5 - QA Legacy Documentation Removal
```

## Migracao manual recomendada via Unity Editor

Executar em corte futuro:

```text
POST-RESET-B6 - QA Asset Migration To ImmersiveFrameworkQA
```

Recomendacao:

1. Abrir o projeto no Unity Editor.
2. Migrar assets QA serializados de `Assets/_Project/ScriptableObjects/ImmersiveFramework/` para `Assets/ImmersiveFrameworkQA/` conforme ownership.
3. Migrar settings QA serializados apenas se as referencias de `Resources`/settings continuarem validas.
4. Salvar cenas/assets e confirmar que nao houve Missing References.
5. So depois avaliar remocao de `_Project`.

## Validacao textual

Buscas obrigatorias em `Assets`:

| Busca | Classificacao |
|---|---|
| `FirstGame` | Sem achado relevante esperado; se aparecer em documentacao, classificar como comparacao de papeis. |
| `planet` | Sem achado relevante esperado; se aparecer em documentacao, classificar como comparacao de papeis. |
| `Assets/_Documentation` | `documentation legacy`; aparece em documentos historicos e deve ser tratado em B5. |
| `Assets/_Project` | `legacy QA` e `documentation legacy`; aparece em assets/docs antigos e deve ser tratado em B6/B5. |
| `ImmersiveFrameworkQA` | `valid QA`; root canonico e namespace/prefixo QA. |
| `Probe` | `valid QA` para probes de smoke; `false positive` para `LightProbe`, `ReflectionProbe`, `ProbeVolume`. |
| `Test` | `valid QA` em cenas QA quando usado como texto de cenario; `false positive` em `ZTest`/URP test components. |
| `Smoke` | `valid QA` em docs/scripts QA; `documentation legacy` em docs antigas. |
| `QA_` | `valid QA`; prefixo permitido no QA Project. |

## Proximo smoke recomendado

Depois de Unity import/compile manual:

1. Abrir a cena QA principal `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity`.
2. Abrir ou carregar `Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity` quando aplicavel.
3. Executar apenas os smokes manuais ja existentes para a superficie QA tocada pelo proximo corte.
4. Confirmar que nenhum asset migrado manualmente aparece como Missing Reference.
